using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Toybox;

public partial class PatternsPanel
{
    private readonly ILogger<PatternsPanel> _logger;
    private readonly PatternFileSelector _selector;
    private readonly PatternManager _manager;
    private readonly TutorialService _guides;

    public PatternsPanel(
        ILogger<PatternsPanel> logger,
        PatternFileSelector selector,
        PatternManager manager,
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
        using (ImRaii.Child("PatternsTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("PatternsBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("PatternsTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawPatternInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawPatternInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedPattern(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedPattern(CkHeader.DrawRegion region)
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
        var tooltip = $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this pattern. "
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
                _logger.LogWarning("Right Click on a pattern that is not in the editor.");
        }
    }

    private void DrawSelectedStatic(Vector2 region, Vector2 labelRegion)
    {
        using var _ = CkRaii.LabelChildText(region, labelRegion, "No Pattern Selected!",
            ImGui.GetStyle().WindowPadding.X, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight);
    }

    // This will draw out the respective information for the pattern info.
    // Displayed information can call the preview or editor versions of each field.
    private void DrawSelectedItemInfo(Pattern pattern, bool isEditorItem)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1));
        //DrawLabel(pattern, isEditorItem);

        CkGui.Separator();
        DrawDescription(pattern, isEditorItem);

        CkGui.Separator();
        DrawDurationLength(pattern, isEditorItem);

        CkGui.Separator();
        DrawPatternTimeSpans(pattern, isEditorItem);

        CkGui.Separator();
        DrawFooter(pattern);
    }
}
