using CkCommons;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Controller;
using GagSpeak.State.Caches;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles the enabling and disabling of various hardcore changes.
/// </summary>
public class PlayerControlHandler
{
    private readonly ILogger<PlayerControlHandler> _logger;
    private readonly MainConfig _config;
    private readonly IpcCallerLifestream _ipc;
    private readonly MovementController _movement;
    private readonly OverlayHandler _overlay;
    private readonly HcTaskManager _hcTasks;
    private readonly KinksterManager _kinksters;

    // Stores the players's movement mode, useful for when we change it.
    private MovementMode _cachedPlayerMoveMode = MovementMode.NotSet;
    public PlayerControlHandler(ILogger<PlayerControlHandler> logger, MainConfig config,
        IpcCallerLifestream ipc, MovementController movement, OverlayHandler overlay, 
        HcTaskManager hcTasks, KinksterManager kinksters)
    {
        _logger = logger;
        _config = config;
        _ipc = ipc;
        _movement = movement;
        _overlay = overlay;
        _hcTasks = hcTasks;
        _kinksters = kinksters;
    }

    public void ApplyHypnoEffect(UserData enactor, HypnoticEffect effect, TimeSpan length, string? customImage)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hypnosis!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your Hypnotic Effect!");
        _overlay.SetTimedHypnoEffectUnsafe(enactor, effect, length, customImage);
    }

    public void RemoveHypnoEffect(UserData enactor)
    {
        _overlay.RemoveHypnoEffect(enactor.UID);
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
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Follow, true, enactor, MainHub.UID);
    }

    public void DisableLockedFollow(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state.", LoggerType.HardcoreMovement);
        _movement.ResetTimeoutTracker();

        // Restore the movement mode of the player.
        GameConfig.UiControl.Set("MoveMode", (uint)_cachedPlayerMoveMode);
        _cachedPlayerMoveMode = MovementMode.NotSet;

        _logger.LogDebug($"Restored Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);
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
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, true, enactor, MainHub.UID);
    }

    public void DisableLockedEmote(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.EmoteState, false, enactor, MainHub.UID);
    }

    public void EnableConfinement(UserData enactor, AddressBookEntry? spesificAddress = null)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your IndoorConfinement!", LoggerType.HardcoreMovement);
        // Standard await for player to load.
        _hcTasks.EnqueueTask(HcCommonTaskFuncs.WaitForPlayerLoading, HcTaskConfiguration.Default);

        // we should PROBABLY run a check to see if the player is close enough to the address that we can run manual override,
        // as lifestream will tend to re-teleport you to your ward's dock even if you are right next to the plot.

        // perform lifestream operation with a custom timeout wait.
        if (spesificAddress is not null && IpcCallerLifestream.APIAvailable)
        {
            _logger.LogInformation($"Lifestream IPC was valid and forced confinement had an address!", LoggerType.HardcoreMovement);
            _hcTasks.BeginStack("LifestreamConfinementExecution", new(HcTaskControl.InLifestreamTask, 60000));
            _hcTasks.AddToStack(() => _ipc.GoToAddress(spesificAddress.AsTuple()));
            // entrust a task to wait for this operation to complete.
            _hcTasks.AddToStack(() => !_ipc.IsCurrentlyBusy());
        }
        // Afterwards, if we are outside, then we need to target the nearest housing node and enter it.
        if (HcTaskUtils.IsOutside())
            HcApproachNearestHousing.AddTaskSequenceToStack(_hcTasks);
        // Insert this all as a stack.
        _hcTasks.InsertStack();

        _logger.LogDebug($"Enqueued Hardcore Task Stack for Indoor Confinement!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Confinement, true, enactor, MainHub.UID);
    }

    public void DisableConfinement(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Indoor Confinement state!", LoggerType.HardcoreMovement);
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
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Imprisonment, true, enactor, MainHub.UID);
    }

    public void DisableImprisonment(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Imprisonment state!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.Imprisonment, false, enactor, MainHub.UID);
    }

    public void EnableHiddenChatBoxes(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Boxes!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(false);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatBox, true, enactor, MainHub.UID);
    }

    public void DisableHiddenChatBoxes(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(true);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatBox, false, enactor, MainHub.UID);
    }

    public void HideChatInputVisibility(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Input!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] concealed your ChatInput visibility!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(false);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatInput, true, enactor, MainHub.UID);
    }

    public void RestoreChatInputVisibility(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] restored your ChatInput Visibility!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(true);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.HiddenChatInput, false, enactor, MainHub.UID);
    }

    public void BlockChatInput(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Blocked Chat Input!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.BlockedChatInput, true, enactor, MainHub.UID);

    }

    public void UnblockChatInputUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HcAttribute.BlockedChatInput, false, enactor, MainHub.UID);
    }
}
