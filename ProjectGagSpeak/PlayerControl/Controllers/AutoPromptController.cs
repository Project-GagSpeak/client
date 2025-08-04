using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
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
}
