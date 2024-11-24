using Dalamud.Game.ClientState.Objects;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore.Movement;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using System.Numerics;
using System.Reflection.Metadata;
using static GagspeakAPI.Extensions.GlobalPermExtensions;

namespace GagSpeak.PlayerData.Handlers;
/// <summary> Responsible for handling hardcore communication from stored data & ui to core logic. </summary>
public class HardcoreHandler : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _playerData;
    private readonly AppearanceManager _appearanceHandler;
    private readonly PairManager _pairManager;
    private readonly MainHub _apiHubMain; // for sending the updates.
    private readonly MoveController _moveController; // for movement logic
    private readonly ChatSender _chatSender; // for sending chat commands
    private readonly EmoteMonitor _emoteMonitor; // for handling the blindfold logic
    private readonly ITargetManager _targetManager; // for targeting pair on follows.

    public unsafe GameCameraManager* cameraManager = GameCameraManager.Instance(); // for the camera manager object
    public HardcoreHandler(ILogger<HardcoreHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, PlayerCharacterData playerData,
        AppearanceManager appearanceHandler, PairManager pairManager,
        MainHub apiHubMain, MoveController moveController, ChatSender chatSender,
        EmoteMonitor emoteMonitor, ITargetManager targetManager) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _appearanceHandler = appearanceHandler;
        _pairManager = pairManager;
        _apiHubMain = apiHubMain;
        _moveController = moveController;
        _chatSender = chatSender;
        _emoteMonitor = emoteMonitor;
        _targetManager = targetManager;

        Mediator.Subscribe<HardcoreActionMessage>(this, (msg) =>
        {
            switch (msg.type)
            {
                case HardcoreAction.ForcedFollow: UpdateForcedFollow(msg.State); break;
                case HardcoreAction.ForcedEmoteState: UpdateForcedEmoteState(msg.State); break;
                case HardcoreAction.ForcedStay: UpdateForcedStayState(msg.State); break;
                case HardcoreAction.ForcedBlindfold: UpdateBlindfoldState(msg.State); break;
                case HardcoreAction.ChatboxHiding: UpdateHideChatboxState(msg.State); break;
                case HardcoreAction.ChatInputHiding: UpdateHideChatInputState(msg.State); break;
                case HardcoreAction.ChatInputBlocking: UpdateChatInputBlocking(msg.State); break;
            }
        });

        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, _ => OnSafewordUsed().ConfigureAwait(false));
    }

    public UserGlobalPermissions? PlayerPerms => _playerData.GlobalPerms;

    public bool IsForcedToFollow => _playerData.GlobalPerms?.IsFollowing() ?? false;
    public bool IsForcedToEmote => !(_playerData.GlobalPerms?.ForcedEmoteState.NullOrEmpty() ?? true); // This is the inverse I think?
    public bool IsForcedToStay => _playerData.GlobalPerms?.IsStaying() ?? false;
    public bool IsBlindfolded => _playerData.GlobalPerms?.IsBlindfolded() ?? false;
    public bool IsHidingChat => _playerData.GlobalPerms?.IsChatHidden() ?? false;
    public bool IsHidingChatInput => _playerData.GlobalPerms?.IsChatInputHidden() ?? false;
    public bool IsBlockingChatInput => _playerData.GlobalPerms?.IsChatInputBlocked() ?? false;
    public GlobalPermExtensions.EmoteState ForcedEmoteState => _playerData.GlobalPerms?.ExtractEmoteState() ?? new GlobalPermExtensions.EmoteState();

    public bool MonitorFollowLogic => IsForcedToFollow;
    public bool MonitorEmoteLogic => IsForcedToEmote;
    public bool MonitorStayLogic => IsForcedToStay;
    public bool MonitorBlindfoldLogic => IsBlindfolded;
    public DateTimeOffset LastMovementTime { get; set; } = DateTimeOffset.Now;
    public Vector3 LastPosition { get; set; } = Vector3.Zero;
    public double StimulationMultiplier { get; set; } = 1.0;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!_clientConfigs.GagspeakConfig.UsingLegacyControls && GameConfig.UiControl.GetBool("MoveMode"))
            GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Standard);
    }

    private async Task OnSafewordUsed()
    {
        Logger.LogInformation("Turning all Hardcore Functionality in 3 seconds.", LoggerType.Safeword);
        await Task.Delay(2000);
        UpdateForcedFollow(NewState.Disabled);
        UpdateForcedEmoteState(NewState.Disabled);
        UpdateForcedStayState(NewState.Disabled);
        UpdateBlindfoldState(NewState.Disabled);
        UpdateHideChatboxState(NewState.Disabled);
        UpdateHideChatInputState(NewState.Disabled);
        UpdateChatInputBlocking(NewState.Disabled);
    }

    public void SendMessageHardcore(string commandNoSlash)
        => _chatSender.SendMessage("/" + commandNoSlash);

    public void UpdateForcedFollow(NewState newState)
    {
        // if we are enabling, adjust the lastMovementTime to now.
        if (newState is NewState.Enabled)
        {
            LastMovementTime = DateTimeOffset.UtcNow;
            Logger.LogDebug("Following UID: [" + _playerData.GlobalPerms?.ForcedFollow.HardcorePermUID()+"]", LoggerType.HardcoreMovement);
            // grab the pair from the pair manager to obtain its game object and begin following it.
            var pairToFollow = _pairManager.DirectPairs.FirstOrDefault(pair => pair.UserData.UID == _playerData.GlobalPerms?.ForcedFollow.HardcorePermUID());
            if (pairToFollow is null)
            {
                Logger.LogWarning("Ordered to follow but the pair who did it is not visible or targetable.");
                return;
            }
            // Begin Following if we should.
            if (pairToFollow.VisiblePairGameObject?.IsTargetable ?? false)
            {
                _targetManager.Target = pairToFollow.VisiblePairGameObject;
                SendMessageHardcore("follow <t>");
                Logger.LogDebug("Enabled forced follow for pair.", LoggerType.HardcoreMovement);
            }
        }

        if (newState is NewState.Disabled)
        {
            LastMovementTime = DateTimeOffset.MinValue;
            // If we are still following someone when this triggers it means we were idle long enough for it to disable.
            if (_playerData.GlobalPerms?.IsFollowing() ?? false)
            {
                // set the client first before push to prevent getting stuck while disconnected
                _playerData.GlobalPerms!.ForcedFollow = string.Empty;
                Logger.LogInformation("ForceFollow Disable was triggered manually before it naturally disabled. Forcibly shutting down.");
                _ = _apiHubMain.UserUpdateOwnGlobalPerm(new(new(MainHub.UID), new KeyValuePair<string, object>("ForcedFollow", string.Empty), MainHub.PlayerUserData));

            }
            else
            {
                // set the client first before push to prevent getting stuck while disconnected
                _playerData.GlobalPerms!.ForcedFollow = string.Empty;
                Logger.LogInformation("Disabled forced follow for pair.", LoggerType.HardcoreMovement);
            }

            // stop the movement mode.
            _moveController.DisableUnfollowHook();
        }

        // toggle movement type to legacy if we are not on legacy (regardless of it being enable or disable)
        if (!_clientConfigs.GagspeakConfig.UsingLegacyControls)
        {
            // if forced follow is still on, dont switch it back to false
            uint mode = newState switch
            {
                NewState.Enabled => (uint)MovementMode.Legacy,
                NewState.Disabled => (uint)MovementMode.Standard,
                _ => (uint)MovementMode.Standard
            };
            GameConfig.UiControl.Set("MoveMode", mode);
        }
    }

    // You can probably break this really fast if you just move while it executes, maybe wait for movement lock?
    public async void UpdateForcedEmoteState(NewState newState)
    {
        if (newState is NewState.Enabled)
        {
            Logger.LogDebug("Enabled forced Emote State for pair.", LoggerType.HardcoreMovement);
            // Lock Movement:
            _moveController.EnableMovementLock();

            // Step 1: Get Players current emoteId
            ushort currentEmote = _emoteMonitor.CurrentEmoteId(); // our current emote ID.

            // if our expected emote is 50, and we are not in any sitting pose, force the sit pose.
            if (ForcedEmoteState.EmoteID is 50 or 52)
            {
                // if we are not sitting, force the sit pose.
                if (!EmoteMonitor.IsSittingAny(currentEmote))
                {
                    Logger.LogDebug("Forcing Emote: /SIT [or /GROUNDSIT]. (Current emote was: " + currentEmote + ").");
                    EmoteMonitor.ExecuteEmote(ForcedEmoteState.EmoteID);
                }

                // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
                await _emoteMonitor.WaitForCondition(() => EmoteMonitor.CanUseEmote(ForcedEmoteState.EmoteID), 5);

                // get our cycle pose.
                byte currentCyclePose = _emoteMonitor.CurrentCyclePose();

                // if its not the expected cycle pose, we need to cycle the emote into the correct state.
                if (currentCyclePose != ForcedEmoteState.CyclePoseByte)
                {
                    Logger.LogDebug("Your CyclePose State ["+ currentCyclePose + "] does not match the requested cycle pose state. ["+ ForcedEmoteState.CyclePoseByte + "]");
                    // After this, we need to handle our current cycle pose state,
                    if (!EmoteMonitor.IsCyclePoseTaskRunning)
                        _emoteMonitor.ForceCyclePose(ForcedEmoteState.CyclePoseByte);
                }
                Logger.LogDebug("Locking Player in Current State until released.");
            }
            else
            {
                // It is a standard emote, so we can just check for a comparison and switch to it. if necessary.
                if (ForcedEmoteState.EmoteID is not 50 or 52)
                {
                    // if we are currently sitting in any manner, stand up first.
                    if (EmoteMonitor.IsSittingAny(currentEmote))
                    {
                        Logger.LogDebug("Forcing Emote: /STAND. (Current emote was: " + currentEmote + ").");
                        EmoteMonitor.ExecuteEmote(51);
                    }

                    // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
                    await _emoteMonitor.WaitForCondition(() => EmoteMonitor.CanUseEmote(ForcedEmoteState.EmoteID), 5);

                    // Execute the desired emote.
                    Logger.LogDebug("Forcing Emote: " + ForcedEmoteState.EmoteID + "(Current emote was: " + currentEmote + ")");
                    EmoteMonitor.ExecuteEmote(ForcedEmoteState.EmoteID);

                    Logger.LogDebug("Locking Player in Current State until released.");
                }
            }
        }

        if (newState is NewState.Disabled)
        {
            Logger.LogDebug("Pair has allowed you to stand again.", LoggerType.HardcoreMovement);
            // set it on client before getting change back from server.
            _playerData.GlobalPerms!.ForcedEmoteState = string.Empty;
            // Disable the movement lock after we set our permissions for validation.
            _moveController.DisableMovementLock();
        }
    }

    public void UpdateForcedStayState(NewState newState)
    {
        Logger.LogDebug(newState is NewState.Enabled ? "Enabled" : "Disabled" + " forced stay for pair.", LoggerType.HardcoreMovement);
        if (newState is NewState.Disabled)
        {
            // set it on client before getting change back from server.
            _playerData.GlobalPerms!.ForcedStay = string.Empty;
        }
    }

    private async void UpdateBlindfoldState(NewState newState)
    {
        Logger.LogDebug(newState is NewState.Enabled
            ? "Enabled forced blindfold for pair." : "Disabled forced blindfold for pair.", LoggerType.HardcoreMovement);
        if (newState is NewState.Enabled && !BlindfoldUI.IsWindowOpen)
        {
            await _appearanceHandler.RecalcAndReload(false); // Fire and Forget
            await HandleBlindfoldLogic(newState);
            return;
        }

        if (newState is NewState.Disabled && BlindfoldUI.IsWindowOpen)
        {
            // set it on client before getting change back from server.
            _playerData.GlobalPerms!.ForcedBlindfold = string.Empty; // Help to prevent getting stuff when offline.
            await _appearanceHandler.RecalcAndReload(true); // Fire and Forget
            await HandleBlindfoldLogic(newState);
            return;
        }
    }

    public void UpdateHideChatboxState(NewState newState)
    {
        Logger.LogDebug(newState is NewState.Enabled ? "Enabled " : "Disabled " + "Chatbox Visibility", LoggerType.HardcoreActions);
        // set new visibility state
        var visibility = newState is NewState.Enabled ? false : true;
        ChatLogAddonHelper.SetChatLogPanelsVisibility(visibility);
        // if this was called while we were not connected, manually set the new value.
        if (newState is NewState.Disabled && (_playerData.GlobalPerms?.IsChatHidden() ?? false))
        {
            Logger.LogWarning("You were disconnected when invoking this disable, meaning it was likely triggered from a safeword. Manually switching off!");
            _playerData.GlobalPerms.ChatBoxesHidden = string.Empty;
        }
    }

    public void UpdateHideChatInputState(NewState newState)
    {
        Logger.LogDebug(newState is NewState.Enabled ? "Enabled " : "Disabled "
            + "Chat Input Visibility", LoggerType.HardcoreActions);
        // set new visibility state
        var visibility = newState is NewState.Enabled ? false : true;
        ChatLogAddonHelper.SetMainChatLogVisibility(visibility);
        // if this was called while we were not connected, manually set the new value.
        if (newState is NewState.Disabled && (_playerData.GlobalPerms?.IsChatInputHidden() ?? false))
        {
            Logger.LogWarning("You were disconnected when invoking this disable, meaning it was likely triggered from a safeword. Manually switching off!");
            _playerData.GlobalPerms.ChatInputHidden = string.Empty;
        }
    }

    public void UpdateChatInputBlocking(NewState newState)
    {
        // No logic handled here. Instead it is handled in the framework updater.
        Logger.LogDebug(newState is NewState.Enabled ? "Enabled " : "Disabled " + "Chat Input Blocking", LoggerType.HardcoreActions);
        // if this was called while we were not connected, manually set the new value.
        if (newState is NewState.Disabled && (_playerData.GlobalPerms?.IsChatInputBlocked() ?? false))
        {
            Logger.LogWarning("You were disconnected when invoking this disable, meaning it was likely triggered from a safeword. Manually switching off!");
            _playerData.GlobalPerms.ChatInputBlocked = string.Empty;
        }
    }

    public async Task HandleBlindfoldLogic(NewState newState)
    {
        // toggle our window based on conditions
        if (newState is NewState.Enabled)
        {
            // if the window isnt open, open it.
            if (!BlindfoldUI.IsWindowOpen)
                Mediator.Publish(new UiToggleMessage(typeof(BlindfoldUI), ToggleType.Show));
            // go in for camera voodoo.
            DoCameraVoodoo(newState);
        }
        else
        {
            if (BlindfoldUI.IsWindowOpen)
                Mediator.Publish(new HardcoreRemoveBlindfoldMessage());
            // wait a bit before doing the camera voodoo
            await Task.Delay(2000);
            DoCameraVoodoo(newState);
        }
    }

    private unsafe void DoCameraVoodoo(NewState newValue)
    {
        // force the camera to first person, but dont loop the force
        if (newValue is NewState.Enabled)
        {
            if (cameraManager is not null && cameraManager->Camera is not null && cameraManager->Camera->Mode is not (int)CameraControlMode.FirstPerson)
                cameraManager->Camera->Mode = (int)CameraControlMode.FirstPerson;
        }
        else
        {
            if (cameraManager is not null && cameraManager->Camera is not null && cameraManager->Camera->Mode is (int)CameraControlMode.FirstPerson)
                cameraManager->Camera->Mode = (int)CameraControlMode.ThirdPerson;
        }
    }

    public void ApplyMultiplier()
    {
        var activeSet = _clientConfigs.GetActiveSet();
        if (activeSet is null)
            return;

        var stimulationLvl = activeSet.SetTraits[activeSet.EnabledBy].StimulationLevel;
        StimulationMultiplier = stimulationLvl switch
        {
            StimulationLevel.None => 1.0,
            StimulationLevel.Light => 1.125,
            StimulationLevel.Mild => 1.25,
            StimulationLevel.Heavy => 1.5,
            _ => 1.0
        };
        Logger.LogDebug(stimulationLvl switch
        {
            StimulationLevel.None => "No Stimulation Multiplier applied from set, defaulting to 1.0x!",
            StimulationLevel.Light => "Light Stimulation Multiplier applied from set with factor of 1.125x!",
            StimulationLevel.Mild => "Mild Stimulation Multiplier applied from set with factor of 1.25x!",
            StimulationLevel.Heavy => "Heavy Stimulation Multiplier applied from set with factor of 1.5x!",
            _ => "No Stimulation Multiplier applied from set"
        }, LoggerType.HardcoreActions);
    }
}
