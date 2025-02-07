using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public class RoomSelectPrompt : BasePrompt
{
    private readonly ILogger<RoomSelectPrompt> _logger;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ITargetManager _targets;

    private DateTime LastSelectionTime = DateTime.MinValue;

    internal RoomSelectPrompt(ILogger<RoomSelectPrompt> logger, ClientConfigurationManager clientConfigs, 
        IAddonLifecycle addonLifecycle, ITargetManager targetManager)
    {
        _logger = logger;
        _clientConfigs = clientConfigs;
        _addonLifecycle = addonLifecycle;
        _targets = targetManager;
    }

    // Run on plugin Enable
    public override void Enable()
    {
        base.Enable();
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "HousingSelectRoom", AddonSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "HousingSelectRoom", SetEntry);
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "MansionSelectRoom", AddonSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, "MansionSelectRoom", SetEntry);

    }

    private unsafe void SetEntry(AddonEvent type, AddonArgs args)
    {
        try
        {
            AtkUnitBase* addon = args.Base();
            // get the name
            var target = _targets.Target;
            var targetName = target != null ? target.Name.ExtractText() : string.Empty;
            _clientConfigs.LastSeenNodeName = targetName;
            // Output all the text nodes in a concatinated string
            _clientConfigs.LastSeenNodeLabel = AddonBaseRoom.ToText(addon, 8);
        }
        catch { }
    }

    // Run on Plugin Disable
    public override void Disable()
    {
        base.Disable();
        _addonLifecycle.UnregisterListener(AddonSetup);
        _addonLifecycle.UnregisterListener(SetEntry);
    }

    protected async void AddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        // return if less than 5 seconds from last interaction to avoid infinite loop spam.
        await Task.Delay(750);

        // get the name
        var target = _targets.Target;
        var targetName = target != null ? target.Name.ExtractText() : string.Empty;
        _clientConfigs.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);

        // Try and locate if we have a match.
        var nodes = _clientConfigs.GetAllNodes().OfType<ChambersTextNode>();
        foreach (var node in nodes)
        {
            // If the node does not have the chamber room set, do not process it.
            if (!node.Enabled || node.ChamberRoomSet < 0)
                continue;

            // If we are only doing it on a spesific node and the names dont match, skip it.
            if (node.TargetRestricted && !_clientConfigs.LastSeenNodeName.Contains(node.TargetNodeName))
                continue;

            // If we have a match, fire the event.
            _logger.LogDebug("RoomSelectPrompt: Matched on " + node.TargetNodeName + " for SetIdx(" + node.ChamberListIdx + ") RoomListIdx(" + node.ChamberRoomSet + ")");
            // if we want to select another list index, change it now.
            if (node.ChamberRoomSet is not 0)
            {
                _logger.LogDebug("We need to switch room sets first to setlistIdx " + node.ChamberRoomSet);
                unsafe
                {
                    AtkUnitBase* addon = addonInfo.Base();
                    ForcedStayCallback.Fire(addon, true, 1, node.ChamberRoomSet);
                }
            }

            // wait delay before selecting the room.
            await Task.Delay(750);

            // after we have switched to the set we want, fire a callback to select the room.
            _logger.LogDebug("Selecting room " + node.ChamberListIdx);
            unsafe
            {
                AtkUnitBase* addon = addonInfo.Base();
                ForcedStayCallback.Fire(addon, true, 0, node.ChamberListIdx);
            }
            LastSelectionTime = DateTime.UtcNow;
            // exit the loop.
            break;
        }
    }
}

