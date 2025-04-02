using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Enums;
using GagSpeak.PlayerData.Services;
using Dalamud.Utility;

namespace GagSpeak.Interop.Ipc;

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
    private readonly ClientMonitor _clientMonitor;
    private readonly OnFrameworkService _frameworkUtil;

    public IpcCallerMoodles(ILogger<IpcCallerMoodles> logger, IDalamudPluginInterface pi,
        GagspeakMediator mediator, ClientMonitor clientMonitor, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _clientMonitor = clientMonitor;
        _frameworkUtil = frameworkUtils;

        _moodlesApiVersion = pi.GetIpcSubscriber<int>("Moodles.Version");

        // API Getter Functions
        GetMoodleInfo = pi.GetIpcSubscriber<Guid, MoodlesStatusInfo>("Moodles.GetRegisteredMoodleInfo");
        GetMoodlesInfo = pi.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetRegisteredMoodlesInfo");
        GetPresetInfo = pi.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetRegisteredPresetInfo");
        GetPresetsInfo = pi.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetRegisteredPresetsInfo");
        GetStatusManager = pi.GetIpcSubscriber<string>("Moodles.GetStatusManagerLP");
        GetStatusManagerByName = pi.GetIpcSubscriber<string, string>("Moodles.GetStatusManagerByName");
        GetStatusManagerInfo = pi.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoLP");
        GetStatusManagerInfoByName = pi.GetIpcSubscriber<string, List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoByName");

        // API Enactor Functions
        _applyStatusByGuid = pi.GetIpcSubscriber<Guid, string, object>("Moodles.AddOrUpdateMoodleByGUIDByName");
        _applyPresetByGuid = pi.GetIpcSubscriber<Guid, string, object>("Moodles.ApplyPresetByGUIDByName");
        _applyStatusesFromPair = pi.GetIpcSubscriber<string, string, List<MoodlesStatusInfo>, object>("Moodles.ApplyStatusesFromGSpeakPair");
        _removeStatusByGuids = pi.GetIpcSubscriber<List<Guid>, string, object>("Moodles.RemoveMoodlesByGUIDByName");

        _setStatusManager = pi.GetIpcSubscriber<string, string, object>("Moodles.SetStatusManagerByName");
        _clearStatusesFromManager = pi.GetIpcSubscriber<string, object>("Moodles.ClearStatusManagerByName");

        // API Action Events:
        OnStatusManagerModified = pi.GetIpcSubscriber<IPlayerCharacter, object>("Moodles.StatusManagerModified");
        OnStatusSettingsModified = pi.GetIpcSubscriber<Guid, object>("Moodles.StatusModified");
        OnPresetModified = pi.GetIpcSubscriber<Guid, object>("Moodles.PresetModified");

        CheckAPI(); // check to see if we have a valid API
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

    /// <summary> This method disposes of the IPC caller moodles </summary>
    public void Dispose() { }

    /// <summary> This method gets the moodles info for a provided GUID from the client. </summary>
    public MoodlesStatusInfo GetStatusDetails(Guid guid)
    {
        if (!APIAvailable) return new MoodlesStatusInfo();
        try { return GetMoodleInfo.InvokeFunc(guid); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Info: " + e); return new MoodlesStatusInfo(); }
    }

    /// <summary> This method gets the list of all our clients Moodles Info </summary>
    public List<MoodlesStatusInfo> GetStatusListDetails()
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        try { return GetMoodlesInfo.InvokeFunc(); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Info: " + e); return new List<MoodlesStatusInfo>(); }
    }

    /// <summary> This method gets the preset info for a provided GUID from the client. </summary>
    public MoodlePresetInfo GetPresetDetails(Guid guid)
    {
        if (!APIAvailable) return new MoodlePresetInfo();
        try { return GetPresetInfo.InvokeFunc(guid); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Preset Info: " + e); return new MoodlePresetInfo(); }
    }

    /// <summary> This method gets the list of all our clients Presets Info </summary>
    public List<MoodlePresetInfo> GetPresetListDetails()
    {
        if (!APIAvailable) return new List<MoodlePresetInfo>();
        try { return GetPresetsInfo.InvokeFunc(); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Preset Info: " + e); return new List<MoodlePresetInfo>(); }
    }

    /// <summary> This method gets the status information of our client player </summary>
    public List<MoodlesStatusInfo> GetStatusManagerDetails()
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        try { return GetStatusManagerInfo.InvokeFunc(); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Status: " + e); return new List<MoodlesStatusInfo>(); }
    }

    /// <summary> Obtain the status information of a visible player </summary>
    public List<MoodlesStatusInfo> GetStatusManagerDetails(string playerNameWithWorld)
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        try { return GetStatusManagerInfoByName.InvokeFunc(playerNameWithWorld); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Status: " + e); return new List<MoodlesStatusInfo>(); }
    }

    /// <summary> To use when we want to obtain the status manager for our client player, without needing to calculate our address. </summary>
    public string GetStatusManagerString()
    {
        if(!APIAvailable) return string.Empty;
        try { return GetStatusManager.InvokeFunc(); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Status: " + e); return string.Empty; }
    }


    /// <summary> This method gets the status of the moodles for a particular address</summary>
    public string GetStatusManagerString(string playerNameWithWorld)
    {
        if (!APIAvailable) return string.Empty;
        try { return GetStatusManagerByName.InvokeFunc(playerNameWithWorld); }
        catch (Exception e) { _logger.LogWarning("Could not Get Moodles Status: " + e); return string.Empty; }
    }

    public bool ApplyOwnStatusByGUID(IEnumerable<Guid> guidsToAdd)
    {
        if (!APIAvailable) return false;
        string clientName = _clientMonitor.ClientPlayer.NameWithWorld();
        if (clientName.IsNullOrWhitespace()) return false;
        // run the tasks in async with each other
        Parallel.ForEach(guidsToAdd, guid => ApplyOwnStatusByGUID(guid, clientName));
        _logger.LogTrace("Applied Moodles: " + string.Join(", ", guidsToAdd), LoggerType.IpcMoodles);
        return true;
    }


    public void ApplyOwnStatusByGUID(Guid guid, string clientName)
    {
        if (!APIAvailable) return;
        ExecuteSafely(() => _applyStatusByGuid.InvokeAction(guid, clientName));
    }

    public void ApplyOwnPresetByGUID(Guid guid)
    {
        if (!APIAvailable) return;
        string clientName = _clientMonitor.ClientPlayer.NameWithWorld();
        if (!clientName.IsNullOrEmpty()) ExecuteSafely(() => _applyPresetByGuid.InvokeAction(guid, clientName));
    }

    /// <summary> This method applies the statuses from a pair to the client </summary>
    public void ApplyStatusesFromPairToSelf(string applierNameWithWorld, string recipientNameWithWorld, List<MoodlesStatusInfo> statuses)
    {
        if (!APIAvailable) return;
        _logger.LogInformation("Applying Moodles Status: " + recipientNameWithWorld + " from " + applierNameWithWorld, LoggerType.IpcMoodles);
        ExecuteSafely(() => _applyStatusesFromPair.InvokeAction(applierNameWithWorld, recipientNameWithWorld, statuses));
    }

    /// <summary> This method removes the moodles from the client </summary>
    public void RemoveOwnStatusByGuid(IEnumerable<Guid> guidsToRemove)
    {
        if (!APIAvailable) return;
        string clientName = _clientMonitor.ClientPlayer.NameWithWorld();
        _logger.LogTrace("Removing Moodles: " + string.Join(", ", guidsToRemove), LoggerType.ClientPlayerData);
        if (!clientName.IsNullOrEmpty()) ExecuteSafely(() => _removeStatusByGuids.InvokeAction(guidsToRemove.ToList(), clientName));
    }

    /// <summary> This method sets the status of the moodles for a game object specified by the pointer </summary>
    public void SetStatus(string playerNameWithWorld, string statusBase64)
    {
        if (!APIAvailable) return;
        _logger.LogInformation("Setting Moodles Status: " + playerNameWithWorld + " to " + statusBase64, LoggerType.IpcMoodles);
        ExecuteSafely(() => _setStatusManager.InvokeAction(playerNameWithWorld, statusBase64));
    }

    public void ClearStatus()
    {
        if(!APIAvailable) return;
        string clientName = _clientMonitor.ClientPlayer.NameWithWorld();
        if (!clientName.IsNullOrEmpty()) ExecuteSafely(() => ClearStatus(clientName));
    }

    /// <summary> Reverts the status of the moodles for a GameObject specified by the pointer</summary>
    public void ClearStatus(string playerNameWithWorld)
    {
        if (!APIAvailable) return;
        ExecuteSafely(() => _clearStatusesFromManager.InvokeAction(playerNameWithWorld));
    }

    private void ExecuteSafely(Action act)
    {
        try { act(); } catch (Exception ex) { _logger.LogCritical(ex, "Error on executing safely"); }
    }
}
