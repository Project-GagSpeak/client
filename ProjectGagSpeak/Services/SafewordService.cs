using CkCommons;
using CkCommons.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Hub;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services;

// The most fundamentally important service in the entire application.
// helps revert any active states applied to the player when used.
// This class should not be connected to anything that links back to the MainHub.
public class SafewordService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly MainHub _hub;
    private readonly ClientData _clientData;
    private readonly CacheStateManager _visualCacheManager;
    private readonly KinksterManager _kinksters;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly BuzzToyManager _toys;
    private readonly HcTaskManager _hcTasks;
    private readonly PlayerCtrlHandler _hcHandler;
    private readonly ClientDataListener _clientDatListener;
    private readonly AchievementEventHandler _achievementHandler;

    private CancellationTokenSource _emergencySafewordCTS = new();
    private Task? _ctrlAltBackspaceSafewordTask = null;

    // The last times each of these Safeword's were used.
    private DateTime _lastSafewordTime = DateTime.MinValue;
    private DateTime _lastHcSafewordTime = DateTime.MinValue;

    public SafewordService(ILogger<SafewordService> logger, GagspeakMediator mediator,
        MainHub hub, 
        ClientData clientData,
        CacheStateManager visualManager,
        KinksterManager kinksters,
        PatternManager patterns, 
        AlarmManager alarms,
        TriggerManager triggers,
        BuzzToyManager toys,
        HcTaskManager hcTasks,
        PlayerCtrlHandler hcHandler,
        ClientDataListener clientDataListener,
        AchievementEventHandler achievements)
        : base(logger, mediator)
    {
        _hub = hub;
        _clientData = clientData;
        _visualCacheManager = visualManager;
        _kinksters = kinksters;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        _toys = toys;
        _hcTasks = hcTasks;
        _hcHandler = hcHandler;
        _clientDatListener = clientDataListener;
        _achievementHandler = achievements;
    }

    public bool SafewordOnCooldown => DateTime.Now < _lastSafewordTime.AddMinutes(5);
    public bool HcSafewordOnCooldown => DateTime.Now < _lastHcSafewordTime.AddMinutes(1);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _emergencySafewordCTS.SafeCancel();
        Generic.Safe(() => _ctrlAltBackspaceSafewordTask?.Wait());
        _emergencySafewordCTS?.SafeDispose();
    }

    public async Task OnSafewordInvoked(string isolatedUID = "")
    {
        // if we used this while the safeword was on cooldown, return with a warning.
        if (SafewordOnCooldown)
        {
            Logger.LogWarning("Safeword was used too soon after the last use. Must wait 5 minutes.");
            return;
        }

        // update the last time triggered.
        _lastSafewordTime = DateTime.Now;
        Logger.LogInformation("[SAFEWORD PROGRESS]: Safeword was used.");


        _achievementHandler.SafewordUsed(isolatedUID);

        // MAINTAINERS NOTE:
        // We will be pushing a CharacterCompositeData with NULL storage to serve
        // as a bulk update for all of our active states being changed due to the
        // safeword.

        // Therefore, it is important that we ONLY call the CLIENT SIDE UPDATES.
        // (aka the calls invoked from the server callbacks) to update our states!

        // We don't need to include anything directly impacted by the safeword, as
        // that information will not be processed by those that receive your data.
        var newActiveData = new CharaCompositeActiveData()
        {
            GlobalAliasData = null!,
            PairAliasData = null!,
            LightStorageData = null!,
            ActiveAlarms = _alarms.ActiveAlarms.Select(x => x.Identifier).ToList(),
            ActiveTriggers = _triggers.Storage.Select(x => x.Identifier).ToList(),
        };

        // Forcibly stop any active patterns.
        if (_patterns.ActivePatternId != Guid.Empty)
        {
            _patterns.DisablePattern(_patterns.ActivePatternId, MainHub.UID, true);
            Logger.LogInformation("[SAFEWORD PROGRESS]: Active pattern stopped due to safeword.");
        }

        // Disable all active toys (WIP)
        _toys.SafewordUsed(); // might need to do something to prevent this from calling a ToysChanged update or whatever.

        // Now we need to disable all global permissions in relation to our safeword that help prevent further abuse by others.
        // We need to make sure that we disable these locally, even if we cannot process them server-side.
        // It is critical we do things this way so we ensure the Safeword effectiveness and prevent achievement miss-matches.
        Logger.LogInformation("[SAFEWORD PROGRESS]: Pushing Client GlobalPerm & HardcoreStatuss for Safeword Changes");
        if (!ClientData.IsNull)
        {
            var prevGlobals = ClientData.Globals!;
            var prevHc = ClientData.Hardcore!;
            // get updated versions.
            var newGlobals = ClientData.GlobalsWithSafewordApplied();
            var newHardcore = new HardcoreStatus();
            // REGARDLESS of the change, we should update things locally!
            _clientData.SetGlobals(newGlobals, newHardcore);
            // Then perform the server call.
            var result = await _hub.UserBulkChangeGlobal(new(MainHub.OwnUserData, newGlobals, newHardcore)).ConfigureAwait(false);
            bool wasSuccessful = result.ErrorCode is GagSpeakApiEc.Success;
            Logger.LogInformation($"[SAFEWORD PROGRESS]: {(wasSuccessful 
                ? "ClientGlobals updated successfully." 
                : $"Failed to push ClientGlobals changes: {result.ErrorCode}.")}");

            // Handle all disabled special globals. ONLY TRIGGER ACHIEVEMENTS IF SERVER CALL WAS SUCCESSFUL.
            _clientDatListener.HandleGlobalPermChanges(MainHub.OwnUserData, prevGlobals, newGlobals);

            // Handle all disabled hardcore states.
            if (prevHc.LockedFollowing.Length > 0)
                _hcHandler.DisableLockedFollow(new(prevHc.LockedFollowing.Split('|')[0]), wasSuccessful);
            if (prevHc.LockedEmoteState.Length > 0)
                _hcHandler.DisableLockedEmote(new(prevHc.LockedEmoteState.Split('|')[0]), wasSuccessful);
            if (prevHc.IndoorConfinement.Length > 0)
                _hcHandler.DisableConfinement(new(prevHc.IndoorConfinement.Split('|')[0]), wasSuccessful);
            if (prevHc.Imprisonment.Length > 0)
                _hcHandler.DisableImprisonment(new(prevHc.Imprisonment.Split('|')[0]), wasSuccessful);
            if (prevHc.ChatBoxesHidden.Length > 0)
                _hcHandler.DisableHiddenChatBoxes(new(prevHc.ChatBoxesHidden.Split('|')[0]), wasSuccessful);
            if (prevHc.ChatInputHidden.Length > 0)
                _hcHandler.RestoreChatInputVisibility(new(prevHc.ChatInputHidden.Split('|')[0]), wasSuccessful);
            if (prevHc.ChatInputBlocked.Length > 0)
                _hcHandler.UnblockChatInput(new(prevHc.ChatInputBlocked.Split('|')[0]), wasSuccessful);
            if (prevHc.HypnoticEffect.Length > 0)
                _hcHandler.RemoveHypnoEffect(new(prevHc.HypnoticEffect.Split('|')[0]), wasSuccessful);

            Logger.LogInformation("[SAFEWORD PROGRESS]: Client GlobalPerms & HardcoreStatus Handlers processed.");
        }

        // IF and only if the uid passed in is not empty, do we restrict the kinksters we revert to only the kinkster with that UID.
        if (!string.IsNullOrEmpty(isolatedUID))
        {
            Logger.LogInformation($"[SAFEWORD PROGRESS]: Safeword Invoked specifically for: {isolatedUID}. Reverting hardcore!");
            if (_kinksters.TryGetKinkster(new(isolatedUID), out var kinkster))
                await _hub.UserChangeOwnPairPerm(new(kinkster.UserData, new KeyValuePair<string, object>(nameof(PairPerms.InHardcore) , false), UpdateDir.Own, MainHub.OwnUserData)).ConfigureAwait(false);
            else
                Logger.LogWarning($"[SAFEWORD PROGRESS]: Kinkster with UID {isolatedUID} not found for safeword revert.");
        }
        else
        {
            // Process it for everyone!
            foreach (var pair in _kinksters.DirectPairs.Where(p => p.OwnPerms.InHardcore))
            {
                Logger.LogInformation($"[SAFEWORD PROGRESS]: Reverting hardcore for Kinkster {pair.UserData.UID}.");
                await _hub.UserChangeOwnPairPerm(new(pair.UserData, new KeyValuePair<string, object>(nameof(PairPerms.InHardcore), false), UpdateDir.Own, MainHub.OwnUserData)).ConfigureAwait(false);
            }
        }

        // now update our composite data for safeword.
        var activeDataUpdate = await _hub.UserPushActiveData(new(_kinksters.GetOnlineUserDatas(), newActiveData, true));
        if (activeDataUpdate.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"[SAFEWORD PROGRESS]: Failed to push safeword active data: {activeDataUpdate.ErrorCode}.");
        else
            Logger.LogInformation("[SAFEWORD PROGRESS]: Safeword active data pushed successfully.");

        // might want to remove all cursed loot and things before syncing up the visual cache manager?
        // TODO::::

        // Now sync up the visuals!
        Logger.LogInformation("[SAFEWORD PROGRESS]: Syncing Visual Cache With Display");
        await _visualCacheManager.ResetCachesDueToSafeword(); // need to make sure we handle achievement updates properly!

        Logger.LogInformation("[SAFEWORD PROGRESS]: ===={ COMPLETED }====");
    }

    public async Task OnHcSafewordUsed(string isolatedUID = "")
    {
        // if we used this while the safeword was on cooldown, return with a warning.
        if (HcSafewordOnCooldown)
        {
            Logger.LogWarning("Hardcore Safeword used too soon after the last use. Must wait 1m between uses!");
            return;
        }

        // update the last time triggered.
        _lastHcSafewordTime = DateTime.Now;
        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Hardcore Safeword used!");

        // stop ALL hardcore tasks being run.
        _achievementHandler.SafewordUsed(isolatedUID);

        // Reset the permissions on all associated Kinksters.
        var kinkstersToReset = (!string.IsNullOrEmpty(isolatedUID) && _kinksters.TryGetKinkster(new(isolatedUID), out var match))
            ? [match] : _kinksters.DirectPairs.Where(p => p.OwnPerms.InHardcore);

        foreach (var kinkster in kinkstersToReset)
        {
            Logger.LogInformation($"[HC-SAFEWORD PROGRESS]: Changing Perms for: {isolatedUID}");
            var newPairPerms = kinkster.OwnPerms.WithSafewordApplied();
            await _hub.UserBulkChangeUnique(new(kinkster.UserData, newPairPerms, kinkster.OwnPermAccess, UpdateDir.Own, MainHub.OwnUserData)).ConfigureAwait(false);
            Logger.LogInformation($"[HC-SAFEWORD PROGRESS]: Hardcore allowances reverted for ({isolatedUID})!");
        }

        if (!ClientData.IsNull)
        {
            Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Syncing HardcoreStatus with player control!");
            var prevHc = ClientData.HardcoreClone()!;
            var newHardcore = new HardcoreStatus();
            // REGARDLESS of the change, we should update things locally!
            _clientData.SetGlobals((GlobalPerms)ClientData.Globals!, newHardcore);
            var result = await _hub.UserBulkChangeGlobal(new(MainHub.OwnUserData, (GlobalPerms)ClientData.Globals!, newHardcore)).ConfigureAwait(false);
            bool success = result.ErrorCode is GagSpeakApiEc.Success;
            
            Logger.LogInformation($"[HC-SAFEWORD PROGRESS]: {(success ? "ClientGlobals updated." : $"Failed to push ClientGlobals changes: {result.ErrorCode}.")}");
            // Handle all disabled hardcore states.
            if (prevHc.LockedFollowing.Length > 0)
                _hcHandler.DisableLockedFollow(new(prevHc.LockedFollowing.Split('|')[0]), success);
            if (prevHc.LockedEmoteState.Length > 0)
                _hcHandler.DisableLockedEmote(new(prevHc.LockedEmoteState.Split('|')[0]), success);
            if (prevHc.IndoorConfinement.Length > 0)
                _hcHandler.DisableConfinement(new(prevHc.IndoorConfinement.Split('|')[0]), success);
            if (prevHc.Imprisonment.Length > 0)
                _hcHandler.DisableImprisonment(new(prevHc.Imprisonment.Split('|')[0]), success);
            if (prevHc.ChatBoxesHidden.Length > 0)
                _hcHandler.DisableHiddenChatBoxes(new(prevHc.ChatBoxesHidden.Split('|')[0]), success);
            if (prevHc.ChatInputHidden.Length > 0)
                _hcHandler.RestoreChatInputVisibility(new(prevHc.ChatInputHidden.Split('|')[0]), success);
            if (prevHc.ChatInputBlocked.Length > 0)
                _hcHandler.UnblockChatInput(new(prevHc.ChatInputBlocked.Split('|')[0]), success);
            if (prevHc.HypnoticEffect.Length > 0)
                _hcHandler.RemoveHypnoEffect(new(prevHc.HypnoticEffect.Split('|')[0]), success);

            Logger.LogInformation("[HC-SAFEWORD PROGRESS]: Client HardcoreStatus Handlers Synced.");
        }

        _hcTasks.AbortTasks();
        Logger.LogInformation("[HC-SAFEWORD PROGRESS]: ===={ COMPLETED }====");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting Safeword Monitor.");
        // Start the safeword monitor when the service starts.
        _emergencySafewordCTS = _emergencySafewordCTS.SafeCancelRecreate();
        _ctrlAltBackspaceSafewordTask = Task.Run(async () =>
        {
            try
            {
                while (!_emergencySafewordCTS.IsCancellationRequested)
                {
                    if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
                        UiService.SetUITask(async () => await OnHcSafewordUsed());

                    // Adjust delay time if there is issues with recognition.
                    await Task.Delay(100, _emergencySafewordCTS.Token);
                }
            }
            catch (TaskCanceledException) { }
            catch (Bagagwa ex)
            {
                Logger.LogError($"Summoned Bagagwa during the Hardcore Safeword Keybind detection loop: {ex}");
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping Safeword Monitor.");
        return Task.CompletedTask;
    }
}
