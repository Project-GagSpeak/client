using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
using GagSpeak.Restrictions;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Handles Moodles status manager, and all updates the Moodles content. 
/// <para> This VisualApplier handles the management of the player characters IPC data as well. </para>
/// <para> In addition, this can also host the management of the creation and destruction of other pair objects. </para>
/// <para> SIDENODE: Moodles does not seem to care about running operations on the framework tick, so no awaits used here. </para>
/// </summary>
/// <remarks> This includes data sourced from both other players, and GagSpeak items. </remarks>
public class VisualApplierMoodles : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerMoodles _moodles;
    private readonly ClientMonitor _clientMonitor;

    private bool _isZoning = false;

    public VisualApplierMoodles(ILogger<VisualApplierMoodles> logger, GagspeakMediator mediator,
        IpcCallerMoodles moodles, ClientMonitor clientMonitor) : base(logger, mediator)
    {
        _moodles = moodles;
        _clientMonitor = clientMonitor;

        // if the moodles API is already available by the time this loads, run OnMoodlesReady.
        // This lets us account for the case where we load before Moodles does.
        if(IpcCallerMoodles.APIAvailable)
            OnMoodlesReady();

        // Helps us handle preventing changes while zoning.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => _isZoning = true);
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, (msg) => _isZoning = false);

        // there was something here about removing objects and reapplying things on zone change, but dont think that applies to moodles.
        Mediator.Subscribe<DalamudLogoutMessage>(this, (msg) => RestrictedMoodles.Clear());
    }

    public static CharaIPCData LatestIpcData = new CharaIPCData();
    // This could be turned into a MoodleStatusInfo tuple, if desired, to preset things at specific stacks or whatever for custom moodles.
    // The integration is there, but right now my sanity is not.
    public HashSet<Guid> RestrictedMoodles = new HashSet<Guid>();

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    public async void OnMoodlesReady()
    {
        LatestIpcData.MoodlesData = await _moodles.GetStatusManagerString().ConfigureAwait(false);
        LatestIpcData.MoodlesDataStatuses = await _moodles.GetStatusManagerDetails().ConfigureAwait(false);
        LatestIpcData.MoodlesStatuses = await _moodles.GetStatusListDetails().ConfigureAwait(false);
        LatestIpcData.MoodlesPresets = await _moodles.GetPresetListDetails().ConfigureAwait(false);
        Logger.LogDebug("Moodles is now ready, pushing to all visible pairs", LoggerType.IpcMoodles);
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.UpdateVisible, LatestIpcData));
    }

    /// <summary> Handles the Moodles Status Manager being modified. </summary>
    /// <remarks> Will also reapply any restricted Moodles gone missing! </remarks>
    public void StatusManagerModified(IntPtr address)
    {
        if(_isZoning || !_clientMonitor.IsPresent)
            return;

        if(address != _clientMonitor.Address)
            return;

        // Check and Update the clients Moodles. Reapply if the moodle removed was restricted.
        CheckAndUpdateClientMoodles().ConfigureAwait(false);
    }

    /// <summary> Checks if any of our restricted Moodles are no longer present in the new status manager update. </summary>
    /// <remarks> If any are found, it will continue to reapply, and only send an update once all are reapplied. </remarks>
    public async Task CheckAndUpdateClientMoodles()
    {
        // Grab the latest data after the status manager update.
        LatestIpcData.MoodlesData = await _moodles.GetStatusManagerString();
        LatestIpcData.MoodlesDataStatuses = await _moodles.GetStatusManagerDetails();

        var PrevManagerData = LatestIpcData.MoodlesDataStatuses.ToHashSet(); // <-- This would technically have the latest appropriate stack, if we dont handle stacks.

        // log the latestManagerData fetched, in string format [Title :: StackCount :: GUID ]. Do this for the previous manager as well.
        Logger.LogTrace("PrevManagerData: " + string.Join(", ", PrevManagerData.Select(x => x.Title + " :: " + x.Stacks + " :: " + x.GUID)), LoggerType.IpcMoodles);
        Logger.LogTrace("LatestManagerData: " + string.Join(", ", LatestIpcData.MoodlesDataStatuses.Select(x => x.Title + " :: " + x.Stacks + " :: " + x.GUID)), LoggerType.IpcMoodles);

        // Get the subset of moodles that are marked as restricted, but no longer in the latestManagerData.
        var missingRestrictedMoodles = RestrictedMoodles.Except(LatestIpcData.MoodlesDataStatuses.Select(x => x.GUID));

        // Log the missing restricted moodles.
        Logger.LogDebug("Missing Restricted Moodles: " + string.Join(", ", missingRestrictedMoodles), LoggerType.IpcMoodles);
        if (missingRestrictedMoodles.Any())
        {
            // instead of sharing an update, immediately reapply them, and continue to do this until all restrictions are properly reapplied.
            Logger.LogTrace("Detected Bratty restrained user trying to click off locked Moodles. Reapplying!", LoggerType.IpcMoodles);
            // obtain the moodles that we need to reapply to the player from the expected moodles.            
            _moodles.ApplyOwnStatusByGUID(missingRestrictedMoodles);
            return;
            // MAINTAINERS NOTE:
            // You can effectively make use of the TryOnMoodleStatus event from GagSpeaks providers if you want to keep stacks.
            // For now im not doing this for the sake of keeping it made for its intended purpose. If that desire ever builds I can append it.
        }

        Logger.LogDebug("Pushing IPC update to CacheCreation for processing", LoggerType.IpcMoodles);
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.StatusManagerChanged, LatestIpcData));
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Statuses via the Moodles UI </summary>
    public async void ClientStatusModified(Guid id)
    {
        if(_isZoning || !_clientMonitor.IsPresent)
            return;

        // locate the index in our status list. We have a list of tuple objects, so need to know which has the matching guid.
        var idx = LatestIpcData.MoodlesStatuses.FindIndex(x => x.GUID == id);
        // now that we have the index, if it is -1, then grab all statuses. If it is present, replace that index.
        if(idx != -1) LatestIpcData.MoodlesStatuses[idx] = await _moodles.GetStatusDetails(id);
        else LatestIpcData.MoodlesStatuses = await _moodles.GetStatusListDetails();

        // Push the update to the visible pairs.
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.StatusesUpdated, LatestIpcData));
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Presets via the Moodles UI </summary>
    public async void ClientPresetModified(Guid id)
    {
        if(_isZoning || !_clientMonitor.IsPresent)
            return;

        // locate the index in our status list. We have a list of tuple objects, so need to know which has the matching guid.
        var idx = LatestIpcData.MoodlesPresets.FindIndex(x => x.GUID == id);
        // now that we have the index, if it is -1, then grab all statuses. If it is present, replace that index.
        if(idx != -1) LatestIpcData.MoodlesPresets[idx] = await _moodles.GetPresetDetails(id);
        else LatestIpcData.MoodlesPresets = await _moodles.GetPresetListDetails();

        // Push the update to the visible pairs.
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.PresetsUpdated, LatestIpcData));
    }

    public void AddRestrictedMoodle(Moodle moodle)
    {
        if (LatestIpcData.MoodlesStatuses.Count == 0)
            return;

        // determine the type. If is it a status, just add the ID. if it is a preset, we need to obtain the list of statuses from that preset.
        if (moodle is MoodlePreset { } preset)
        {
            // determine what moodles should be applied by taking the moodlePreset.StatusIds and doing an except/union with Restricted moodles to get it.
            var missing = preset.StatusIds.Except(RestrictedMoodles);
            Logger.LogDebug("Adding Restricted Moodles not yet applied to the player: " + string.Join(", ", missing), LoggerType.IpcMoodles);
            RestrictedMoodles.Union(preset.StatusIds);
            // Apply the missing.
            if (missing.Any())
                _moodles.ApplyOwnStatusByGUID(missing);
        }
        else
        {
            RestrictedMoodles.Add(moodle.Id);
            // try and apply it if it is not already applied.
            if (!LatestIpcData.MoodlesDataStatuses.Any(x => x.GUID == moodle.Id))
                _moodles.ApplyOwnStatusByGUID(new[] { moodle.Id });
        }
    }

    public void AddRestrictedMoodle(IEnumerable<Moodle> moodles)
    {
        foreach (var moodle in moodles)
            AddRestrictedMoodle(moodle);
    }

    // This will be handled better later i think.
    public void RemoveRestrictedMoodle(Moodle moodle)
    {
        if(LatestIpcData.MoodlesStatuses.Count == 0)
            return;

        IEnumerable<Guid> moodlesToRemove = moodle switch
        {
            MoodlePreset p => p.StatusIds,
            Moodle m => new[] { m.Id },
            _ => Enumerable.Empty<Guid>()
        };
        // apply the updates if it contained anything.
        if(moodlesToRemove.Any())
        {
            RestrictedMoodles.ExceptWith(moodlesToRemove);
            _moodles.RemoveOwnStatusByGuid(moodlesToRemove);
        }
    }

    public void RemoveRestrictedMoodle(IEnumerable<Moodle> moodles)
    {
        foreach (var moodle in moodles)
            RemoveRestrictedMoodle(moodle);
    }
}
