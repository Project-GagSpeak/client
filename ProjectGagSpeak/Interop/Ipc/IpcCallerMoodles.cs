using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerMoodles : IIpcCaller
{
    // TERMINOLOGY:
    // StatusManager == The manager handling the current active statuses on you.
    // Status == The individual "Moodle" in your Moodles tab under the Moodles UI.
    // Preset == The collection of Statuses to apply at once. Stored in a preset.
    
    // Remember, all these are called only when OUR client changes. Not other pairs.
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;

    public readonly ICallGateSubscriber<IPlayerCharacter, object> OnStatusManagerModified;  // The status manager of a player has changed.
    public readonly ICallGateSubscriber<Guid, object>             OnStatusSettingsModified; // Client changed a status's settings.
    public readonly ICallGateSubscriber<Guid, object>             OnPresetModified;         // Client changed a preset's settings.

    // API Getter Functions
    private readonly ICallGateSubscriber<Guid, MoodlesStatusInfo>         GetMoodleInfo;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>>         GetMoodlesInfo;
    private readonly ICallGateSubscriber<Guid, MoodlePresetInfo>          GetPresetInfo;
    private readonly ICallGateSubscriber<List<MoodlePresetInfo>>          GetPresetsInfo;
    private readonly ICallGateSubscriber<string>                          GetStatusManager;
    private readonly ICallGateSubscriber<string, string>                  GetStatusManagerByName;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>>         GetStatusManagerInfo;
    private readonly ICallGateSubscriber<string, List<MoodlesStatusInfo>> GetStatusManagerInfoByName;

    // API Enactor Functions
    private readonly ICallGateSubscriber<Guid, string, object> _applyStatusByGuid;
    private readonly ICallGateSubscriber<Guid, string, object> _applyPresetByGuid;
    private readonly ICallGateSubscriber<string, string, List<MoodlesStatusInfo>, object> _applyStatusesFromPair;
    private readonly ICallGateSubscriber<List<Guid>, string, object> _removeStatusByGuids;

    private readonly ICallGateSubscriber<string, string, object> _setStatusManager;
    private readonly ICallGateSubscriber<string, object> _clearStatusesFromManager;


    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly OnFrameworkService _frameworkUtils;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, GagspeakMediator mediator, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _frameworkUtils = frameworkUtils;

        _moodlesApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version");

        // API Getter Functions
        GetMoodleInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlesStatusInfo>("Moodles.GetRegisteredMoodleInfo");
        GetMoodlesInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetRegisteredMoodlesInfo");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetRegisteredPresetInfo");
        GetPresetsInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetRegisteredPresetsInfo");
        GetStatusManager = Svc.PluginInterface.GetIpcSubscriber<string>("Moodles.GetStatusManagerLP");
        GetStatusManagerByName = Svc.PluginInterface.GetIpcSubscriber<string, string>("Moodles.GetStatusManagerByName");
        GetStatusManagerInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoLP");
        GetStatusManagerInfoByName = Svc.PluginInterface.GetIpcSubscriber<string, List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoByName");

        // API Enactor Functions
        _applyStatusByGuid = Svc.PluginInterface.GetIpcSubscriber<Guid, string, object>("Moodles.AddOrUpdateMoodleByGUIDByName");
        _applyPresetByGuid = Svc.PluginInterface.GetIpcSubscriber<Guid, string, object>("Moodles.ApplyPresetByGUIDByName");
        _applyStatusesFromPair = Svc.PluginInterface.GetIpcSubscriber<string, string, List<MoodlesStatusInfo>, object>("Moodles.ApplyStatusesFromGSpeakPair");
        _removeStatusByGuids = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, string, object>("Moodles.RemoveMoodlesByGUIDByName");

        _setStatusManager = Svc.PluginInterface.GetIpcSubscriber<string, string, object>("Moodles.SetStatusManagerByName");
        _clearStatusesFromManager = Svc.PluginInterface.GetIpcSubscriber<string, object>("Moodles.ClearStatusManagerByName");

        // API Action Events:
        OnStatusManagerModified = Svc.PluginInterface.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        OnStatusSettingsModified = Svc.PluginInterface.GetIpcSubscriber<Guid, object>("Moodles.StatusModified");
        OnPresetModified = Svc.PluginInterface.GetIpcSubscriber<Guid, object>("Moodles.PresetModified");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var result = _moodlesApiVersion.InvokeFunc() >= 1;
            if(!APIAvailable && result)
                _mediator.Publish(new MoodlesReady());
            APIAvailable = result;
        }
        catch
        {
            // Moodles was not ready yet / went offline. Set back to false. (Statuses are auto-cleared by moodles)
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        // Disposing my pain, sweat, tears, and blood that this had to become async to work.
        // If this is being read down the line, I hope support is added back.
    }

    /// <summary> This method gets the moodles info for a provided GUID from the client. </summary>
    public async Task<MoodlesStatusInfo> GetStatusDetails(Guid guid)
    {
        return await ExecuteIpcOnThread(() => GetMoodleInfo.InvokeFunc(guid));
    }

    /// <summary> This method gets the list of all our clients Moodles Info </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusListDetails()
    {
        return await ExecuteIpcOnThread(GetMoodlesInfo.InvokeFunc) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> This method gets the preset info for a provided GUID from the client. </summary>
    public async Task<MoodlePresetInfo> GetPresetDetails(Guid guid)
    {
        return await ExecuteIpcOnThread(() => GetPresetInfo.InvokeFunc(guid));
    }

    /// <summary> This method gets the list of all our clients Presets Info </summary>
    public async Task<IEnumerable<MoodlePresetInfo>> GetPresetListDetails()
    {
        return await ExecuteIpcOnThread(GetPresetsInfo.InvokeFunc) ?? Enumerable.Empty<MoodlePresetInfo>();
    }

    /// <summary> This method gets the status information of our client player </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusManagerDetails()
    {
        return await ExecuteIpcOnThread(GetStatusManagerInfo.InvokeFunc) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> Obtain the status information of a visible player </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusManagerDetails(string playerNameWithWorld)
    {
        return await ExecuteIpcOnThread(() => GetStatusManagerInfoByName.InvokeFunc(playerNameWithWorld)) ?? Enumerable.Empty<MoodlesStatusInfo>();
    }

    /// <summary> Gets the ClientPlayer's StatusManager string. </summary>
    public async Task<string> GetStatusManagerString()
    {
        return await ExecuteIpcOnThread(GetStatusManager.InvokeFunc) ?? string.Empty;
    }


    /// <summary> Gets the status of the moodles for a particular PlayerCharacter </summary>
    public async Task<string> GetStatusManagerString(string playerNameWithWorld)
    {
        return await ExecuteIpcOnThread(() => GetStatusManagerByName.InvokeFunc(playerNameWithWorld)) ?? string.Empty;
    }

    public async Task ApplyOwnStatusByGUID(Guid guid, string clientName)
    {
        await ExecuteIpcOnThread(() => _applyStatusByGuid.InvokeAction(guid, clientName));
    }

    public async Task ApplyOwnStatusByGUID(IEnumerable<Guid> guidsToAdd)
    {
        await ExecuteIpcOnThread(() =>
        {
            var clientNameWorld = PlayerData.NameWithWorld;
            Parallel.ForEach(guidsToAdd, guid => _applyStatusByGuid.InvokeAction(guid, PlayerData.NameWithWorld));
        });
    }

    public async Task ApplyOwnPresetByGUID(Guid guid)
    {
        await ExecuteIpcOnThread(() =>
        {
            _applyPresetByGuid.InvokeAction(guid, PlayerData.NameWithWorld);
        });
    }

    /// <summary> This method applies the statuses from a pair to the client </summary>
    public async Task ApplyStatusesFromPairToSelf(string applierNameWithWorld, string recipientNameWithWorld, IEnumerable<MoodlesStatusInfo> statuses)
    {
        await ExecuteIpcOnThread(() => _applyStatusesFromPair.InvokeAction(applierNameWithWorld, recipientNameWithWorld, [.. statuses]));
    }

    /// <summary> This method removes the moodles from the client </summary>
    public async Task RemoveOwnStatusByGuid(IEnumerable<Guid> guidsToRemove)
    {
        await ExecuteIpcOnThread(() => _removeStatusByGuids.InvokeAction(guidsToRemove.ToList(), PlayerData.NameWithWorld));
    }

    /// <summary> This method sets the status of the moodles for a game object specified by the pointer </summary>
    public async Task SetStatus(string playerNameWithWorld, string statusBase64)
    {
        await ExecuteIpcOnThread(() => _setStatusManager.InvokeAction(playerNameWithWorld, statusBase64));
    }

    public async Task ClearStatus()
    {
        await ExecuteIpcOnThread(() =>
        {
            var clientNameWorld = PlayerData.NameWithWorld;
            _clearStatusesFromManager.InvokeAction(clientNameWorld);
        });
    }

    /// <summary> Reverts the status of the moodles for a GameObject specified by the pointer</summary>
    public async Task ClearStatus(string playerNameWithWorld)
    {
        await ExecuteIpcOnThread(() => _clearStatusesFromManager.InvokeAction(playerNameWithWorld));
    }

    /// <summary> Executes a Moodles Ipc Action on the framework thread. </summary>
    /// <remarks> This action will not execute if APIAvailable is false. </remarks>
    private async Task ExecuteIpcOnThread(Action act)
    {
        if (!APIAvailable)
            return;

        try
        {
            await _frameworkUtils.RunOnFrameworkThread(() => act()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Moodles IPC Action Operation had an Error:\n");
        }
    }

    /// <summary> Executes a Moodles Ipc Func on the framework thread and returns a result. </summary>
    /// <returns> Returns default(T) on failure. </returns>
    /// <remarks> not all types may have valid default(T)'s. If you find this to be the case, do a result check. </remarks>
    private async Task<T?> ExecuteIpcOnThread<T>(Func<T> func)
    {
        if (!APIAvailable)
            return default;

        try
        {
            return await _frameworkUtils.RunOnFrameworkThread(() => func()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Moodles IPC Func Operation had an Error:\n");
            return default;
        }
    }

}
