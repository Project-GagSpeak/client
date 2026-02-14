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
    private readonly ICallGateSubscriber<string>                        GetOwnStatusManager;
    private readonly ICallGateSubscriber<nint, string>                  GetStatusManagerByPtr;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>>       GetOwnStatusManagerInfo;
    private readonly ICallGateSubscriber<nint, List<MoodlesStatusInfo>> GetStatusManagerInfoByPtr;
    private readonly ICallGateSubscriber<Guid, MoodlesStatusInfo>       GetStatusInfo;
    private readonly ICallGateSubscriber<List<MoodlesStatusInfo>>       GetStatusInfoList;
    private readonly ICallGateSubscriber<Guid, MoodlePresetInfo>        GetPresetInfo;
    private readonly ICallGateSubscriber<List<MoodlePresetInfo>>        GetPresetsInfoList;

    // Used when a pair applies their statuses to us.
    // private readonly ICallGateSubscriber<string, List<MoodlesStatusInfo>, object> ApplyStatusFromPair;

    // API Enactors
    private readonly ICallGateSubscriber<nint, string, object>      SetStatusManagerByPtr;
    private readonly ICallGateSubscriber<nint, object>              ClearStatusMangerByPtr;
    private readonly ICallGateSubscriber<Guid, nint, object>        ApplyStatusByPtr;
    private readonly ICallGateSubscriber<Guid, nint, object>        ApplyPresetByPtr;
    private readonly ICallGateSubscriber<List<Guid>, nint, object>  RemoveStatusesByPtr;
    // Other calls are made via the static calls of the IpcProvider,
    // such as tuple application and locking/unlocking.

    private readonly GagspeakMediator _mediator;

    public IpcCallerMoodles(GagspeakMediator mediator)
    {
        _mediator = mediator;

        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version");

        // API Getters (Status Managers)
        GetOwnStatusManager = Svc.PluginInterface.GetIpcSubscriber<string>("Moodles.GetClientStatusManagerV2");
        GetOwnStatusManagerInfo = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetClientStatusManagerInfoV2");
        GetStatusManagerByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, string>("Moodles.GetStatusManagerByPtrV2");
        GetStatusManagerInfoByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, List<MoodlesStatusInfo>>("Moodles.GetStatusManagerInfoByPtrV2");

        // API Getters (Status and Preset Info)
        GetStatusInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlesStatusInfo>("Moodles.GetStatusInfoV2");
        GetStatusInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodlesStatusInfo>>("Moodles.GetStatusInfoListV2");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, MoodlePresetInfo>("Moodles.GetPresetInfoV2");
        GetPresetsInfoList = Svc.PluginInterface.GetIpcSubscriber<List<MoodlePresetInfo>>("Moodles.GetPresetsInfoListV2");

        // API Enactors
        SetStatusManagerByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, string, object>("Moodles.SetStatusManagerByPtrV2");
        ClearStatusMangerByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, object>("Moodles.ClearStatusManagerByPtrV2");
        ApplyStatusByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Moodles.AddOrUpdateMoodleByPtrV2");
        ApplyPresetByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Moodles.ApplyPresetByPtrV2");
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
    ///     Gets the ClientPlayer's StatusManager string.
    /// </summary>
    public async Task<string> GetOwnDataStr()
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetOwnStatusManager.InvokeFunc() ?? string.Empty).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets the ClientPlayer's StatusManager in tuple format.
    /// </summary>
    public async Task<List<MoodlesStatusInfo>> GetOwnDataInfo()
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(GetOwnStatusManagerInfo.InvokeFunc).ConfigureAwait(false);
    }

    /// <summary> 
    ///     Gets the StatusManager by pointer.
    /// </summary>
    public async Task<string?> GetDataStrByPtr(nint charaAddr)
    {
        if (!APIAvailable) return null;
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusManagerByPtr.InvokeFunc(charaAddr)).ConfigureAwait(false);
    }

    /// <summary>
    ///     Gets another player's StatusManager in tuple format by pointer.
    /// </summary>
    public async Task<List<MoodlesStatusInfo>> GetDataInfoByPtr(nint charaAddr)
    {
        if (!APIAvailable) return new List<MoodlesStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusManagerInfoByPtr.InvokeFunc(charaAddr)).ConfigureAwait(false);
    }

    #region Info Collection

    /// <summary>
    ///     Gets the StatusTuple for a specified GUID.
    /// </summary>
    public async Task<MoodlesStatusInfo> GetStatusDetails(Guid guid)
    {
        if (!APIAvailable) return new MoodlesStatusInfo();
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusInfo.InvokeFunc(guid)).ConfigureAwait(false);
    }

    /// <summary> 
    ///     Gets the list of all our clients Moodles Info
    /// </summary>
    public async Task<IEnumerable<MoodlesStatusInfo>> GetStatusListDetails()
    {
        if (!APIAvailable) return Enumerable.Empty<MoodlesStatusInfo>();
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
        if (!APIAvailable) return Enumerable.Empty<MoodlePresetInfo>();
        return await Svc.Framework.RunOnFrameworkThread(GetPresetsInfoList.InvokeFunc).ConfigureAwait(false);
    }

    #endregion Info Collection.

    #region StatusManager DataSync

    /// <summary>
    ///     Sets the StatusManager by pointer.
    /// </summary>
    public async Task SetByPtr(nint charaAddr, string statusString)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => SetStatusManagerByPtr.InvokeAction(charaAddr, statusString)).ConfigureAwait(false);
    }

    /// <summary>
    ///     Clears a players StatusManager by pointer.
    /// </summary>
    public async Task ClearByPtr(nint charaAddr)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ClearStatusMangerByPtr.InvokeAction(charaAddr)).ConfigureAwait(false);
    }
    #endregion StatusManager DataSync

    #region Moodle Manipulation
    /// <summary>
    ///     Applies one of our Client's own moodles to them by GUID. <br/>
    ///     Optionally, if you want to lock the applied status, you can do so.
    /// </summary>
    public async Task ApplyOwnStatus(Guid guid)
    {
        if (!APIAvailable) return;
        // To my knowledge this no longer should be required to run on the main thread but we'll see.
        ApplyStatusByPtr.InvokeAction(guid, PlayerData.Address);
    }

    /// <inheritdoc cref="ApplyOwnStatus(Guid, bool)"/>
    public async Task ApplyOwnStatus(IEnumerable<Guid> guidsToAdd)
    {
        if (!APIAvailable) return;

        foreach (var guid in guidsToAdd)
            ApplyStatusByPtr.InvokeAction(guid, PlayerData.Address);
    }

    /// <summary>
    ///     Applies one of our Client's own presets to them by GUID.
    /// </summary>
    public async Task ApplyOwnPreset(Guid guid)
    {
        if (!APIAvailable) return;
        ApplyPresetByPtr.InvokeAction(guid, PlayerData.Address);
    }

    /// <summary> 
    ///     Removes statuses from the Client's StatusManager by GUID. <para />
    ///     If the statuses are locked, this action will be ignored unless unlocked.
    /// </summary>
    public async Task RemoveOwnStatuses(IEnumerable<Guid> toRemove)
    {
        if (!APIAvailable) return;
        RemoveStatusesByPtr.InvokeAction(toRemove.ToList(), PlayerData.Address);
    }
    #endregion Moodle Manipulation
}
