using Dalamud.Interface.Utility;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;
using System.Drawing;
using GagspeakAPI.Data;
using OtterGui;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Utils;
using GagspeakAPI.Data.Permissions;
using OtterGui.Text.EndObjects;
using GagSpeak.CkCommons.Widgets;
using FFXIVClientStructs.FFXIV.Common.Lua;

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
        HypnoEffectEditorPopUp(hypnoticItem.Properties);

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
                if (_textures.GetImageMetadataPath(ImageDataType.Hypnosis, hypnoticItem.Properties.OverlayPath) is { } validImage)
                {
                    // scale down the image to match the available widthInner.X.
                    var scaler = rightWidth / validImage.Width;
                    var scaledImage = validImage.Size * scaler;

                    pos = ImGui.GetCursorScreenPos();
                    ImGui.GetWindowDrawList().AddDalamudImageRounded(validImage, pos, scaledImage, CkStyle.HeaderRounding());
                }
            }
            CkGui.AttachToolTip("This is the image that the hypnotic overlay on your screen while active.");

            ImUtf8.SameLineInner();

            using (ImRaii.Group())
            {
                var isFirstPerson = hypnoticItem.Properties.ForceFirstPerson;
                if (ImGui.Checkbox("1st Person", ref isFirstPerson))
                    hypnoticItem.Properties.ForceFirstPerson = isFirstPerson;

                // Editor Button for the effect.
                if (CkGui.IconTextButton(FAI.BookOpen, "Effect Editor"))
                {
                    Entry = hypnoticItem.Properties.Effect;
                }

                // Maybe do a 'preview' action here or something.. idk
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


    private static ImGuiColorEditFlags ColorPickerFlags = ImGuiColorEditFlags.DisplayHex
        | ImGuiColorEditFlags.AlphaPreview | ImGuiColorEditFlags.AlphaBar;

    private static HypnoticEffect? Entry = null;
    private static TagCollection HypnoEffectPhrases = new();
    private static bool Open = false;
    private void HypnoEffectEditorPopUp(HypnoticOverlay overlay)
    {
        if (Entry is null)
            return;
        if (!ImGui.IsPopupOpen("###HypnoEditModal"))
        {
            Open = true;
            ImGui.OpenPopup("###HypnoEditModal");
        }

        if (ImGui.BeginPopupModal($"Effect Editor###HypnoEditModal", ref Open, ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (ImGui.BeginTable("HypnoEffectEditTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 110);
                ImGui.TableSetupColumn("Input", ImGuiTableColumnFlags.WidthFixed, 290);

                // Spin Speed
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Image Spin Speed");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(290);
                ImGui.DragFloat("##SpinSpeed", ref Entry.SpinSpeed, 0.01f, 0f, 5f, "%.2fx Speed");

                // Image Tint Color
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Image Tint Color");
                ImGui.TableNextColumn();
                Vector4 tintVec = ColorHelpers.RgbaUintToVector4(Entry.TintColor);
                if (ImGui.ColorPicker4("##TintCol", ref tintVec, ColorPickerFlags))
                    Entry.TintColor = ColorHelpers.RgbaVector4ToUint(tintVec);

                // Text Color
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Text Color");
                ImGui.TableNextColumn();
                Vector4 textColVec = ColorHelpers.RgbaUintToVector4(Entry.TextColor);
                if (ImGui.ColorPicker4("##TextColor", ref textColVec, ColorPickerFlags))
                    Entry.TextColor = ColorHelpers.RgbaVector4ToUint(textColVec);

                // Text Mode
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Text Display Order");
                ImGui.TableNextColumn();
                var selectedAttributes = (uint)Entry.Attributes;
                var currentMode = Entry.Attributes & HypnoAttributes.ModeMask;

                if (ImGui.RadioButton("Sequential", currentMode == HypnoAttributes.TextIsSequential))
                    Entry.Attributes = (Entry.Attributes & ~HypnoAttributes.ModeMask) | HypnoAttributes.TextIsSequential;
                CkGui.AttachToolTip("The text is displayed in the order displayed below.");

                ImGui.SameLine();
                if (ImGui.RadioButton("Random", currentMode == HypnoAttributes.TextIsRandom))
                    Entry.Attributes = (Entry.Attributes & ~HypnoAttributes.ModeMask) | HypnoAttributes.TextIsRandom;
                CkGui.AttachToolTip("The text is displayed in a random order each time it is cycled.");

                // Text Cycle Speed
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Text Cycle Speed");
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(290);
                ImGui.DragFloat("##TextCycleSpeed", ref Entry.TextCycleSpeed, 0.05f, 0f, 10f, "%.2f");
                CkGui.AttachToolTip("How frequently the text cycles through the display words.");

                // Attributes
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImUtf8.TextFrameAligned("Attributes");
                ImGui.TableNextColumn();

                using (var t = ImRaii.Table("###AttributesTable", 2))
                {
                    if (!t) return;

                    foreach (var attribute in Enum.GetValues<HypnoAttributes>().Skip(2))
                    {
                        if (ImGui.CheckboxFlags($"{attribute}", ref selectedAttributes, (uint)attribute))
                            Entry.Attributes ^= attribute;
                        ImGui.TableNextColumn();
                    }
                }

                ImGui.EndTable();
            }
            var size = ImGui.GetItemRectSize();

            // Display Words
            using (var c = CkRaii.HeaderChild("Display Text Phrases", new Vector2(size.X, CkStyle.GetFrameRowsHeight(3).AddWinPadY()), HeaderFlags.AddPaddingToHeight))
            {
                using (CkRaii.FramedChildPaddedW("Display Phrases", c.InnerRegion.X, CkStyle.GetFrameRowsHeight(3), CkColor.FancyHeaderContrast.Uint(), DFlags.RoundCornersAll))
                    if (HypnoEffectPhrases.DrawTagsEditor("##EffectPhrases", Entry.DisplayWords, out var newDisplayWords))
                        Entry.DisplayWords = newDisplayWords.ToArray();
            }

            CkGui.SeparatorSpaced(width: size.X, col: CkColor.LushPinkLine.Uint());

            CkGui.SetCursorXtoCenter(CkGui.IconTextButtonSize(FAI.Save, "Save and Close"));
            if (CkGui.IconTextButton(FAI.Save, "Save and Close"))
            {
                Open = false;
                overlay.Effect = Entry;
            }

            ImGui.EndPopup();
        }

        if (!Open)
            Entry = null;
    }
}
