using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;

namespace GagSpeak.State.Listeners;

public class MoodleListener : DisposableMediatorSubscriberBase
{
    private readonly MoodleCache _cache;
    private readonly IpcCallerMoodles _ipc;
    private readonly DataDistributionService _dds;

    private bool _isZoning = false;

    public MoodleListener(ILogger<MoodleListener> logger, GagspeakMediator mediator,
        MoodleCache cache, IpcCallerMoodles ipc, DataDistributionService dds)
        : base(logger, mediator)
    {
        _cache = cache;
        _ipc = ipc;
        _dds = dds;

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
        MoodleCache.IpcData.UpdateDataInfo(dataStr, dataInfo);
        MoodleCache.IpcData.SetStatuses(statuses);
        MoodleCache.IpcData.SetPresets(presets);
        Logger.LogDebug("Moodles is now ready, pushing to all visible pairs", LoggerType.IpcMoodles);
        await _dds.UpdateAllVisibleWithMoodles();
    }

    /// <summary> Handles the Moodles Status Manager being modified. </summary>
    /// <remarks> Will also reapply any restricted Moodles gone missing! </remarks>
    public void OnStatusManagerModified(IPlayerCharacter chara)
    {
        if (_isZoning || !PlayerData.Available)
            return;

        if (chara.Address != PlayerData.ObjectAddress)
            return;

        // Check and Update the clients Moodles. Reapply if the moodle removed was restricted.
        CheckAndUpdateClientMoodles().ConfigureAwait(false);
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Statuses via the Moodles UI </summary>
    public async Task OnStatusModified(Guid id)
    {
        if (_isZoning || !PlayerData.Available)
            return;

        if (MoodleCache.IpcData.Statuses.ContainsKey(id))
            MoodleCache.IpcData.TryUpdateStatus(await _ipc.GetStatusDetails(id));
        else
            MoodleCache.IpcData.SetStatuses(await _ipc.GetStatusListDetails());

        // now push it out to our server.
        await _dds.PushMoodleStatusList();
    }

    /// <summary> Fired whenever we change any setting in any of our Moodles Presets via the Moodles UI </summary>
    public async Task OnPresetModified(Guid id)
    {
        if (_isZoning || !PlayerData.Available)
            return;

        if (MoodleCache.IpcData.Presets.ContainsKey(id))
            MoodleCache.IpcData.TryUpdatePreset(await _ipc.GetPresetDetails(id));
        else
            MoodleCache.IpcData.SetPresets(await _ipc.GetPresetListDetails());

        // now push it out to our server.
        await _dds.PushMoodlePresetList();
    }

    /// <summary>
    ///     Checks if any of our restricted Moodles are no longer present in the new status manager update.
    /// </summary>
    /// <remarks> If any are found, it will reapply every missing moodle that should be active. </remarks>
    public async Task CheckAndUpdateClientMoodles()
    {
        Logger.LogTrace("PrevData: " + string.Join("\n", MoodleCache.IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}")), LoggerType.IpcMoodles);
        var newDataStr = await _ipc.GetStatusManagerString().ConfigureAwait(false);
        var newDataInfo = await _ipc.GetStatusManagerDetails().ConfigureAwait(false);
        MoodleCache.IpcData.UpdateDataInfo(newDataStr, newDataInfo);
        Logger.LogTrace("NewData: " + string.Join("\n", MoodleCache.IpcData.DataInfo.Values.Select(x => $"{x.Title} | {x.Stacks} | {x.GUID}")), LoggerType.IpcMoodles);

        // Get the subset of moodles that are marked as restricted, but no longer in the latestManagerData.
        var missingRequired = _cache.FinalStatusIds.Except(MoodleCache.IpcData.DataInfo.Keys);
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
        await _dds.PushMoodleStatusManager();
    }
}
