using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.MainWindow;
using OtterGui.Text;

namespace GagSpeak.Gui.Components;

public class RequestTabs : IconTextTabBar<RequestTabs.SelectedTab>
{
    public enum SelectedTab
    {
        Incoming,
        Outgoing,
    }
    public override SelectedTab TabSelection
    {
        get => base.TabSelection;
        set
        {
            _sidePanel.ClearDisplay();
            base.TabSelection = value;
        }
    }

    private readonly SidePanelService _sidePanel;
    public RequestTabs(SidePanelService sidePanel)
    {
        _sidePanel = sidePanel;

        AddDrawButton(FontAwesomeIcon.CloudDownloadAlt, "Incoming", SelectedTab.Incoming, "Requests sent by from others");
        AddDrawButton(FontAwesomeIcon.CloudUploadAlt, "Outgoing", SelectedTab.Outgoing, "Requests you've sent out");
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
