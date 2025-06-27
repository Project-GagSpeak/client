using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class RestrictionsPanel
{
    private static TriStateBoolCheckbox HelmetCheckbox = new();
    private static TriStateBoolCheckbox VisorCheckbox = new();

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
                HelmetCheckbox.Draw("##RestrictionHelmetMeta", TriStateBool.Null, out var _, true);
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.HardHat);
                CkGui.AttachToolTip("The Forced Helmet State.--SEP--Note: conflicts priorize ON over OFF.");
            }
            ImGui.SameLine(0, itemSpacing);
            using (ImRaii.Child("VisorMetaGroup", childGroupSize))
            {
                VisorCheckbox.Draw("##RestrictionVisorMeta", TriStateBool.Null, out var _, true);
                ImUtf8.SameLineInner();
                CkGui.IconText(FAI.Glasses);
                CkGui.AttachToolTip("The Forced Visor State.--SEP--Note: conflicts priorize ON over OFF.");
            }
        }
    }

    private void DrawEditorLeft(float width)
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } item)
            return;

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(5));

        _equipDrawer.DrawAssociatedGlamour("RestrictionGlamour", item.Glamour, width);

        // Draw collar information, if our type is a collar.
        if (item is CollarRestriction collarRestriction)
            DrawCollarInfo(collarRestriction, width);

        // Draw hypnotic section, if our type is a hypnotic restriction.
        if (item is HypnoticRestriction hypnoticRestriction)
            DrawHypnoInfo(hypnoticRestriction, width);

        // Draw blindfold section, if our type is a blindfold restriction.
        if (item is BlindfoldRestriction blindfoldRestriction) 
            DrawBlindfoldInfo(blindfoldRestriction, width);

        // Determine the disabled traits based on the restriction type.
        var shownTraits = item switch
        {
            CollarRestriction => Traits.None,
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
        var leftWidth = CkGui.CalcCheckboxWidth("1st Person");
        var rightWidth = width.RemoveWinPadX() - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var scaledPreview = displaySize * (rightWidth / ImGui.GetIO().DisplaySize.X);
        var winSize = new Vector2(width, scaledPreview.Y.AddWinPadY());

        using (var bfInfo = CkRaii.IconButtonHeaderChild("Blindfold Information", FAI.Edit, winSize, () => OpenEditor(ImageDataType.Blindfolds, displaySize),
            HeaderFlags.AddPaddingToHeight, "Click me to select or import a Blindfold Image."))
        {
            using (CkRaii.FramedChild("Blindfold_Preview", scaledPreview, CkColor.FancyHeaderContrast.Uint()))
            {
                if (TextureManagerEx.GetMetadataPath(ImageDataType.Blindfolds, blindfoldItem.Properties.OverlayPath) is { } validImage)
                {
                    // scale down the image to match the available widthInner.X.
                    var scaler = rightWidth / validImage.Width;
                    var scaledImage = validImage.Size * scaler;

                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledImage, CkStyle.HeaderRounding());
                }
            }
            CkGui.AttachToolTip("The overlay image when applied.--SEP--Right-Click to clear.");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                blindfoldItem.Properties.OverlayPath = string.Empty;

            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                var isFirstPerson = blindfoldItem.Properties.ForceFirstPerson;
                if (ImGui.Checkbox("1st Person", ref isFirstPerson))
                    blindfoldItem.Properties.ForceFirstPerson = isFirstPerson;

                // Maybe do a 'preview' action here or something.. idk
            }
        }
    }

    private void DrawHypnoInfo(HypnoticRestriction hypnoticItem, float width)
    {
        // render it if we should.
        _hypnoEditor.DrawPopup(_textures, hypnoticItem.Properties);

        var pos = ImGui.GetCursorScreenPos();
        var displaySize = ImGui.GetIO().DisplaySize;
        var leftWidth = CkGui.IconTextButtonSize(FAI.BookOpen, "Effect Editor");
        var rightWidth = width.RemoveWinPadX() - leftWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        var scaledPreview = displaySize * (rightWidth / ImGui.GetIO().DisplaySize.X);
        var winSize = new Vector2(width, scaledPreview.Y.AddWinPadY());

        using (var bfInfo = CkRaii.IconButtonHeaderChild("Hypnotic Information", FAI.Edit, winSize, () => OpenEditor(ImageDataType.Hypnosis, displaySize), 
            HeaderFlags.AddPaddingToHeight, "Click me to select or import a Hypnosis Image."))
        {
            using (CkRaii.FramedChild("Hypnotic_Preview", scaledPreview, CkColor.FancyHeaderContrast.Uint()))
            {
                if (TextureManagerEx.GetMetadataPath(ImageDataType.Hypnosis, hypnoticItem.Properties.OverlayPath) is { } validImage)
                {
                    // scale down the image to match the available widthInner.X.
                    var scaler = rightWidth / validImage.Width;
                    var scaledImage = validImage.Size * scaler;

                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledImage, CkStyle.HeaderRounding());
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
                    _hypnoEditor.SetHypnoEffect(hypnoticItem.Properties.Effect);
            }
        }
    }

    private void DrawCollarInfo(CollarRestriction collarItem, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var iconH = ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y;
        var winSize = new Vector2(width, iconH);
        using (CkRaii.HeaderChild("Collar Information", winSize, HeaderFlags.AddPaddingToHeight))
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

    private void OpenEditor(ImageDataType type, Vector2 size)
    {
        var metaData = new ImageMetadataGS(type, size, Guid.Empty);
        Mediator.Publish(new OpenThumbnailBrowser(metaData));
    }
}
