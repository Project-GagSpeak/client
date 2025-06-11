using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Interop.Ipc;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using System.Threading.Tasks;

namespace GagSpeak.PlayerState.Visual;

// As much as i hate partial classes this must be done to avoid circular dependancies.
// - This section of the partial class handles caching.
public partial class MoodleHandler : DisposableMediatorSubscriberBase
{
    private readonly ClientMonitor _clientData;
    private readonly IpcCallerMoodles _ipc;

    private bool _isZoning = false;

    public MoodleHandler(ILogger<MoodleHandler> logger, GagspeakMediator mediator,
    ClientMonitor client, IpcCallerMoodles ipc) : base(logger, mediator)
    {
        _clientData = client;
        _ipc = ipc;

        // Subscribers for Moodles
        _ipc.OnStatusManagerModified.Subscribe(OnStatusManagerModified);
        _ipc.OnStatusSettingsModified.Subscribe(msg => _ = OnStatusModified(msg));
        _ipc.OnPresetModified.Subscribe(msg => _ = OnPresetModified(msg));

        // if the moodles API is already available by the time this loads, run OnMoodlesReady.
        // This lets us account for the case where we load before Moodles does.
        if (IpcCallerMoodles.APIAvailable)
            OnMoodlesReady();

        // Helps us handle preventing changes while zoning.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => _isZoning = true);
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, (msg) => _isZoning = false);
    }

    /// <summary> The current stored IPCData for the clients Moodles Data. </summary>
    public static CharaIPCData IpcData = new CharaIPCData();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnStatusManagerModified.Unsubscribe(OnStatusManagerModified);
        _ipc.OnStatusSettingsModified.Unsubscribe(msg => _ = OnStatusModified(msg));
        _ipc.OnPresetModified.Unsubscribe(msg => _ = OnPresetModified(msg));
    }

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    public async void OnMoodlesReady()
    {
        var dataStr = await _ipc.GetStatusManagerString().ConfigureAwait(false);
        var dataInfo = await _ipc.GetStatusManagerDetails().ConfigureAwait(false);
        var statuses = await _ipc.GetStatusListDetails().ConfigureAwait(false);
        var presets = await _ipc.GetPresetListDetails().ConfigureAwait(false);
        IpcData.UpdateDataInfo(dataStr, dataInfo);
        IpcData.SetStatuses(statuses);
        IpcData.SetPresets(presets);
        Logger.LogDebug("Moodles is now ready, pushing to all visible pairs", LoggerType.IpcMoodles);
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.UpdateVisible, IpcData));
    }

    /// <summary> Handles the Moodles Status Manager being modified. </summary>
    /// <remarks> Will also reapply any restricted Moodles gone missing! </remarks>
    public void OnStatusManagerModified(IPlayerCharacter chara)
    {
        if (_isZoning || !_clientData.IsPresent)
            return;

        if (chara.Address != _clientData.Address)
            return;

        // Check and Update the clients Moodles. Reapply if the moodle removed was restricted.
        CheckAndUpdateClientMoodles().ConfigureAwait(false);
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Statuses via the Moodles UI </summary>
    public async Task OnStatusModified(Guid id)
    {
        if (_isZoning || !_clientData.IsPresent)
            return;

        if (IpcData.Statuses.ContainsKey(id))
            IpcData.TryUpdateStatus(await _ipc.GetStatusDetails(id));
        else
            IpcData.SetStatuses(await _ipc.GetStatusListDetails());

        // Push the update to the visible pairs.
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.StatusesUpdated, IpcData));
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Presets via the Moodles UI </summary>
    public async Task OnPresetModified(Guid id)
    {
        if (_isZoning || !_clientData.IsPresent)
            return;

        if (IpcData.Presets.ContainsKey(id))
            IpcData.TryUpdatePreset(await _ipc.GetPresetDetails(id));
        else
            IpcData.SetPresets(await _ipc.GetPresetListDetails());

        // Push the update to the visible pairs.
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.PresetsUpdated, IpcData));
    }

    /// <summary> Checks if any of our restricted Moodles are no longer present in the new status manager update. </summary>
    /// <remarks> If any are found, it will continue to reapply, and only send an update once all are reapplied. </remarks>
    public async Task CheckAndUpdateClientMoodles()
    {
        Logger.LogTrace("PrevData: " + string.Join("\n", IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}")), LoggerType.IpcMoodles);
        var newDataStr = await _ipc.GetStatusManagerString().ConfigureAwait(false);
        var newDataInfo = await _ipc.GetStatusManagerDetails().ConfigureAwait(false);
        IpcData.UpdateDataInfo(newDataStr, newDataInfo);
        Logger.LogTrace("NewData: " + string.Join("\n", IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}")), LoggerType.IpcMoodles);

        // Get the subset of moodles that are marked as restricted, but no longer in the latestManagerData.
        var missingRequired = _finalStatusIds.Except(IpcData.DataInfo.Keys);
        if (missingRequired.Any())
        {
            Logger.LogInformation("Detected Bratty restrained user trying to click off locked Moodles. Reapplying!", LoggerType.IpcMoodles);
            Logger.LogDebug("Reapplying Missing Required Moodles: " + string.Join(", ", missingRequired), LoggerType.IpcMoodles);
            // obtain the moodles that we need to reapply to the player from the expected moodles.
            await _ipc.ApplyOwnStatusByGUID(missingRequired);
            return;

            // MAINTAINERS NOTE:
            // You can effectively make use of the TryOnMoodleStatus event from GagSpeaks providers if you want to keep stacks.
            // For now im not doing this for the sake of keeping it made for its intended purpose. If that desire ever builds I can append it.
        }

        Logger.LogTrace("Pushing IPC update to CacheCreation for processing", LoggerType.IpcMoodles);
        Mediator.Publish(new IpcDataChangedMessage(DataUpdateType.StatusManagerChanged, IpcData));
    }

    // Likely makes things fucky with timed moodles but yeah, idk.
    // Could add more overhead but not in the mood, just want efficiency rn.
    /// <summary> Applies all cached restricted moodles to the client. </summary>
    public async Task ApplyMoodleCache()
    {
        await _ipc.ApplyOwnStatusByGUID(_finalStatusIds.Except(IpcData.DataInfo.Keys));
        Logger.LogDebug("Applied all cached moodles to the client.", LoggerType.IpcMoodles);
    }

    /// <summary> Removes moodles no longer meant to be present, then reapplies restricted ones. </summary>
    public async Task RestoreAndReapplyCache(IEnumerable<Guid> moodlesToRemove)
    {
        await _ipc.RemoveOwnStatusByGuid(moodlesToRemove);
        Logger.LogDebug($"Removed Moodles: {string.Join(", ", moodlesToRemove)}", LoggerType.IpcMoodles);
        // Reapply restricted.
        await ApplyMoodleCache();
    }

    /// <summary> Applies a moodle to the client. Can be souced from anything. </summary>
    /// <remarks> If this moodle is not present in the client's Moodle Status List, it will not work. </remarks>
    public async Task ApplyMoodle(Moodle moodle)
    {
        await _ipc.ApplyOwnStatusByGUID(moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]);
    }

    public async Task ApplyMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await ApplyMoodle(moodle));
    }

    /// <summary> Assumes they have already been removed from the finalMoodles cache. </summary>
    private async Task RemoveMoodle(Moodle moodle)
    {
        await _ipc.RemoveOwnStatusByGuid((moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]).Except(_finalStatusIds));
    }

    /// <summary> Hopefully we never have to fun this... </summary>
    /// <remarks> Assumes they have already been removed from the finalMoodles cache. </remarks>
    public async Task RemoveMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await RemoveMoodle(moodle));
    }
}
