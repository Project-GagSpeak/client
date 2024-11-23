using Dalamud.Utility;
using ImGuiNET;
using OtterGui.Log;
using OtterGui.Widgets;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Gui;

/// <summary> A combo to select an NPC of a specific type. </summary>
/// <param name="label"> The label for the combo. </param>
/// <param name="names"> The dictionary of NPC names to use. </param>
/// <param name="log"> A logger. </param>
public sealed class NpcCombo(string label, NameDictionary names, Logger log)
    // On creation, group NPCs by their name so that every name represents all IDs that share it.
    // Then sort by that name using the comparer that prioritizes alphanumerics before special symbols.
    : FilterComboCache<(string Name, NpcId[] Ids)>(() => names
        .GroupBy(kvp => kvp.Value)
        .Select(g => (g.Key, g
            .Select(g => g.Key)
            .ToArray()))
        .OrderBy(g => g.Key, Comparer)
        .ToList(), MouseWheelType.None, log)
{
    /// <summary> Just print the name. </summary>
    protected override string ToString((string Name, NpcId[] Ids) obj)
        => obj.Name;

    /// <summary> Draw a selectable of the name and display a tooltip on hover showing all the associated NPC IDs. </summary>
    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var (name, ids) = Items[globalIdx];
        var ret = ImGui.Selectable(name, selected);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(string.Join('\n', ids.Select(i => i.ToString())));

        return ret;
    }

    /// <summary> Invoke normal Draw. </summary>
    public bool Draw(float width)
        => Draw(label, CurrentSelection.Name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());

    /// <summary> Compare strings in a way that letters and numbers are sorted before any special symbols. </summary>
    private class NameComparer : IComparer<string>
    {
        /// <inheritdoc/>
        public int Compare(string? x, string? y)
        {
            if (x.IsNullOrEmpty() || y.IsNullOrEmpty())
                return StringComparer.OrdinalIgnoreCase.Compare(x, y);

            return (char.IsAsciiLetterOrDigit(x[0]), char.IsAsciiLetterOrDigit(y[0])) switch
            {
                (true, false) => -1,
                (false, true) => 1,
                _             => StringComparer.OrdinalIgnoreCase.Compare(x, y),
            };
        }
    }

    /// <summary> The comparer we use. </summary>
    private static readonly NameComparer Comparer = new();
}
