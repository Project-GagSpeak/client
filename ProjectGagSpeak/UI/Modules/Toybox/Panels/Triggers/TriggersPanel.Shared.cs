using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.UiToybox;
public partial class TriggersPanel
{
    private void DrawLabelWithToggle(Vector2 region, Trigger trigger, bool isEditingItem)
    {
        bool isHovered = false;
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
                CkGui.AttachToolTip(tooltip, displayAnyways: true);
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

    private void DrawPrioritySetter(Trigger trigger, bool isEditing)
    {
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero);

        var size = new Vector2(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemSpacing.X, ImGui.GetFrameHeight());
        using var c = CkRaii.FramedChild("Priority", size, CkColor.FancyHeaderContrast.Uint(), CkRaii.GetFrameThickness(), DFlags.RoundCornersAll);
        
        if (isEditing)
        {
            var priority = trigger.Priority;
            ImGui.SetNextItemWidth(c.InnerRegion.X);
            if (ImGui.DragInt("##DragID", ref priority, 1.0f, 0, 100, "%d"))
                trigger.Priority = priority;
            CkGui.AttachToolTip("Set the Priority of this Trigger.--SEP--Double-Click to edit directly.");
        }
        else
        {
            ImGuiUtil.Center(trigger.Priority.ToString());
            CkGui.AttachToolTip("The priority of this Trigger.");
        }
    }

    private void DrawDescription(Trigger trigger, bool isEditing)
    {
        var flags = isEditing ? WFlags.None : WFlags.AlwaysUseWindowPadding;
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().FramePadding);
        using var color = ImRaii.PushColor(ImGuiCol.FrameBg, 0);
        using var c = CkRaii.FramedChild("Description", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(),
            CkRaii.GetChildRoundingLarge(), 2f, DFlags.RoundCornersAll, flags);

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

    public bool DrawTriggerTypeSelector(float width, Trigger trigger, bool isEditing)
    {
        // get offset for drawn space.
        var col = isEditing ? 0 : CkColor.FancyHeaderContrast.Uint();
        var offset = (ImGui.GetContentRegionAvail().X - width) / 2;
        
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        using (var c = CkRaii.Child("TriggerKindCombo", new Vector2(width, ImGui.GetFrameHeight()), col,
            CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var curType = trigger.Type;
            if (isEditing)
            {
                if (ImGuiUtil.GenericEnumCombo("##TriggerKind", c.InnerRegion.X, curType, out var newType, t => t.ToName()))
                    if (newType != curType)
                    {
                        _manager.ChangeTriggerType(trigger, newType);
                        return true;
                    }
            }
            else
            {
                CkGui.CenterTextAligned(curType.ToName());
            }
        }
        return false;
    }

    public bool DrawTriggerActionType(float width, Trigger trigger, bool isEditing)
    {
        // get offset for drawn space.
        var col = isEditing ? CkColor.FancyHeaderContrast.Uint() : 0;
        var offset = (ImGui.GetContentRegionAvail().X - width) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        using (var c = CkRaii.Child("Sel_ActType", new Vector2(width, ImGui.GetFrameHeight()), col, CkRaii.GetChildRounding(), ImDrawFlags.RoundCornersAll))
        {
            var curType = trigger.ActionType;
            if (isEditing)
            {
                if (ImGuiUtil.GenericEnumCombo("##TriggerAction", c.InnerRegion.X,   curType, out var newType, t => t.ToName()))
                {
                    if(newType == curType)
                        return false;

                    trigger.InvokableAction = newType switch
                    {
                        InvokableActionType.TextOutput => new TextAction(),
                        InvokableActionType.Gag => new GagAction(),
                        InvokableActionType.Restriction => new RestrictionAction(),
                        InvokableActionType.Restraint => new RestraintAction(),
                        InvokableActionType.Moodle => new MoodleAction(),
                        InvokableActionType.ShockCollar => new PiShockAction(),
                        _ => new SexToyAction(),
                    };
                    return true;
                }
            }
            else
            {
                CkGui.CenterTextAligned(curType.ToName());
            }
        }
        return false;
    }
}
