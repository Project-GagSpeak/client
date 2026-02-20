using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
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
        using var c = ImRaii.PushColor(ImGuiCol.Button, CkCol.CurvedHeaderFade.Uint())
            .Push(ImGuiCol.ChildBg, CkCol.CurvedHeaderFade.Uint());

        if (CkGui.IconButton(FAI.ArrowLeft))
            _manager.StopEditing();
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.CancelingChanges, WardrobeUI.LastPos, WardrobeUI.LastSize);

        // Create a child that spans the remaining region.
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(width - CkGui.IconButtonSize(FAI.ArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var curLabel = item.Label;
        if (ImGui.InputTextWithHint("##EditorNameField", "Enter Name...", ref curLabel, 48))
            item.Label = curLabel;
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EditName, WardrobeUI.LastPos, WardrobeUI.LastSize);
    }

    private void DrawEditorHeaderRight(Vector2 contentRegionAvail)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } item)
            return;

        using var group = ImRaii.Group();
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 10f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var col = ImRaii.PushColor(ImGuiCol.Button, CkCol.CurvedHeaderFade.Uint())
            .Push(ImGuiCol.FrameBg, CkCol.CurvedHeaderFade.Uint())
            .Push(ImGuiCol.ChildBg, CkCol.CurvedHeaderFade.Uint());

        var styler = ImGui.GetStyle();
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + styler.ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (contentRegionAvail.X - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 3)) / 5;

        using (ImRaii.Group())
        {
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
        }

        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EditMeta, WardrobeUI.LastPos, WardrobeUI.LastSize);

        ImGui.SameLine(0, itemSpacing);
        style.Push(ImGuiStyleVar.FrameRounding, 10f);
        if (CkGui.IconButton(FAI.Save))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes to this Restriction.");
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SavingChanges, WardrobeUI.LastPos, WardrobeUI.LastSize,
            () => _manager.SaveChangesAndStopEditing());

        ImGui.SetWindowFontScale(1f);
    }

    private void DrawEditorLeft(float width)
    {
        // Don't draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } item)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _equipDrawer.DrawAssociatedGlamour("RestrictionGlamour", item.Glamour, width);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.ItemGlamour, WardrobeUI.LastPos, WardrobeUI.LastSize);

        switch (item)
        {
            // Draw hypnotic section, if our type is a hypnotic restriction.
            case HypnoticRestriction hypnoticRestriction:
            {
                using (ImRaii.Group())
                    DrawHypnoInfo(hypnoticRestriction, width);
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.HypnoInfo, WardrobeUI.LastPos, WardrobeUI.LastSize);
                break;
            }
            // Draw blindfold section, if our type is a blindfold restriction.
            case BlindfoldRestriction blindfoldRestriction:
                DrawBlindfoldInfo(blindfoldRestriction, width);
                break;
        }

        // Determine the disabled traits based on the restriction type.
        var shownTraits = item switch
        {
            BlindfoldRestriction => Traits.Blindfolded,
            HypnoticRestriction => Traits.Blindfolded,
            _ => Traits.All
        };
        _attributeDrawer.DrawAttributesChild(item, width, 4, shownTraits);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.HardcoreTraits, WardrobeUI.LastPos, WardrobeUI.LastSize);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.Arousal, WardrobeUI.LastPos, WardrobeUI.LastSize);

        _moodleDrawer.DrawAssociatedMoodle("RestrictionMoodle", item, width, MoodleDrawer.IconSizeFramed);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.AttachedMoodle, WardrobeUI.LastPos, WardrobeUI.LastSize);
    }

    public void DrawEditorRight(float width)
    {
        if (_manager.ItemInEditor is not { } item)
            return;

        _modDrawer.DrawModPresetBox("RestrictionModPreset", item, width);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.AttachedMod, WardrobeUI.LastPos, WardrobeUI.LastSize);
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
            using (CkRaii.FramedChild("Blindfold_Preview", scaledPreview, CkCol.CurvedHeaderFade.Uint(), 0))
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
            using (CkRaii.FramedChild("Hypnotic_Preview", scaledPreview, CkCol.CurvedHeaderFade.Uint(), 0))
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
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.FirstPersonLock, WardrobeUI.LastPos, WardrobeUI.LastSize);

                // Editor Button for the effect.
                if (CkGui.IconTextButton(FAI.BookOpen, "Effect Editor"))
                    _hypnoEditor.SetGenericEffect(hypnoticItem.Properties.Effect);
                _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.EffectEditing, WardrobeUI.LastPos, WardrobeUI.LastSize,
                    () => _hypnoEditor.SetGenericEffect(hypnoticItem.Properties.Effect));
            }
        }

        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectingImage, WardrobeUI.LastPos, WardrobeUI.LastSize,
            // set the editor open but also set the path to the item as the default included spiral, hypno editor breaks with no item selected.
            () => { OpenEditor(); hypnoticItem.Properties.OverlayPath = "Hypno Spiral.png"; });

        void OpenEditor() => _thumbnails.SetThumbnailSource(_selector.Selected!.Identifier, displaySize, ImageDataType.Hypnosis);
    }
}
