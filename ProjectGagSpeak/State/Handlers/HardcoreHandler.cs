using CkCommons;
using Dalamud.Game.ClientState.Objects;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Controller;
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using JetBrains.Annotations;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles the enabling and disabling of various hardcore changes.
/// </summary>
public class HardcoreHandler
{
    private readonly ILogger<HardcoreHandler> _logger;
    private readonly AutoPromptController _prompts;
    private readonly KeystateController _keyStates;
    private readonly MovementController _movement;
    private readonly ChatboxController _chatbox;
    private readonly OverlayHandler _overlay;

    // Stores the players's movement mode, useful for when we change it.
    private MovementMode _cachedPlayerMoveMode = MovementMode.NotSet;
    public HardcoreHandler(ILogger<HardcoreHandler> logger, AutoPromptController prompts, 
        KeystateController keyStates, MovementController movement, ChatboxController chatbox,
        OverlayHandler overlay)
    {
        _logger = logger;
        _prompts = prompts;
        _keyStates = keyStates;
        _movement = movement;
        _chatbox = chatbox;
        _overlay = overlay;
    }

    public void EnableLockedFollowing(Kinkster? pair = null)
    {
        if(pair is null)
        {
            _logger.LogError("Cannot enable forced follow without a valid pair.");
            return;
        }

        _logger.LogInformation($"[{pair.GetNickAliasOrUid()}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        
        // begin by caching the current movement mode of the player.
        _cachedPlayerMoveMode = GameConfig.UiControl.GetBool("MoveMode") ? MovementMode.Legacy : MovementMode.Standard;
        _logger.LogDebug($"Cached Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);
        GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Legacy);

        // Reset the movement tracker and position values.
        _movement.RestartTimeoutTracker();
        _movement.AddControlSources(PlayerControlSource.LockedFollowing);

        // Identify the player to target, and begin following them.
        if (pair.VisiblePairGameObject?.IsTargetable ?? false)
        {
            Svc.Targets.Target = pair.VisiblePairGameObject;
            ChatService.SendCommand("follow <t>");
            _logger.LogDebug("Enabled forced follow for pair.", LoggerType.HardcoreMovement);
        }
    }

    public void DisableLockedFollowing(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your LockedFollowing state.", LoggerType.HardcoreMovement);
        // If the source is already missing it means we stopped it manually.
        if (!_movement.Sources.HasAny(PlayerControlSource.LockedFollowing))
        {
            _logger.LogDebug("Forced follow is not active, nothing to disable.", LoggerType.HardcoreMovement);
            return;
        }

        _movement.ResetTimeoutTracker();
        _movement.RemoveControlSources(PlayerControlSource.LockedFollowing);

        // Restore the movement mode of the player.
        GameConfig.UiControl.Set("MoveMode", (uint)_cachedPlayerMoveMode);
        _logger.LogDebug($"Restored Player Movement Mode: {_cachedPlayerMoveMode}", LoggerType.HardcoreMovement);
        _cachedPlayerMoveMode = MovementMode.NotSet;
    }

    public async void EnableLockedEmote(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        _movement.AddControlSources(PlayerControlSource.LockedEmote);
        _keyStates.AddControlSources(PlayerControlSource.LockedEmote);

        // get our current emoteID.
        var currentEmote = EmoteService.CurrentEmoteId(PlayerData.ObjectAddress);
        var expectedEmote = OwnGlobals.LockedEmoteState;

        // Handle forcing the state based on what is expected.
        if (expectedEmote.EmoteID is 50 or 52)
            await EnsureForceSitState(currentEmote, expectedEmote);
        else
            await EnsureEmoteState(currentEmote, expectedEmote);
    }

    public void DisableLockedEmote(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your LockedFollowing state!", LoggerType.HardcoreMovement);
        _movement.RemoveControlSources(PlayerControlSource.LockedEmote);
        _keyStates.RemoveControlSources(PlayerControlSource.LockedEmote);
    }

    public void EnableForcedStay(string enactorUid, AddressBookEntry? spesificAddress = null)
    {
        // if the address is null, fallback to nearestNode behavior.
        _logger.LogInformation($"Manually invoked forcedStay from [{enactorUid}]!", LoggerType.HardcoreMovement);
        _prompts.AddControlSources(PlayerControlSource.IndoorConfinement);
    }

    public void EnableForcedStay(string enactorUid, AddressBookEntryTuple spesificAddress)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your ForcedStay state!", LoggerType.HardcoreMovement);
        _prompts.AddControlSources(PlayerControlSource.IndoorConfinement);
    }

    public void DisableForcedStay(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your ForcedStay state!", LoggerType.HardcoreMovement);
        _prompts.RemoveControlSources(PlayerControlSource.IndoorConfinement);
    }

    public void EnableImprisonment(string enactorUid, Vector3 originPos, float maxDistanceFromOrigin)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your Imprisonment state!", LoggerType.HardcoreMovement);
        // do magic here.
    }

    public void DisableImprisonment(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your Imprisonment state!", LoggerType.HardcoreMovement);
        // do magic here.
    }

    public void EnableHiddenChatBoxes(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(false);
    }

    public void DisableHiddenChatBoxes(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your HiddenChatBoxes state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatPanelVisibility(true);
    }

    public void EnableHiddenChatInput(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your HiddenChatInput state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(false);
    }

    public void DisableHiddenChatInput(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your HiddenChatInput state!", LoggerType.HardcoreActions);
        AddonChatLog.SetChatInputVisibility(true);
    }

    public void EnableBlockedChatInput(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Enabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        _chatbox.AddControlSources(PlayerControlSource.ChatInputBlocked);
    }

    public void DisableBlockedChatInput(string enactorUid)
    {
        _logger.LogInformation($"[{enactorUid}] Disabled your BlockedChatInput state!", LoggerType.HardcoreActions);
        _chatbox.RemoveControlSources(PlayerControlSource.ChatInputBlocked);
    }


    // Helper Methods Below:
    private async Task EnsureForceSitState(ushort currentId, EmoteState expected)
    {
        // If we are not sitting, make sure we sit.
        if (!EmoteService.IsSittingAny(currentId))
        {
            _logger.LogDebug($"Forcing Emote: {(expected.EmoteID is 50 ? "/SIT" : "/GROUNDSIT")}. (Current was: {currentId}).");
            EmoteService.ExecuteEmote(expected.EmoteID);
        }

        // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
        if (!await EmoteService.WaitForCondition(() => EmoteService.CanUseEmote(expected.EmoteID), 5))
        {
            _logger.LogWarning("Forced Emote State was not allowed to be executed. Cancelling.");
            return;
        }

        // get our cycle pose.
        var curCyclePose = EmoteService.CurrentCyclePose(PlayerData.ObjectAddress);
        // If it doesnt match, force into that pose.
        if (curCyclePose != expected.CyclePoseByte)
        {
            _logger.LogDebug($"Your CyclePose ({curCyclePose}) isnt the expected ({expected.CyclePoseByte})");
            if (!EmoteService.IsCyclePoseTaskRunning)
                EmoteService.ForceCyclePose(PlayerData.ObjectAddress, expected.CyclePoseByte);
        }
    }

    private async Task EnsureEmoteState(ushort currentId, EmoteState expected)
    {
        // If we are not sitting, make sure we sit.
        if (!EmoteService.IsSittingAny(currentId))
        {
            _logger.LogDebug($"Forcing Emote: /STAND, as you were sitting! (Current was: {currentId}).");
            EmoteService.ExecuteEmote(51);
        }

        // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
        if (!await EmoteService.WaitForCondition(() => EmoteService.CanUseEmote(expected.EmoteID), 5))
        {
            _logger.LogWarning("Forced Emote State was not allowed to be executed. Cancelling.");
            return;
        }

        // Perform the desired emote.
        EmoteService.ExecuteEmote(expected.EmoteID);
    }
}
