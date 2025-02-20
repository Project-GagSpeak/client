using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.UI.Toybox;

public partial class AlarmsPanel
{
    private readonly ILogger<AlarmsPanel> _logger;
    private readonly AlarmFileSelector _selector;
    private readonly AlarmManager _manager;
    private readonly UiSharedService _ui;
    private readonly TutorialService _guides;

    public AlarmsPanel(
        ILogger<AlarmsPanel> logger,
        AlarmFileSelector selector,
        AlarmManager manager,
        UiSharedService ui,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _ui = ui;
        _guides = guides;
    }

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        using var group = ImRaii.Group();

        // within this group, if we are editing an item, draw the editor.
        if (_manager.ActiveEditorItem is not null)
        {
            DrawEditor(remainingRegion);
            return;
        }
        else
        {
            _selector.Draw(selectorSize);
            ImGui.SameLine();
            using (ImRaii.Group())
            {
                DrawActiveItemInfo();
                DrawSelectedItemInfo();
            }
        }
    }

    private void DrawActiveItemInfo()
    {
        if (_manager.ActiveAlarms is not { } activeItems)
            return;
        ImGui.Text("Active Alarms:");
    }

    private void DrawSelectedItemInfo()
    {
        // Draws additional information about the selected item. Uses the Selector for reference.
        if (_selector.Selected is null)
            return;

        ImGui.Text("Selected Item:" + _selector.Selected.Label);

        if (ImGui.Button("Begin Editing"))
            _manager.StartEditing(_selector.Selected);
    }
}
