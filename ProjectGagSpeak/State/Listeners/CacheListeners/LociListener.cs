using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GagSpeak.State.Listeners;

public class LociListener : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly IpcCallerLoci _ipc;
    private readonly KinksterManager _kinksters;
    private readonly DistributorService _dds;

    public LociListener(ILogger<LociListener> logger, GagspeakMediator mediator,
        MainHub hub, IpcCallerLoci ipc, KinksterManager kinksters, DistributorService dds)
        : base(logger, mediator)
    {
        _hub = hub;
        _ipc = ipc;
        _kinksters = kinksters;
        _dds = dds;

        _ipc.OnReady.Subscribe(OnLociReady);
        _ipc.OnDisposing.Subscribe(OnLociDisposed);
        _ipc.OnManagerModified.Subscribe(OnManagerModified);
        _ipc.OnStatusUpdated.Subscribe(OnStatusModified);
        _ipc.OnPresetUpdated.Subscribe(OnPresetModified);
        _ipc.OnTargetApplyStatus.Subscribe(OnApplyToTarget);
        _ipc.OnTargetApplyStatuses.Subscribe(OnApplyToTargetBulk);

        if (IpcCallerLoci.APIAvailable)
            OnLociReady();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnReady.Unsubscribe(OnLociReady);
        _ipc.OnDisposing.Unsubscribe(OnLociDisposed);
        _ipc.OnManagerModified.Unsubscribe(OnManagerModified);
        _ipc.OnStatusUpdated.Unsubscribe(OnStatusModified);
        _ipc.OnPresetUpdated.Unsubscribe(OnPresetModified);
        _ipc.OnTargetApplyStatus.Unsubscribe(OnApplyToTarget);
        _ipc.OnTargetApplyStatuses.Unsubscribe(OnApplyToTargetBulk);
    }

    public async void OnLociReady()
    {
        var dataInfo = await _ipc.GetManagerInfo().ConfigureAwait(false);
        var statuses = await _ipc.GetStatusListDetails().ConfigureAwait(false);
        var presets = await _ipc.GetPresetListDetails().ConfigureAwait(false);
        LociCache.Data.UpdateDataInfo(dataInfo);
        LociCache.Data.SetStatuses(statuses);
        LociCache.Data.SetPresets(presets);
        Logger.LogDebug("Loci ready, pushing to visible kinksters", LoggerType.IpcLoci);
        await _dds.UserPushLociData(_kinksters.GetVisibleConnected());
    }

    public async void OnLociDisposed()
    {
        LociCache.Data.DataInfo.Clear();
        LociCache.Data.Statuses.Clear();
        LociCache.Data.Presets.Clear();
        Logger.LogDebug("Loci disposed, pushing empty data to visible kinksters", LoggerType.IpcLoci);
    }

    public async void OnManagerModified(nint charaAddr)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        // get the updated info.
        var newInfo = await _ipc.GetManagerInfo(charaAddr).ConfigureAwait(false);

        // If it was us, update our cache, otherwise, update the kinksters.
        if (charaAddr == PlayerData.Address)
            LociCache.Data.UpdateDataInfo(newInfo);
        // Update the kinksters info, if valid.
        else if (_kinksters.DirectPairs.FirstOrDefault(x => x.PlayerAddress == charaAddr) is { } match)
            match.LociData.UpdateDataInfo(newInfo);
    }

    public async void OnStatusModified(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;

        // Attempt to update the effected.
        var effected = new LociStatusInfo() { GUID = id };
        // If deleted, remove it.
        if (wasDeleted)
            LociCache.Data.Statuses.Remove(id, out var removed);
        // Otherwise add or update it.
        else
        {
            effected = await _ipc.GetStatusDetails(id);
            LociCache.Data.TryUpdateStatus(effected);
        }

        // Reject update not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        var visChara = _kinksters.GetVisibleConnected();
        if (visChara.Count is 0)
            return;
        // It is valid, so push the update out
        try
        {
            await _hub.UserPushStatusModified(new(visChara, effected, wasDeleted));
        }
        catch (Bagagwa)
        {
            Logger.LogError($"Hub is still undergoing reworks with calls, safely exiting.", LoggerType.IpcLoci);
        }
    }

    public async void OnPresetModified(Guid id, bool wasDeleted)
    {
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;
        // Attempt to update the effected.
        var effected = new LociPresetInfo() { GUID = id };
        // If deleted, remove it.
        if (wasDeleted)
            LociCache.Data.Presets.Remove(id, out var removed);
        // Otherwise add or update it.
        else
        {
            effected = await _ipc.GetPresetDetails(id);
            LociCache.Data.TryUpdatePreset(effected);
        }

        // Reject update not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        var visChara = _kinksters.GetVisibleConnected();
        if (visChara.Count is 0)
            return;
        // now push it out to our server.
        try
        {
            await _hub.UserPushPresetModified(new(visChara, effected, wasDeleted));
        }
        catch (Bagagwa)
        {
            Logger.LogError($"Hub is still undergoing reworks with calls, safely exiting.", LoggerType.IpcLoci);
        }
    }

    private async void OnApplyToTarget(nint targetAddr, string targetHost, LociStatusInfo data)
        => OnApplyToTargetBulk(targetAddr, targetHost, [data]);

    private async void OnApplyToTargetBulk(nint targetAddr, string targetHost, List<LociStatusInfo> data)
    {
        // Ignore if not for us
        if (!string.Equals(targetHost, IpcCallerLoci.LOCI_IDENTIFIER, StringComparison.Ordinal))
            return;
        // Ignore if zoning or unavailable
        if (PlayerData.IsZoning || !PlayerData.Available)
            return;
        // Try to locate the Kinkster
        if (_kinksters.DirectPairs.FirstOrDefault(x => x.IsRendered && x.PlayerAddress == targetAddr) is not { } match)
            return;
        // Ensure we have the correct permissions to apply to them.
        if (!LociEx.CanApply(match.PairPerms, data))
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
}
