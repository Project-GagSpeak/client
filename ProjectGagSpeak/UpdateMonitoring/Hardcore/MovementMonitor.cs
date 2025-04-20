using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.CkCommons;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Hardcore.Movement;
using GagSpeak.Localization;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Extensions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using XivControl = FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace GagSpeak.UpdateMonitoring;
public class MovementMonitor : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly GlobalData _globals;
    private readonly ChatSender _chatSender; // Ensures 0 delay from the point of send.
    private readonly GagspeakConfigService _mainConfig;
    private readonly SelectStringPrompt _promptsString;
    private readonly YesNoPrompt _promptsYesNo;
    private readonly RoomSelectPrompt _promptsRooms;
    private readonly PairManager _pairs;
    private readonly TraitsManager _traits;
    private readonly ClientMonitor _clientMonitor;
    private readonly EmoteMonitor _emoteMonitor;
    private readonly MoveController _moveController;
    private readonly IKeyState _keyState;
    private readonly IObjectTable _objectTable;
    private readonly ITargetManager _targetManager;

    private CancellationTokenSource _emoteCTS = new();

    // for controlling walking speed, follow movement manager, and sitting/standing.
    public unsafe GameCameraManager* cameraManager = GameCameraManager.Instance(); // for the camera manager object
    public unsafe XivControl.Control* gameControl = XivControl.Control.Instance(); // instance to have control over our walking

    // get the keystate ref values
    delegate ref int GetRefValue(int vkCode);
    private static GetRefValue? getRefValue;
    private bool WasCancelled = false; // if true, we have cancelled any movement keys

    public MovementMonitor(
        ILogger<MovementMonitor> logger,
        GagspeakMediator mediator,
        MainHub hub,
        GlobalData globals,
        ChatSender chatSender,
        GagspeakConfigService config,
        SelectStringPrompt stringPrompts,
        YesNoPrompt yesNoPrompts,
        RoomSelectPrompt rooms,
        PairManager pairs,
        TraitsManager traits,
        ClientMonitor clientMonitor,
        EmoteMonitor emoteMonitor,
        MoveController moveController, 
        IKeyState keyState,
        IObjectTable objectTable,
        ITargetManager targetManager) : base(logger, mediator)
    {
        _hub = hub;
        _globals = globals;
        _chatSender = chatSender;
        _mainConfig = config;
        _promptsString = stringPrompts;
        _promptsYesNo = yesNoPrompts;
        _promptsRooms = rooms;
        _pairs = pairs;
        _traits = traits;
        _clientMonitor = clientMonitor;
        _emoteMonitor = emoteMonitor;
        _moveController = moveController;
        _keyState = keyState;
        _objectTable = objectTable;
        _targetManager = targetManager;

        // attempt to set the value safely
        Generic.ExecuteSafely(delegate
        {
            getRefValue = (GetRefValue)Delegate.CreateDelegate(typeof(GetRefValue), _keyState,
                            _keyState.GetType().GetMethod("GetRefValue", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int) }, null)!);
        });

        Mediator.Subscribe<SafewordHardcoreUsedMessage>(this, _ => SafewordUsed());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());

        traits.OnTraitStateChanged += ProcessTraitChange;
        traits.OnHardcoreStateChanged += ProcessHardcoreTraitChange;

    }

    public Stopwatch LastMovement { get; set; } = new Stopwatch();
    private Vector3 LastPosition = Vector3.Zero;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // enable movement
        ResetCancelledMoveKeys();
    }

    #region Process Trait Changes
    private void ProcessTraitChange(Traits prevTraits, Traits newTraits)
    {

    }

    private async void ProcessHardcoreTraitChange(HardcoreTraits prevTraits, HardcoreTraits newTraits)
    {
        var changed = prevTraits ^ newTraits;

        if (_globals.GlobalPerms is not { } perms)
            return;

        // Enable Forced Follow if it was set to enabled.
        if(changed.HasAny(HardcoreTraits.ForceFollow))
        {
            if(newTraits.HasAny(HardcoreTraits.ForceFollow))
            {
                // Cache movement mode to keep original movement set afterwards.
                _traits.CachedMovementMode = GameConfig.UiControl.GetBool("MoveMode") ? MovementMode.Legacy : MovementMode.Standard;
                Logger.LogDebug("Cached MoveMode: " + _traits.CachedMovementMode, LoggerType.HardcoreMovement);
                GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Legacy);

                if(LastMovement.IsRunning) LastMovement.Restart();
                else LastMovement.Start();

                if (_pairs.DirectPairs.FirstOrDefault(p => p.UserData.UID == perms.ForcedFollow.HardcorePermUID()) is { } match)
                {
                    if(match.VisiblePairGameObject?.IsTargetable ?? false)
                    {
                        _targetManager.Target = match.VisiblePairGameObject;
                        ChatMonitor.EnqueueMessage("/follow <t>");
                        Logger.LogDebug("Enabled forced follow for pair.", LoggerType.HardcoreMovement);
                    }
                }
            }
            else
            {
                DisableForcedFollow();
            }
        }

        // Handle Forced Emote if it was changed.
        if (changed.HasAny(HardcoreTraits.ForceEmote))
        {
            if (newTraits.HasAny(HardcoreTraits.ForceFollow))
            {
                Logger.LogDebug("Enabled forced Emote State for pair.", LoggerType.HardcoreMovement);
                _moveController.EnableMovementLock();

                // Reset cancellation token source.
                if (!_emoteCTS.TryReset()) { _emoteCTS.Dispose(); _emoteCTS = new(); }

                // cache emote state set.
                _traits.CachedEmoteState = perms.ExtractEmoteState();

                var currentEmote = _emoteMonitor.CurrentEmoteId();
                if (_traits.CachedEmoteState.EmoteID is 50 or 52)
                {
                    if (!EmoteMonitor.IsSittingAny(currentEmote))
                    {
                        Logger.LogDebug("Forcing Emote: /SIT [or /GROUNDSIT]. (Current emote was: " + currentEmote + ").");
                        EmoteMonitor.ExecuteEmote(_traits.CachedEmoteState.EmoteID);
                    }

                    // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
                    var emoteID = _traits.CachedEmoteState.EmoteID; // Assigned for condition below to avoid accessing the _traits.CachedEmoteState getter multiple times.
                    if (!await _emoteMonitor.WaitForCondition(() => EmoteMonitor.CanUseEmote(emoteID), 5, _emoteCTS.Token))
                    {
                        Logger.LogWarning("Forced Emote State was not allowed to be executed. Cancelling.");
                        return;
                    }

                    // get our cycle pose.
                    var currentCyclePose = _emoteMonitor.CurrentCyclePose();
                    if (currentCyclePose != _traits.CachedEmoteState.CyclePoseByte)
                    {
                        Logger.LogDebug("Your Cpose [" + currentCyclePose + "] doesn't match requested cpose [" + _traits.CachedEmoteState.CyclePoseByte + "]");
                        if (!EmoteMonitor.IsCyclePoseTaskRunning)
                            _emoteMonitor.ForceCyclePose(_traits.CachedEmoteState.CyclePoseByte);
                    }
                    Logger.LogDebug("Locking Player in Current State until released.");
                }
                else
                {
                    // if we are currently sitting in any manner, stand up first.
                    if (EmoteMonitor.IsSittingAny(currentEmote))
                    {
                        Logger.LogDebug("Forcing Emote: /STAND. (Current emote was: " + currentEmote + ").");
                        EmoteMonitor.ExecuteEmote(51);
                    }

                    // Wait until we are allowed to use another emote again, after which point, our cycle pose will have registered.
                    var emoteID = _traits.CachedEmoteState.EmoteID; // Assigned for condition below to avoid accessing the _traits.CachedEmoteState getter multiple times.
                    if (!await _emoteMonitor.WaitForCondition(() => EmoteMonitor.CanUseEmote(emoteID), 5, _emoteCTS.Token))
                    {
                        Logger.LogWarning("Forced Emote State was not allowed to be executed. Cancelling.");
                        return;
                    }

                    // Execute the desired emote.
                    Logger.LogDebug("Forcing Emote: " + _traits.CachedEmoteState.EmoteID + "(Current emote was: " + currentEmote + ")");
                    EmoteMonitor.ExecuteEmote(_traits.CachedEmoteState.EmoteID);
                    Logger.LogDebug("Locking Player in Current State until released.");
                }
            }
            else
            {
                Logger.LogDebug("Pair has allowed you to stand again.", LoggerType.HardcoreMovement);
                _emoteCTS.Cancel();
                _moveController.DisableMovementLock();
            }
        }

        if (changed.HasAny(HardcoreTraits.ChatBoxHidden))
        {
            if (newTraits.HasAny(HardcoreTraits.ChatBoxHidden))
            {
                Logger.LogDebug("Hiding ChatBox", LoggerType.HardcoreActions);
                ChatLogAddonHelper.SetChatLogPanelsVisibility(false);
            }
            else
            {
                Logger.LogDebug("Showing ChatBox", LoggerType.HardcoreActions);
                ChatLogAddonHelper.SetChatLogPanelsVisibility(true);
            }
        }

        if (changed.HasAny(HardcoreTraits.ChatInputHidden))
        {
            if (newTraits.HasAny(HardcoreTraits.ChatInputHidden))
            {
                Logger.LogDebug("Hiding Chat Input", LoggerType.HardcoreActions);
                ChatLogAddonHelper.SetMainChatLogVisibility(false);
            }
            else
            {
                Logger.LogDebug("Showing Chat Input", LoggerType.HardcoreActions);
                ChatLogAddonHelper.SetMainChatLogVisibility(false);
            }
        }
    }
    #endregion Process Trait Changes

    public async void SafewordUsed()
    {
        // Wait 3 seconds to let everything else from the safeword process first.
        Logger.LogDebug("Safeword has been used, re-enabling movement in 3 seconds");
        await Task.Delay(3000);
        // Fix walking state
        ResetCancelledMoveKeys();
    }

    private async void DisableForcedFollow()
    {
        LastMovement.Stop();
        // assume true for safety.
        if (_globals.GlobalPerms?.IsFollowing() ?? true)
        {
            Logger.LogInformation("ForceFollow Disable was triggered manually before it naturally disabled. Forcibly shutting down.");
            await _hub.UserUpdateOwnGlobalPerm(new(new(MainHub.UID), MainHub.PlayerUserData,
                new KeyValuePair<string, object>("ForcedFollow", string.Empty), UpdateDir.Own));
        }

        // stop the movement mode.
        _moveController.DisableUnfollowHook();

        // reset movement mode if cached was standard.
        if (_traits.CachedMovementMode is MovementMode.Standard)
            GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Standard);

        // make cached mode not set again.
        if (_traits.CachedMovementMode is not MovementMode.NotSet)
            _traits.CachedMovementMode = MovementMode.NotSet;
    }

    #region Framework Updates
    private unsafe void FrameworkUpdate()
    {
        // make sure we only do checks when we are properly logged in and have a character loaded
        if (!_clientMonitor.IsPresent || _clientMonitor.IsDead)
            return;

        if (_traits.ActiveHcTraits.HasAny(HardcoreTraits.ForceFollow))
        {
            _moveController.EnableUnfollowHook();
            if (LastMovement.IsRunning)
            {
                if (_clientMonitor.ClientPlayer!.Position != LastPosition)
                {
                    LastMovement.Restart();
                    LastPosition = _clientMonitor.ClientPlayer!.Position;
                }

                if (LastMovement.Elapsed > TimeSpan.FromSeconds(6))
                    DisableForcedFollow();
            }
        }

        if (_traits.ForceWalking)
        {
            uint isWalking = Marshal.ReadByte((nint)gameControl, 30211);
            if (isWalking is 0) Marshal.WriteByte((nint)gameControl, 30211, 0x1);
        }

        if (_traits.ActiveHcTraits.HasAny(HardcoreTraits.ForceStay))
        {
            if (!_clientMonitor.InQuestEvent)
            {
                // grab all the event object nodes (door interactions)
                var nodes = _objectTable.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj).ToList();
                foreach (var node in nodes)
                {
                    var distance = _clientMonitor.ClientPlayer?.GetTargetDistance(node) ?? float.MaxValue;

                    if ((node.Name.TextValue == GSLoc.Settings.ForcedStay.EnterEstateName || node.Name.TextValue == GSLoc.Settings.ForcedStay.EnterAPTOneName))
                    {
                        // if we are not within the distance to interact with entrance nodes, attempt to execute the task.
                        if (distance > 3.5f && distance < 7f)
                        {
                            if (_moveToChambersTask is null)
                            {
                                Logger.LogDebug("Moving to Large Estate Entrance", LoggerType.HardcoreMovement);
                                _moveToChambersTask = GoToChambersEntrance(node);
                            }
                        }
                        if (distance <= 3.5f)
                        {
                            Logger.LogDebug("Entrance Node Interactable?" + node.IsTargetable);
                            _targetManager.Target = node;
                            if (node.IsTargetable)
                            {
                                TargetSystem.Instance()->InteractWithObject((GameObject*)node.Address, false);
                            }
                        }
                        break;
                    }

                    // If its a node that is an Entrance to Additional Chambers.
                    if (node.Name.TextValue == GSLoc.Settings.ForcedStay.EnterFCOneName && node.IsTargetable)
                    {
                        // if we are not within 2f of it, attempt to execute the task.
                        if (distance > 2f && _mainConfig.Config.MoveToChambersInEstates)
                        {
                            if (_moveToChambersTask is null)
                            {
                                Logger.LogDebug("Moving to Additional Chambers", LoggerType.HardcoreMovement);
                                _moveToChambersTask = GoToChambersEntrance(node);
                            }
                        }

                        // if we are within 2f, interact with it.
                        if (distance <= 2f)
                        {
                            Logger.LogDebug("Node Interactable?" + node.IsTargetable);
                            _targetManager.Target = node;
                            if(node.IsTargetable)
                            {
                                TargetSystem.Instance()->InteractWithObject((GameObject*)node.Address, false);
                            }
                        }
                        break;
                    }
                }
            }
        }

        // Handle Prompt Logic.
        if (_traits.ActiveHcTraits.HasAny(HardcoreTraits.ForceStay) || _clientMonitor.InCutscene)
        {
            _promptsString.Enable();
            _promptsYesNo.Enable();
            _promptsRooms.Enable();
        }
        else
        {
            _promptsString.Disable();
            _promptsYesNo.Disable();
            _promptsRooms.Disable();
        }

        // Cancel Keys if forced follow or immobilization is active. (Also disable our keys we are performing the Chambers Task)
        if (_traits.ShouldBlockKeys || _moveToChambersTask is not null) CancelMoveKeys();
        else ResetCancelledMoveKeys();

        // We need to prevent LMB+RMB movement.
        if (_traits.IsImmobile) _moveController.EnableMouseAutoMoveHook();
        else _moveController.DisableMouseAutoMoveHook();

        // Force Lock First Person if desired.
        if (_traits.ActiveBlindfoldForcesFirstPerson)
        {
            if (cameraManager->Camera is not null && cameraManager->Camera->Mode is not (int)CameraControlMode.FirstPerson)
                cameraManager->Camera->Mode = (int)CameraControlMode.FirstPerson;
        }

        // Ensure restricted movement.
        if (_traits.ActiveHcTraits.HasAny(HardcoreTraits.ForceEmote))
            _moveController.EnableMovementLock();
    }

    private Task? _moveToChambersTask;

    private async Task GoToChambersEntrance(IGameObject nodeToWalkTo)
    {
        try
        {
            Logger.LogDebug("Node for Chambers Detected, Auto Walking to it for 5 seconds.");
            // Set the target to the node.
            _targetManager.Target = nodeToWalkTo;
            // lock onto the object
            _chatSender.SendMessage("/lockon");
            await Task.Delay(500);
            _chatSender.SendMessage("/automove");
            // set mode to run
            unsafe
            {
                uint isWalking = Marshal.ReadByte((nint)gameControl, 30211);
                // they are walking, so make them run.
                if (isWalking is not 0)
                    Marshal.WriteByte((nint)gameControl, 30211, 0x0);
            }
            // await for 5 seconds then complete the task.
            await Task.Delay(5000);
        }
        finally
        {
            _moveToChambersTask = null;
        }
    }

    private void CancelMoveKeys()
    {
        MoveKeys.Each(x =>
        {
            // the action to execute for each of our moved keys
            if (_keyState.GetRawValue(x) != 0)
            {
                // if the value is set to execute, cancel it.
                _keyState.SetRawValue(x, 0);
                WasCancelled = true;
            }
        });
    }

    private void ResetCancelledMoveKeys()
    {
        if (WasCancelled)
        {
            WasCancelled = false;
            // Restore the state of the virtual keys
            MoveKeys.Each(x =>
            {
                if (KeyMonitor.IsKeyPressed((int)(Keys)x))
                    SetKeyState(x, 3);
            });
        }
    }

    // set the key state (if you start crashing when using this you probably have a fucked up getrefvalue)
    private static void SetKeyState(VirtualKey key, int state) => getRefValue!((int)key) = state;

    public HashSet<VirtualKey> MoveKeys = new() {
        VirtualKey.W,
        VirtualKey.A,
        VirtualKey.S,
        VirtualKey.D,
        VirtualKey.SPACE,
    };
    #endregion Framework Updates
}
