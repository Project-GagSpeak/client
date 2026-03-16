using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using LociApi.Enums;
using LociApi.Helpers;

namespace GagSpeak.State.Listeners;

public class LociListener : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcCallerMoodles _moodles;
    private readonly IpcCallerLoci _loci;
    private readonly KinksterManager _kinksters;
    private readonly CharaDataDistributor _dds;

    private readonly EventSubscriber<nint, ManagerChangeType> ManagerModified;
    private readonly EventSubscriber<nint, string, List<LociStatusInfo>> ApplyToTargetSent;
    private readonly EventSubscriber<Guid, bool> StatusUpdated;
    private readonly EventSubscriber<Guid, bool> PresetUpdated;
    private readonly EventSubscriber<nint, Guid, ChainTrigger, ChainType, Guid> ChainTriggerHit;

    public LociListener(ILogger<LociListener> logger, GagspeakMediator mediator,
        MainHub hub, IpcCallerMoodles moodles, IpcCallerLoci ipc, 
        KinksterManager kinksters, CharaDataDistributor dds)
        : base(logger, mediator)
    {
        _hub = hub;
        _moodles = moodles;
        _loci = ipc;
        _kinksters = kinksters;
        _dds = dds;

        // Fallback listeners
        _moodles.OnStatusUpdated.Subscribe(OnMoodleStatusModified);
        _moodles.OnPresetUpdated.Subscribe(OnMoodlePresetModified);

        ManagerModified = LociApi.Ipc.ManagerChanged.Subscriber(Svc.PluginInterface, OnManagerModified);
        StatusUpdated = LociApi.Ipc.StatusUpdated.Subscriber(Svc.PluginInterface, OnStatusUpdated);
        PresetUpdated = LociApi.Ipc.PresetUpdated.Subscriber(Svc.PluginInterface, OnPresetUpdated);
        ApplyToTargetSent = LociApi.Ipc.ApplyToTargetSent.Subscriber(Svc.PluginInterface, OnApplyToTarget);
        ChainTriggerHit = LociApi.Ipc.ChainTriggerHit.Subscriber(Svc.PluginInterface, OnChainTriggerHit);

        if (IpcCallerLoci.APIAvailable)
            OnLociReady();
        else if (IpcCallerMoodles.APIAvailable && !IpcCallerLoci.APIAvailable)
            OnMoodlesReady();

        Mediator.Subscribe<LociReady>(this, _ => OnLociReady());
        Mediator.Subscribe<LociDisposed>(this, _ => OnLociDisposed());
        Mediator.Subscribe<MoodlesReady>(this, _ => OnMoodlesReady());
        Mediator.Subscribe<MoodlesDisposed>(this, _ => OnMoodlesDisposed());
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        ManagerModified.Dispose();
        StatusUpdated.Dispose();
        PresetUpdated.Dispose();
        ApplyToTargetSent.Dispose();
        ChainTriggerHit.Dispose();
    }

    // We could technically if we really put in the effort make these sharable,
    // but they will quickly fall out of compatibility as LociData grows and is likely not worth the effort..
    private async void OnMoodlesReady()
    {
        // Prioritize Loci.
        if (IpcCallerLoci.APIAvailable)
            return;

        Logger.LogDebug("Moodles ready, storing initial data.");
        var statuses = await _moodles.GetStatusListDetails().ConfigureAwait(false);
        var presets = await _moodles.GetPresetListDetails().ConfigureAwait(false);
        // Do not support active status manager.
        LociCache.Data.SetStatuses(statuses);
        LociCache.Data.SetPresets(presets);
    }

    private void OnMoodlesDisposed()
    {
        // Prioritize Loci.
        if (IpcCallerLoci.APIAvailable)
            return;
        Logger.LogDebug("Moodles disposed, clearing data.");
        LociCache.Data.Statuses.Clear();
        LociCache.Data.Presets.Clear();
    }

    private async void OnMoodleStatusModified(Guid id, bool wasDeleted)
    {
        // Prioritize Loci.
        if (IpcCallerLoci.APIAvailable || PlayerData.IsZoning || !PlayerData.Available)
            return;

        // Update the data, but do not send the updated.
        if (wasDeleted)
            LociCache.Data.Statuses.Remove(id);
        else
            LociCache.Data.Statuses[id] = await _moodles.GetStatusDetails(id).ConfigureAwait(false);
    }

    private async void OnMoodlePresetModified(Guid id, bool wasDeleted)
    {
        // Prioritize Loci.
        if (IpcCallerLoci.APIAvailable || PlayerData.IsZoning || !PlayerData.Available)
            return;

        // Update the data, but do not send the update.
        if (wasDeleted)
            LociCache.Data.Presets.Remove(id);
        else
            LociCache.Data.Presets[id] = await _moodles.GetPresetDetails(id).ConfigureAwait(false);
    }

    private async void OnLociReady()
    {
        Logger.LogDebug("Loci ready, pushing to visible kinksters", LoggerType.IpcLoci);
        var dataInfo = await _loci.GetOwnManagerInfo().ConfigureAwait(false);
        var statuses = await _loci.GetStatusInfos().ConfigureAwait(false);
        var presets = await _loci.GetPresetInfos().ConfigureAwait(false);
        LociCache.Data.SetDataInfo(dataInfo);
        LociCache.Data.SetStatuses(statuses);
        LociCache.Data.SetPresets(presets);
        await _dds.UserPushLociData(_kinksters.GetVisibleConnected());
    }

    private async void OnLociDisposed()
    {
        LociCache.Data.DataInfo.Clear();
        LociCache.Data.Statuses.Clear();
        LociCache.Data.Presets.Clear();
        await _dds.UserPushLociData(_kinksters.GetVisibleConnected());
        Logger.LogDebug("Loci disposed, pushing empty data to visible kinksters", LoggerType.IpcLoci);
    }

    public async void OnManagerModified(nint charaAddr, ManagerChangeType changeType)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        // get the updated info.
        var newInfo = await _loci.GetActorSMInfo(charaAddr).ConfigureAwait(false);

        // If it was us, update our cache, otherwise, update the kinksters dataInfo list.
        // We dont need to push out to distribute this info, as others will do the same once they update.
        if (charaAddr == PlayerData.Address)
            LociCache.Data.SetDataInfo(newInfo);
        // Update the kinksters info, if valid.
        else if (_kinksters.DirectPairs.FirstOrDefault(x => x.PlayerAddress == charaAddr) is { } match)
            match.LociData.SetDataInfo(newInfo);
    }

    public async void OnStatusUpdated(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        if (wasDeleted)
            LociCache.Data.Statuses.Remove(id);
        else
            LociCache.Data.Statuses[id] = await _loci.GetStatusInfo(id).ConfigureAwait(false);

        if (!MainHub.IsConnectionDataSynced)
            return;

        // push the update.
        try
        {
            var toPush = wasDeleted ? new() : LociCache.Data.Statuses[id];
            var visChara = _kinksters.GetVisibleConnected();
            if (visChara.Count is 0) return;
            await _hub.UserPushStatusModified(new(visChara, toPush.ToStruct(), wasDeleted));
        }
        catch (Bagagwa)
        {
            Logger.LogError($"Hub is still undergoing reworks with calls, safely exiting.");
        }
    }

    public async void OnPresetUpdated(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        if (wasDeleted)
            LociCache.Data.Presets.Remove(id);
        else
            LociCache.Data.Presets[id] = await _loci.GetPresetInfo(id).ConfigureAwait(false);

        if (!MainHub.IsConnectionDataSynced)
            return;
        try
        {
            // now push it out to our server.
            var toPush = wasDeleted ? new() : LociCache.Data.Presets[id];
            var visChara = _kinksters.GetVisibleConnected();
            if (visChara.Count is 0) return;
            await _hub.UserPushPresetModified(new(visChara, toPush.ToStruct(), wasDeleted));
        }
        catch (Bagagwa)
        {
            Logger.LogError($"Hub is still undergoing reworks with calls, safely exiting.", LoggerType.IpcLoci);
        }
    }

    private async void OnApplyToTarget(nint targetAddr, string targetHost, List<LociStatusInfo> data)
    {
        // Ignore if not for us
        if (!string.Equals(targetHost, IpcCallerLoci.GAGSPEAK_TAG, StringComparison.Ordinal))
            return;
        // Ignore if zoning or unavailable
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;
        // Try to locate the Kinkster
        if (_kinksters.DirectPairs.FirstOrDefault(x => x.IsRendered && x.PlayerAddress == targetAddr) is not { } match)
            return;
        // Ensure we have the correct permissions to apply to them.
        if (!LociHelpers.CanApply(match.PairPerms, data))
            return;
        // It is valid, so push the update out
        try
        {
            await _dds.ApplyTuplesToKinkster(match.UserData, data, false).ConfigureAwait(false);
        }
        catch (Bagagwa)
        {
            Logger.LogError($"Hub is still undergoing reworks with calls, safely exiting.", LoggerType.IpcLoci);
        }
    }

    private async void OnChainTriggerHit(nint targetAddr, Guid statusId, ChainTrigger triggerCond, ChainType type, Guid chainedId)
    {
        // Ignore if not for us
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        // Dont do anything yet, but eventually we can use this to invoke triggers or do other devious things.
    }
}
