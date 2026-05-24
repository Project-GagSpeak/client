using CkCommons;
using System.Net;
using System.Text.Json;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Network;
using SysJsonSerializer = System.Text.Json.JsonSerializer;

namespace GagSpeak.WebAPI;

public sealed class PiShockProvider : DisposableMediatorSubscriberBase
{
    private const string ApiBaseUri = "https://api.pishock.com";

    private readonly HttpClient _httpClient;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;

    public enum ConnectState { NotAttempted, Success, AuthFailed, NetworkError }

    private List<(int Id, string Name)> _cachedShockers = [];
    private List<JsonElement> _cachedShockerData = [];
    private ConnectState _connectState = ConnectState.NotAttempted;

    public IReadOnlyList<(int Id, string Name)> CachedShockers => _cachedShockers;
    public ConnectState LastConnectState => _connectState;
    public bool IsConfigured => !string.IsNullOrEmpty(_mainConfig.Current.PiShockApiKey);
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
        _cachedShockerData = [];
        try
        {
            var resp = await _httpClient.SendAsync(AuthedRequest(HttpMethod.Get, $"{ApiBaseUri}/Share/GetShared")).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            Logger.LogDebug("PiShock Connect: status={s} body={b}", (int)resp.StatusCode, body);

            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _connectState = ConnectState.AuthFailed;
                Logger.LogWarning("PiShock authentication failed (HTTP {code}).", (int)resp.StatusCode);
                return;
            }

            if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
            {
                _connectState = ConnectState.NetworkError;
                Logger.LogWarning("PiShock connection error (HTTP {code}).", (int)resp.StatusCode);
                return;
            }

            _connectState = ConnectState.Success;
            (_cachedShockers, _cachedShockerData) = ParseShockers(body);
            Logger.LogInformation("PiShock connected: {count} device(s) found.", _cachedShockers.Count);
        }
        catch (Exception ex)
        {
            _connectState = ConnectState.NetworkError;
            Logger.LogError(ex, "PiShock ConnectAsync network error.");
        }
    }

    private static (List<(int Id, string Name)> Shockers, List<JsonElement> Data) ParseShockers(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            JsonElement arr;
            if (root.ValueKind == JsonValueKind.Array)
                arr = root;
            else if (root.TryGetProperty("value", out var nested) && nested.ValueKind == JsonValueKind.Array)
                arr = nested;
            else
                return ([], []);

            var data = arr.EnumerateArray().Select(e => e.Clone()).ToList();
            var shockers = data
                .Where(s => (s.TryGetProperty("Id", out _) || s.TryGetProperty("id", out _)) &&
                            (s.TryGetProperty("Name", out _) || s.TryGetProperty("name", out _)))
                .Select(s =>
                {
                    var id = s.TryGetProperty("Id", out var ip) ? ip.GetInt32() : s.GetProperty("id").GetInt32();
                    var name = s.TryGetProperty("Name", out var np) ? np.GetString() : s.GetProperty("name").GetString();
                    return (id, name ?? "Unknown");
                })
                .DistinctBy(s => s.id)
                .ToList();
            return (shockers, data);
        }
        catch { return ([], []); }
    }

    public int GetPairShockerId(string uid)
    {
        if (_mainConfig.Current.PairShockerIds.TryGetValue(uid, out var id) && id != 0)
            return id;
        return _mainConfig.Current.GlobalShockerId;
    }

    public void SetPairShockerId(string uid, int id)
    {
        _mainConfig.Current.PairShockerIds[uid] = id;
        _mainConfig.Save();
    }

    private HttpRequestMessage AuthedRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-PiShock-Api-Key", _mainConfig.Current.PiShockApiKey);
        return req;
    }

    public Task<PiShockPermissions> GetPermissionsFromCode(string shareCode)
    {
        if (shareCode.IsNullOrEmpty())
        {
            Logger.LogWarning("Attempted to get PiShock permissions with empty share code.");
            return Task.FromResult(new PiShockPermissions());
        }

        if (_connectState != ConnectState.Success || _cachedShockerData.Count == 0)
        {
            Logger.LogWarning("PiShock not connected or no devices cached. Connect via Settings first.");
            return Task.FromResult(new PiShockPermissions());
        }

        try
        {
            foreach (var shocker in _cachedShockerData)
            {
                var code = shocker.TryGetProperty("Code", out var cp)       ? cp.GetString()
                         : shocker.TryGetProperty("code", out cp)           ? cp.GetString()
                         : shocker.TryGetProperty("ShareCode", out var scp) ? scp.GetString()
                         : shocker.TryGetProperty("shareCode", out scp)     ? scp.GetString()
                         : null;

                if (code == null || !code.Equals(shareCode, StringComparison.OrdinalIgnoreCase))
                    continue;

                return Task.FromResult(ExtractPermissions(shocker));
            }

            var targetId = _mainConfig.Current.GlobalShockerId;
            if (targetId != 0)
            {
                foreach (var shocker in _cachedShockerData)
                {
                    if ((shocker.TryGetProperty("Id", out var idp) || shocker.TryGetProperty("id", out idp)) && idp.GetInt32() == targetId)
                    {
                        Logger.LogDebug("Share code not found by name, falling back to GlobalShockerId {id}", targetId);
                        return Task.FromResult(ExtractPermissions(shocker));
                    }
                }
            }

            if (_cachedShockerData.Count > 0)
            {
                Logger.LogDebug("Share code not found, using first available shocker permissions");
                return Task.FromResult(ExtractPermissions(_cachedShockerData[0]));
            }

            Logger.LogWarning("Share code {code} not found and no shockers available.", shareCode);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock GetPermissionsFromCode error");
        }
        return Task.FromResult(new PiShockPermissions());
    }

    private PiShockPermissions ExtractPermissions(JsonElement shocker)
    {
        var canShock   = (shocker.TryGetProperty("AllowShock",   out var s) || shocker.TryGetProperty("CanShock",   out s) ||
                          shocker.TryGetProperty("allowShock",   out s)    || shocker.TryGetProperty("canShock",   out s)) && s.GetBoolean();
        var canVibrate = (shocker.TryGetProperty("AllowVibrate", out var v) || shocker.TryGetProperty("CanVibrate", out v) ||
                          shocker.TryGetProperty("allowVibrate", out v)    || shocker.TryGetProperty("canVibrate", out v)) && v.GetBoolean();
        var canBeep    = (shocker.TryGetProperty("AllowBeep",    out var b) || shocker.TryGetProperty("CanBeep",    out b) ||
                          shocker.TryGetProperty("allowBeep",    out b)    || shocker.TryGetProperty("canBeep",    out b)) && b.GetBoolean();
        var maxIntensity = (shocker.TryGetProperty("MaxIntensity", out var mi) || shocker.TryGetProperty("maxIntensity", out mi)) ? mi.GetInt32() : 100;
        var maxDuration  = (shocker.TryGetProperty("MaxDuration",  out var md) || shocker.TryGetProperty("maxDuration",  out md)) ? md.GetInt32() : 15;
        Logger.LogDebug("PiShock permissions: shock={s} vibe={v} beep={b} maxI={i} maxD={d}", canShock, canVibrate, canBeep, maxIntensity, maxDuration);
        return new PiShockPermissions(canShock, canVibrate, canBeep, maxIntensity, maxDuration);
    }

    public void PerformShockCollarAct(ShockCollarAction dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var enactor))
            throw new InvalidOperationException($"Shock Collar Action received from non-kinkster user: {dto.User.AliasOrUID}");

        var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
        var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
        Logger.LogDebug($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

        if (dto.Duration < 1000)
        {
            Logger.LogDebug("Shock duration {orig}ms below minimum, raising to 1000ms.", dto.Duration);
            dto = dto with { Duration = 1000 };
        }

        var shockerId = GetPairShockerId(dto.User.UID);
        if (shockerId == 0)
        {
            Logger.LogWarning("Received shock instruction but no shocker is configured for user {uid}.", dto.User.UID);
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

        Logger.LogDebug("Executing Shock Instruction via pair permissions.", LoggerType.Callbacks);
        Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
        ExecuteOperation(shockerId, dto.OpCode, dto.Intensity, dto.Duration);
        if (dto.OpCode is 0)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
    }

    public async void ExecuteOperation(int shockerId, int opCode, int intensity, int duration)
    {
        try
        {
            var req = AuthedRequest(HttpMethod.Post, $"{ApiBaseUri}/Shockers/{shockerId}");
            var durationMs   = Math.Clamp(duration, 300, 15000);
            var intensityPct = Math.Clamp(intensity, 0, 100);
            req.Content = new StringContent(
                SysJsonSerializer.Serialize(new
                {
                    AgentName            = "GagSpeak",
                    Operation            = opCode,
                    Duration             = durationMs,
                    Intensity            = intensityPct,
                    IntensityAsPercentage = true,
                }), Encoding.UTF8, "application/json");
            var resp = await _httpClient.SendAsync(req).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
                Logger.LogDebug("PiShock operation sent successfully (HTTP {code})", (int)resp.StatusCode);
            else
                Logger.LogWarning("PiShock operation returned unexpected status: {status} body={b}", (int)resp.StatusCode, body);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock operation error");
        }
    }
}
