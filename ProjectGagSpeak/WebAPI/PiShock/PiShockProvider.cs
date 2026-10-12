using CkCommons;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.User;
using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text.Json;
using SysJsonSerializer = System.Text.Json.JsonSerializer;

namespace GagSpeak.WebAPI;

public sealed class PiShockProvider : DisposableMediatorSubscriberBase
{
    private const string AccountUrl        = "https://api.pishock.com/Account";
    private const string OperateByShareUrl = "https://api.pishock.com/Shockers/OperateByShare";
    private const string DevicesUrl        = "https://ps.pishock.com/PiShock/GetUserDevices";
    private const string BrokerUrl         = "wss://broker.pishock.com/v2";

    private readonly HttpClient _httpClient;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;

    public enum ConnectState { NotAttempted, Success, AuthFailed, NetworkError }

    private sealed record PiShockAccount(int UserId);

    private int _userId;
    private List<(int Id, string Name)> _cachedShockers = [];
    private Dictionary<int, int> _shockerClientIds = []; // ShockerId → ClientId
    private ConnectState _connectState = ConnectState.NotAttempted;

    public IReadOnlyList<(int Id, string Name)> CachedShockers => _cachedShockers;
    public ConnectState LastConnectState => _connectState;
    public bool IsConfigured => !string.IsNullOrEmpty(_mainConfig.Data.PiShockApiKey) && !string.IsNullOrEmpty(_mainConfig.Data.PiShockUsername);
    public int ShockerCount => _cachedShockers.Count;

    public PiShockProvider(ILogger<PiShockProvider> logger, GagspeakMediator mediator, MainConfig mainConfig,
        KinksterManager kinksters)
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _kinksters = kinksters;
        _httpClient = new HttpClient();

        Svc.ClientState.Login += OnLogin;
        if (PlayerData.IsLoggedIn && IsConfigured)
            _ = ConnectAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Login -= OnLogin;
        _httpClient.Dispose();
    }

    private void OnLogin()
    {
        if (IsConfigured)
            _ = ConnectAsync();
    }

    public async Task ConnectAsync()
    {
        _cachedShockers = [];
        _shockerClientIds = [];
        try
        {
            var authResp = await _httpClient.SendAsync(AuthedRequest(HttpMethod.Get, AccountUrl)).ConfigureAwait(false);
            if (authResp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _connectState = ConnectState.AuthFailed;
                Logger.LogWarning("PiShock authentication failed (HTTP {code}).", (int)authResp.StatusCode);
                return;
            }

            var account = authResp.IsSuccessStatusCode ? await authResp.Content.ReadFromJsonAsync<PiShockAccount>().ConfigureAwait(false) : null;
            if (account is null || account.UserId == 0)
            {
                _connectState = ConnectState.NetworkError;
                Logger.LogWarning("PiShock auth error (HTTP {code}).", (int)authResp.StatusCode);
                return;
            }
            Logger.LogDebug("PiShock Auth: userId={u}", account.UserId);

            var devicesResp = await _httpClient.SendAsync(AuthedRequest(HttpMethod.Get, $"{DevicesUrl}?UserId={account.UserId}")).ConfigureAwait(false);
            var devicesBody = await devicesResp.Content.ReadAsStringAsync().ConfigureAwait(false);
            Logger.LogDebug("PiShock GetUserDevices: status={s} body={b}", (int)devicesResp.StatusCode, devicesBody);
            if (!devicesResp.IsSuccessStatusCode)
            {
                _connectState = ConnectState.NetworkError;
                Logger.LogWarning("PiShock device fetch error (HTTP {code}).", (int)devicesResp.StatusCode);
                return;
            }

            _userId = account.UserId;
            (_cachedShockers, _shockerClientIds) = ParseShockers(devicesBody);
            _connectState = ConnectState.Success;
            Logger.LogInformation("PiShock connected: {count} device(s) found.", _cachedShockers.Count);
            if (ShockerCount > 0)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.DeviceConnected);
        }
        catch (Exception ex)
        {
            _connectState = ConnectState.NetworkError;
            Logger.LogError(ex, "PiShock ConnectAsync network error.");
        }
    }

    private static (List<(int Id, string Name)>, Dictionary<int, int>) ParseShockers(string body)
    {
        var shockers = new List<(int Id, string Name)>();
        var clientIds = new Dictionary<int, int>();

        using var doc = JsonDocument.Parse(body);
        foreach (var hub in doc.RootElement.EnumerateArray())
        {
            var clientId = hub.GetProperty("ClientId").GetInt32();
            foreach (var shocker in hub.GetProperty("Shockers").EnumerateArray())
            {
                var id = shocker.GetProperty("ShockerId").GetInt32();
                if (!clientIds.TryAdd(id, clientId))
                    continue;
                shockers.Add((id, shocker.GetProperty("Name").GetString() ?? "Unknown"));
            }
        }
        return (shockers, clientIds);
    }

    private HttpRequestMessage AuthedRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-PiShock-Api-Key", _mainConfig.Data.PiShockApiKey);
        return req;
    }

    public void PerformShockCollarAct(ShockCollarAction dto)
    {
        if (!_kinksters.TryGetValue(dto.User, out var enactor))
            throw new InvalidOperationException($"Shock Collar Action received from non-kinkster user: {dto.User.AliasOrUID}");

        var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
        var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
        Logger.LogDebug($"Received Instruction for {eventLogMessage}", LogFilter.Callbacks);

        if (dto.Duration < 1000)
        {
            Logger.LogDebug("Shock duration {orig}ms below minimum, raising to 1000ms.", dto.Duration);
            dto = dto with { Duration = 1000 };
        }

        var shareCode = enactor.OwnPerms.PiShockShareCode;
        if (string.IsNullOrWhiteSpace(shareCode))
        {
            Logger.LogWarning("Received shock instruction but no share code is set for user {uid}.", dto.User.UID);
            return;
        }

        var opAllowed = dto.OpCode switch
        {
            0 => enactor.OwnPerms.AllowShocks,
            1 => enactor.OwnPerms.AllowVibrations,
            2 => enactor.OwnPerms.AllowBeeps,
            _ => false
        };
        if (!opAllowed)
        {
            Logger.LogWarning("Received opcode {op} but that operation is not permitted for {uid}.", dto.OpCode, dto.User.UID);
            return;
        }

        if (dto.Duration / 1000f > enactor.OwnPerms.GetTimespanFromDuration().TotalSeconds || (dto.OpCode != 2 && dto.Intensity > enactor.OwnPerms.MaxIntensity))
        {
            Logger.LogWarning("Received instruction that exceeds the max duration or intensity for this user. Ignoring.");
            return;
        }

        Logger.LogDebug("Executing Shock Instruction via pair permissions.", LogFilter.Callbacks);
        Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.User.UID, InteractionType.PiShockUpdate, eventLogMessage)));
        OperateByShare(shareCode, dto.OpCode, dto.Intensity, dto.Duration);
    }

    // PiShock enforces the share code's own limits, rejecting anything outside them.
    private async void OperateByShare(string shareCode, int opCode, int intensity, int duration)
    {
        try
        {
            var req = AuthedRequest(HttpMethod.Post, $"{OperateByShareUrl}/{Uri.EscapeDataString(shareCode)}");
            req.Content = JsonContent.Create(new
            {
                AgentName = "GagSpeak",
                Operation = opCode,
                Duration = Math.Clamp(duration, 300, 15000),
                Intensity = Math.Clamp(intensity, 0, 100),
                IntensityAsPercentage = false,
            });

            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                Logger.LogDebug("PiShock share operation sent (HTTP {code}).", (int)resp.StatusCode);
                if (opCode is 0)
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
                return;
            }

            var reason = (int)resp.StatusCode switch
            {
                404 => "share code not found",
                405 => "operation not allowed by the share code",
                406 => "device is not V3",
                410 => "share is locked",
                412 => "intensity exceeds the share code's limit",
                416 => "duration exceeds the share code's limit",
                503 => "share or shocker is paused",
                _   => "unexpected response",
            };
            Logger.LogWarning("PiShock share operation rejected (HTTP {code}): {reason}.", (int)resp.StatusCode, reason);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock share operation error");
        }
    }

    public async void ExecuteOperation(int shockerId, int opCode, int intensity, int duration)
    {
        if (!_shockerClientIds.TryGetValue(shockerId, out var clientId))
        {
            Logger.LogWarning("PiShock shocker {id} not found on this account. Reconnect via Settings.", shockerId);
            return;
        }

        var mode = opCode switch { 0 => "s", 1 => "v", 2 => "b", _ => null };
        if (mode is null)
            return;

        var command = new
        {
            Operation = "PUBLISH",
            PublishCommands = new[]
            {
                new
                {
                    Target = $"c{clientId}-ops",
                    Body = new
                    {
                        id = shockerId,
                        m  = mode,
                        i  = Math.Clamp(intensity, 0, 100),
                        d  = Math.Clamp(duration, 300, 15000),
                        r  = true,
                        l  = new { u = _userId, ty = "api", w = false, h = false, o = "GagSpeak" },
                    },
                },
            },
        };

        try
        {
            var username = Uri.EscapeDataString(_mainConfig.Data.PiShockUsername);
            var apiKey = Uri.EscapeDataString(_mainConfig.Data.PiShockApiKey);
            using var ws = new ClientWebSocket();
            await ws.ConnectAsync(new Uri($"{BrokerUrl}?Username={username}&ApiKey={apiKey}"), CancellationToken.None).ConfigureAwait(false);
            await ws.SendAsync(SysJsonSerializer.SerializeToUtf8Bytes(command), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);

            var buffer = new byte[4096];
            var reply = await ws.ReceiveAsync(buffer, CancellationToken.None).ConfigureAwait(false);
            Logger.LogDebug("PiShock publish response: {r}", Encoding.UTF8.GetString(buffer, 0, reply.Count));
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock operation error");
        }
    }
}
