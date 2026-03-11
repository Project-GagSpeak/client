using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerMoodles : IIpcCaller
{
    private readonly ICallGateSubscriber<int> ApiVersion;

    public readonly ICallGateSubscriber<nint, object>       OnStatusManagerModified;
    public readonly ICallGateSubscriber<Guid, bool, object> OnStatusUpdated;
    public readonly ICallGateSubscriber<Guid, bool, object> OnPresetUpdated;

    // API Getters
    private readonly ICallGateSubscriber<List<MoodleStatusInfo>>       GetManagerInfo;
    private readonly ICallGateSubscriber<nint, List<MoodleStatusInfo>> GetManagerInfoByPtr;
    private readonly ICallGateSubscriber<Guid, MoodleStatusInfo>       GetStatusInfo;
    private readonly ICallGateSubscriber<List<MoodleStatusInfo>>       GetStatusInfoList;
    private readonly ICallGateSubscriber<Guid, MoodlePresetInfo>       GetPresetInfo;
    private readonly ICallGateSubscriber<List<MoodlePresetInfo>>       GetPresetsInfoList;
    // API Enactors
    private readonly ICallGateSubscriber<Guid, nint, object>        ApplyStatusByPtr;
    private readonly ICallGateSubscriber<List<Guid>, nint, object>  RemoveStatusesByPtr;
    // Other calls are made via the static calls of the IpcProvider,
    // such as tuple application and locking/unlocking.

    private readonly GagspeakMediator _mediator;

    public IpcCallerMoodles(GagspeakMediator mediator)
    {
        _mediator = mediator;

        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version");

        // API Getters (Status Managers)
        GetManagerInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodleStatusInfo>>("Moodles.GetClientStatusManagerInfoV2");
        GetManagerInfoByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, List<MoodleStatusInfo>>("Moodles.GetStatusManagerInfoByPtrV2");
        // API Getters (Status and Preset Info)
        GetStatusInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodleStatusInfo>("Moodles.GetStatusInfoV2");
        GetStatusInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodleStatusInfo>>("Moodles.GetStatusInfoListV2");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetPresetInfoV2");
        GetPresetsInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetPresetsInfoListV2");
        // API Enactors
        ApplyStatusByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Moodles.AddOrUpdateMoodleByPtrV2");
        RemoveStatusesByPtr = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, nint, object>("Moodles.RemoveMoodlesByPtrV2");
        // API Action Events:
        OnStatusManagerModified = Svc.PluginInterface.GetIpcSubscriber<nint, object>("Moodles.StatusManagerModified");
        OnStatusUpdated = Svc.PluginInterface.GetIpcSubscriber<Guid, bool, object>("Moodles.StatusUpdated");
        OnPresetUpdated = Svc.PluginInterface.GetIpcSubscriber<Guid, bool, object>("Moodles.PresetUpdated");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var result = ApiVersion.InvokeFunc() >= 4;
            if (!APIAvailable && result)
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
    { }

    /// <summary>
    ///     Gets the ClientPlayer's StatusManager in tuple format.
    /// </summary>
    public async Task<List<MoodleStatusInfo>> GetOwnDataInfo()
    {
        if (!APIAvailable) return new List<MoodleStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(GetManagerInfo.InvokeFunc).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets another player's StatusManager in tuple format by pointer.
    /// </summary>
    public async Task<List<MoodleStatusInfo>> GetDataInfoByPtr(nint charaAddr)
    {
        if (!APIAvailable) return new List<MoodleStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerInfoByPtr.InvokeFunc(charaAddr)).ConfigureAwait(false);
    }

    #region Info Collection

    /// <summary>
    ///     Gets the StatusTuple for a specified GUID.
    /// </summary>
    public async Task<MoodleStatusInfo> GetStatusDetails(Guid guid)
    {
        if (!APIAvailable) return new MoodleStatusInfo();
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusInfo.InvokeFunc(guid)).ConfigureAwait(false);
    }

    /// <summary> 
    ///     Gets the list of all our clients Moodles Info
    /// </summary>
    public async Task<IEnumerable<MoodleStatusInfo>> GetStatusListDetails()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetStatusInfoList.InvokeFunc).ConfigureAwait(false);
    }

    /// <summary> 
    ///     Gets the preset info for a provided GUID from the client.
    /// </summary>
    public async Task<MoodlePresetInfo> GetPresetDetails(Guid guid)
    {
        if (!APIAvailable) return new MoodlePresetInfo();
        return await Svc.Framework.RunOnFrameworkThread(() => GetPresetInfo.InvokeFunc(guid)).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the list of all our clients Presets Info 
    /// </summary>
    public async Task<IEnumerable<MoodlePresetInfo>> GetPresetListDetails()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetPresetsInfoList.InvokeFunc).ConfigureAwait(false);
    }

    #endregion Info Collection.

    #region Moodle Manipulation
    public async Task ApplyOwnStatus(Guid guid)
    {
        if (!APIAvailable) return;
        // To my knowledge this no longer should be required to run on the main thread but we'll see.
        ApplyStatusByPtr.InvokeAction(guid, PlayerData.Address);
    }

    public async Task ApplyOwnStatus(IEnumerable<Guid> guidsToAdd)
    {
        if (!APIAvailable) return;

        foreach (var guid in guidsToAdd)
            ApplyStatusByPtr.InvokeAction(guid, PlayerData.Address);
    }
    public async Task RemoveOwnStatuses(IEnumerable<Guid> toRemove)
    {
        if (!APIAvailable) return;
        RemoveStatusesByPtr.InvokeAction(toRemove.ToList(), PlayerData.Address);
    }
    #endregion Moodle Manipulation
}
