using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui.Toybox;

public partial class AlarmsPanel
{
    private readonly ILogger<AlarmsPanel> _logger;
    private readonly AlarmFileSelector _selector;
    private readonly AlarmManager _manager;
    private readonly TutorialService _guides;

    public AlarmsPanel(
        ILogger<AlarmsPanel> logger,
        AlarmFileSelector selector,
        AlarmManager manager,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _guides = guides;
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, ToyboxTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("AlarmsTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("AlarmsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("AlarmsTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawAlarmInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawAlarmInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedAlarm(region);
        var lineTopLeft = ImGui.GetItemRectMin() with { X = ImGui.GetItemRectMax().X };
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedAlarm(CkHeader.DrawRegion region)
    {
        if (_selector.Selected is null)
            return;
        using (ImRaii.Child("AlarmInfo", region.Size, true, WFlags.NoScrollbar))
        {
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(0, ImGui.GetStyle().ItemSpacing.Y));
            DrawActiveItemInfo();
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMin() + new Vector2(0, ImGui.GetStyle().ItemSpacing.Y));
            DrawSelectedItemInfo();
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
