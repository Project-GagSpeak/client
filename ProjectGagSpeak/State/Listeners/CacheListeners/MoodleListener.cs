using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.State.Listeners;

public class MoodleListener : DisposableMediatorSubscriberBase
{
    private readonly MoodleCache _cache;
    private readonly IpcProvider _ipcProvider;
    private readonly MainHub _hub;
    private readonly IpcCallerMoodles _ipc;
    private readonly KinksterManager _kinksters;
    private readonly DistributorService _dds;

    public MoodleListener(ILogger<MoodleListener> logger, GagspeakMediator mediator,
        MoodleCache cache, IpcProvider ipcProvider, MainHub hub, IpcCallerMoodles ipc,
        KinksterManager kinksters, DistributorService dds)
        : base(logger, mediator)
    {
        _cache = cache;
        _ipcProvider = ipcProvider;
        _hub = hub;
        _ipc = ipc;
        _kinksters = kinksters;
        _dds = dds;

        _ipc.OnStatusManagerModified.Subscribe(OnStatusManagerModified);
        _ipc.OnStatusUpdated.Subscribe(OnStatusModified);
        _ipc.OnPresetUpdated.Subscribe(OnPresetModified);

        // if the moodles API is already available by the time this loads, run OnMoodlesReady.
        // This lets us account for the case where we load before Moodles does.
        if (IpcCallerMoodles.APIAvailable)
            OnMoodlesReady();

        // Ensure we re-sync moodles when we connect to the main hub.
        Mediator.Subscribe<ConnectedMessage>(this, async _ => PushUpdatedSM());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnStatusManagerModified.Unsubscribe(OnStatusManagerModified);
        _ipc.OnStatusUpdated.Unsubscribe(OnStatusModified);
        _ipc.OnPresetUpdated.Unsubscribe(OnPresetModified);
    }

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    public async void OnMoodlesReady()
    {
        var dataStr = await _ipc.GetOwnDataStr().ConfigureAwait(false);
        var dataInfo = await _ipc.GetOwnDataInfo().ConfigureAwait(false);
        var statuses = await _ipc.GetStatusListDetails().ConfigureAwait(false);
        var presets = await _ipc.GetPresetListDetails().ConfigureAwait(false);
        MoodleCache.IpcData.UpdateDataInfo(dataStr, dataInfo);
        MoodleCache.IpcData.SetStatuses(statuses);
        MoodleCache.IpcData.SetPresets(presets);
        Logger.LogDebug("Moodles is now ready, pushing to all visible pairs", LoggerType.IpcMoodles);
        await _dds.DistributeFullMoodlesData(_kinksters.GetVisibleConnected());
    }

    /// <summary>
    ///     Triggers when our Client's StatusManager was modified. <para />
    ///     Whenever a chain happens we should immidiate distribute the change 
    ///     to all visible Kinksters if different from the last fetched datastring.
    /// </summary>
    public void OnStatusManagerModified(nint charaAddr)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        if (charaAddr != PlayerData.Address)
            return;

        if (!MainHub.IsConnectionDataSynced)
            return;

        if (DistributorService.LastMoodlesDataString.Equals(MoodleCache.IpcData.DataString))
            return;

        // Replace with this when fixed
        //CheckAndUpdateClientMoodles().ConfigureAwait(false);

        PushUpdatedSM();
    }

    private async void PushUpdatedSM()
    {
        // Reject when not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        if (DistributorService.LastMoodlesDataString.Equals(MoodleCache.IpcData.DataString))
            return;

        // Update moodles string, get visible kinksters, and push out update.
        DistributorService.LastMoodlesDataString = MoodleCache.IpcData.DataString;
        var visChara = _kinksters.GetVisibleConnected();
        Logger.LogTrace($"Pushing updated StatusManager to: ({string.Join(", ", visChara.Select(v => v.AliasOrUID))})", LoggerType.VisiblePairs);
        // this will never fail, so no point in scanning the return. 
        await _hub.UserPushMoodlesSM(new(visChara, MoodleCache.IpcData.DataString, MoodleCache.IpcData.DataInfoList.ToList())).ConfigureAwait(false);
    }

    /// <summary>
    ///     Checks if any of our restricted Moodles are no longer present in the new status manager update.
    /// </summary>
    /// <remarks> If any are found, it will reapply every missing moodle that should be active. </remarks>
    public async void CheckAndUpdateClientMoodles()
    {
        Logger.LogTrace($"PrevData: {string.Join("\n", MoodleCache.IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}"))}", LoggerType.IpcMoodles);
        var newDataStr = await _ipc.GetOwnDataStr().ConfigureAwait(false);
        var newDataInfo = await _ipc.GetOwnDataInfo().ConfigureAwait(false);
        MoodleCache.IpcData.UpdateDataInfo(newDataStr, newDataInfo);
        Logger.LogTrace($"NewData: {string.Join("\n", MoodleCache.IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}"))}", LoggerType.IpcMoodles);

        // Get the subset of moodles that are marked as restricted, but no longer in the latestManagerData.
        var missingIds = _cache.FinalStatusIds.Except(MoodleCache.IpcData.DataInfo.Keys);
        // Now locate which of these ids are tuples that need tuple reapplication.
        var missingTuples = _cache.FinalMoodleItems.OfType<MoodleTuple>().Where(t => missingIds.Contains(t.Id)).Select(t => t.Tuple);
        // remove those tuple id's from the missing ids.
        missingIds = missingIds.Except(missingTuples.Select(t => t.GUID));
        // Reapply the missing moodles by their GUID.
        if (missingIds.Any())
        {
            Logger.LogInformation("Detected Bratty restrained user trying to click off locked Moodles. Reapplying!", LoggerType.IpcMoodles);
            Logger.LogDebug("Reapplying Missing Required Moodles by ID: " + string.Join(", ", missingIds), LoggerType.IpcMoodles);
            // obtain the moodles that we need to reapply to the player from the expected moodles.
            await _ipc.ApplyOwnStatus(missingIds);
            return;

            // MAINTAINERS NOTE:
            // You can effectively make use of the TryOnMoodleStatus event from GagSpeaks providers if you want to keep stacks.
            // For now im not doing this for the sake of keeping it made for its intended purpose. If that desire ever builds I can append it.
        }
        // reapply missing tuples.
        if (missingTuples.Any())
        {
            Logger.LogInformation("Detected Bratty restrained user trying to click off locked Tuples. Reapplying!", LoggerType.IpcMoodles);
            Logger.LogDebug("Reapplying Missing Required Tuples: " + string.Join(", ", missingTuples.Select(t => t.GUID)), LoggerType.IpcMoodles);
            // obtain the tuples that we need to reapply to the player from the expected moodles.
            // _ipcProvider.ApplyTuples(missingTuples);
            return;
        }

        DistributorService.LastMoodlesDataString = MoodleCache.IpcData.DataString;
        var visChara = _kinksters.GetVisibleConnected();
        Logger.LogTrace($"Pushing updated StatusManager to: ({string.Join(", ", visChara.Select(v => v.AliasOrUID))})", LoggerType.VisiblePairs);
        // this will never fail, so no point in scanning the return. 
        await _hub.UserPushMoodlesSM(new(visChara, MoodleCache.IpcData.DataString, MoodleCache.IpcData.DataInfoList.ToList())).ConfigureAwait(false);
    }

    /// <summary> 
    ///     Fired whenever we change any setting in any of our Moodles Statuses via the Moodles UI.
    /// </summary>
    public async void OnStatusModified(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        MoodlesStatusInfo effected = new() { GUID = id };

        // If the moodle was deleted, set the effected as the removed tuple.
        if (wasDeleted)
            effected = MoodleCache.IpcData.Statuses.Remove(id, out var removed) ? removed : new() { GUID = id };
        // Otherwise add or update it.
        else
        {
            MoodleCache.IpcData.AddOrUpdateStatus(await _ipc.GetStatusDetails(id));
            effected = MoodleCache.IpcData.Statuses[id];
        }

        // Reject update not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        var visChara = _kinksters.GetVisibleConnected();
        if (visChara.Count is 0)
            return;
        // this will never fail, so no point in scanning the return.
        await _hub.UserPushStatusModified(new(visChara, effected, wasDeleted));
    }

    /// <summary> 
    ///     Fired whenever we change any setting in any of our Moodles Presets via the Moodles UI.
    /// </summary>
    public async void OnPresetModified(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        MoodlePresetInfo effected = new() { GUID = id };

        if (wasDeleted)
            effected = MoodleCache.IpcData.Presets.Remove(id, out var removed) ? removed : new() { GUID = id };
        else
        {
            MoodleCache.IpcData.AddOrUpdatePreset(await _ipc.GetPresetDetails(id));
            effected = MoodleCache.IpcData.Presets[id];
        }

        // Reject update not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        var visChara = _kinksters.GetVisibleConnected();
        if (visChara.Count is 0)
            return;
        // now push it out to our server.
        await _hub.UserPushPresetModified(new(visChara, effected, wasDeleted));
    }
}
