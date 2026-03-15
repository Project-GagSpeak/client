using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;

namespace GagSpeak.Gui.Components;

public class PublicationTabs : IconTabBar<PublicationTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Patterns,
        Loci,
    }

    public PublicationTabs()
    {
        AddDrawButton(FontAwesomeIcon.WaveSquare, SelectedTab.Patterns, "Pattern Publisher");
        AddDrawButton(FontAwesomeIcon.TheaterMasks, SelectedTab.Loci, "Loci Publisher");
    }

    public override void Draw(float width)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new(0, 0, 0, 0)));
        var spacing = ImGui.GetStyle().ItemSpacing;
        var buttonX = (width - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonY = CkGui.IconButtonSize(FontAwesomeIcon.Pause).Y;
        var buttonSize = new Vector2(buttonX, buttonY);
        var drawList = ImGui.GetWindowDrawList();

        ImGuiHelpers.ScaledDummy(spacing.Y / 2f);

        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, drawList);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        color.Dispose();

        ImGuiHelpers.ScaledDummy(3f);
        ImGui.Separator();
    }

}
