using CkCommons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;
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
    private readonly KeystateController _keyController;
    private readonly MovementController _moveController;
    private readonly HcTaskManager _hcTaskManager;

    // Dictates controlling the player's AutoPrompt selecting.
    private PlayerControlSource _sources = PlayerControlSource.None;

    public AutoPromptController(ILogger<AutoPromptController> logger, GagspeakMediator mediator,
        MainConfig mainConfig, KeystateController keyCtrl, MovementController moveCtrl, 
        HcTaskManager hardcoreTaskManager) : base(logger, mediator)
    {
        _config = mainConfig;
        _keyController = keyCtrl;
        _moveController = moveCtrl;
        _hcTaskManager = hardcoreTaskManager;

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", OnYesNoSetup);

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnStringSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnStringFinalize);

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingSelectRoom", OnRoomSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "HousingSelectRoom", OnRoomFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MansionSelectRoom", OnApartmentSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MansionSelectRoom", OnApartmentFinalize);
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    private void OnYesNoSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the YesNo Setup!");
    }

    private void OnStringSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the String Finalize!");
    }

    private void OnStringFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("Im now in the String Finalize!");
    }

    private void OnRoomSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Room Setup!");
    }

    private void OnRoomFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Room Finalize!");
    }

    private void OnApartmentSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Apartment Setup!");
    }

    private void OnApartmentFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Apartment Finalize!");
    }

    public PlayerControlSource Sources => _sources;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.AddonLifecycle.UnregisterListener(OnYesNoSetup);
        Svc.AddonLifecycle.UnregisterListener(OnStringSetup);
        Svc.AddonLifecycle.UnregisterListener(OnStringFinalize);
        Svc.AddonLifecycle.UnregisterListener(OnRoomSetup);
        Svc.AddonLifecycle.UnregisterListener(OnRoomFinalize);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentSetup);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentFinalize);
    }
    private unsafe void FrameworkUpdate()
    {
        // bagagwa
    }

    // likely not a reliable way to handle this!
    public void AddControlSources(PlayerControlSource sources)
        => _sources |= sources;

    public void RemoveControlSources(PlayerControlSource sources)
        => _sources &= ~sources;

    /// <summary>
    ///     Task preset for locating the nearest room/estate/apartment, moving to it, and entering it. <para />
    ///     If Lifestream IPC is present, it can be accounted for (Handle soon!)
    /// </summary>
    public unsafe void EnqueueEnterNearestHousingRoom()
    {
        // do not perform if the task manager is busy.
        if (_hcTaskManager.IsBusy)
        {
            Logger.LogWarning("Cannot enqueue EnterNearestHousingRoom task, Hardcore Task Manager is busy.");
            return;
        }
        // do not enqueue while player is not available.
        if (!PlayerData.AvailableThreadSafe)
            return;

        // if we are not outside, do not attempt to enter a room.
        if (!HousingManager.Instance()->IsOutside())
        {
            Logger.LogWarning("Cannot enqueue EnterNearestHousingRoom task, player is not outside.");
            return;
        }

        // Begin the Task assignment.
        _hcTaskManager.BeginStack();
        try
        {
            // Enqueue Waiting for the player to be interactable and loading screen finished.
            _hcTaskManager.EnqueueTask(() => ForceStayUtils.IsScreenReady() && PlayerData.Interactable, "Wait for Loaded and Interactable");
            // Attempt to identify the nearest entrance, if none exist, throw an exception to early abort the task.
            _hcTaskManager.EnqueueTask(() =>
            {
                // obtain the nearest housing entrance, housing or apartment.
                var node = HcStayHousingEntrance.GetNearestHousingEntrance(out var distance);
                // if the node is too far away, or the node further than the maximum yalm distance, return false.
                if (node is null || distance >= 20f)
                    return false;

                // We know that we have a valid node. If we are not yet targetting it, we should target it.
                if (!node.IsTarget())
                {
                    if (node.IsTargetable && NodeThrottler.Throttle("HousingEntrance.Target", 200))
                    {
                        Svc.Targets.Target = node;
                        return false;
                    }
                }
                else
                {
                    // it is the target, so return true.
                    return true;
                }
                // operation failed, so return false.
                return false;
            }, "Target Nearest Valid Housing Entrance");
            // get the correct distance.
            var tName = Svc.Targets.Target?.Name.ToString() ?? string.Empty;
            var isApartment = NodeStringLang.EnterApartment.Any(n => n.Equals(tName, StringComparison.OrdinalIgnoreCase));
            var distThreshold = isApartment ? 3.5f : 2.75f;
            // Approach and target the node.
            _hcTaskManager.EnqueueTask(HcCommonTasks.ApproachNode(() => Svc.Targets.Target!, distThreshold));
            _hcTaskManager.EnqueueTask(() =>
            {
                // do not interact if animation locked.
                if (PlayerData.IsAnimationLocked)
                    return false;
                // if the target is not an event object and it's ID is not 2007402, then return false.
                if (Svc.Targets.Target?.ObjectKind != ObjectKind.EventObj || Svc.Targets.Target?.DataId != 2002737)
                    return false;

                // target was valid, so perform a throttled interaction with the apartment entrance.
                if (NodeThrottler.Throttle("InteractWithHouse", 1000))
                {
                    TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.ToStruct(), false);
                    return true; // return true regardless so we do not endlessly interact with something not in LOS.
                }
                // failed to throttle, return false.
                return false;


            }, "Interact with Housing Entrance");
            _hcTaskManager.EnqueueTask(HcStayHousingEntrance.ConfirmHouseEntranceAndEnter);
            // Insert all of these tasks into the Hardcore Task Manager for immidiate execution.
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Failed to Entering Nearest Housing Room: {ex}");
        }
        // Insert the stack regardless.
        _hcTaskManager.InsertStack();
    }
}
