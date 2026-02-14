using CkCommons;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;

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

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        DisableListeners();
    }

    private void UpdateHardcoreStatus()
    {
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
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnStringSelected);
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
        Svc.AddonLifecycle.UnregisterListener(OnStringSelected);
        Svc.AddonLifecycle.UnregisterListener(OnRoomSetup);
        Svc.AddonLifecycle.UnregisterListener(OnRoomFinalize);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentSetup);
        Svc.AddonLifecycle.UnregisterListener(OnApartmentFinalize);
        _promptsEnabled = false;
    }

    private unsafe void OnYesNoSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        var yesno = (AddonSelectYesno*)addonInfo.Addon.Address;
        var baseAddon = (AtkUnitBase*)yesno;

        string[] nodesToDecline = [ ..GsLang.ConfirmHouseExit, ..GsLang.ConfirmChamberLeave ];
        string[] nodesToAccept = [ ..GsLang.ConfirmHouseEntrance ];

        // Check for auto-no responces
        if (HcTaskUtils.YesNoMatches(baseAddon, contains: false, nodesToDecline))
        {
            // if addon is ready, check for validation to hit the yes button prior to pressing it.
            if (yesno->NoButton is not null && !yesno->NoButton->IsEnabled)
            {
                // forcibly enable the yes button through node flag manipulation.
                Svc.Logger.Verbose($"{nameof(AddonSelectYesno)}: Force enabling [No]");
                var flagsPtr = (ushort*)&yesno->NoButton->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags;
                *flagsPtr ^= 1 << 5; // Toggle the 5th bit to enable the button.
            }
            HcTaskUtils.ClickButtonIfEnabled(baseAddon, yesno->NoButton);
            return;
        }

        // For force accept
        if (HcTaskUtils.YesNoMatches(baseAddon, contains: false, nodesToAccept))
        {
            // if addon is ready, check for validation to hit the yes button prior to pressing it.
            if (yesno->YesButton is not null && !yesno->YesButton->IsEnabled)
            {
                // forcibly enable the yes button through node flag manipulation.
                Svc.Logger.Verbose($"{nameof(AddonSelectYesno)}: Force enabling [Yes]");
                var flagsPtr = (ushort*)&yesno->YesButton->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags;
                *flagsPtr ^= 1 << 5; // Toggle the 5th bit to enable the button.
            }
            HcTaskUtils.ClickButtonIfEnabled(baseAddon, yesno->YesButton);
            return;
        }
    }

    private unsafe void OnStringSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        // If we are not inside, we should ignore auto selecting prompts, as our goal is to keep people inside.
        var haus = HousingManager.Instance();
        if (haus is null || !haus->IsInside())
            return;

        var selectStr = (AddonSelectString*)addonInfo.Addon.Address;
        var baseAddon = (AtkUnitBase*)selectStr;

        var promptTxt = MemoryHelper.ReadSeString(&baseAddon->GetTextNodeById(2)->NodeText).ExtractText();
        var entries = HcTaskUtils.GetEntries(selectStr);

        if (GsLang.ConfirmChamberLeave.Any(promptTxt.Equals))
        {
            // Try and select no
            if (GsLang.RejectChamberLeave.FirstOrDefault(x => entries.Any(e => e.Equals(x))) is { } match)
            {
                var index = entries.IndexOf(match);
                if (index >= 0)
                {
                    Svc.Logger.Debug($"[HcTaskUtils] SelectSpesificEntry: selecting {match}/{index} requested from [{string.Join(',', entries)}].");
                    StaticDetours.FireCallback((AtkUnitBase*)addonInfo.Addon.Address, true, index);
                }
                return;
            }
        }
        // It was a target, and it was exit.
        else if (Svc.Targets.Target is { } target && GsLang.ExitApartment.Any(target.Name.ToString().Equals))
        {

            // if there are no entries, we cannot select anything.
            if (entries.FirstOrDefault(x => GsLang.RejectApartmentLeave.Any(e => e.Equals(x))) is { } match)
            {
                // the entry does exist, so try and select it. (if our throttle allows it)
                var index = entries.IndexOf(match);
                if (index >= 0)
                {
                    Svc.Logger.Debug($"[HcTaskUtils] SelectSpesificEntry: selecting {match}/{index} requested from [{string.Join(',', entries)}].");
                    StaticDetours.FireCallback((AtkUnitBase*)baseAddon, true, index);
                    return;
                }
            }
        }
        else
        {
            // Other use cases can be handled here.
        }
    }

    private unsafe void OnStringSelected(AddonEvent eventType, AddonArgs addonInfo)
    {
        // This was originally used to detect if we skipped a cutscene. If we wanted to do this another way,
        // or we find another way to detect selection history that is easier, this could be avoided entirely.
        return;

        // Just some placeholder stuff for now.
        var lastLabel = string.Empty;
        var lastSelected = string.Empty;

        if (lastLabel.Contains("Skip cutscene", StringComparison.OrdinalIgnoreCase) && lastSelected.Contains("Yes", StringComparison.OrdinalIgnoreCase))
        {
            Logger.LogTrace("Cutscene Skip Detected, Halting Achievement WarriorOfLewd", LoggerType.Achievements);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.CutsceneInturrupted);
        }
    }

    // These would be mainly for automating the entrance of apartments / chambers, which we could do via automation if possible.
    // Apartment automation is already doable, so OnApartmentSetup and OnApartmentFinalize likely will not need much with lifestream.
    private void OnApartmentSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Apartment Setup!");
    }

    private void OnApartmentFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Apartment Finalize!");
    }

    // This can be setup later when we want advanced functionality
    private void OnRoomSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Room Setup!");
    }

    private void OnRoomFinalize(AddonEvent eventType, AddonArgs addonInfo)
    {
        Logger.LogTrace("I'm Now in the Room Finalize!");
    }
}
