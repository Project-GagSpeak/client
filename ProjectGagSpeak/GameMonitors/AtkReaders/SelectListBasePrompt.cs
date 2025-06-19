using Dalamud.Game.ClientState.Objects;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerClient;

namespace GagSpeak.Game.Readers;
public abstract class SetupSelectListPrompt : BasePrompt
{
    protected readonly ILogger _logger;
    protected readonly IAddonLifecycle _addonLifecycle;
    protected readonly ITargetManager _targets;

    internal SetupSelectListPrompt(
        ILogger logger,
        IAddonLifecycle addonLifecycle,
        ITargetManager targets)
    {
        _logger = logger;
        _addonLifecycle = addonLifecycle;
        _targets = targets;
    }

    protected unsafe int? GetMatchingIndex(string[] entries, string nodeLabel)
    {
        var nodes = MainConfig.GetAllNodes().OfType<TextEntryNode>();
        foreach (var node in nodes)
        {
            if (!node.Enabled || string.IsNullOrEmpty(node.SelectedOptionText))
                continue;

            var (matched, index) = EntryMatchesTexts(node, entries);
            if (!matched)
                continue;

            // If the node is target restricted and TargetNodeLabel is provided, use it for matching.
            // Otherwise, fall back to matching based on the node name.
            if (node.TargetRestricted)
            {
                // Use TargetNodeLabel if it's not null or empty
                if (!string.IsNullOrEmpty(node.TargetNodeLabel))
                {
                    if (EntryMatchesTargetName(node, nodeLabel))
                    {
                        _logger.LogDebug($"SelectListPrompt: Matched on {node.TargetNodeLabel} for option ({node.SelectedOptionText})", LoggerType.HardcorePrompt);
                        MainConfig.LastSelectedListNode = node;
                        return index;
                    }
                }
                // Fallback to using LastSeenNodeName if TargetNodeLabel is not provided
                else if (!string.IsNullOrEmpty(MainConfig.LastSeenNodeName))
                {
                    if (string.Equals(node.TargetNodeName, MainConfig.LastSeenNodeName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug($"SelectListPrompt: Matched on node name for option ({node.SelectedOptionText})", LoggerType.HardcorePrompt);
                        MainConfig.LastSelectedListNode = node;
                        return index;
                    }
                }
            }
            else
            {
                _logger.LogDebug("SelectListPrompt: Matched on " + node.TargetNodeLabel + " for option (" + node.SelectedOptionText + ")", LoggerType.HardcorePrompt);
                MainConfig.LastSelectedListNode = node;
                return index;
            }
        }
        return null;
    }

    private static (bool Matched, int Index) EntryMatchesTexts(TextEntryNode node, string?[] texts)
    {
        for (var i = 0; i < texts.Length; i++)
        {
            var text = texts[i];
            if (text == null)
                continue;

            if (EntryMatchesText(node, text))
                return (true, i);
        }

        return (false, -1);
    }

    private static bool EntryMatchesText(TextEntryNode node, string text)
        => node.IsTextRegex && (node.TextRegex?.IsMatch(text) ?? false)
        || !node.IsTextRegex && text.Contains(node.SelectedOptionText);

    private static bool EntryMatchesTargetName(TextEntryNode node, string targetNodeLabel)
        => node.TargetNodeLabelIsRegex && (node.TargetNodeTextRegex?.IsMatch(targetNodeLabel) ?? false)
        || !node.TargetNodeLabelIsRegex && targetNodeLabel.Contains(node.TargetNodeLabel);
}
