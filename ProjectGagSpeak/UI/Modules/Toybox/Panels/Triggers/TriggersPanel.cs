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

namespace GagSpeak.CkCommons.Gui.UiToybox;

public partial class TriggersPanel
{
    private readonly ILogger<TriggersPanel> _logger;
    private readonly TriggerFileSelector _selector;
    private readonly TriggerDrawer _drawer;
    private readonly TriggerManager _manager;
    private readonly TutorialService _guides;

    public TriggersPanel(
        ILogger<TriggersPanel> logger,
        TriggerFileSelector selector,
        TriggerDrawer drawer,
        TriggerManager manager,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _drawer = drawer;
        _manager = manager;
        _guides = guides;
    }

    private TriggerTab _selectedTab = TriggerTab.Detection;

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
        var item = _manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!;
        var IsEditorItem = item.Identifier == _manager.ItemInEditor?.Identifier;

        // Need to keep original frameBgCol for the search BG.
        var frameBgCol = ImGui.GetColorU32(ImGuiCol.FrameBg);

        // Styles shared throughout all draw settings.
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 10f);
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());

        // Begin the child constraint.
        using (var c = CkRaii.Child("Sel_Outer", region.Size, CkColor.FancyHeader.Uint(), ImGui.GetFrameHeight(), DFlags.RoundCornersRight))
        {
            try
            {
                var minPos = ImGui.GetItemRectMin();
                DrawSelectedHeader(labelSize, item, IsEditorItem);

                ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
                DrawTabSelector();

                ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
                DrawSelectedBody(item, IsEditorItem, frameBgCol);

                ImGui.SetCursorScreenPos(minPos);
                DrawLabelWithToggle(labelSize, item, IsEditorItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while drawing the selected trigger.");
            }
        }
    }

    private void DrawSelectedHeader(Vector2 region, Trigger trigger, bool isEditorItem)
    {
        var descH = ImGui.GetTextLineHeightWithSpacing() * 2;
        var height = descH.AddWinPadY() + ImGui.GetFrameHeightWithSpacing();
        var bgCol = CkGui.Color(new Vector4(0.25f, 0.2f, 0.2f, 0.4f));
        using var _ = CkRaii.ChildPaddedW("Sel_Header", ImGui.GetContentRegionAvail().X, height, bgCol, ImGui.GetFrameHeight(), DFlags.RoundCornersTopRight);

        // Dummy is a placeholder for the label area drawn afterward.
        ImGui.Dummy(region + new Vector2(CkRaii.GetFrameThickness()) - ImGui.GetStyle().ItemSpacing - ImGui.GetStyle().WindowPadding / 2);
        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
        DrawPrioritySetter(trigger, isEditorItem);

        DrawDescription(trigger, isEditorItem);
    }

    private void DrawTabSelector()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        var wdl = ImGui.GetWindowDrawList();
        var width = ImGui.GetContentRegionAvail().X;
        var stripSize = new Vector2(width, CkRaii.GetFrameThickness());
        var tabSize = new Vector2(width / 2, ImGui.GetFrameHeight());
        var textYOffset = (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) / 2;

        // Top Strip.
        ImGui.Dummy(stripSize);
        wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementSplit.Uint());
        // Left Button.
        if (ImGui.InvisibleButton("tab_left", tabSize))
            _selectedTab = TriggerTab.Detection;

        DrawButtonText("Detection", TriggerTab.Detection);

        // Right Button.
        ImGui.SameLine();
        if (ImGui.InvisibleButton("tab_right", tabSize))
            _selectedTab = TriggerTab.Action;

        DrawButtonText("Applied Action", TriggerTab.Action);

        // Bot Strip.
        ImGui.Dummy(stripSize);
        wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementSplit.Uint());

        void DrawButtonText(string text, TriggerTab tab)
        {
            var min = ImGui.GetItemRectMin();
            var col = ImGui.IsItemHovered()
                ? CkColor.VibrantPinkHovered 
                : (_selectedTab == tab ? CkColor.VibrantPink : CkColor.FancyHeaderContrast);
            wdl.AddRectFilled(min, ImGui.GetItemRectMax(), col.Uint());
            var textPos = min + new Vector2((tabSize.X - ImGui.CalcTextSize(text).X) / 2, textYOffset);
            wdl.AddText(textPos, ImGui.GetColorU32(ImGuiCol.Text), text);
        }
    }

    private void DrawSelectedBody(Trigger trigger, bool isEditorItem, uint? customSearchBg)
    {
        using var bodyChild = CkRaii.Child("Sel_Body", ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding);

        if (_selectedTab is TriggerTab.Detection)
        {
            ImGui.Spacing();
            DrawTriggerTypeSelector(bodyChild.InnerRegion.X * .65f, trigger, isEditorItem);
            CkGui.SeparatorSpaced(col: CkColor.FancyHeaderContrast.Uint());
            // re-aquire the trigger item.
            var triggerItem = _manager.ItemInEditor is { } editorItem ? editorItem : _selector.Selected!;
            _drawer.DrawDetectionInfo(triggerItem, isEditorItem, customSearchBg);
            
            DrawFooter(triggerItem);
        }
        else
        {
            ImGui.Spacing();
            DrawTriggerActionType(bodyChild.InnerRegion.X * .65f, trigger, isEditorItem);
            CkGui.SeparatorSpaced(col: CkColor.FancyHeaderContrast.Uint());
            _drawer.DrawActionInfo(trigger, isEditorItem);

            DrawFooter(trigger);
        }
    }
}
