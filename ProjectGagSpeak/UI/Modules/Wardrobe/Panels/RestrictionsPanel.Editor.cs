using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Wardrobe;
public partial class RestrictionsPanel
{
    private static OptionalBoolCheckbox HelmetCheckbox = new();
    private static OptionalBoolCheckbox VisorCheckbox = new();

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
        if (_manager.ItemInEditor is not { } item)
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
        var leftWidth = CkGui.CalcCheckboxWidth("Force 1st Person");
        var rightWidth = width.RemoveWinPadX() - leftWidth - ImGui.GetFrameHeightWithSpacing();
        var scaledPreview = displaySize * (rightWidth / ImGui.GetIO().DisplaySize.X);
        var winSize = new Vector2(width, scaledPreview.Y);
        using (var bfInfo = CkRaii.HeaderChild("Blindfold Information", winSize, HeaderFlags.AddPaddingToHeight))
        {
            // We will want to group together the first few elements together for the blindfold type & 1st PoV option.
            using (ImRaii.Group())
            {
                var isFirstPerson = blindfoldItem.Properties.ForceFirstPerson;
                if (ImGui.Checkbox("Force 1st Person", ref isFirstPerson))
                    blindfoldItem.Properties.ForceFirstPerson = isFirstPerson;

                var rightSize = CkGui.IconTextButtonSize(FAI.PenSquare, "Edit Image");

                if (CkGui.IconTextButton(FAI.PenSquare, "Edit Image", leftWidth))
                {
                    var metaData = new ImageMetadataGS(ImageDataType.Blindfolds, displaySize, Guid.Empty);
                    Mediator.Publish(new OpenThumbnailBrowser(metaData));
                }
            }

            ImGui.SameLine(0, ImGui.GetFrameHeightWithSpacing());
            using (CkRaii.FramedChild("Blindfold_Preview", scaledPreview, CkColor.FancyHeaderContrast.Uint()))
            {
                if (_textures.GetImageMetadataPath(ImageDataType.Blindfolds, blindfoldItem.Properties.OverlayPath) is { } validImage)
                {
                    // scale down the image to match the available widthInner.X.
                    var scaler = rightWidth / validImage.Width;
                    var scaledImage = validImage.Size * scaler;

                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledImage, CkStyle.HeaderRounding());
                }
            }
            CkGui.AttachToolTip("This is the image that the blindfold will overlay on your screen while active.");
        }
    }

    private void DrawCollarInfo(CollarRestriction collarItem, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var iconH = ImGui.GetFrameHeight() * 2 + style.ItemSpacing.Y;
        var winSize = new Vector2(width, iconH);
        using (CkRaii.HeaderChild("Collar Information", winSize))
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
