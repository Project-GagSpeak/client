using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals.Addons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.Utils;

namespace GagSpeak.Game.Readers;
public class YesNoPrompt : BasePrompt
{
    private readonly ILogger<YesNoPrompt> _logger;
    private readonly MainConfig _config;
    public YesNoPrompt(ILogger<YesNoPrompt> logger, MainConfig config)
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
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", AddonSetup);
        }
    }

    // Run on Plugin Disable
    public override void Disable()
    {
        if(Enabled)
        {
            base.Disable();
            Svc.AddonLifecycle.UnregisterListener(AddonSetup);
        }
    }

    // Run whenever we open a prompt that is a Yes/No prompt
    protected unsafe void AddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addon = (AddonSelectYesno*)addonInfo.Base();
        // get the name
        var targetName = Svc.Targets.Target?.Name.ExtractText() ?? string.Empty;
        MainConfig.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);

        // store the label of the node
        var yesNoNodeLabelText = MainConfig.LastSeenNodeLabel = AddonBaseYesNo.GetTextLegacy(addon);
        _logger.LogDebug("Node Label Text: " + yesNoNodeLabelText, LoggerType.HardcorePrompt);

        _logger.LogDebug($"AddonSelectYesNo: text={yesNoNodeLabelText}", LoggerType.HardcorePrompt);

        // grab the nodes from our storage to see if we have a match.
        var nodes = MainConfig.GetAllNodes().OfType<TextEntryNode>();
        foreach (var node in nodes)
        {
            // if the node is not enabled or has no text, skip it.
            if (!node.Enabled || string.IsNullOrEmpty(node.SelectedOptionText))
                continue;

            // if the node requires a target but the target label is null or empty, skip.
            if (node.TargetRestricted && string.IsNullOrEmpty(node.TargetNodeLabel))
                continue;

            // if the node should be target restricted and doesnt match the target name, skip it.
            if (node.TargetRestricted && (!EntryMatchesTargetName(node, yesNoNodeLabelText)))
                continue;

            // otherwise, declare a match has landed.
            _logger.LogDebug($"AddonSelectYesNo: Node ["+node.TargetNodeName+"] Matched on ["+node.SelectedOptionText+"] for target ["+node.TargetNodeLabel+"]");
            if (node.SelectedOptionText is "Yes")
            {
                StaticDetours.CallbackFuncFire((AtkUnitBase*)addon, true, 0);
                MainConfig.LastSelectedListNode = node;
                MainConfig.LastSeenListSelection = "Yes";
                _logger.LogTrace($"YesNoPrompt: LastSeenListSelection={MainConfig.LastSeenListSelection}, LastSeenListTarget={MainConfig.LastSeenNodeLabel}");

            }
            else
            {
                StaticDetours.CallbackFuncFire((AtkUnitBase*)addon, true, 1);
                MainConfig.LastSelectedListNode = node;
                MainConfig.LastSeenListSelection = "No";
                _logger.LogTrace($"YesNoPrompt: LastSeenListSelection={MainConfig.LastSeenListSelection}, LastSeenListTarget={MainConfig.LastSeenNodeLabel}");
            }
            return;
        }
    }

    private static bool EntryMatchesTargetName(TextEntryNode node, string targetNodeLabel)
    {
        if (node.TargetNodeLabelIsRegex)
        {
            Svc.Logger.Verbose("Entry is regex: " + node.TargetNodeTextRegex);
            if (node.TargetNodeTextRegex?.IsMatch(targetNodeLabel) ?? false)
            {
                Svc.Logger.Verbose("Matched regex: " + node.TargetNodeTextRegex);
                return true;
            }
        }

        if (targetNodeLabel.Contains(node.TargetNodeLabel))
        {
            Svc.Logger.Verbose("Matched string: " + node.TargetNodeLabel);
            return true;
        }
        return false;
    }
       
}

