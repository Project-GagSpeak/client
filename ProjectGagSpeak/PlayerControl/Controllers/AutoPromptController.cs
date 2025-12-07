using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.State.Caches;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Handles automatically opening and responding to prompts for the player.
///     
///     Ideally we should be adapting more of Lifestreams behavior for this, but
///     wait until we turn to the dark side of the force for that.
/// </summary>
public sealed class AutoPromptController : DisposableMediatorSubscriberBase
{
    private readonly PlayerControlCache _cache;

    private bool _promptsEnabled = false;
    public AutoPromptController(ILogger<AutoPromptController> logger, GagspeakMediator mediator,
        PlayerControlCache cache) : base(logger, mediator)
    {
        _cache = cache;

        EnableListeners();

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreState());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisableListeners();
    }

    private void UpdateHardcoreState()
    {
        // always enable right now for debug purposes.
        return;

        if (_cache.DoAutoPrompts && !_promptsEnabled)
            EnableListeners();

        else if (!_cache.DoAutoPrompts && _promptsEnabled)
            DisableListeners();
    }

    private void EnableListeners()
    {
        if (_promptsEnabled)
            return;

        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", OnYesNoSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectString", OnStringSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnStringFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingSelectRoom", OnRoomSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "HousingSelectRoom", OnRoomFinalize);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MansionSelectRoom", OnApartmentSetup);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MansionSelectRoom", OnApartmentFinalize);
        _promptsEnabled = true;
    }

    private void DisableListeners()
    {
        if (!_promptsEnabled)
            return;
        Svc.AddonLifecycle.UnregisterListener(OnYesNoSetup);
        Svc.AddonLifecycle.UnregisterListener(OnStringSetup);
        Svc.AddonLifecycle.UnregisterListener(OnStringFinalize);
        Svc.AddonLifecycle.UnregisterListener(OnRoomSetup);
        Svc.AddonLifecycle.UnregisterListener(OnRoomFinalize);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentSetup);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentFinalize);
        _promptsEnabled = false;
    }

    private unsafe void OnYesNoSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        var stuff = (AddonSelectYesno*)addonInfo.Addon.Address;
        var moreStuff = stuff->NameString;
        var moreId = stuff->Id;

        Logger.LogInformation($"YesNo Setup for {moreStuff} (ID: {moreId})");
        Logger.LogInformation($"Text: {stuff->PromptText->NodeText}");
        Logger.LogInformation($"Yes: {stuff->YesButton->ButtonTextNode->NodeText}");
        Logger.LogInformation($"No: {stuff->NoButton->ButtonTextNode->NodeText}");
        Logger.LogInformation($"Target ID: {Svc.Targets.Target?.BaseId.ToString() ?? "UNK"}");

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
}
