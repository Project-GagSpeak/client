using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

public class TriggerPartTabs : IconTextTabBar<TriggerPartTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Detection,
        Reaction,
    }

    public TriggerPartTabs()
    {
        AddDrawButton(FontAwesomeIcon.AssistiveListeningSystems, "Detection", SelectedTab.Detection, "How this trigger is detected");
        AddDrawButton(FontAwesomeIcon.Exclamation, "Reaction", SelectedTab.Reaction, "How to react after detection");
    }

    public override void Draw(float availableWidth)
    {
        if (_tabButtons.Count == 0)
            return;

        using var color = ImRaii.PushColor(ImGuiCol.Button, 0xFF000000);
        var spacing = ImUtf8.ItemSpacing;
        var buttonW = (availableWidth - (spacing.X * (_tabButtons.Count - 1))) / _tabButtons.Count;
        var buttonSize = new Vector2(buttonW, ImUtf8.FrameHeight);
        var wdl = ImGui.GetWindowDrawList();

        // Draw out the buttons, then newline after.
        foreach (var tab in _tabButtons)
            DrawTabButton(tab, buttonSize, spacing, wdl);

        // advance to the new line and dispose of the button color.
        ImGui.NewLine();
        ImGuiHelpers.ScaledDummy(3f);
    }
}
