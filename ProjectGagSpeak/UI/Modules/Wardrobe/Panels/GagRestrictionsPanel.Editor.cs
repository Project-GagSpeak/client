using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Util;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class GagRestrictionsPanel
{
    private CustomizeProfileCombo _profileCombo;
    private static TriStateBoolCheckbox HelmetCheckbox = new();
    private static TriStateBoolCheckbox VisorCheckbox = new();
    private void DrawEditorHeaderLeft(Vector2 region)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } gagItem)
            return;

        using var group = ImRaii.Group();
        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        // precalc sizes to make remaining drawing easier.
        var spacing = ImGui.GetStyle().ItemSpacing.X;
        var buttonSize = CkGui.IconButtonSize(FAI.ArrowLeft);

        if (CkGui.IconButton(FAI.ArrowLeft))
            _manager.StopEditing();

        // Create a child that spans the remaining region.
        ImGui.SameLine();
        using (ImRaii.Child("EditorNameField", new Vector2(region.X - buttonSize.X - spacing, region.Y)))
        {
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemSpacing.X);
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted("Editing: " + gagItem.GagType.GagName());
        }
    }

    private void DrawEditorHeaderRight(Vector2 region)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } gagItem)
            return;

        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var col = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());
        ImGui.SetWindowFontScale(1.25f);
        var styler = ImGui.GetStyle();
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + styler.ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (region.X - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 3)) / 4;

        // Shift this grouped set down so it is centered on Y axis.
        ImGui.Dummy(new Vector2(region.X, ((region.Y - ImGui.GetFrameHeight()) / 2) - styler.ItemSpacing.Y));
        
        // Cast a child group for the drawer.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
        {
            ImGui.AlignTextToFramePadding();
            if (HelmetCheckbox.Draw("##GagHelmetMeta", gagItem.HeadgearState, out var newHelmValue))
                gagItem.HeadgearState = newHelmValue;
            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.HardHat);
            CkGui.AttachToolTip("The Forced Helmet State when wearing this Gag.--SEP--Note: conflicts priorize ON over OFF.");
        }
        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("VisorMetaGroup", childGroupSize))
        {
            if (VisorCheckbox.Draw("##GagVisorMeta", gagItem.VisorState, out var newVisorValue))
                gagItem.VisorState = newVisorValue;
            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Glasses);
            CkGui.AttachToolTip("The Forced Visor State when wearing this Gag.--SEP--Note: conflicts priorize ON over OFF.");
        }
        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("RedrawMetaGroup", childGroupSize))
        {
            var doRedraw = gagItem.DoRedraw;
            if (ImGui.Checkbox("##GagRedrawMeta", ref doRedraw))
                gagItem.DoRedraw = doRedraw;
            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Repeat);
            CkGui.AttachToolTip("If you redraw after application.");
        }

        // beside this, enhances the font scale to 1.5x, draw the save icon, then restore the font scale.
        ImGui.SameLine(0, itemSpacing);
        style.Push(ImGuiStyleVar.FrameRounding, 10f);
        if (CkGui.IconButton(FAI.Save))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes to this Gag Restriction.");

        ImGui.SetWindowFontScale(1f);
    }

    private void DrawEditorLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } gagItem)
            return;

        var defaultChildBg = ImGui.GetColorU32(ImGuiCol.ChildBg);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _equipDrawer.DrawAssociatedGlamour("GagGlamour", gagItem.Glamour, width);
        _attributeDrawer.DrawAttributesChild(gagItem, width, 4, Traits.Gagged | Traits.Blindfolded);

        DrawCustomizeProfile(gagItem, width);
        
        _moodleDrawer.DrawAssociatedMoodle("AssociatedGagMoodle", gagItem, width);
    }

    public void DrawEditorRight(float width)
    {
        if (_manager.ItemInEditor is not { } gagItem)
            return;

        _modDrawer.DrawModPresetBox("GagModPreset", gagItem, width);
    }

    private void DrawCustomizeProfile(GarblerRestriction gagItem, float width)
    {
        // construct a child object here.
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        using var child = CkRaii.HeaderChild("Customize+ Preset", new Vector2(width, ImGui.GetFrameHeight()), HeaderFlags.AddPaddingToHeight);

        var change = _profileCombo.Draw("Customize Profile", gagItem.CPlusProfile.ProfileGuid, child.InnerRegion.X * .6f, child.InnerRegion.X * .8f);
        if (change && !gagItem.CPlusProfile.Equals(_profileCombo.Current))
        {
            _logger.LogTrace($"Profile Guid changed to {_profileCombo.Current.ProfileGuid} " +
                $"[{_profileCombo.Current.ProfileName}] from {gagItem.CPlusProfile.ProfileName}");
            gagItem.CPlusProfile = _profileCombo.Current;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Profile Guid item was cleared. and is now Guid.Empty");
            gagItem.CPlusProfile = CustomizeProfile.Empty;
        }

        ImGui.SameLine();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        // Get the length of a inputInt box.
        var priority = gagItem.CPlusProfile.Priority;
        if (ImGui.InputInt("##PriorityAdjuster", ref priority))
            gagItem.CPlusProfile.SetPriority(priority);
    }
}
