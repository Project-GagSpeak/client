using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Toybox;

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
        if (_selector.Selected is null)
        {
            using var _ = CkRaii.LabelChildText(region.Size, .7f, "No Pattern Selected!", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight);
        }
        else
            DrawSelectedDisplay(region);
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        var IsEditorItem = _selector.Selected!.Identifier == _manager.ItemInEditor?.Identifier;
        var disabled = _selector.Selected is null || _manager.ActivePattern?.Identifier == _selector.Selected.Identifier;
        var tooltip = disabled
            ? "Cannot edit an Active Pattern!"
            : $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this Pattern. "
            + "--SEP-- Right Click to cancel and exit Editor.";

        using (CkRaii.LabelChildAction("##SelPattern", region.Size, .7f, LabelDraw, ImGui.GetFrameHeight(), BeginEdits, tooltip, DFlags.RoundCornersRight))
        {
            // Show the info for either the editor item details, or the selected item details.
            DrawSelectedItemInfo(_manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!, IsEditorItem);
        }

        bool LabelDraw()
        {
            ImUtf8.TextFrameAligned(IsEditorItem ? _manager.ItemInEditor!.Label : _selector.Selected!.Label);
            ImGui.SameLine((region.SizeX * .7f) - ImGui.GetFrameHeight() * 1.5f);
            CkGui.FramedIconText(IsEditorItem ? FAI.Save : FAI.Edit);
            return !IsEditorItem;
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || disabled)
                return;

            if (IsEditorItem)
                _manager.SaveChangesAndStopEditing();
            else
                _manager.StartEditing(_selector.Selected!);
        }
    }

    // This will draw out the respective information for the pattern info.
    // Displayed information can call the preview or editor versions of each field.
    private void DrawSelectedItemInfo(Pattern pattern, bool isEditorItem)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1));

        CkGui.Separator();
        DrawDescription(pattern, isEditorItem);

        CkGui.Separator();
        DrawDurationLength(pattern, isEditorItem);

        CkGui.Separator();
        DrawPatternTimeSpans(pattern, isEditorItem);

        DrawFooter(pattern);
    }
}
