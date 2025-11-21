using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class RestrictionsPanel
{
    private static TriStateBoolCheckbox TriCheckbox = new();
    private void DrawEditorHeaderLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } item)
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
        if (_manager.ItemInEditor is not { } item)
            return;

        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var col = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        //ImGui.SetWindowFontScale(1.1f);
        var styler = ImGui.GetStyle();
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + styler.ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (contentRegionAvail.X - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 3)) / 5;


        // Shift this grouped set down so it is centered on Y axis.
        //ImGui.Dummy(new Vector2(contentRegionAvail.X, ((contentRegionAvail.Y - ImGui.GetFrameHeight()) / 2) - styler.ItemSpacing.Y));

        // Handle the Meta ONLY if they are a blindfold restriction.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
        {
            ImGui.AlignTextToFramePadding();
            if (TriCheckbox.Draw("##RestrictionHelmetMeta", item.HeadgearState, out var newHelmValue))
                item.HeadgearState = newHelmValue;
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.HardHat);
            CkGui.AttachToolTip("The Forced Helmet State.--SEP--Note: conflicts priorize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("VisorMetaGroup", childGroupSize))
        {
            if (TriCheckbox.Draw("##RestrictionVisorMeta", item.VisorState, out var newVisorValue))
                item.VisorState = newVisorValue;
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Glasses);
            CkGui.AttachToolTip("The Forced Visor State.--SEP--Note: conflicts priorize ON over OFF.");
        }
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
        CkGui.AttachToolTip("Save Changes to this Restriction.");

        ImGui.SetWindowFontScale(1f);
    }

    private void DrawEditorLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } item)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _equipDrawer.DrawAssociatedGlamour("RestrictionGlamour", item.Glamour, width);

        // Draw hypnotic section, if our type is a hypnotic restriction.
        if (item is HypnoticRestriction hypnoticRestriction)
            DrawHypnoInfo(hypnoticRestriction, width);

        // Draw blindfold section, if our type is a blindfold restriction.
        if (item is BlindfoldRestriction blindfoldRestriction) 
            DrawBlindfoldInfo(blindfoldRestriction, width);

        // Determine the disabled traits based on the restriction type.
        var shownTraits = item switch
        {
            BlindfoldRestriction => Traits.Blindfolded,
            HypnoticRestriction => Traits.Blindfolded,
            _ => Traits.All
        };
        _attributeDrawer.DrawAttributesChild(item, width, 4, shownTraits);

        _moodleDrawer.DrawAssociatedMoodle("RestrictionMoodle", item, width, MoodleDrawer.IconSizeFramed);
    }

    public void DrawEditorRight(float width)
    {
        if (_manager.ItemInEditor is not { } item)
            return;

        _modDrawer.DrawModPresetBox("RestrictionModPreset", item, width);
    }

    private void DrawBlindfoldInfo(BlindfoldRestriction blindfoldItem, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var displaySize = ImGui.GetIO().DisplaySize;
        var rightWidth = CkGui.IconTextButtonSize(FAI.BookOpen, "Effect Editor");
        var leftWidth = width.RemoveWinPadX() - rightWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var scaledPreview = displaySize * (leftWidth / ImGui.GetIO().DisplaySize.X);
        var winSize = new Vector2(width, scaledPreview.Y.AddWinPadY());
        var headerTT = "Click me to select or import a Blindfold Image.";

        using (CkRaii.IconButtonHeaderChild("Blindfold Information", FAI.Edit, winSize, OpenEditor, HeaderFlags.AddPaddingToHeight, headerTT))
        {
            using (CkRaii.FramedChild("Blindfold_Preview", scaledPreview, CkColor.FancyHeaderContrast.Uint(), 0))
            {
                if (TextureManagerEx.GetMetadataPath(ImageDataType.Blindfolds, blindfoldItem.Properties.OverlayPath) is { } validImage)
                {
                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledPreview, CkStyle.HeaderRounding());
                }
            }
            CkGui.AttachToolTip("The overlay image when applied.--SEP--Right-Click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                blindfoldItem.Properties.OverlayPath = string.Empty;

            ImUtf8.SameLineInner();
            var isFirstPerson = blindfoldItem.Properties.ForceFirstPerson;
            if (ImGui.Checkbox("1st Person", ref isFirstPerson))
                blindfoldItem.Properties.ForceFirstPerson = isFirstPerson;
        }

        void OpenEditor() => _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, displaySize, ImageDataType.Blindfolds);
    }

    private void DrawHypnoInfo(HypnoticRestriction hypnoticItem, float width)
    {
        // render it if we should.
        _hypnoEditor.DrawPopup(hypnoticItem.Properties, (effect) => hypnoticItem.Properties.Effect = new HypnoticEffect(effect));


        var pos = ImGui.GetCursorScreenPos();
        var displaySize = ImGui.GetIO().DisplaySize;
        var rightWidth = CkGui.IconTextButtonSize(FAI.BookOpen, "Effect Editor");
        var leftWidth = width.RemoveWinPadX() - rightWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var scaledPreview = displaySize * (leftWidth / ImGui.GetIO().DisplaySize.X);
        var winSize = new Vector2(width, scaledPreview.Y.AddWinPadY());
        var headerTT = "Click me to select or import a Hypnosis Image.";

        using (CkRaii.IconButtonHeaderChild("Hypnotic Information", FAI.Edit, winSize, OpenEditor, HeaderFlags.AddPaddingToHeight, headerTT))
        {
            using (CkRaii.FramedChild("Hypnotic_Preview", scaledPreview, CkColor.FancyHeaderContrast.Uint(), 0))
            {
                if (TextureManagerEx.GetMetadataPath(ImageDataType.Hypnosis, hypnoticItem.Properties.OverlayPath) is { } validImage)
                {
                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledPreview, CkStyle.HeaderRounding());
                }
            }
            CkGui.AttachToolTip("The overlay image when applied.--SEP--Right-Click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                hypnoticItem.Properties.OverlayPath = string.Empty;
            ImUtf8.SameLineInner();

            using (ImRaii.Group())
            {
                var isFirstPerson = hypnoticItem.Properties.ForceFirstPerson;
                if (ImGui.Checkbox("1st Person", ref isFirstPerson))
                    hypnoticItem.Properties.ForceFirstPerson = isFirstPerson;

                // Editor Button for the effect.
                if (CkGui.IconTextButton(FAI.BookOpen, "Effect Editor"))
                    _hypnoEditor.SetGenericEffect(hypnoticItem.Properties.Effect);
            }
        }

        void OpenEditor() => _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, displaySize, ImageDataType.Hypnosis);
    }
}
