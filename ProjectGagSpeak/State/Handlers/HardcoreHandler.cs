using CkCommons;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Controller;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles the enabling and disabling of various hardcore changes.
/// </summary>
public class HardcoreHandler
{
    private readonly ILogger<HardcoreHandler> _logger;
    private readonly IpcCallerLifestream _lifestream;
    private readonly PlayerMetaData _metadata;
    private readonly AutoPromptController _prompts;
    private readonly KeystateController _keyStates;
    private readonly MovementController _movement;
    private readonly ChatboxController _chatbox;
    private readonly OverlayHandler _overlay;
    private readonly KinksterManager _kinksters;
    private readonly HcTaskManager _hcTaskManager;

    // Stores the players's movement mode, useful for when we change it.
    private MovementMode _cachedPlayerMoveMode = MovementMode.NotSet;
    public HardcoreHandler(ILogger<HardcoreHandler> logger, IpcCallerLifestream lifestream,
        PlayerMetaData metadata, AutoPromptController prompts, KeystateController keys, 
        MovementController movement, ChatboxController chatbox, OverlayHandler overlay, 
        KinksterManager kinksters, HcTaskManager hcTaskManager)
    {
        _logger = logger;
        _lifestream = lifestream;
        _metadata = metadata;
        _prompts = prompts;
        _keyStates = keys;
        _movement = movement;
        _chatbox = chatbox;
        _overlay = overlay;
        _kinksters = kinksters;
        _hcTaskManager = hcTaskManager;
    }

    public void ProcessBulkGlobalUpdate(IReadOnlyGlobalPerms? previous, IReadOnlyGlobalPerms current)
    {
        // Will need to iterate through each special operation and check the validity of the values,
        // syncronizing their achievements and locks in the process.

        // TODO;;;
    }

    #region Hypnosis
    public bool CanApplyTimedHypnoEffect(UserData enactor, IReadOnlyGlobalPerms perms, HypnoticEffect effect, TimeSpan length, string? customImage)
        => !perms.HypnoState() && _overlay.CanApplyTimedEffect(effect, customImage);

    public bool CanRemoveHypnosis(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!_overlay.IsHypnotized)
            return false;
        // if pairlocked, reject if not assigner. (allow Client for safewords)
        if (perms.HypnoIsDevotional() && (enactor.UID != MainHub.UID && enactor.UID != perms.HypnoEnactor()))
        {
            _logger.LogWarning($"[{enactor.UID}] Failed removing Deviotionally locked Hypnosis, they were not the assigner!");
            return false;
        }
        // Valid otherwise.
        return true;
    }

    public bool ApplyTimedHypnoEffect(UserData enactor, IReadOnlyGlobalPerms perms, HypnoticEffect effect, TimeSpan length, string? customImage)
    {
        if (!CanApplyTimedHypnoEffect(enactor, perms, effect, length, customImage))
            return false;
        ApplyTimedHypnoEffectUnsafe(enactor, effect, length, customImage);
        return true;
    }

    // Applies the effect regardless of the conditions, and updates metadata.
    public void ApplyTimedHypnoEffectUnsafe(UserData enactor, HypnoticEffect effect, TimeSpan length, string? customImage)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hypnosis!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your Hypnotic Effect!", LoggerType.HardcoreMovement);
        _overlay.SetTimedHypnoEffectUnsafe(enactor, effect, length, customImage);
    }

    public void RemoveHypnoEffect(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanRemoveHypnosis(enactor, perms))
            return;
        RemoveHypnoEffectUnsafe(enactor);
    }

    public void RemoveHypnoEffectUnsafe(UserData enactor)
        => _overlay.RemoveHypnoEffect(enactor.UID);
    #endregion Hypnosis

    #region Locked Follow
    public bool CanEnableLockedFollow(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        // if not a kinkster, or already following, return false.
        if (perms.HcFollowState() || !_kinksters.TryGetKinkster(enactor, out var k))
            return false;

        // Ensure they are visible, present, and targetable.
        return k.IsVisible && k.VisiblePairGameObject is not null && k.VisiblePairGameObject.IsTargetable;
    }

    public bool CanDisableLockedFollow(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        // if not a kinkster, or not following, return false.
        if (!perms.HcFollowState() || !_kinksters.TryGetKinkster(enactor, out var k))
            return false;

        // if the movement sources dont have any source for lockedFollowing, then return false.
        if (!_movement.Sources.HasAny(PlayerControlSource.LockedFollowing))
        {
            _logger.LogWarning($"Error while validating disable: [LockedFollow] not present in movement control!");
            return false;
        }

        // Ensure they are visible at least, dont need to be targetable.
        return k.IsVisible;
    }

    public bool EnableFollow(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanEnableLockedFollow(enactor, perms))
            return false;
        EnableFollowUnsafe(enactor);
        return true;
    }

    public void EnableFollowUnsafe(UserData enactor)
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
        _movement.AddControlSources(PlayerControlSource.LockedFollowing);

        // Inject the hardcore task operation.
        _hcTaskManager.BeginStack();
        _hcTaskManager.EnqueueTask(HcCommonTasks.TargetNode(() => kinkster.VisiblePairGameObject!));
        _hcTaskManager.EnqueueTask(HcTaskUtils.FollowTarget);
        _hcTaskManager.InsertStack();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedFollowing, true, enactor, MainHub.UID);
    }

    // Will not perform the action if in invalid state.
    public bool DisableFollow(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanDisableLockedFollow(enactor, perms))
            return false;
        DisableFollowUnsafe(enactor);
        return true;
    }

    public void DisableFollowUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state.", LoggerType.HardcoreMovement);
        // revert the timeout tracker.
        _movement.ResetTimeoutTracker();
        _movement.RemoveControlSources(PlayerControlSource.LockedFollowing);

        // Restore the movement mode of the player.
        GameConfig.UiControl.Set("MoveMode", (uint)_cachedPlayerMoveMode);
        _cachedPlayerMoveMode = MovementMode.NotSet;

        _logger.LogDebug($"Restored Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedFollowing, false, enactor, MainHub.UID);
    }
    #endregion Locked Follow

    #region Locked EmoteState
    // Only need to ensure kinkster exists. Everything else was handled prior.
    public bool CanEnableLockedEmote(UserData enactor, IReadOnlyGlobalPerms perms)
        => _kinksters.TryGetKinkster(enactor, out var k);

    // false if not a kinkster, or if not in locked emote state.
    public bool CanDisableLockedEmote(UserData enactor, IReadOnlyGlobalPerms perms)
        => _kinksters.TryGetKinkster(enactor, out var k) && perms.HcEmoteState();

    public bool EnableLockedEmote(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanEnableLockedEmote(enactor, perms))
            return false;
        EnableLockedEmoteUnsafe(enactor);
        return true;
    }

    public void EnableLockedEmoteUnsafe(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Exception($"Failed to get Kinkster for UID: {enactor.UID} for Locked Emote!");

        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        // block out movement and keystates here.
        _movement.AddControlSources(PlayerControlSource.LockedEmote);
        _keyStates.AddControlSources(PlayerControlSource.LockedEmote);

        // Enqueue to the hardcore task manager our emote operation so that we block all movement during it.
        _hcTaskManager.BeginStack();
        _hcTaskManager.EnqueueTask(HcCommonTasks.WaitForPlayerLoading());

        // OPTIONAL STEP: If the kinkster is present, attempt to target them.
        if (kinkster.VisiblePairGameObject is not null && kinkster.VisiblePairGameObject.IsTargetable)
            _hcTaskManager.EnqueueTask(HcCommonTasks.TargetNode(() => kinkster.VisiblePairGameObject));

        _hcTaskManager.EnqueueTask(HcCommonTasks.PerformExpectedEmote(OwnGlobals.LockedEmoteState));
        _hcTaskManager.InsertStack();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedEmote, true, enactor, MainHub.UID);
    }

    public void DisableLockedEmote(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanDisableLockedEmote(enactor, perms))
            return;
        DisableLockedEmoteUnsafe(enactor);
    }

    public void DisableLockedEmoteUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        _movement.RemoveControlSources(PlayerControlSource.LockedEmote);
        _keyStates.RemoveControlSources(PlayerControlSource.LockedEmote);
        // nothing else to do here, the task manager will handle the rest.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.LockedEmote, false, enactor, MainHub.UID);
    }
    #endregion Locked EmoteState

    #region Indoor Confinement
    public bool CanEnableIndoorConfinement(UserData enactor, IReadOnlyGlobalPerms perms)
        => !perms.HcConfinedState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool CanDisableIndoorConfinement(UserData enactor, IReadOnlyGlobalPerms perms)
        => perms.HcConfinedState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool EnableConfinement(UserData enactor, IReadOnlyGlobalPerms perms, AddressBookEntry? spesificAddress = null)
    {
        if (!CanEnableIndoorConfinement(enactor, perms))
            return false;
        EnableConfinementUnsafe(enactor, spesificAddress);
        return true;
    }

    public void EnableConfinementUnsafe(UserData enactor, AddressBookEntry? spesificAddress = null)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your IndoorConfinement!", LoggerType.HardcoreMovement);
        _prompts.AddControlSources(PlayerControlSource.IndoorConfinement);
        // update the metadata to reflect the confinement state. (if for an address).
        if (spesificAddress is not null)
            _metadata.SetConfinementAddress(spesificAddress);

        // Handle the hardcore task execution operation here!
        _hcTaskManager.BeginStack();
        _hcTaskManager.EnqueueTask(HcCommonTasks.WaitForPlayerLoading());

        // if Lifestream is enabled and the device is non-null, go to it first.
        if (spesificAddress is not null && IpcCallerLifestream.APIAvailable)
        {
            _logger.LogInformation($"Lifestream IPC was valid and forced confinement had an address!", LoggerType.HardcoreMovement);
            // Entrust Lifestream with the task to go to the address you set.
            _lifestream.GoToAddress(spesificAddress.AsTuple());
            // entrust a task to wait for this operation to complete.
            _hcTaskManager.EnqueueTask(() =>
            {
                // return false if still busy.
                if (_lifestream.IsCurrentlyBusy())
                    return false;

                // if not busy, we are at the address.
                return true;
            });
        }
        // Afterwards, if we are outside, then we need to target the nearest housing node and enter it.
        if (HcTaskUtils.IsOutside())
            HcStayMain.EnqueueAsTask(_hcTaskManager);
        // Insert this all as a stack.
        _hcTaskManager.InsertStack();

        _logger.LogDebug($"Enqueued Hardcore Task Stack for Indoor Confinement!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.IndoorConfinement, true, enactor, MainHub.UID);
    }

    public bool DisableConfinement(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanDisableIndoorConfinement(enactor, perms))
            return false;
        DisableConfinementUnsafe(enactor);
        return true;
    }

    public void DisableConfinementUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Indoor Confinement state!", LoggerType.HardcoreMovement);
        _prompts.RemoveControlSources(PlayerControlSource.IndoorConfinement);
        // clear the confinement address metadata.
        _metadata.ClearConfinementAddress();
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.IndoorConfinement, false, enactor, MainHub.UID);
    }
    #endregion Indoor Confinement

    #region Imprisonment
    public bool CanEnableImprisonment(UserData enactor, IReadOnlyGlobalPerms perms, Vector3 position)
    {
        // if they are in a cage state or not a kinkster, return false.
        if (perms.HcCageState() || !_kinksters.TryGetKinkster(enactor, out var kinkster))
            return false;

        // if the position is Vector3.Zero, it means cage client at this current position.
        // If it isnt, it defines a spesific position.
        return (position != Vector3.Zero)
            ? kinkster.IsVisible && Vector3.Distance(kinkster.VisiblePairGameObject!.Position, PlayerData.Position) < 20
            : true;
    }

    public bool CanDisableImprisonment(UserData enactor, IReadOnlyGlobalPerms perms)
        => perms.HcCageState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool EnableImprisonment(UserData enactor, IReadOnlyGlobalPerms perms, Vector3 position, float freedomRadius)
    {
        if (!CanEnableImprisonment(enactor, perms, position))
            return false;
        EnableImprisonmentUnsafe(enactor, position, freedomRadius);
        return true;
    }

    public void EnableImprisonmentUnsafe(UserData enactor, Vector3 position, float freedomRadius)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"[{enactor.AliasOrUID}] Enabled your Imprisonment!", LoggerType.HardcoreMovement);
        _prompts.AddControlSources(PlayerControlSource.Imprisonment);

        // default the anchors position to your current position if the pos is Vector3.Zero.
        var anchoredPos = (position == Vector3.Zero) ? PlayerData.Object.Position : position;
        // update the metadata to reflect the imprisonment state.
        _metadata.AnchorToPosition(anchoredPos, freedomRadius);

        // set up some task for auto moving here.
        // will need to override player movement for this to work.
        // Can handle this later!

        _logger.LogDebug($"Enqueued Hardcore Task Stack for Imprisonment!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.Imprisoned, true, enactor, MainHub.UID);
    }

    public bool DisableImprisonment(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanDisableImprisonment(enactor, perms))
            return false;
        DisableImprisonmentUnsafe(enactor);
        return true;
    }

    public void DisableImprisonmentUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your Imprisonment state!", LoggerType.HardcoreMovement);
        _prompts.RemoveControlSources(PlayerControlSource.Imprisonment);
        // clear the imprisonment metadata.
        _metadata.ClearCageAnchor();

        _logger.LogDebug($"Removed Imprisonment state for {enactor.AliasOrUID}!", LoggerType.HardcoreMovement);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.Imprisoned, false, enactor, MainHub.UID);
    }
    #endregion Imprisonment

    #region Hidden Chat Boxes
    public bool CanEnableHiddenChatBoxes(UserData enactor, IReadOnlyGlobalPerms perms)
        => !perms.HcChatVisState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool CanDisableHiddenChatBoxes(UserData enactor, IReadOnlyGlobalPerms perms)
        => perms.HcChatVisState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool EnableHiddenChatBoxes(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanEnableHiddenChatBoxes(enactor, perms))
            return false;
        EnableHiddenChatBoxesUnsafe(enactor);
        return true;
    }

    public void EnableHiddenChatBoxesUnsafe(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Boxes!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(false);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatBoxesHidden, true, enactor, MainHub.UID);
    }

    public bool DisableHiddenChatBoxes(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanDisableHiddenChatBoxes(enactor, perms))
            return false;
        DisableHiddenChatBoxesUnsafe(enactor);
        return true;
    }

    public void DisableHiddenChatBoxesUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(true);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatBoxesHidden, false, enactor, MainHub.UID);
    }
    #endregion Hidden Chat Boxes

    #region Hidden Chat Input
    public bool CanHideChatInputVis(UserData enactor, IReadOnlyGlobalPerms perms)
        => !perms.HcChatInputVisState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool CanRestoreChatInputVis(UserData enactor, IReadOnlyGlobalPerms perms)
        => perms.HcChatInputVisState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool HideChatInputVis(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanHideChatInputVis(enactor, perms))
            return false;
        HideChatInputVisUnsafe(enactor);
        return true;
    }

    public void HideChatInputVisUnsafe(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Hidden Chat Input!");

        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] concealed your ChatInput visibility!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(false);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputHidden, true, enactor, MainHub.UID);
    }

    public bool RestoreChatInputVis(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanRestoreChatInputVis(enactor, perms))
            return false;
        RestoreChatInputVisUnsafe(enactor);
        return true;
    }

    public void RestoreChatInputVisUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] restored your ChatInput Visibility!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(true);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputHidden, false, enactor, MainHub.UID);
    }
    #endregion Hidden Chat Input

    #region Blocked Chat Input
    public bool CanBlockChatInput(UserData enactor, IReadOnlyGlobalPerms perms)
        => !perms.HcBlockChatInputState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool CanUnblockChatInput(UserData enactor, IReadOnlyGlobalPerms perms)
        => perms.HcBlockChatInputState() && _kinksters.TryGetKinkster(enactor, out var _);

    public bool BlockChatInput(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanBlockChatInput(enactor, perms))
            return false;
        BlockChatInputUnsafe(enactor);
        return true;
    }

    public void BlockChatInputUnsafe(UserData enactor)
    {
        if (!_kinksters.TryGetKinkster(enactor, out var kinkster))
            throw new Bagagwa($"Failed to get Kinkster for UID: {enactor.UID} for Blocked Chat Input!");
        
        _logger.LogInformation($"[{kinkster.GetNickAliasOrUid()}] Enabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        _chatbox.AddControlSources(PlayerControlSource.ChatInputBlocked);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputBlocked, true, enactor, MainHub.UID);

    }

    public bool UnblockChatInput(UserData enactor, IReadOnlyGlobalPerms perms)
    {
        if (!CanUnblockChatInput(enactor, perms))
            return false;
        UnblockChatInputUnsafe(enactor);
        return true;
    }

    public void UnblockChatInputUnsafe(UserData enactor)
    {
        _logger.LogInformation($"[{enactor.AliasOrUID}] Disabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        _chatbox.RemoveControlSources(PlayerControlSource.ChatInputBlocked);
        GagspeakEventManager.AchievementEvent(UnlocksEvent.HardcoreAction, HardcoreSetting.ChatInputBlocked, false, enactor, MainHub.UID);

    }
    #endregion Blocked Chat Input
}
