using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using SysJsonSerializer = System.Text.Json.JsonSerializer;

namespace GagSpeak.WebAPI;

public sealed class PiShockProvider : DisposableMediatorSubscriberBase
{
    private const string WsBrokerUri = "wss://broker.pishock.com/v2";
    private const string PsBaseUri = "https://ps.pishock.com";
    private const string AuthBaseUri = "https://auth.pishock.com";

    private readonly HttpClient _httpClient;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;
    private readonly ClientData _clientData;

    private ClientWebSocket? _ws;
    private readonly SemaphoreSlim _wsSend = new(1, 1);
    private CancellationTokenSource _wsCts = new();
    private readonly Dictionary<string, (int ClientId, int ShockerId)> _resolvedShockers = new();
    private int _userId = 0;

    public PiShockProvider(ILogger<PiShockProvider> logger, GagspeakMediator mediator, MainConfig mainConfig,
        KinksterManager kinksters, ClientData clientData)
        : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _kinksters = kinksters;
        _clientData = clientData;
        _httpClient = new HttpClient();
    }

    protected override void Dispose(bool disposing)
    {
        _wsCts.Cancel();
        _ws?.Abort();
        _ws?.Dispose();
        _wsSend.Dispose();
        _wsCts.Dispose();
        base.Dispose(disposing);
        _httpClient.Dispose();
    }

    private async Task FetchUserIdAsync()
    {
        if (_userId != 0)
            return;

        var apikey = Uri.EscapeDataString(_mainConfig.Current.PiShockApiKey);
        var username = Uri.EscapeDataString(_mainConfig.Current.PiShockUsername);
        try
        {
            var resp = await _httpClient.GetAsync($"{AuthBaseUri}/Auth/GetUserIfAPIKeyValid?apikey={apikey}&username={username}").ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode) return;
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(body)) return;

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var obj = root.ValueKind == JsonValueKind.Array ? root[0] : root;
            if (obj.TryGetProperty("UserId", out var idProp))
            {
                _userId = idProp.GetInt32();
                Logger.LogDebug("PiShock UserId resolved: {id}", _userId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock UserId fetch error");
        }
    }

    private async Task<bool> EnsureWsConnectedAsync()
    {
        if (_ws?.State == WebSocketState.Open)
            return true;

        try
        {
            _ws?.Dispose();
            if (_wsCts.IsCancellationRequested)
            {
                _wsCts.Dispose();
                _wsCts = new CancellationTokenSource();
            }
            _ws = new ClientWebSocket();
            var username = Uri.EscapeDataString(_mainConfig.Current.PiShockUsername);
            var apikey = Uri.EscapeDataString(_mainConfig.Current.PiShockApiKey);
            var token = _wsCts.Token;
            await _ws.ConnectAsync(new Uri($"{WsBrokerUri}?Username={username}&ApiKey={apikey}"), token).ConfigureAwait(false);

            var ping = Encoding.UTF8.GetBytes("{\"Operation\":\"PING\"}");
            await _ws.SendAsync(ping, WebSocketMessageType.Text, true, token).ConfigureAwait(false);

            using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            pingCts.CancelAfter(4000);
            var buf = new byte[4096];
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), pingCts.Token).ConfigureAwait(false);
            var pongMsg = Encoding.UTF8.GetString(buf, 0, result.Count);
            Logger.LogDebug("PiShock WS ping response: {msg}", pongMsg);

            if (!pongMsg.Contains("PONG"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(pongMsg);
                    if (doc.RootElement.TryGetProperty("IsError", out var isErr) && isErr.GetBoolean())
                    {
                        var errMsg = doc.RootElement.TryGetProperty("Message", out var m) ? m.GetString() : "unknown";
                        Logger.LogError("PiShock WS auth failed: {msg}", errMsg);
                        if (pongMsg.Contains("Redis") || pongMsg.Contains("API_KEY"))
                            Logger.LogError("Fix: log out and back in at login.pishock.com to validate your API key");
                    }
                }
                catch (System.Text.Json.JsonException) { }
                _ws.Abort();
                return false;
            }

            Logger.LogDebug("PiShock WebSocket connected and authenticated");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock WebSocket connection failed");
            return false;
        }
    }

    private async Task<string?> WsSendReceiveAsync(string json, int timeoutMs = 5000)
    {
        var token = _wsCts.Token;
        await _wsSend.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_ws == null || _ws.State != WebSocketState.Open)
                return null;

            await _ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeoutMs);
            var buffer = new byte[8192];
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token).ConfigureAwait(false);
            return result.MessageType == WebSocketMessageType.Text
                ? Encoding.UTF8.GetString(buffer, 0, result.Count)
                : null;
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("PiShock WS timed out for: {json}", json);
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock WS send/receive error");
            return null;
        }
        finally
        {
            _wsSend.Release();
        }
    }

    private async Task<List<int>> FetchShareIdsAsync(string apikey)
    {
        await FetchUserIdAsync().ConfigureAwait(false);

        var urls = new List<string>();
        if (_userId != 0)
            urls.Add($"{PsBaseUri}/PiShock/GetShareCodesByOwner?UserId={_userId}&Token={apikey}&api=true");
        urls.Add($"{PsBaseUri}/PiShock/GetShareCodesByOwner?Token={apikey}&api=true");

        foreach (var url in urls)
        {
            try
            {
                var resp = await _httpClient.GetAsync(url).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.LogDebug("PiShock GetShareCodesByOwner: status={s} body={b}", (int)resp.StatusCode, body);

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body) || body == "{}") continue;

                var ids = new List<int>();
                using var doc = JsonDocument.Parse(body);
                foreach (var entry in doc.RootElement.EnumerateObject())
                    foreach (var idElem in entry.Value.EnumerateArray())
                        ids.Add(idElem.GetInt32());

                if (ids.Count > 0) return ids;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PiShock GetShareCodesByOwner error");
            }
        }
        return [];
    }

    private async Task<List<JsonElement>> FetchShockersByIdsAsync(string apikey, List<int> shareIds)
    {
        var qs = string.Join("", shareIds.Select(id => $"&shareIds={id}"));

        var urls = new List<string>();
        if (_userId != 0)
            urls.Add($"{PsBaseUri}/PiShock/GetShockersByShareIds?UserId={_userId}&Token={apikey}&api=true{qs}");
        urls.Add($"{PsBaseUri}/PiShock/GetShockersByShareIds?Token={apikey}&api=true{qs}");

        foreach (var url in urls)
        {
            try
            {
                var resp = await _httpClient.GetAsync(url).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.LogDebug("PiShock GetShockersByShareIds: status={s} body={b}", (int)resp.StatusCode, body);

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body)) continue;

                var shockers = new List<JsonElement>();
                using var doc = JsonDocument.Parse(body);
                foreach (var entry in doc.RootElement.EnumerateObject())
                    foreach (var shocker in entry.Value.EnumerateArray())
                        shockers.Add(shocker.Clone());

                if (shockers.Count > 0) return shockers;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "PiShock GetShockersByShareIds error");
            }
        }
        return [];
    }

    private async Task TryResolveShockerAsync(string shareCode)
    {
        if (_resolvedShockers.ContainsKey(shareCode))
            return;

        var apikey = Uri.EscapeDataString(_mainConfig.Current.PiShockApiKey);
        var shareIds = await FetchShareIdsAsync(apikey).ConfigureAwait(false);
        if (shareIds.Count == 0) return;

        var shockers = await FetchShockersByIdsAsync(apikey, shareIds).ConfigureAwait(false);
        foreach (var shocker in shockers)
        {
            if (!shocker.TryGetProperty("shareCode", out var scProp)) continue;
            if (scProp.GetString()?.Equals(shareCode, StringComparison.OrdinalIgnoreCase) != true) continue;

            var clientId = shocker.GetProperty("clientId").GetInt32();
            var shockerId = shocker.GetProperty("shockerId").GetInt32();
            _resolvedShockers[shareCode] = (clientId, shockerId);
            Logger.LogDebug("PiShock shocker resolved: clientId={c} shockerId={s}", clientId, shockerId);
            return;
        }
    }

    public async Task<PiShockPermissions> GetPermissionsFromCode(string shareCode)
    {
        if (shareCode.IsNullOrEmpty())
        {
            Logger.LogWarning("Attempted to get PiShock permissions with empty share code.");
            return new();
        }

        try
        {
            var apikey = Uri.EscapeDataString(_mainConfig.Current.PiShockApiKey);
            var shareIds = await FetchShareIdsAsync(apikey).ConfigureAwait(false);

            if (shareIds.Count > 0)
            {
                var shockers = await FetchShockersByIdsAsync(apikey, shareIds).ConfigureAwait(false);
                foreach (var shocker in shockers)
                {
                    if (!shocker.TryGetProperty("shareCode", out var scProp)) continue;
                    if (scProp.GetString()?.Equals(shareCode, StringComparison.OrdinalIgnoreCase) != true) continue;

                    var clientId = shocker.GetProperty("clientId").GetInt32();
                    var shockerId = shocker.GetProperty("shockerId").GetInt32();
                    var maxIntensity = shocker.GetProperty("maxIntensity").GetInt32();
                    var canShock = shocker.GetProperty("canShock").GetBoolean();
                    var canVibrate = shocker.GetProperty("canVibrate").GetBoolean();
                    var canBeep = shocker.GetProperty("canBeep").GetBoolean();

                    _resolvedShockers[shareCode] = (clientId, shockerId);
                    var maxDur = _mainConfig.Current.PiShockMaxDuration;
                    Logger.LogTrace("PiShock permissions: shock={s} vibe={v} beep={b} maxIntensity={i} maxDuration={d}", canShock, canVibrate, canBeep, maxIntensity, maxDur);
                    return new PiShockPermissions(canShock, canVibrate, canBeep, maxIntensity, maxDur);
                }
                Logger.LogWarning("PiShock share code {shareCode} not found among account shockers.", shareCode);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PiShock permission discovery error");
        }

        Logger.LogWarning("PiShock could not resolve permissions for {shareCode}.", shareCode);
        return new();
    }

    public void PerformShockCollarAct(ShockCollarAction dto)
    {
        if (!_kinksters.TryGetKinkster(dto.User, out var enactor))
            throw new InvalidOperationException($"Shock Collar Action received from non-kinkster user: {dto.User.AliasOrUID}");

        var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
        var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
        Logger.LogDebug($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

        // PiShock treats d<=15 as seconds and d>15 as milliseconds; enforce 100ms minimum to avoid the ambiguous range.
        if (dto.Duration < 100)
            dto = dto with { Duration = 100 };

        if (!enactor.OwnPerms.PiShockShareCode.IsNullOrEmpty())
        {
            if (dto.Duration / 1000f > enactor.OwnPerms.MaxDuration || (dto.OpCode != 2 && dto.Intensity > enactor.OwnPerms.MaxIntensity))
            {
                Logger.LogWarning("Received instruction that exceeds the max duration or intensity for this user. Ignoring.");
                return;
            }

            Logger.LogDebug("Executing Shock Instruction to UniquePair ShareCode", LoggerType.Callbacks);
            Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
            ExecuteOperation(enactor.OwnPerms.PiShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
            if (dto.OpCode is 0)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
        }
        else if (ClientData.Globals is { } g && !g.GlobalShockShareCode.IsNullOrEmpty())
        {
            if (dto.Duration / 1000f > g.MaxDuration || (dto.OpCode != 2 && dto.Intensity > g.MaxIntensity))
            {
                Logger.LogWarning("Received instruction that exceeds the max duration or intensity for this user. Ignoring.");
                return;
            }

            Logger.LogDebug("Executing Shock Instruction to Global ShareCode", LoggerType.Callbacks);
            Mediator.Publish(new EventMessage(new(enactor.GetNickAliasOrUid(), enactor.UserData.UID, InteractionType.PiShockUpdate, eventLogMessage)));
            ExecuteOperation(g.GlobalShockShareCode, dto.OpCode, dto.Intensity, dto.Duration);
            if (dto.OpCode is 0)
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ShockReceived);
        }
        else
        {
            Logger.LogWarning("Someone Attempted to execute an instruction to you, but you don't have any share codes enabled!");
        }
    }

    public async void ExecuteOperation(string shareCode, int opCode, int intensity, int duration)
    {
        if (!_resolvedShockers.ContainsKey(shareCode))
            await TryResolveShockerAsync(shareCode).ConfigureAwait(false);

        if (!await EnsureWsConnectedAsync().ConfigureAwait(false))
        {
            Logger.LogError("PiShock WebSocket unavailable, cannot execute operation");
            return;
        }

        var mode = opCode switch { 0 => "s", 1 => "v", 2 => "b", _ => "s" };
        string target;
        int shockerId;

        if (!_resolvedShockers.TryGetValue(shareCode, out var shocker))
        {
            Logger.LogError("PiShock shocker unresolved for {shareCode}, cannot send operation", shareCode);
            return;
        }

        target = $"c{shocker.ClientId}-sops-{shareCode}";
        shockerId = shocker.ShockerId;

        var payload = SysJsonSerializer.Serialize(new
        {
            Operation = "PUBLISH",
            PublishCommands = new[]
            {
                new
                {
                    Target = target,
                    Body = new
                    {
                        id = shockerId,
                        m = mode,
                        i = intensity,
                        d = duration,
                        r = true,
                        l = new { u = _userId, ty = "sc", w = false, o = "GagSpeak" }
                    }
                }
            }
        });

        Logger.LogTrace("PiShock WS PUBLISH: {payload}", payload);
        var wsResponse = await WsSendReceiveAsync(payload).ConfigureAwait(false);
        Logger.LogDebug("PiShock WS response: {response}", wsResponse ?? "(null)");

        if (wsResponse != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(wsResponse);
                if (doc.RootElement.TryGetProperty("IsError", out var isErrorProp) && isErrorProp.GetBoolean())
                    Logger.LogError("PiShock WS error: {msg}",
                        doc.RootElement.TryGetProperty("Message", out var msg) ? msg.GetString() : "unknown");
            }
            catch (System.Text.Json.JsonException) { }
        }
    }
}
