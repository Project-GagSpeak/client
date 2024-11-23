using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Routes;
using System.Net;
using System.Text.Json;
using SysJsonSerializer = System.Text.Json.JsonSerializer;

namespace GagSpeak.WebAPI;

public sealed class PiShockProvider : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly PairManager _pairManager;
    private readonly MainHub _mainHub;
    private readonly HttpClient _httpClient;

    public PiShockProvider(ILogger<PiShockProvider> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, PairManager pairManager, MainHub mainHub) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _pairManager = pairManager;
        _mainHub = mainHub;
        _httpClient = new HttpClient();

        Mediator.Subscribe<PiShockExecuteOperation>(this, (msg) => ExecuteOperation(msg.shareCode, msg.OpCode, msg.Intensity, msg.Duration));
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
        try
        {
            var jsonContent = CreateGetInfoContent(shareCode);

            Logger.LogTrace("PiShock Request Info URI Firing: {piShockUri}", GagspeakPiShock.GetInfoPath());
            var response = await _httpClient.PostAsync(GagspeakPiShock.GetInfoPath(), jsonContent).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Logger.LogTrace("PiShock Request Info Response: {response}", response);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                int maxIntensity = root.GetProperty("maxIntensity").GetInt32();
                int maxShockDuration = root.GetProperty("maxDuration").GetInt32();

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
        bool shocks = false;
        bool vibrations = false;
        bool beeps = false;

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
