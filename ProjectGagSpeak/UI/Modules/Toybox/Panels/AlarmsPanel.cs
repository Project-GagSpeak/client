using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Toybox;

public partial class AlarmsPanel
{
    private readonly ILogger<AlarmsPanel> _logger;
    private readonly AlarmFileSelector _selector;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _manager;
    private readonly TutorialService _guides;

    private PatternCombo _patternCombo;
    public AlarmsPanel(
        ILogger<AlarmsPanel> logger,
        AlarmFileSelector selector,
        PatternManager patterns,
        AlarmManager manager,
        FavoritesManager favorites,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _patterns = patterns;
        _manager = manager;
        _guides = guides;

        _patternCombo = new PatternCombo(logger, favorites, () => [
            ..patterns.Storage.OrderByDescending(p => favorites._favoritePatterns.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
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
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedAlarm(CkHeader.DrawRegion region)
    {
        var labelSize = new Vector2(region.SizeX * .7f, ImGui.GetFrameHeight());

        // Draw either the interactable label child, or the static label.
        if (_selector.Selected is null)
            DrawSelectedStatic(region.Size, labelSize);
        else
            DrawSelectedDisplay(region, labelSize);
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region, Vector2 labelSize)
    {
        var IsEditorItem = _selector.Selected!.Identifier == _manager.ItemInEditor?.Identifier;
        var tooltip = $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this Alarm. "
            + "--SEP-- Right Click to cancel changes and edit Editor.";

        using (var c = CkRaii.LabelChildAction("Selected", region.Size, LabelDraw, ImGui.GetFrameHeight(),
            OnLeftClick, OnRightClick, tooltip, ImDrawFlags.RoundCornersRight))
        {
            // Show the info for either the editor item details, or the selected item details.
            DrawSelectedItemInfo(_manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!, IsEditorItem);
        }

        void LabelDraw()
        {
            ImGui.Dummy(labelSize);
            ImGui.SetCursorScreenPos(region.Pos + new Vector2(ImGui.GetStyle().WindowPadding.X, 0));
            ImUtf8.TextFrameAligned(IsEditorItem ? _manager.ItemInEditor!.Label : _selector.Selected!.Label);
            ImGui.SameLine(labelSize.X - ImGui.GetFrameHeight() * 1.5f);
            CkGui.FramedIconText(IsEditorItem ? FAI.Save : FAI.Edit);
        }

        void OnLeftClick()
        {
            if (IsEditorItem)
                _manager.SaveChangesAndStopEditing();
            else
                _manager.StartEditing(_selector.Selected!);
        }

        void OnRightClick()
        {
            if (IsEditorItem)
                _manager.StopEditing();
            else
                _logger.LogWarning("Right Click on a Alarm that is not in the editor.");
        }
    }

    private void DrawSelectedStatic(Vector2 region, Vector2 labelRegion)
    {
        using var _ = CkRaii.LabelChildText(region, labelRegion, "No Alarm Selected!",
            ImGui.GetStyle().WindowPadding.X, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight);
    }

    // This will draw out the respective information for the Alarm info.
    // Displayed information can call the preview or editor versions of each field.
    private void DrawSelectedItemInfo(Alarm alarm, bool isEditorItem)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1));
        // DrawLabel(alarm, isEditorItem);

        CkGui.Separator();
        DrawAlarmTime(alarm, isEditorItem);

        CkGui.Separator();
        DrawPatternSelection(alarm, isEditorItem);

        CkGui.Separator();
        DrawPatternTimeSpans(alarm, isEditorItem);

        CkGui.Separator();
        DrawAlarmFrequency(alarm, isEditorItem);

        CkGui.Separator();
        DrawFooter(alarm);
    }
}
