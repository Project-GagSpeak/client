using CkCommons;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using TerraFX.Interop.Windows;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles the enabling and disabling of various hardcore changes.
/// </summary>
public class PlayerCtrlHandler
{
    private readonly ILogger<PlayerCtrlHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly IpcCallerLifestream _ipc;
    private readonly MovementController _movement;
    private readonly OverlayHandler _overlay;
    private readonly HcTaskManager _hcTasks;
    private readonly KinksterManager _kinksters;

    // Stores the players's movement mode, useful for when we change it.
    private MovementMode _cachedPlayerMoveMode = MovementMode.NotSet;
    public PlayerCtrlHandler(ILogger<PlayerCtrlHandler> logger, GagspeakMediator mediator,
        MainConfig config, IpcCallerLifestream ipc, MovementController movement, 
        OverlayHandler overlay, HcTaskManager hcTasks, KinksterManager kinksters)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
        _ipc = ipc;
        _movement = movement;
        _overlay = overlay;
        _hcTasks = hcTasks;
        _kinksters = kinksters;
        _cachedPlayerMoveMode = Svc.GameConfig.UiControl.TryGetUInt("MoveMode", out var mode) && mode == 1 ? MovementMode.Legacy : MovementMode.Standard;
    }

    public async void ApplyHypnoEffect(UserData enactor, HypnoticEffect effect, DateTimeOffset expireTimeUTC, string? image)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hypnosis!");
        
        try
        {
            await _overlay.ApplyKinkstersHypnoEffect(enactor, effect, expireTimeUTC, image);
        }
        catch (Bagagwa)
        {
            _logger.LogWarning($"Error while attempting to apply hypnotic effect! Flagging ValidEffect was flagged as false.");
        }
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your Hypnotic Effect!");
    }

    /// <summary>
    ///     It is possible that the removal effect is triggered on a safeword or natural timer falloff. <para />
    /// 
    ///     If the server it down or the change cannot be processed, we want to still remove the player controls 
    ///     client-side, but not invoke the achievements for the change. <para />
    ///     
    ///     This way, on reconnection, the hardcore state will be reapplied, timer will immediately expire, and
    ///     they will get the achievement then. It also ensures they are not 'stuck' in restricted controls, so
    ///     a safeword is still effective.
    /// </summary>
    public void RemoveHypnoEffect(UserData enactor, bool giveAchievements, bool fromPluginDisposal = false)
    {
        _overlay.RemoveHypnoEffect(enactor.UID, giveAchievements, fromPluginDisposal);
        _mediator.Publish(new HcStateCacheChanged());
        _logger.LogInformation($"[{enactor.AliasOrUID}] Removed your Hypnotic Effect!");
    }

    public void EnableLockedFollow(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Follow!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        // Cache the movement mode.
        _cachedPlayerMoveMode = GameConfig.UiControl.GetBool("MoveMode") ? MovementMode.Legacy : MovementMode.Standard;
        _logger.LogDebug($"Cached Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);
        // perform the task collection for initialization.
        _hcTasks.CreateCollection("Locked Follow Startup", new(HcTaskControl.MustFollow | HcTaskControl.BlockAllKeys))
            .Add(new HardcoreTask(() => GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Legacy)))
            .Add(new HardcoreTask(_movement.RestartTimeoutTracker))
            .Add(new HardcoreTask(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject!)))
            .Add(new HardcoreTask(HcTaskUtils.FollowTarget))
            .Add(new HardcoreTask(() => _mediator.Publish(new HcStateCacheChanged())))
            .Enqueue();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Follow, true, enactor, MainHub.UID);
    }

    public void DisableLockedFollow(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state.", LoggerType.HardcoreMovement);

        // Reset movement mode and timeout trackers, and update the cache.
        _hcTasks.RemoveIfPresent("Locked Follow Startup");
        _movement.ResetTimeoutTracker();
        GameConfig.UiControl.Set("MoveMode", (uint)_cachedPlayerMoveMode);
        _cachedPlayerMoveMode = MovementMode.NotSet;
        _mediator.Publish(new HcStateCacheChanged());
        _logger.LogDebug($"Restored Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);

        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Follow, false, enactor, MainHub.UID);
    }

    public void EnableLockedEmote(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Emote!");

        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        _hcTasks.CreateCollection("Perform LockedEmote", new(HcTaskControl.BlockAllKeys | HcTaskControl.InRequiredTurnTask))
            .Add(new HardcoreTask(HcCommonTaskFuncs.WaitForPlayerLoading))
            .Add(_hcTasks.CreateBranch(() => (kinkster.VisiblePairGameObject != null && kinkster.VisiblePairGameObject.IsTargetable), "TargetIfVisible")
                .SetTrueTask(new HardcoreTask(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject!)))
                .AsBranch())
            .Add(new HardcoreTask(() => HcCommonTaskFuncs.PerformExpectedEmote(ClientData.Hardcore!.EmoteId, ClientData.Hardcore.EmoteCyclePose)))
            .Add(new HardcoreTask(() => _mediator.Publish(new HcStateCacheChanged())))
            .Enqueue();

        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, true, enactor, MainHub.UID);
    }

    public void UpdateLockedEmote(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Emote Update!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Updated your LockedFollowing state!", LoggerType.HardcoreMovement);
        _hcTasks.CreateCollection("ForcePerformInitialEmote", new(HcTaskControl.BlockAllKeys | HcTaskControl.InRequiredTurnTask))
            .Add(new HardcoreTask(HcCommonTaskFuncs.WaitForPlayerLoading))
            .Add(_hcTasks.CreateBranch(() => (kinkster.VisiblePairGameObject != null && kinkster.VisiblePairGameObject.IsTargetable), "TargetIfVisible")
                .SetTrueTask(new HardcoreTask(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject!)))
                .AsBranch())
            .Add(new HardcoreTask(() => HcCommonTaskFuncs.PerformExpectedEmote(ClientData.Hardcore!.EmoteId, ClientData.Hardcore.EmoteCyclePose)))
            .Add(new HardcoreTask(() => _mediator.Publish(new HcStateCacheChanged())))
            .Enqueue();
    }

    public void DisableLockedEmote(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedEmote state!", LoggerType.HardcoreMovement);
        // abort the task if running still, or remove it from the queue.
        _hcTasks.RemoveIfPresent("Perform LockedEmote");
        _mediator.Publish(new HcStateCacheChanged());

        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, false, enactor, MainHub.UID);
    }

    public void EnableConfinement(UserData enactor, AddressBookEntry? address = null)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your IndoorConfinement!", LoggerType.HardcoreMovement);
        // Standard await for player to load.
        var doLifestreamMethod = address is not null && IpcCallerLifestream.APIAvailable;
        var taskCtrlFlags = HcTaskControl.LockThirdPerson | HcTaskControl.BlockAllKeys | HcTaskControl.DoConfinementPrompts;
        if (doLifestreamMethod) taskCtrlFlags |= HcTaskControl.InLifestreamTask;

        // enqueue the task collection based on if we are doing lifestream of not.
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            _hcTasks.CreateCollection("Travel To Location", HcTaskConfiguration.Branch with { Flags = taskCtrlFlags })
                .Add(_hcTasks.CreateBranch(() => doLifestreamMethod, "LifestreamTravelTask", HcTaskConfiguration.Branch)
                    .SetTrueTask(_hcTasks.CreateGroup("TravelTaskGroup", HcTaskConfiguration.Default with { TimeoutAt = 120000 })
                        .Add(HcCommonTaskFuncs.WaitForPlayerLoading)
                        .Add(() => _ipc.GoToAddress(address!.AsTuple()))
                        .Add(() => !_ipc.IsCurrentlyBusy())
                        .AsGroup())
                    .AsBranch())
                .Add(_hcTasks.CreateBranch(() => doLifestreamMethod && HcApproachNearestHousing.AtHouseButMustBeCloser(), "Close Gap For Arrival")
                    .SetTrueTask(new HardcoreTask(HcApproachNearestHousing.MoveToAcceptableRange, HcTaskConfiguration.Rapid with { OnEnd = () => StaticDetours.MoveOverrides.Disable() }))
                    .AsBranch())
                .Add(_hcTasks.CreateBranch(HcTaskUtils.IsOutside, "AppraochNearestNode", HcTaskConfiguration.Short)
                    .SetTrueTask(HcApproachNearestHousing.GetTaskCollection(_hcTasks))
                    .AsBranch())
                .Add(new HardcoreTask(() => _mediator.Publish(new HcStateCacheChanged()), HcTaskConfiguration.Quick))
                .Enqueue();
        });
        _logger.LogDebug($"Enqueued Hardcore Task Stack for Indoor Confinement!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Confinement, true, enactor, MainHub.UID);
    }

    public void DisableConfinement(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Indoor Confinement state!", LoggerType.HardcoreMovement);

        _hcTasks.RemoveIfPresent("Travel To Confinement");
        _mediator.Publish(new HcStateCacheChanged());

        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Confinement, false, enactor, MainHub.UID);
    }

    public void EnableImprisonment(UserData enactor)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your Imprisonment!", LoggerType.HardcoreMovement);
        // Calling this will begin the imprisonment process.
        _mediator.Publish(new HcStateCacheChanged());

        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Imprisonment, true, enactor, MainHub.UID);
    }

    public void UpdateImprisonment(UserData enactor)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Updated your Imprisonment!", LoggerType.HardcoreMovement);
        // Calling this will begin the imprisonment process.
        _mediator.Publish(new HcStateCacheChanged());
    }

    public void DisableImprisonment(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Imprisonment state!", LoggerType.HardcoreMovement);
        // nothing was really pushed out to the hardcore task manager, so nothing to disable.
        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Imprisonment, false, enactor, MainHub.UID);
    }

    public void EnableHiddenChatBoxes(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Boxes!");

        AddonChatLog.SetChatPanelVisibility(false);
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        
        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatBox, true, enactor, MainHub.UID);
    }

    /// <summary>
    ///     It is possible that the removal effect is triggered on a safeword or natural timer falloff. <para />
    /// 
    ///     If the server it down or the change cannot be processed, we want to still remove the player controls 
    ///     client-side, but not invoke the achievements for the change. <para />
    ///     
    ///     This way, on reconnection, the hardcore state will be reapplied, timer will immediately expire, and
    ///     they will get the achievement then. It also ensures they are not 'stuck' in restricted controls, so
    ///     a safeword is still effective.
    /// </summary>
    public void DisableHiddenChatBoxes(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(true);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatBox, false, enactor, MainHub.UID);
    }

    public void HideChatInputVisibility(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Input!");

        AddonChatLog.SetChatInputVisibility(false);
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] concealed your ChatInput visibility!", LoggerType.HardcoreActions);

        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatInput, true, enactor, MainHub.UID);
    }

    /// <summary>
    ///     It is possible that the removal effect is triggered on a safeword or natural timer falloff. <para />
    /// 
    ///     If the server it down or the change cannot be processed, we want to still remove the player controls 
    ///     client-side, but not invoke the achievements for the change. <para />
    ///     
    ///     This way, on reconnection, the hardcore state will be reapplied, timer will immediately expire, and
    ///     they will get the achievement then. It also ensures they are not 'stuck' in restricted controls, so
    ///     a safeword is still effective.
    /// </summary>
    public void RestoreChatInputVisibility(UserData enactor, bool giveAchievements)
    {
        AddonChatLog.SetChatInputVisibility(true);
        _logger.LogInformation($"[{enactor.AliasOrUID}] restored your ChatInput Visibility!", LoggerType.HardcoreActions);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatInput, false, enactor, MainHub.UID);
    }

    public void BlockChatInput(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Blocked Chat Input!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        
        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.BlockedChatInput, true, enactor, MainHub.UID);
    }

    /// <summary>
    ///     It is possible that the removal effect is triggered on a safeword or natural timer falloff. <para />
    /// 
    ///     If the server it down or the change cannot be processed, we want to still remove the player controls 
    ///     client-side, but not invoke the achievements for the change. <para />
    ///     
    ///     This way, on reconnection, the hardcore state will be reapplied, timer will immediately expire, and
    ///     they will get the achievement then. It also ensures they are not 'stuck' in restricted controls, so
    ///     a safeword is still effective.
    /// </summary>
    public void UnblockChatInput(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your BlockedChatInput state!", LoggerType.HardcoreActions);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.BlockedChatInput, false, enactor, MainHub.UID);
    }
}
