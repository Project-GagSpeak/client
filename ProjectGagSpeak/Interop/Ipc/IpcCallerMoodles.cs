using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

// Honestly dont really know what to use here.
// I guess we can check for version... maybe use it to apply own Ids?
// But others wouldnt know the ID's unless they had the data..
// There is also the nightmare of trying to design combos for each type.
// If anything, include the typedefs here.... not globally.
public sealed class IpcCallerMoodles : IIpcCaller
{
    private readonly ICallGateSubscriber<int> ApiVersion;

    // Guess we can monitor these to not completely screw people over. But dont use it in transfer either.
    public readonly ICallGateSubscriber<Guid, bool, object> OnStatusUpdated;
    public readonly ICallGateSubscriber<Guid, bool, object> OnPresetUpdated;

    // Dont fetch status manager info as we only allow removal via Loci.

    // This data when obtained could be used for initial storage if absolutely nessisary 
    private readonly ICallGateSubscriber<Guid, DeprecatedStatusInfo>       GetStatusInfo;
    private readonly ICallGateSubscriber<List<DeprecatedStatusInfo>>       GetStatusInfoList;
    private readonly ICallGateSubscriber<Guid, DeprecatedPresetInfo>       GetPresetInfo;
    private readonly ICallGateSubscriber<List<DeprecatedPresetInfo>>       GetPresetsInfoList;
    
    // Basic interaction for GUID application, but will can only be used to apply ones own statuses.
    private readonly ICallGateSubscriber<Guid, nint, object>        ApplyStatusByPtr;
    private readonly ICallGateSubscriber<List<Guid>, nint, object>  RemoveStatusesByPtr;

    private readonly GagspeakMediator _mediator;

    public IpcCallerMoodles(GagspeakMediator mediator)
    {
        _mediator = mediator;

        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Moodles.Version");

        // Helps the client obtain their own moodles data.
        GetStatusInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, DeprecatedStatusInfo>("Moodles.GetStatusInfoV2");
        GetStatusInfoList = Svc.PluginInterface.GetIpcSubscriber<List<DeprecatedStatusInfo>>("Moodles.GetStatusInfoListV2");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, DeprecatedPresetInfo>("Moodles.GetPresetInfoV2");
        GetPresetsInfoList = Svc.PluginInterface.GetIpcSubscriber<List<DeprecatedPresetInfo>>("Moodles.GetPresetsInfoListV2");
        
        // Helps the client apply their own moodle stuff.
        ApplyStatusByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Moodles.AddOrUpdateMoodleByPtrV2");
        RemoveStatusesByPtr = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, nint, object>("Moodles.RemoveMoodlesByPtrV2");

        // Help keep the clients stored data in check.
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
            else if (APIAvailable && !result)
                _mediator.Publish(new MoodlesDisposed());

            APIAvailable = result;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
    { }

    // Grab the client's Moodle Statuses. These should be converted to Loci format.
    public async Task<LociStatusInfo> GetStatusDetails(Guid guid)
    {
        if (!APIAvailable) return new LociStatusInfo();
        var deprecatedData = await Svc.Framework.RunOnFrameworkThread(() => GetStatusInfo.InvokeFunc(guid)).ConfigureAwait(false);
        return deprecatedData.ToLociTuple();
    }

    /// <summary> 
    ///     Gets the list of all our clients Moodles Info
    /// </summary>
    public async Task<List<LociStatusInfo>> GetStatusListDetails()
    {
        if (!APIAvailable) return [];
        var deprecatedData = await Svc.Framework.RunOnFrameworkThread(GetStatusInfoList.InvokeFunc).ConfigureAwait(false);
        return deprecatedData.Select(x => x.ToLociTuple()).ToList();
    }

    /// <summary> 
    ///     Gets the preset info for a provided GUID from the client.
    /// </summary>
    public async Task<LociPresetInfo> GetPresetDetails(Guid guid)
    {
        if (!APIAvailable) return new LociPresetInfo();
        var deprecatedData = await Svc.Framework.RunOnFrameworkThread(() => GetPresetInfo.InvokeFunc(guid)).ConfigureAwait(false);
        return deprecatedData.ToLociTuple();
    }

    /// <summary>
    ///     Gets the list of all our clients Presets Info 
    /// </summary>
    public async Task<List<LociPresetInfo>> GetPresetListDetails()
    {
        if (!APIAvailable) return [];
        var deprecatedData = await Svc.Framework.RunOnFrameworkThread(GetPresetsInfoList.InvokeFunc).ConfigureAwait(false);
        return deprecatedData.Select(x => x.ToLociTuple()).ToList();
    }

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
}
