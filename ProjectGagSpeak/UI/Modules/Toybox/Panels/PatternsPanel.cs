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
        DrawSelectedDisplay(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        var item = _selector.Selected;
        var editorItem = _manager.ItemInEditor;

        var isEditing = item is not null && item.Identifier == editorItem?.Identifier;
        var isActive = item is not null && item.Identifier.Equals(_manager.ActivePattern?.Identifier);

        var label = item is null ? "No Item Selected!" : isEditing ? $"{item.Label} - (Editing)" : item.Label;
        var tooltip = item is null ? "No item selected!" : isActive ? "Pattern is Active!"
                : $"Double Click to {(editorItem is null ? "Edit" : "Save Changes to")} this Pattern.--SEP--Right Click to cancel and exit Editor.";

        using (CkRaii.ChildLabelCustomButton("##PatternSel", region.Size, ImGui.GetFrameHeight(), DrawLabel, BeginEdits, tooltip, DFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
        {
            if (item is null)
                return;

            if (isEditing && editorItem is not null)
                DrawSelectedInner(editorItem, true);
            else
                DrawSelectedInner(item, false);
        }

        void DrawLabel()
        {
            using var c = CkRaii.Child("##PatternSelLabel", new Vector2(region.SizeX * .6f, ImGui.GetFrameHeight()));
            ImGui.Spacing();
            ImGui.SameLine();
            ImUtf8.TextFrameAligned(label);
            ImGui.TextUnformatted(label);
            ImGui.SameLine(c.InnerRegion.X * .7f - (ImGui.GetFrameHeight() * 1.5f));
            CkGui.FramedIconText(isEditing ? FAI.Save : FAI.Edit);
        }

        void BeginEdits(ImGuiMouseButton b)
        {
            if (b is not ImGuiMouseButton.Left || item is null || isActive)
                return;

            if (isEditing)
                _manager.SaveChangesAndStopEditing();
            else
                _manager.StartEditing(_selector.Selected!);
        }
    }

    // This will draw out the respective information for the pattern info.
    // Displayed information can call the preview or editor versions of each field.
    private void DrawSelectedInner(Pattern pattern, bool isEditorItem)
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
