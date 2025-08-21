using CkCommons;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
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
    }

    public async void ApplyHypnoEffect(UserData enactor, HypnoticEffect effect, TimeSpan length, string? image)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hypnosis!");
        
        await _overlay.SetTimedHypnoEffect(enactor, effect, length, image);
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

        // Update type to legacy controls.
        GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Legacy);

        // Reset the movement tracker and position values.
        _movement.RestartTimeoutTracker();
        
        // Inject the hardcore task operation.
        _hcTasks.EnqueueTask(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject!), new(HcTaskControl.MustFollow | HcTaskControl.BlockAllKeys));
        _hcTasks.EnqueueTask(HcTaskUtils.FollowTarget, new(HcTaskControl.MustFollow | HcTaskControl.BlockAllKeys));

        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Follow, true, enactor, MainHub.UID);
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
    public void DisableLockedFollow(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state.", LoggerType.HardcoreMovement);
        _movement.ResetTimeoutTracker();

        // Restore the movement mode of the player.
        GameConfig.UiControl.Set("MoveMode", (uint)_cachedPlayerMoveMode);
        _cachedPlayerMoveMode = MovementMode.NotSet;
        _logger.LogDebug($"Restored Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Follow, false, enactor, MainHub.UID);
    }

    public void EnableLockedEmote(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Emote!");

        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        
        // Enqueue to the hardcore task manager our emote operation so that we block all movement during it.
        _hcTasks.BeginStack("PrepareEmotePerformance", new(HcTaskControl.FreezePlayer));
        _hcTasks.AddToStack(HcCommonTaskFuncs.WaitForPlayerLoading);
        
        // OPTIONAL STEP: If the kinkster is present, attempt to target them.
        if (kinkster.VisiblePairGameObject is not null && kinkster.VisiblePairGameObject.IsTargetable)
            _hcTasks.AddToStack(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject));
        
        // perform the emote operation.
        _hcTasks.AddToStack(() => HcCommonTaskFuncs.PerformExpectedEmote(ClientData.Hardcore!.EmoteId, ClientData.Hardcore.EmoteCyclePose));
        _hcTasks.InsertStack();

        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, true, enactor, MainHub.UID);
    }

    public void UpdateLockedEmote(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Emote Update!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Updated your LockedFollowing state!", LoggerType.HardcoreMovement);
        
        // Enqueue to the hardcore task manager our emote operation so that we block all movement during it.
        _hcTasks.BeginStack("PrepareEmotePerformance", new(HcTaskControl.FreezePlayer));
        _hcTasks.AddToStack(HcCommonTaskFuncs.WaitForPlayerLoading);
        
        // OPTIONAL STEP: If the kinkster is present, attempt to target them.
        if (kinkster.VisiblePairGameObject is not null && kinkster.VisiblePairGameObject.IsTargetable)
            _hcTasks.AddToStack(() => HcCommonTaskFuncs.TargetNode(() => kinkster.VisiblePairGameObject));
        
        // perform the emote operation.
        _hcTasks.AddToStack(() => HcCommonTaskFuncs.PerformExpectedEmote(ClientData.Hardcore!.EmoteId, ClientData.Hardcore.EmoteCyclePose));
        _hcTasks.InsertStack();

        _mediator.Publish(new HcStateCacheChanged());
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
    public void DisableLockedEmote(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state!", LoggerType.HardcoreMovement);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, false, enactor, MainHub.UID);
    }

    public void EnableConfinement(UserData enactor, AddressBookEntry? address = null)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your IndoorConfinement!", LoggerType.HardcoreMovement);
        // Standard await for player to load.
        _hcTasks.EnqueueTask(HcCommonTaskFuncs.WaitForPlayerLoading, HcTaskConfiguration.Default);

        // we should PROBABLY run a check to see if the player is close enough to the address that we can run manual override,
        // as lifeStream will tend to re-teleport you to your ward's dock even if you are right next to the plot.

        // perform lifeStream operation with a custom timeout wait.
        if (address is not null && IpcCallerLifestream.APIAvailable)
        {
            _logger.LogInformation($"LifeStream IPC was valid and forced confinement had an address!", LoggerType.HardcoreMovement);
            _hcTasks.BeginStack("LifeStream Confinement Execution", new(HcTaskControl.InLifestreamTask, 60000));
            _hcTasks.AddToStack(() => _ipc.GoToAddress(address.AsTuple()));
            // entrust a task to wait for this operation to complete.
            _hcTasks.AddToStack(() => !_ipc.IsCurrentlyBusy());
        }
        // Afterwards, if we are outside, then we need to target the nearest housing node and enter it.
        if (HcTaskUtils.IsOutside())
            HcApproachNearestHousing.AddTaskSequenceToStack(_hcTasks);
        // Insert this all as a stack.
        _hcTasks.InsertStack();

        _logger.LogDebug($"Enqueued Hardcore Task Stack for Indoor Confinement!", LoggerType.HardcoreMovement);
        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Confinement, true, enactor, MainHub.UID);
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
    public void DisableConfinement(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Indoor Confinement state!", LoggerType.HardcoreMovement);

        _mediator.Publish(new HcStateCacheChanged());
        if (giveAchievements)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Confinement, false, enactor, MainHub.UID);
    }

    public void EnableImprisonment(UserData enactor, Vector3 position, float freedomRadius)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your Imprisonment!", LoggerType.HardcoreMovement);
        var anchoredPos = (position == Vector3.Zero) ? PlayerData.Object.Position : position;
        // set up some task for auto moving here.
        // will need to override player movement for this to work.
        // Can handle this later!

        _logger.LogDebug($"Enqueued Hardcore Task Stack for Imprisonment!", LoggerType.HardcoreMovement);
        _mediator.Publish(new HcStateCacheChanged());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Imprisonment, true, enactor, MainHub.UID);
    }

    public void UpdateImprisonment(UserData enactor, Vector3 position, float freedomRadius)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Updated your Imprisonment!", LoggerType.HardcoreMovement);
        var anchoredPos = (position == Vector3.Zero) ? PlayerData.Object.Position : position;
        // set up some task for auto moving here.
        // will need to override player movement for this to work.
        // Can handle this later!
        _logger.LogDebug($"Enqueued Hardcore Task Stack for Imprisonment Update!", LoggerType.HardcoreMovement);

        // No need to trigger achievement, as we only updated the imprisonment position and radius.
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
    public void DisableImprisonment(UserData enactor, bool giveAchievements)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Imprisonment state!", LoggerType.HardcoreMovement);

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
