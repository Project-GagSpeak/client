using System.Net;
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
    private readonly HttpClient _httpClient;
    private readonly MainConfig _mainConfig;
    private readonly KinksterManager _kinksters;
    private readonly ClientData _clientData;

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
        base.Dispose(disposing);
        _httpClient.Dispose();
    }

    // grab basic information from shock collar.
    private StringContent CreateGetInfoContent(string shareCode)
    {
        StringContent content = new(SysJsonSerializer.Serialize(new
        {
            UserName = _mainConfig.Current.PiShockUsername,
            Code = shareCode,
            Apikey = _mainConfig.Current.PiShockApiKey,
        }), Encoding.UTF8, "application/json");
        return content;
    }

    // For grabbing boolean permissions from a share code.
    private StringContent CreateDummyExecuteContent(string shareCode, int opCode)
    {
        StringContent content = new(SysJsonSerializer.Serialize(new
        {
            Username = _mainConfig.Current.PiShockUsername,
            Name = "GagSpeakProvider",
            Op = opCode,
            Intensity = 0,
            Duration = 0,
            Code = shareCode,
            Apikey = _mainConfig.Current.PiShockApiKey,
        }), Encoding.UTF8, "application/json");
        return content;
    }

    // Sends operation to shock collar
    private StringContent CreateExecuteOperationContent(string shareCode, int opCode, int intensity, int duration)
    {
        StringContent content = new(SysJsonSerializer.Serialize(new
        {
            Username = _mainConfig.Current.PiShockUsername,
            Name = "GagSpeakProvider",
            Op = opCode,
            Intensity = intensity,
            Duration = duration,
            Code = shareCode,
            Apikey = _mainConfig.Current.PiShockApiKey,
        }), Encoding.UTF8, "application/json");
        return content;
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
            var jsonContent = CreateGetInfoContent(shareCode);

            Logger.LogTrace("PiShock Request Info URI Firing: {piShockUri}", GagspeakPiShock.GetInfoPath());
            var response = await _httpClient.PostAsync(GagspeakPiShock.GetInfoPath(), jsonContent).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Logger.LogTrace("PiShock Request Info Response: {response}", response);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Logger.LogTrace("PiShock Request Info Content: {content}", content);
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var maxIntensity = root.GetProperty("maxIntensity").GetInt32();
                var maxShockDuration = root.GetProperty("maxDuration").GetInt32();

                Logger.LogTrace("Obtaining boolean values by passing dummy requests to share code");
                var result = await ConstructPermissionObject(shareCode, maxIntensity, maxShockDuration);
                Logger.LogTrace("PiShock Permissions obtained: {result}", result);
                return result;
            }
            else if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                Logger.LogWarning("The Credentials for your API Key and Username do not match any profile in PiShock");
                return new();
            }
            else
            {
                Logger.LogError("The ShareCode for this profile does not exist, or this is a simple error 404: {statusCode}", response.StatusCode);
                return new();
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Error getting PiShock permissions from share code");
            return new PiShockPermissions();
        }
    }

    private async Task<PiShockPermissions> ConstructPermissionObject(string shareCode, int intensityLimit, int durationLimit)
    {
        // Shock, Vibrate, Beep. In that order
        int[] opCodes = { 0, 1, 2 };
        var shocks = false;
        var vibrations = false;
        var beeps = false;

        try
        {
            foreach (var opCode in opCodes)
            {
                var jsonContent = CreateDummyExecuteContent(shareCode, opCode);
                var response = await _httpClient.PostAsync(GagspeakPiShock.ExecuteOperationPath(), jsonContent).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.OK) continue;

                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                switch (opCode)
                {
                    case 0:
                        shocks = content! == "Operation Attempted."; break;
                    case 1:
                        vibrations = content! == "Operation Attempted."; break;
                    case 2:
                        beeps = content! == "Operation Attempted."; break;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Error executing operation on PiShock");
        }

        return new PiShockPermissions() { AllowShocks = shocks, AllowVibrations = vibrations, AllowBeeps = beeps, MaxIntensity = intensityLimit, MaxDuration = durationLimit };
    }

    public void PerformShockCollarAct(ShockCollarAction dto)
    {
        // figure out who sent the command, and see if we have a unique sharecode setup for them.
        if (!_kinksters.TryGetKinkster(dto.User, out var enactor))
            throw new InvalidOperationException($"Shock Collar Action received from non-kinkster user: {dto.User.AliasOrUID}");

        var interactionType = dto.OpCode switch { 0 => "shocked", 1 => "vibrated", 2 => "beeped", _ => "unknown" };
        var eventLogMessage = $"Pishock {interactionType}, intensity: {dto.Intensity}, duration: {dto.Duration}";
        Logger.LogDebug($"Received Instruction for {eventLogMessage}", LoggerType.Callbacks);

        // Handle quirk in pishock API where it accepts durations in seconds up to 15, but anything above 15 is treated as milliseconds.
        // Our slider only accepts 0.1 second increments, so we will enforce a minimum of 100 milliseconds to avoid the aforementioned issue.
        if (dto.Duration < 100)
        {
            dto = dto with { Duration = 100 };
        }

        if (!enactor.OwnPerms.PiShockShareCode.IsNullOrEmpty())
        {
            // MaxDuration is in seconds while the incoming duration is in ms, so we need to convert before comparing. Ignore intensity for beeps.
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
            // MaxDuration is in seconds while the incoming duration is in ms, so we need to convert before comparing. Ignore intensity for beeps.
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
        try
        {
            var jsonContent = CreateExecuteOperationContent(shareCode, opCode, intensity, duration);
            var response = await _httpClient.PostAsync(GagspeakPiShock.ExecuteOperationPath(), jsonContent).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError("Error executing operation on PiShock. Status returned: " + response.StatusCode);
                return;
            }
            var contentStr = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Logger.LogDebug("PiShock Request Sent to Shock Collar Successfully! Content returned was:\n" + contentStr);
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "Error executing operation on PiShock");
        }
    }
}
