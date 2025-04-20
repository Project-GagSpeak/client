using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerState.Models;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.UI.Wardrobe;
public partial class RestrictionsPanel
{
    private static IconCheckboxStimulation StimulationIconCheckbox = new(FAI.VolumeUp, FAI.VolumeDown, FAI.VolumeOff, FAI.VolumeMute, CkGui.Color(ImGuiColors.DalamudGrey), CkColor.FancyHeaderContrast.Uint());
    private static OptionalBoolCheckbox HelmetCheckbox = new();
    private static OptionalBoolCheckbox VisorCheckbox = new();

    private void DrawEditorHeaderLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ActiveEditorItem is not { } item)
            return;

        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        if (CkGui.IconButton(FAI.ArrowLeft))
            _manager.StopEditing();

        // Create a child that spans the remaining region.
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(width - CkGui.IconButtonSize(FAI.ArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var curLabel = item.Label;
        if(ImGui.InputTextWithHint("##EditorNameField", "Enter Name...", ref curLabel, 48))
            item.Label = curLabel;
    }

    private void DrawEditorHeaderRight(Vector2 contentRegionAvail)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ActiveEditorItem is not { } item)
            return;

        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var col = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        ImGui.SetWindowFontScale(1.25f);
        var styler = ImGui.GetStyle();
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + styler.ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (contentRegionAvail.X - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 3)) / 5;


        // Shift this grouped set down so it is centered on Y axis.
        ImGui.Dummy(new Vector2(contentRegionAvail.X, ((contentRegionAvail.Y - ImGui.GetFrameHeight()) / 2) - styler.ItemSpacing.Y));

        // Handle the Meta ONLY if they are a blindfold restriction.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        DrawMetaGroups();

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("RedrawMetaGroup", childGroupSize))
        {
            var doRedraw = item.DoRedraw;
            if (ImGui.Checkbox("##GagRedrawMeta", ref doRedraw))
                item.DoRedraw = doRedraw;
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

        ///////////////////////////////////////
        void DrawMetaGroups()
        {
            if (item is BlindfoldRestriction blindfoldRestriction)
                DrawInteractableMeta(blindfoldRestriction);
            else
                DrawDisabledMeta();
        }

        void DrawInteractableMeta(BlindfoldRestriction blindfold)
        {
            using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
            {
                ImGui.AlignTextToFramePadding();
                if (HelmetCheckbox.Draw("##RestrictionHelmetMeta", blindfold.HeadgearState, out var newHelmValue))
                    blindfold.HeadgearState = newHelmValue;
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.HardHat);
                CkGui.AttachToolTip("The Forced Helmet State.--SEP--Note: conflicts priorize ON over OFF.");
            }
            ImGui.SameLine(0, itemSpacing);
            using (ImRaii.Child("VisorMetaGroup", childGroupSize))
            {
                if (VisorCheckbox.Draw("##RestrictionVisorMeta", blindfold.VisorState, out var newVisorValue))
                    blindfold.VisorState = newVisorValue;
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.Glasses);
                CkGui.AttachToolTip("The Forced Visor State.--SEP--Note: conflicts priorize ON over OFF.");
            }
        }

        void DrawDisabledMeta()
        {
            using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
            {
                HelmetCheckbox.Draw("##RestrictionHelmetMeta", OptionalBool.Null, out var _, true);
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.HardHat);
                CkGui.AttachToolTip("The Forced Helmet State.--SEP--Note: conflicts priorize ON over OFF.");
            }
            ImGui.SameLine(0, itemSpacing);
            using (ImRaii.Child("VisorMetaGroup", childGroupSize))
            {
                VisorCheckbox.Draw("##RestrictionVisorMeta", OptionalBool.Null, out var _, true);
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.Glasses);
                CkGui.AttachToolTip("The Forced Visor State.--SEP--Note: conflicts priorize ON over OFF.");
            }
        }
    }

    private void DrawEditorLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ActiveEditorItem is not { } item)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _equipDrawer.DrawAssociatedGlamour("RestrictionGlamour", item.Glamour, width);

        // Draw blindfold section, if our type is a blindfold restriction.
        if (item is BlindfoldRestriction blindfoldRestriction)
            DrawBlindfoldInfo(blindfoldRestriction, width);

        // Draw collar information, if our type is a collar.
        if (item is CollarRestriction collarRestriction)
            DrawCollarInfo(collarRestriction, width);

        // Determine the disabled traits based on the restriction type.
        Traits disabled = item switch
        {
            BlindfoldRestriction => Traits.ArmsRestrained | Traits.LegsRestrained | Traits.Gagged | Traits.Immobile | Traits.Weighty,
            CollarRestriction => Traits.ArmsRestrained | Traits.LegsRestrained | Traits.Gagged | Traits.Blindfolded | Traits.Immobile | Traits.Weighty,
            _ => Traits.Gagged
        };
        _traitsDrawer.DrawTwoRowTraits(item, width, disabled, false);

        _moodleDrawer.DrawAssociatedMoodle("RestrictionMoodle", item, width);
    }

    public void DrawEditorRight(float width)
    {
        if (_manager.ActiveEditorItem is not { } item)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _modDrawer.DrawModPresetBox("RestrictionModPreset", item, width);
    }

    private void DrawBlindfoldInfo(BlindfoldRestriction blindfoldItem, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var iconH = ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y;
        var winSize = new Vector2(width, iconH);
        using (CkComponents.CenterHeaderChild("BlindfoldItem" + blindfoldItem.Label, "Blindfold Information", winSize, WFlags.AlwaysUseWindowPadding))
        {
            var widthInner = ImGui.GetContentRegionAvail().X;

            // We will want to group together the first few elements together for the blindfold type & 1st PoV option.
            using (ImRaii.Group())
            {
                DrawTypeAndOptionPref();
                DrawTexturePathLocation();
                CkGui.AttachToolTip("You can set a custom path here for a blindfold texture." +
                    "--SEP--Any custom textures must reside within the BlindfoldTextures folder of the ProjectGagSpeak Config Folder.");
            }

            void DrawTypeAndOptionPref()
            {
                if (CkGuiUtils.EnumCombo("##BlindfoldType", widthInner * .6f, blindfoldItem.Kind, out BlindfoldType newValue))
                {
                    blindfoldItem.Kind = newValue;
                    if (newValue is not BlindfoldType.CustomPath)
                        blindfoldItem.CustomPath = string.Empty;
                }
                ImGui.SameLine();
                var isFirstPerson = blindfoldItem.ForceFirstPerson;
                if (ImGui.Checkbox("Force 1st Person", ref isFirstPerson))
                    blindfoldItem.ForceFirstPerson = isFirstPerson;
            }

            void DrawTexturePathLocation()
            {
                // Draw an input textbox with hint that spans the entire width. Disable if kind is not custom.
                using var disabled = ImRaii.Disabled(blindfoldItem.Kind != BlindfoldType.CustomPath);
                var customPath = blindfoldItem.CustomPath;
                ImGui.SetNextItemWidth(widthInner);
                if (ImGui.InputTextWithHint("##BlindfoldTexturePath", "Enter Custom Path...", ref customPath, 128))
                    blindfoldItem.CustomPath = blindfoldItem.CustomPath;
            }
        }
    }

    private void DrawCollarInfo(CollarRestriction collarItem, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var iconH = ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y;
        var winSize = new Vector2(width, iconH);
        using (CkComponents.CenterHeaderChild("CollarItem" + collarItem.Label, "Collar Information", winSize, WFlags.AlwaysUseWindowPadding))
        {
            var widthInner = ImGui.GetContentRegionAvail().X;
            var collarOwner = collarItem.OwnerUID;
            var engravedWriting = collarItem.CollarWriting;

            ImGui.SetNextItemWidth(widthInner);
            if (ImGui.InputTextWithHint("##CollarOwner", "Enter Owner Kinkster UID...", ref collarOwner, 128))
                collarItem.OwnerUID = collarOwner;

            ImGui.SetNextItemWidth(widthInner);
            if (ImGui.InputTextWithHint("##CollarWriting", "Enter Engraved Writing...", ref engravedWriting, 128))
                collarItem.CollarWriting = engravedWriting;
        }
    }
}
