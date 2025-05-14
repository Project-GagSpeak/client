using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Tutorial;
using GagSpeak.Triggers;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.UiToybox;

public partial class TriggersPanel
{
    private readonly ILogger<TriggersPanel> _logger;
    private readonly TriggerFileSelector _selector;
    private readonly TriggerManager _manager;
    private readonly TutorialService _guides;

    public TriggersPanel(
        ILogger<TriggersPanel> logger,
        TriggerFileSelector selector,
        TriggerManager manager,
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
        using (ImRaii.Child("TriggersTL", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("TriggersBL", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("TriggersTR", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawTriggerInfo(drawRegions.BotRight, curveSize);
    }

    private void DrawTriggerInfo(CkHeader.DrawRegion region, float curveSize)
    {
        DrawSelectedTrigger(region);
        var lineTopLeft = ImGui.GetItemRectMin() - new Vector2(ImGui.GetStyle().WindowPadding.X, 0);
        var lineBotRight = lineTopLeft + new Vector2(ImGui.GetStyle().WindowPadding.X, ImGui.GetItemRectSize().Y);
        ImGui.GetWindowDrawList().AddRectFilled(lineTopLeft, lineBotRight, CkGui.Color(ImGuiColors.DalamudGrey));
    }

    private void DrawSelectedTrigger(CkHeader.DrawRegion region)
    {
        var labelSize = new Vector2(region.SizeX * .7f, ImGui.GetFrameHeight());

        // Draw either the interactable label child, or the static label.
        if (_selector.Selected is null)
        {
            using var _ = CkRaii.LabelChildText(region.Size, labelSize, "No Trigger Selected!",
                ImGui.GetStyle().WindowPadding.X, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight);
        }
        else
        {
            DrawSelectedDisplay(region, labelSize);
        }
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region, Vector2 labelSize)
    {
        var IsEditorItem = _selector.Selected!.Identifier == _manager.ItemInEditor?.Identifier;
        var tooltip = $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this Trigger. "
            + "--SEP-- Right Click to cancel and exit Editor.";
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);

        using (var c = CkRaii.LabelChildAction("Sel_Trigger", region.Size, LabelDraw, ImGui.GetFrameHeight(), OnLeftClick,
            OnRightClick, tooltip, ImDrawFlags.RoundCornersRight))
        {
            using (ImRaii.Child("Trigger_Selected_Inner", c.InnerRegion with { Y = c.InnerRegion.Y - c.LabelRegion.Y }))
                DrawSelectedInner(_manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!, IsEditorItem);
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
            if (IsEditorItem) _manager.SaveChangesAndStopEditing();
            else _manager.StartEditing(_selector.Selected!);
        }

        void OnRightClick()
        {
            if (IsEditorItem) _manager.StopEditing();
            else _logger.LogWarning("Right Clicked on a Trigger that isn't in the editor.");
        }
    }

    private void DrawSelectedInner(Trigger trigger, bool isEditorItem)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var s = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(ImGui.GetStyle().ItemSpacing.X, 1));



        CkGui.Separator();
        DrawTriggerTypeSelector(trigger, isEditorItem);

        CkGui.Separator();
        DrawDescription(trigger, isEditorItem);

        DrawFooter(trigger);
    }
}
