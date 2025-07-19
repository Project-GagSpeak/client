using CkCommons;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.Game.Readers;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Handles automatically opening and responding to prompts for the player.
///     
///     Ideally we should be adapting more of Lifestreams behavior for this, but
///     wait until we turn to the dark side of the force for that.
/// </summary>
public sealed class AutoPromptController : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly SelectStringPrompt _promptsString;
    private readonly YesNoPrompt _promptsYesNo;
    private readonly RoomSelectPrompt _promptsRooms;
    private readonly KeystateController _keyController;
    private readonly MovementController _moveController;

    // Dictates controlling the player's AutoPrompt selecting.
    private PlayerControlSource _sources = PlayerControlSource.None;

    public AutoPromptController(ILogger<AutoPromptController> logger, GagspeakMediator mediator,
        MainConfig mainConfig, SelectStringPrompt strPrompts, YesNoPrompt ynPrompts, 
        RoomSelectPrompt roomPrompts, KeystateController keyCtrl, MovementController moveCtrl) 
        : base(logger, mediator)
    {
        _config = mainConfig;
        _promptsString = strPrompts;
        _promptsYesNo = ynPrompts;
        _promptsRooms = roomPrompts;
        _keyController = keyCtrl;
        _moveController = moveCtrl;

        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    public PlayerControlSource Sources => _sources;

    private unsafe void FrameworkUpdate()
    {
        if (PlayerData.IsDead)
            return;

        // Handle the ForcedStayMode
        if (_sources is not 0)
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

        // No reason to look for nodes if not in blocked by any sources.
        if (_sources is 0 || PlayerData.InQuestEvent)
            return;

        // NOTE: The Below is purely to make sure we automate people going into their destination.
        // If we can automate the process of enforcing someone to go to their destination,
        // without lifestream dependancy, we no longer need this.

        // grab all the event object nodes (door interactions) (Maybe find a better way to handle this later idk)
        TryInteractWithNodes();
    }

    public void AddControlSources(PlayerControlSource sources)
        => _sources |= sources;

    public void RemoveControlSources(PlayerControlSource sources)
        => _sources &= ~sources;

    private unsafe void TryInteractWithNodes()
    {
        var nodes = Svc.Objects.Where(x => x.ObjectKind is Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj);
        foreach (var node in nodes)
        {
            var distance = PlayerData.DistanceTo(node);

            if (NameIsApartmentOrEstate(node))
            {
                if (ApartmentOrEstateNodeInRange(distance) && !_moveController.IsMoveTaskRunning)
                {
                    Logger.LogDebug("Moving to Large Estate Entrance", LoggerType.HardcoreMovement);
                    SetMoveToNodeTask(node);
                }
                if (distance <= 3.5f)
                {
                    Logger.LogDebug("Entrance Node Interactable?" + node.IsTargetable);
                    if (node.IsTargetable)
                    {
                        Svc.Targets.Target = node;
                        TargetSystem.Instance()->InteractWithObject((GameObject*)node.Address, false);
                    }
                }
                break;
            }

            // If its a node that is an Entrance to Additional Chambers.
            if (NameIsChamberEntrance(node) && node.IsTargetable)
            {
                // if we are not within 2f of it, attempt to execute the task.
                if (ChambersNodeInRange(distance) && !_moveController.IsMoveTaskRunning)
                {
                    Logger.LogDebug("Moving to Additional Chambers", LoggerType.HardcoreMovement);
                    SetMoveToNodeTask(node);
                }

                // if we are within 2f, interact with it.
                if (distance <= 2f)
                {
                    Logger.LogDebug("Node Interactable?" + node.IsTargetable);
                    if (node.IsTargetable)
                    {
                        Svc.Targets.Target = node;
                        TargetSystem.Instance()->InteractWithObject((GameObject*)node.Address, false);
                    }
                }
                break;
            }
        }
    }

    private bool NameIsApartmentOrEstate(IGameObject obj)
        => obj.Name.TextValue == GSLoc.Settings.ForcedStay.EnterEstateName
        || obj.Name.TextValue == GSLoc.Settings.ForcedStay.EnterAPTOneName;

    private bool NameIsChamberEntrance(IGameObject obj)
        => obj.Name.TextValue == GSLoc.Settings.ForcedStay.EnterFCOneName;

    private bool ApartmentOrEstateNodeInRange(float distance)
        => (3.5f <= distance && distance < 7f);

    private bool ChambersNodeInRange(float distance)
        => (2f <= distance && _config.Current.MoveToChambersInEstates);

    private void SetMoveToNodeTask(IGameObject moveToThisNode)
    {
        var movementTask = Task.Run(async () =>
        {
            Logger.LogDebug("Node for Chambers Detected, Auto Walking to it for 5 seconds.");
            Svc.Targets.Target = moveToThisNode;
            ChatService.SendCommand("lockon");
            await Task.Delay(500);

            ChatService.SendCommand("automove");
            // await for 5 seconds then complete the task.
            await Task.Delay(5000);
        });
        _moveController.AssignMovementTask(movementTask, true);
    }
}
