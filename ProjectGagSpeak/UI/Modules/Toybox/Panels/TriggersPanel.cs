using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.Gui.UiToybox;

public partial class TriggersPanel
{
    private readonly ILogger<TriggersPanel> _logger;
    private readonly TriggerFileSelector _selector;
    private readonly TriggerDrawer _drawer;
    private readonly TriggerManager _manager;
    private readonly TutorialService _guides;

    public TriggersPanel(ILogger<TriggersPanel> logger, TriggerFileSelector selector,
        TriggerDrawer drawer, TriggerManager manager, TutorialService guides)
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
        // Draw either the interactable label child, or the static label.
        if (_selector.Selected is null)
        {
            using (CkRaii.ChildLabelText(region.Size, .7f, "No Trigger Selected!", ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersRight, LabelFlags.SizeIncludesHeader))
            { }
        }
        else
        {
            DrawSelectedDisplay(region);
        }
    }

    private void DrawSelectedDisplay(CkHeader.DrawRegion region)
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
                DrawSelectedHeader(new Vector2(region.SizeX * .7f, ImGui.GetFrameHeight()), item, IsEditorItem);

                ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
                DrawTabSelector();

                ImGui.SetCursorScreenPos(minPos with { Y = ImGui.GetItemRectMax().Y });
                DrawSelectedBody(item, IsEditorItem, frameBgCol);

                ImGui.SetCursorScreenPos(minPos);
                DrawLabelWithToggle(new Vector2(region.SizeX * .7f, ImGui.GetFrameHeight()), item, IsEditorItem);
            }
            catch (Bagagwa ex)
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
        ImGui.Dummy(region + new Vector2(CkStyle.FrameThickness()) - ImGui.GetStyle().ItemSpacing - ImGui.GetStyle().WindowPadding / 2);
        ImGui.SameLine(0, ImGui.GetStyle().ItemSpacing.X * 2);
        DrawPrioritySetter(trigger, isEditorItem);

        DrawDescription(trigger, isEditorItem);
    }

    private void DrawTabSelector()
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        var wdl = ImGui.GetWindowDrawList();
        var width = ImGui.GetContentRegionAvail().X;
        var stripSize = new Vector2(width, CkStyle.FrameThickness());
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

    private void DrawSelectedBody(Trigger trigger, bool isEditorItem, uint searchBg)
    {
        using var bodyChild = CkRaii.Child("Sel_Body", ImGui.GetContentRegionAvail(), wFlags: WFlags.AlwaysUseWindowPadding);

        ImGui.Spacing();

        // get offset for drawn space.
        var comboW = bodyChild.InnerRegion.X * .65f;
        var offset = (ImGui.GetContentRegionAvail().X - comboW) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        // Draw it based on the tab.
        if (_selectedTab is TriggerTab.Detection)
        {
            CkGuiUtils.FramedEditDisplay("##DetectionType", comboW, isEditorItem, trigger.Type.ToName(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##DetectionType", comboW, trigger.Type, out var newVal, _ => _.ToName(), flags: CFlags.None))
                {
                    if (newVal != trigger.Type)
                    {
                        _logger.LogInformation($"Trigger Type changed from {trigger.Type} to {newVal}");
                        _manager.ChangeTriggerType(trigger, newVal);
                    }
                }
            });

            CkGui.SeparatorSpaced(CkColor.FancyHeaderContrast.Uint());
            
            // re-aquire the trigger item.
            _drawer.DrawDetectionInfo(trigger, isEditorItem, searchBg);
            DrawFooter(trigger);
        }
        else
        {
            CkGuiUtils.FramedEditDisplay("##ActionType", comboW, isEditorItem, trigger.ActionType.ToName(), _ =>
            {
                if (CkGuiUtils.EnumCombo("##ActionType", comboW, trigger.ActionType, out var newVal, _ => _.ToName(), flags: CFlags.None))
                {
                    if (newVal != trigger.ActionType)
                    {
                        trigger.InvokableAction = newVal switch
                        {
                            InvokableActionType.TextOutput => new TextAction(),
                            InvokableActionType.Gag => new GagAction(),
                            InvokableActionType.Restriction => new RestrictionAction(),
                            InvokableActionType.Restraint => new RestraintAction(),
                            InvokableActionType.Moodle => new MoodleAction(),
                            InvokableActionType.ShockCollar => new PiShockAction(),
                            _ => new SexToyAction(),
                        };
                    }
                }
            });

            CkGui.SeparatorSpaced(CkColor.FancyHeaderContrast.Uint());

            _drawer.DrawActionInfo(trigger, isEditorItem, searchBg);
            DrawFooter(trigger);
        }
    }

    private void DrawPrioritySetter(Trigger trigger, bool isEditing)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        var size = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X, ImGui.GetFrameHeight());
        using (var c = CkRaii.FramedChild("Priority", size, CkColor.FancyHeaderContrast.Uint(), 0, CkStyle.ThinThickness(), 0, DFlags.RoundCornersAll))
        {
            if (isEditing)
            {
                var priority = trigger.Priority;
                ImGui.SetNextItemWidth(c.InnerRegion.X);
                if (ImGui.DragInt("##DragID", ref priority, 1.0f, 0, 100, "%d"))
                    trigger.Priority = priority;
            }
            else
            {
                ImGuiUtil.Center(trigger.Priority.ToString());
            }
        }
        CkGui.AttachToolTip(isEditing
            ? "Set the Priority of this Trigger.--SEP--Double-Click to edit directly."
            : "The priority of this Trigger.");
    }

    private void DrawDescription(Trigger trigger, bool isEditing)
    {
        var flags = isEditing ? WFlags.None : WFlags.AlwaysUseWindowPadding;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().FramePadding);
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var c = CkRaii.FramedChild("Description", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(), 0,
            CkStyle.ChildRoundingLarge(), CkStyle.ThinThickness(), DFlags.RoundCornersAll, flags);

        // Display the correct text field based on the editing state.
        if (isEditing)
        {
            var description = trigger.Description;
            if (ImGui.InputTextMultiline("##DescriptionField", ref description, 200, c.InnerRegion))
                trigger.Description = description;
        }
        else
            ImGui.TextWrapped(trigger.Description);

        // Draw a hint if no text is present.
        if (trigger.Description.IsNullOrWhitespace())
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding,
                0xFFBBBBBB, "Input a description in the space provided...");
    }

    private void DrawLabelWithToggle(Vector2 region, Trigger trigger, bool isEditingItem)
    {
        var isHovered = false;
        var tooltip = $"Double Click to {(_manager.ItemInEditor is null ? "Edit" : "Save Changes to")} this Trigger."
            + "--SEP-- Right Click to cancel and exit Editor.";

        using (CkRaii.Child("Sel_Label_Piece", new Vector2(region.X, ImGui.GetFrameHeight())))
        {
            // Advance forward by window padding.
            ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X / 2);
            CkGui.BooleanToColoredIcon(trigger.Enabled, false);
            if (ImGui.IsItemHovered() && ImGui.IsItemClicked())
                trigger.Enabled = !trigger.Enabled;
            CkGui.AttachToolTip((trigger.Enabled ? "Enable" : "Disable") + " this Trigger.");

            // Now draw out the label and icon.
            ImGui.SameLine();
            var remainingSpace = ImGui.GetContentRegionAvail();
            using (ImRaii.Group())
            {
                ImUtf8.TextFrameAligned(trigger.Label);
                ImGui.SameLine(remainingSpace.X - ImGui.GetFrameHeight() * 1.5f);
                CkGui.FramedIconText(isEditingItem ? FAI.Save : FAI.Edit);
            }
            var minInner = ImGui.GetItemRectMin();
            isHovered = ImGui.IsMouseHoveringRect(minInner, minInner + remainingSpace);

            // Handle Interaction.
            if (isHovered)
                CkGui.AttachToolTip(tooltip);
            if (isHovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                if (isEditingItem) _manager.SaveChangesAndStopEditing();
                else _manager.StartEditing(_selector.Selected!);
            }
            if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                if (isEditingItem) _manager.StopEditing();
                else _logger.LogWarning("Right Clicked on a Trigger that isn't in the editor.");
            }
        }
        var min = ImGui.GetItemRectMin();
        var col = isHovered ? CkColor.VibrantPinkHovered.Uint() : CkColor.VibrantPink.Uint();
        ImGui.GetWindowDrawList().AddRectFilled(min, min + region + new Vector2(ImGui.GetStyle().WindowPadding.X / 2),
            CkColor.ElementSplit.Uint(), ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersBottomRight);
        ImGui.GetWindowDrawList().AddRectFilled(min, min + region, col, ImGui.GetFrameHeight(), ImDrawFlags.RoundCornersBottomRight);
    }

    private void DrawFooter(Trigger trigger)
    {
        // get the remaining region.
        var regionLeftover = ImGui.GetContentRegionAvail().Y;

        // Determine how to space the footer.
        if (regionLeftover < (CkGui.GetSeparatorHeight() + ImGui.GetFrameHeight()))
            CkGui.Separator();
        else
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + regionLeftover - ImGui.GetFrameHeight());

        // Draw it.
        ImUtf8.TextFrameAligned("ID:");
        ImGui.SameLine();
        ImUtf8.TextFrameAligned(trigger.Identifier.ToString());
    }
}
