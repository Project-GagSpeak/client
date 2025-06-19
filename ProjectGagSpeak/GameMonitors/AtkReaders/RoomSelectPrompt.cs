using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.Utils;

namespace GagSpeak.Game.Readers;
public class RoomSelectPrompt : BasePrompt
{
    private readonly ILogger<RoomSelectPrompt> _logger;
    private readonly MainConfig _config;

    private DateTime LastSelectionTime = DateTime.MinValue;

    public RoomSelectPrompt(ILogger<RoomSelectPrompt> logger, MainConfig config)
    {
        _logger = logger;
        _config = config;
    }

    // Run on plugin Enable
    public override void Enable()
    {
        if(!Enabled)
        {
            base.Enable();
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingSelectRoom", AddonSetup);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "HousingSelectRoom", SetEntry);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "MansionSelectRoom", AddonSetup);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MansionSelectRoom", SetEntry);
        }
    }

    private unsafe void SetEntry(AddonEvent type, AddonArgs args)
    {
        try
        {
            var addon = args.Base();
            // get the name
            var targetName = Svc.Targets.Target?.Name.ExtractText() ?? string.Empty;
            MainConfig.LastSeenNodeName = targetName;
            // Output all the text nodes in a concatinated string
            MainConfig.LastSeenNodeLabel = AddonBaseRoom.ToText(addon, 8);
        }
        catch { }
    }

    // Run on Plugin Disable
    public override void Disable()
    {
        if(Enabled)
        {
            base.Disable();
            Svc.AddonLifecycle.UnregisterListener(AddonSetup);
            Svc.AddonLifecycle.UnregisterListener(SetEntry);
        }
    }

    protected async void AddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        // return if less than 5 seconds from last interaction to avoid infinite loop spam.
        await Task.Delay(750);

        // get the name
        var targetName = Svc.Targets.Target?.Name.ExtractText() ?? string.Empty;
        MainConfig.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);

        // Try and locate if we have a match.
        var nodes = MainConfig.GetAllNodes().OfType<ChambersTextNode>();
        foreach (var node in nodes)
        {
            // If the node does not have the chamber room set, do not process it.
            if (!node.Enabled || node.ChamberRoomSet < 0)
                continue;

            // If we are only doing it on a spesific node and the names dont match, skip it.
            if (node.TargetRestricted && !MainConfig.LastSeenNodeName.Contains(node.TargetNodeName))
                continue;

            // If we have a match, fire the event.
            _logger.LogDebug("RoomSelectPrompt: Matched on " + node.TargetNodeName + " for SetIdx(" + node.ChamberListIdx + ") RoomListIdx(" + node.ChamberRoomSet + ")");
            // if we want to select another list index, change it now.
            if (node.ChamberRoomSet is not 0)
            {
                _logger.LogDebug("We need to switch room sets first to setlistIdx " + node.ChamberRoomSet);
                unsafe
                {
                    var addon = addonInfo.Base();
                    StaticDetours.CallbackFuncFire(addon, true, 1, node.ChamberRoomSet);
                }
            }

            // wait delay before selecting the room.
            await Task.Delay(750);

            // after we have switched to the set we want, fire a callback to select the room.
            _logger.LogDebug("Selecting room " + node.ChamberListIdx);
            unsafe
            {
                var addon = addonInfo.Base();
                StaticDetours.CallbackFuncFire(addon, true, 0, node.ChamberListIdx);
            }
            LastSelectionTime = DateTime.UtcNow;
            // exit the loop.
            break;
        }
    }
}

