using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Enums;
using GagSpeak.PlayerData.Services;

namespace GagSpeak.Interop.Ipc;

public sealed class IpcCallerMoodles : IIpcCaller
{
    // TERMINOLOGY:
    // StatusManager == The manager handling the current active statuses on you.
    // Status == The individual "Moodle" in your Moodles tab under the Moodles UI.
    // Preset == The collection of Statuses to apply at once. Stored in a preset.
    
    // Remember, all these are called only when OUR client changes. Not other pairs.
    private readonly ICallGateSubscriber<int> _moodlesApiVersion;
    
    private readonly ICallGateSubscriber<IPlayerCharacter, object> _onStatusManagerModified;
    private readonly ICallGateSubscriber<Guid, object> _onStatusSettingsModified;
    private readonly ICallGateSubscriber<Guid, object> _onPresetModified;

    // API Getter Functions
    private readonly ICallGateSubscriber<Guid, MoodlesStatusInfo> _getMoodleInfo;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>> _getMoodlesInfo;
    private readonly ICallGateSubscriber<Guid, MoodlePresetInfo> _getPresetInfo;
    private readonly ICallGateSubscriber<List<MoodlePresetInfo>> _getPresetsInfo;
    private readonly ICallGateSubscriber<string> _getStatusManager;
    private readonly ICallGateSubscriber<string, string> _getStatusManagerByName;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>> _getStatusManagerInfo;
    private readonly ICallGateSubscriber<string, List<MoodlesStatusInfo>> _getStatusManagerInfoByName;

    // API Enactor Functions
    private readonly ICallGateSubscriber<Guid, string, object> _applyStatusByGuid;
    private readonly ICallGateSubscriber<Guid, string, object> _applyPresetByGuid;
    private readonly ICallGateSubscriber<string, string, List<MoodlesStatusInfo>, object> _applyStatusesFromPair;
    private readonly ICallGateSubscriber<List<Guid>, string, object> _removeStatusByGuids;

    private readonly ICallGateSubscriber<string, string, object> _setStatusManager;
    private readonly ICallGateSubscriber<string, object> _clearStatusesFromManager;


    private readonly ILogger<IpcCallerMoodles> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtil;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, IDalamudPluginInterface pi,
        GagspeakMediator mediator, ClientMonitorService clientService, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _clientService = clientService;
        _frameworkUtil = frameworkUtils;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");

        // API Getter Functions
        _getMoodleInfo = pi.GetIpcSubscriber<Guid, MoodlesStatusInfo>("Moodles.GetRegisteredMoodleInfo");
        _getMoodlesInfo = pi.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetRegisteredMoodlesInfo");
        _getPresetInfo = pi.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetRegisteredPresetInfo");
        _getPresetsInfo = pi.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetRegisteredPresetsInfo");
        _getStatusManager = pi.GetIpcSubscriber<string>("Moodles.GetStatusManagerLP");
        _getStatusManagerByName = pi.GetIpcSubscriber<string, string>("Moodles.GetStatusManagerByName");
        _getStatusManagerInfo = pi.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoLP");
        _getStatusManagerInfoByName = pi.GetIpcSubscriber<string, List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoByName");

        // API Enactor Functions
        _applyStatusByGuid = pi.GetIpcSubscriber<Guid, string, object>("Moodles.AddOrUpdateMoodleByGUIDByName");
        _applyPresetByGuid = pi.GetIpcSubscriber<Guid, string, object>("Moodles.ApplyPresetByGUIDByName");
        _applyStatusesFromPair = pi.GetIpcSubscriber<string, string, List<MoodlesStatusInfo>, object>("Moodles.ApplyStatusesFromGSpeakPair");
        _removeStatusByGuids = pi.GetIpcSubscriber<List<Guid>, string, object>("Moodles.RemoveMoodlesByGUIDByName");

        _setStatusManager = pi.GetIpcSubscriber<string, string, object>("Moodles.SetStatusManagerByName");
        _clearStatusesFromManager = pi.GetIpcSubscriber<string, object>("Moodles.ClearStatusManagerByName");


        // API Action Events:
        _onStatusManagerModified = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        _onStatusSettingsModified = pi.GetIpcSubscriber<Guid, object>("Moodles.StatusModified");
        _onPresetModified = pi.GetIpcSubscriber<Guid, object>("Moodles.PresetModified");

        _onStatusManagerModified.Subscribe(OnStatusManagerModified); // fires whenever our client's status manager changes.
        _onStatusSettingsModified.Subscribe(OnStatusModified); // fires whenever our client's changes the settings of a Moodle.
        _onPresetModified.Subscribe(OnPresetModified); // fires whenever our client's changes the settings of a Moodle preset.

        CheckAPI(); // check to see if we have a valid API
    }

    /// <summary> This method is called when the moodles status list changes </summary>
    /// <param name="character">The character that had modified moodles.</param>
    private void OnStatusManagerModified(IPlayerCharacter character)
        => IpcFastUpdates.InvokeStatusManagerChanged(character.Address);

    /// <summary> This method is called when the moodles change </summary>
    private void OnStatusModified(Guid guid)
        => _mediator.Publish(new MoodlesStatusModified(guid));

    /// <summary> This method is called when the moodles change </summary>
    private void OnPresetModified(Guid guid)
        => _mediator.Publish(new MoodlesPresetModified(guid));


    /// <summary> this boolean determines if the moodles API is available or not.</summary>
    public static bool APIAvailable { get; private set; } = false;

    /// <summary> This method checks if the API is available </summary>
    public void CheckAPI()
    {
        try
        {
            var result = _moodlesApiVersion.InvokeFunc() >= 1;
            // Handle condition where it goes from off to on.
            if(APIAvailable is false && result is true)
            {
                _logger.LogInformation("IPC Connection to Moodles now Established!");
                _mediator.Publish(new MoodlesReady());
            }
            // update result.
            APIAvailable = result;

        }
        catch
        {
            APIAvailable = false;
        }
    }

    /// <summary> This method disposes of the IPC caller moodles</summary>
    public void Dispose()
    {
        _onStatusManagerModified.Unsubscribe(OnStatusManagerModified);
        _onStatusSettingsModified.Unsubscribe(OnStatusModified);
        _onPresetModified.Unsubscribe(OnPresetModified);
    }

    /// <summary> This method gets the moodles info for a provided GUID from the client. </summary>
    public async Task<MoodlesStatusInfo?> GetMoodleInfoAsync(Guid guid)
    {
        if (!APIAvailable) return null; 

        try // otherwise, try and return an awaited task that gets the moodles info for a provided GUID
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getMoodleInfo.InvokeFunc(guid)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Info: "+e, LoggerType.IpcMoodles);
            return null;
        }
    }

    /// <summary> This method gets the list of all our clients Moodles Info </summary>
    public async Task<List<MoodlesStatusInfo>?> GetMoodlesInfoAsync()
    {
        if (!APIAvailable) return null; 

        try // otherwise, try and return an awaited task that gets the list of all our clients Moodles Info
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getMoodlesInfo.InvokeFunc()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Info: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }

    /// <summary> This method gets the preset info for a provided GUID from the client. </summary>
    public async Task<MoodlePresetInfo?> GetPresetInfoAsync(Guid guid)
    {
        if (!APIAvailable) return null; 

        try // otherwise, try and return an awaited task that gets the preset info for a provided GUID
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getPresetInfo.InvokeFunc(guid)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Preset Info: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }

    /// <summary> This method gets the list of all our clients Presets Info </summary>
    public async Task<List<MoodlePresetInfo>?> GetPresetsInfoAsync()
    {
        if (!APIAvailable) return null; 
        try
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getPresetsInfo.InvokeFunc()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Preset Info: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }

    /// <summary> This method gets the status information of our client player </summary>
    public async Task<List<MoodlesStatusInfo>> GetStatusInfoAsync()
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        try
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getStatusManagerInfo.InvokeFunc()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Status: " + e, LoggerType.IpcMoodles);
            return new List<MoodlesStatusInfo>();
        }
    }

    /// <summary> Obtain the status information of a visible player </summary>
    public async Task<List<MoodlesStatusInfo>?> GetStatusInfoAsync(string playerNameWithWorld)
    {
        if (!APIAvailable) return null;
        try
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getStatusManagerInfoByName.InvokeFunc(playerNameWithWorld)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Status: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }

    /// <summary> To use when we want to obtain the status manager for our client player, without needing to calculate our address. </summary>
    public async Task<string?> GetClientPlayerStatusAsync()
    {
        if(!APIAvailable) return null;
        try
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getStatusManager.InvokeFunc()).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Status: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }


    /// <summary> This method gets the status of the moodles for a partiular address</summary>
    public async Task<string?> GetStatusAsync(string playerNameWithWorld)
    {
        if (!APIAvailable) return null; 
        try
        {
            return await _frameworkUtil.RunOnFrameworkThread(() => _getStatusManagerByName.InvokeFunc(playerNameWithWorld)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Get Moodles Status: " + e, LoggerType.IpcMoodles);
            return null;
        }
    }

    public async Task ApplyOwnStatusByGUID(List<Guid> guidsToAdd)
    {
        if (!APIAvailable) return;

        string playerNameWithWorld = _clientService.ClientPlayer.NameWithWorld();
        if (string.IsNullOrEmpty(playerNameWithWorld))
        {
            _logger.LogError("Could not get player name with world for Client Player!!!!", LoggerType.IpcMoodles);
            return;
        }

        // run the tasks in async with each other
        var tasks = new List<Task>();
        foreach (var g in guidsToAdd) tasks.Add(ApplyOwnStatusByGUID(g, playerNameWithWorld));

        await Task.WhenAll(tasks);
        _logger.LogTrace("Applied Moodles: " + string.Join(", ", guidsToAdd), LoggerType.IpcMoodles);
        return;
    }


    public async Task ApplyOwnStatusByGUID(Guid guid, string playerNameWithWorld)
    {
        if (!APIAvailable) return;
        try
        {
            await _frameworkUtil.RunOnFrameworkThread(() => _applyStatusByGuid.InvokeAction(guid, playerNameWithWorld)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Apply Moodles Status: " + e, LoggerType.IpcMoodles);
        }
    }

    public async Task ApplyOwnPresetByGUID(Guid guid)
    {
        if (!APIAvailable) return;
        try
        {
            await _frameworkUtil.RunOnFrameworkThread(() =>
            {
                string playerNameWithWorld = _clientService.ClientPlayer.NameWithWorld();
                if (string.IsNullOrEmpty(playerNameWithWorld))
                {
                    _logger.LogError("Could not get player name with world for Client Player!!!!", LoggerType.IpcMoodles);
                }
                else
                {
                    _applyPresetByGuid.InvokeAction(guid, playerNameWithWorld);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Apply Moodles Preset: " + e, LoggerType.IpcMoodles);
        }
    }

    /// <summary> This method applies the statuses from a pair to the client </summary>
    public async Task ApplyStatusesFromPairToSelf(string applierNameWithWorld, string recipientNameWithWorld, List<MoodlesStatusInfo> statuses)
    {
        if (!APIAvailable) return;
        try
        {
            _logger.LogInformation("Applying Moodles Status: " + recipientNameWithWorld + " from " + applierNameWithWorld, LoggerType.IpcMoodles);
            await _frameworkUtil.RunOnFrameworkThread(() => _applyStatusesFromPair.InvokeAction(applierNameWithWorld, recipientNameWithWorld, statuses)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Apply Moodles Status: " + e, LoggerType.IpcMoodles);
        }
    }

    public async Task RemoveOwnStatusByGuid(List<Guid> guidsToRemove)
    {
        if (!APIAvailable) return;
        try
        {
            await _frameworkUtil.RunOnFrameworkThread(() =>
            {
                string playerNameWithWorld = _clientService.ClientPlayer.NameWithWorld();
                if (string.IsNullOrEmpty(playerNameWithWorld))
                {
                    _logger.LogError("Could not get player name with world for Client Player!!!!", LoggerType.IpcMoodles);
                }
                else
                {
                    _removeStatusByGuids.InvokeAction(guidsToRemove, playerNameWithWorld);
                    _logger.LogTrace("Removing Moodles: " + string.Join(", ", guidsToRemove), LoggerType.ClientPlayerData);
                }
            }).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Remove Moodles Status: " + e, LoggerType.IpcMoodles);
        }
    }

    public async Task SetStatusAsync(string playerNameWithWorld, string status)
    {
        if (!APIAvailable) return;
        try
        {
            _logger.LogInformation("Setting Moodles Status: " + playerNameWithWorld + " to " + status, LoggerType.IpcMoodles);
            await _frameworkUtil.RunOnFrameworkThread(() => 
                _setStatusManager.InvokeAction(playerNameWithWorld, status)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Set Moodles Status: " + e, LoggerType.IpcMoodles);
        }
    }

    public async Task ClearStatusAsync()
    {
        string playerNameWithWorld = _clientService.ClientPlayer.NameWithWorld();
        if (string.IsNullOrEmpty(playerNameWithWorld))
        {
            _logger.LogError("Could not get player name with world for Client Player!!!!", LoggerType.IpcMoodles);
        }
        await ClearStatusAsync(playerNameWithWorld);
    }

    /// <summary> Reverts the status of the moodles for a gameobject spesified by the pointer</summary>
    public async Task ClearStatusAsync(string playerNameWithWorld)
    {
        if (!APIAvailable) return;
        try
        {
            await _frameworkUtil.RunOnFrameworkThread(() =>
                _clearStatusesFromManager.InvokeAction(playerNameWithWorld)).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            _logger.LogWarning("Could not Clear Moodles Statuses: " + e, LoggerType.IpcMoodles);
        }
    }
}
