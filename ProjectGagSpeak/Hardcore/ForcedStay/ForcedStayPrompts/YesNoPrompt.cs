using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;

namespace GagSpeak.Hardcore.ForcedStay;
public class YesNoPrompt : BasePrompt
{
    private readonly ILogger<YesNoPrompt> _logger;
    private readonly GagspeakConfigService _config;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ITargetManager _targets;


    internal YesNoPrompt(ILogger<YesNoPrompt> logger,
        GagspeakConfigService config, IAddonLifecycle addonLifecycle, ITargetManager targetManager)
    {
        _logger = logger;
        _config = config;
        _addonLifecycle = addonLifecycle;
        _targets = targetManager;
    }

    // Run on plugin Enable
    public override void Enable()
    {
        if(!Enabled)
        {
            base.Enable();
            _addonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", AddonSetup);
        }
    }

    // Run on Plugin Disable
    public override void Disable()
    {
        if(Enabled)
        {
            base.Disable();
            _addonLifecycle.UnregisterListener(AddonSetup);
        }
    }

    // Run whenever we open a prompt that is a Yes/No prompt
    protected unsafe void AddonSetup(AddonEvent eventType, AddonArgs addonInfo)
    {
        var addon = (AddonSelectYesno*)addonInfo.Base();
        // get the name
        var target = _targets.Target;
        var targetName = target != null ? target.Name.ExtractText() : string.Empty;
        _config.LastSeenNodeName = targetName;
        _logger.LogDebug("Node Name: " + targetName);

        // store the label of the node
        var yesNoNodeLabelText = _config.LastSeenNodeLabel = AddonBaseYesNo.GetTextLegacy(addon);
        _logger.LogDebug("Node Label Text: " + yesNoNodeLabelText, LoggerType.HardcorePrompt);

        _logger.LogDebug($"AddonSelectYesNo: text={yesNoNodeLabelText}", LoggerType.HardcorePrompt);

        // grab the nodes from our storage to see if we have a match.
        var nodes = _config.GetAllNodes().OfType<TextEntryNode>();
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
                ForcedStayCallback.Fire((AtkUnitBase*)addon, true, 0);
                _config.LastSelectedListNode = node;
                _config.LastSeenListSelection = "Yes";
                _logger.LogTrace($"YesNoPrompt: LastSeenListSelection={_config.LastSeenListSelection}, LastSeenListTarget={_config.LastSeenNodeLabel}");

            }
            else
            {
                ForcedStayCallback.Fire((AtkUnitBase*)addon, true, 1);
                _config.LastSelectedListNode = node;
                _config.LastSeenListSelection = "No";
                _logger.LogTrace($"YesNoPrompt: LastSeenListSelection={_config.LastSeenListSelection}, LastSeenListTarget={_config.LastSeenNodeLabel}");
            }
            return;
        }
    }

    private static bool EntryMatchesTargetName(TextEntryNode node, string targetNodeLabel)
    {
        if (node.TargetNodeLabelIsRegex)
        {
            StaticLogger.Logger.LogTrace("Entry is regex: " + node.TargetNodeTextRegex);
            if (node.TargetNodeTextRegex?.IsMatch(targetNodeLabel) ?? false)
            {
                StaticLogger.Logger.LogTrace("Matched regex: " + node.TargetNodeTextRegex);
                return true;
            }
        }

        if (targetNodeLabel.Contains(node.TargetNodeLabel))
        {
            StaticLogger.Logger.LogTrace("Matched string: " + node.TargetNodeLabel);
            return true;
        }
        return false;
    }
       
}

