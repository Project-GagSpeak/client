using ImGuiNET;
using OtterGui.Log;
using OtterGui.Widgets;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.Gui;

/// <summary> A combo for selecting worlds by name, or the 'Any World' entry. </summary>
public sealed class WorldCombo : FilterComboCache<KeyValuePair<WorldId, string>>
{
    /// <summary> Always the first entry that can be selected. </summary>
    private const string AnyWorldString = "Any World";

    /// <summary> Create a new WorldCombo. </summary>
    /// <param name="worlds"> The dictionary of worlds. </param>
    /// <param name="log"> A logger. </param>
    public WorldCombo(DictWorld worlds, Logger log)
        : base(worlds.OrderBy(kvp => kvp.Value).Prepend(new KeyValuePair<WorldId, string>(WorldId.AnyWorld, AnyWorldString)), MouseWheelType.None, log)
    {
        // Start with the Any World entry selected.
        CurrentSelection    = new KeyValuePair<WorldId, string>(WorldId.AnyWorld, AnyWorldString);
        CurrentSelectionIdx = 0;
    }

    /// <summary> Just print the name. </summary>
    protected override string ToString(KeyValuePair<WorldId, string> obj)
        => obj.Value;

    /// <summary> Simple draw invoke. </summary>
    public bool Draw(float width)
        => Draw("##worldCombo", CurrentSelection.Value, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
}
