using Dalamud.Interface.Colors;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Text;
using SixLabors.ImageSharp;
using System.Drawing;

namespace GagSpeak.UI.Wardrobe;

// it might be wise to move the selector draw into the panel so we have more control over the editor covering both halves.
public partial class RestraintsPanel : DisposableMediatorSubscriberBase
{
    private readonly ILogger<RestraintsPanel> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly ActiveItemsDrawer _activeDrawer;
    private readonly RestraintManager _manager;
    private readonly PairManager _pairs;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public bool IsEditing => _manager.ActiveEditorItem != null;
    public RestraintsPanel(
        ILogger<RestraintsPanel> logger, 
        GagspeakMediator mediator,
        RestraintSetFileSelector selector,
        ActiveItemsDrawer activeDrawer,
        RestraintManager manager,
        RestraintEditorInfo editorInfo,
        RestraintEditorLayers editorLayers,
        RestraintEditorEquipment editorEquipment,
        RestraintEditorModsMoodles editorModsMoodles,
        PairManager pairs,
        CosmeticService cosmetics,
        TutorialService guides) : base(logger, mediator)
    {
        _logger = logger;
        _selector = selector;
        _activeDrawer = activeDrawer;
        _manager = manager;
        _pairs = pairs;
        _cosmetics = cosmetics;
        _guides = guides;

        // create some dummy tabs to see if it even works.
        EditorTabs = new ICkTab[]
        {
            editorInfo,
            editorEquipment,
            editorLayers,
            editorModsMoodles,
        };

        Mediator.Subscribe<TooltipSetItemToEditorMessage>(this, (msg) =>
        {
            if (_manager.ActiveEditorItem != null && _manager.ActiveEditorItem.RestraintSlots[msg.Slot] is RestraintSlotBasic basicSlot)
            {
                basicSlot.Glamour.GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on edited set " + "[" + _manager.ActiveEditorItem.Label + "]", LoggerType.Restraints);
            }
        });

        Mediator.Subscribe<ThumbnailImageSelected>(this, (msg) =>
        {
            if (msg.MetaData.Kind is not ImageDataType.Restraints)
                return;

            if (manager.Storage.TryGetRestraint(msg.MetaData.SourceId, out var match))
            {
                _selector.SelectByValue(match);
                manager.UpdateThumbnail(match, msg.Name);
            }
        });
    }

    private static OptionalBoolCheckbox HelmetCheckbox = new();
    private static OptionalBoolCheckbox VisorCheckbox = new();
    private static OptionalBoolCheckbox WeaponCheckbox = new();
    public ICkTab[] EditorTabs;

    /// <summary> All Content in here is grouped. Can draw either editor or overview left panel. </summary>
    public void DrawEditorContents(DrawerHelpers.HeaderVec topRegion, DrawerHelpers.HeaderVec botRegion)
    {
        ImGui.SetCursorScreenPos(topRegion.Pos);
        using (ImRaii.Child("RestraintEditorTop", topRegion.Size))
            DrawEditorHeader();

        ImGui.SetCursorScreenPos(botRegion.Pos);
        using (ImRaii.Child("RestraintEditorBot", botRegion.Size, false, WFlags.AlwaysUseWindowPadding))
        {
            // Draw out the tab bar, and the items respective contents.
            using (CkComponents.TabBarChild("AllowanceTabBars", WFlags.AlwaysUseWindowPadding, out var selected, EditorTabs))
                selected?.DrawContents(botRegion.SizeX);
        }
    }

    public void DrawContents(DrawerHelpers.CkHeaderDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.Topleft.Pos);
        using (ImRaii.Child("RestraintsTopLeft", drawRegions.Topleft.Size))
            _selector.DrawFilterRow(drawRegions.Topleft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestraintsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestraintsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        var style = ImGui.GetStyle();
        var selectedH = ImGui.GetFrameHeight() * 2 + ImGui.GetTextLineHeight() * 4 + style.ItemSpacing.Y + style.WindowPadding.Y * 2;
        var selectedSize = new Vector2(drawRegions.BotRight.SizeX, selectedH);
        var linePos = drawRegions.BotRight.Pos - new Vector2(style.WindowPadding.X, 0);
        var linePosEnd = linePos + new Vector2(style.WindowPadding.X, selectedSize.Y);
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkColor.FancyHeader.Uint());
        ImGui.GetWindowDrawList().AddRectFilled(linePos, linePosEnd, CkGui.Color(ImGuiColors.DalamudGrey));

        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        using (ImRaii.Child("RestraintsBottomRight", drawRegions.BotRight.Size))
        {
            DrawSelectedItemInfo(selectedSize, curveSize);
            DrawActiveItemInfo();
        }
    }

    private void DrawSelectedItemInfo(Vector2 region, float rounding)
    {
        var ItemSelected = _selector.Selected is not null;

        var style = ImGui.GetStyle();
        using (ImRaii.Child("SelectedItemOuter", region))
        {
            var imgSize = new Vector2(region.Y / 1.2f, region.Y) - style.WindowPadding * 2;
            var imgDrawPos = ImGui.GetCursorScreenPos() + new Vector2(region.X - imgSize.X - style.WindowPadding.X, style.WindowPadding.Y);
            // Draw the left items.
            if (ItemSelected)
                SelectedRestraintInternal();

            // move to the cursor position and attempt to draw it.
            var hoveringImg = ImGui.IsMouseHoveringRect(imgDrawPos, imgDrawPos + imgSize);
            var imgCol = hoveringImg ? CkColor.FancyHeader.Uint() : CkColor.FancyHeaderContrast.Uint();
            ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, imgCol, rounding);
            ImGui.SetCursorScreenPos(imgDrawPos);
            if (ItemSelected)
            {
                _activeDrawer.DrawImage(_selector.Selected!, imgSize, rounding);
                if (ImGui.IsMouseHoveringRect(imgDrawPos, imgDrawPos + imgSize))
                {
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        var metaData = new ImageMetadataGS(ImageDataType.Restraints, new Vector2(120, 120f * 1.2f), _selector.Selected!.Identifier);
                        Mediator.Publish(new OpenThumbnailBrowser(metaData));
                    }
                    CkGui.AttachToolTip("The Thumbnail for this item.--SEP--Double Click to change the image.", displayAnyways: true);
                }
            }
        }
        // draw the actual design element.
        var minPos = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        var wdl = ImGui.GetWindowDrawList();
        // base background right right rounded corners.
        wdl.AddRectFilled(minPos, minPos + size, CkColor.FancyHeader.Uint(), rounding, ImDrawFlags.RoundCornersRight);
        // Draw the 3 label rects.
        var descPosMax = minPos + new Vector2(size.X * .6f + style.ItemInnerSpacing.Y, ImGui.GetFrameHeight() + ImGui.GetTextLineHeight() * 4 + style.ItemSpacing.Y);
        var labelWrapMax = minPos + new Vector2(size.X * .6f, ImGui.GetFrameHeight()) + style.ItemInnerSpacing / 2;
        var labelMax = minPos + new Vector2(size.X * .6f, ImGui.GetFrameHeight());
        // Description Rect.
        wdl.AddRectFilled(minPos, descPosMax, CkColor.FancyHeaderContrast.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);
        // Label Wrap Rect.
        wdl.AddRectFilled(minPos, labelWrapMax, CkColor.SideButton.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);
        // Label Rect.
        var hoveringTitle = ImGui.IsMouseHoveringRect(minPos, labelMax);
        var col = hoveringTitle ? CkColor.VibrantPinkHovered.Uint() : CkColor.VibrantPink.Uint();
        wdl.AddRectFilled(minPos, labelMax, col, rounding, ImDrawFlags.RoundCornersBottomRight);

        if (hoveringTitle)
        {
            if (ItemSelected && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _manager.StartEditing(_selector.Selected!);
            CkGui.AttachToolTip("Double Click me to begin editing!", displayAnyways: true);
        }
    }

    private void SelectedRestraintInternal()
    {
        using var group = ImRaii.Group();

        // Label draw.
        ImUtf8.SameLineInner();
        ImUtf8.TextFrameAligned(_selector.Selected!.Label);

        // Description draw on text wrapped for the width * .6f.
        var descSize = new Vector2(ImGui.GetContentRegionAvail().X * .6f, ImGui.GetTextLineHeight() * 4);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemInnerSpacing.X);
        using (ImRaii.Child("RestraintPreviewDescription", descSize))
            CkGui.TextWrapped(_selector.Selected.Description);

        // Draw the button icon row centered with proper spacing.
        var trueCol = 0xFFFFFFFF;
        var falseCol = CkColor.FancyHeaderContrast.Uint();
        var helmCol = _selector.Selected.HeadgearState == OptionalBool.Null ? falseCol : trueCol;
        var visorCol = _selector.Selected.VisorState == OptionalBool.Null ? falseCol : trueCol;
        var weaponCol = _selector.Selected.WeaponState == OptionalBool.Null ? falseCol : trueCol;
        var redrawCol = _selector.Selected.DoRedraw ? falseCol : trueCol;
        var layersCol = _selector.Selected.Layers.Count > 0 ? trueCol : falseCol;
        var moodleCol = _selector.Selected.RestraintMoodles.Count > 0 ? trueCol : falseCol;
        var modsCol = _selector.Selected.RestraintMods.Count > 0 ? trueCol : falseCol;
        var traitsCol = _selector.Selected.Traits is not Traits.None || _selector.Selected.Stimulation is not Stimulation.None ? trueCol : falseCol;

        // Get the remaining Y content region, and set our cursorPos to be center of it.
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() + new Vector2(0, ImGui.GetStyle().WindowPadding.Y));
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.HatCowboy, helmCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Glasses, visorCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Shield, weaponCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Repeat, redrawCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.LayerGroup, layersCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.TheaterMasks, moodleCol);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.PersonRays, modsCol);
        }
    }

    private void DrawActiveItemInfo()
    {
        if(_manager.ActiveRestraintData is not { } activeData)
            return;

        using var _ = ImRaii.Child("ActiveRestraintItems", ImGui.GetContentRegionAvail(), false, WFlags.AlwaysUseWindowPadding);


        // get the size of the child for this centered header.
        var styler = ImGui.GetStyle();
        var activeSetHeight = ImGui.GetFrameHeightWithSpacing() * 5 + styler.WindowPadding.Y * 2;
        var activeSetSize = new Vector2(ImGui.GetContentRegionAvail().X, activeSetHeight);
        if (_manager.ActiveRestraint is null)
        {
            using var inactive = CkComponents.CenterHeaderChild("InactiveRestraint", "No Restraint Set Is Active", activeSetSize);
        }
        else
        {
            using (CkComponents.ButtonHeaderChild("ActiveRestraintSet", "Active Restraint Set", activeSetSize, CkComponents.HeaderRounding, FAI.Minus, TryRemoveRestraint))
            {
                // Draw the restraint set equipment, grouped.
                _activeDrawer.DrawRestraintSlots(_manager.ActiveRestraint, new Vector2(ImGui.GetFrameHeightWithSpacing()));

                // i wont really be able to debug this until we have proper server interaction lol.

                // Sameline, beside it, draw the padlock management state.

                // Under this create a secondary group.
                // On the left of this group, show the attached moodles of the base.
                // Then the attached traits of the base.
                // On the right, draw the thumbnail.
            }
        }
    }

    private void TryRemoveRestraint()
    {
        if (_manager.ActiveRestraintData is not { } activeSet)
            return;

        // If the set is locked, log error and return.
        if (activeSet.IsLocked() || !activeSet.CanRemove())
        {
            Logger.LogError("Set is Locked, or you cannot remove. Aborting!", LoggerType.Restraints);
            return;
        }

        // Attempt to remove it.
        _logger.LogDebug("Attempting to remove active restraint set", LoggerType.Restraints);
        Mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Removed, new CharaActiveRestraint()));
    }

    private void DrawEditorHeader()
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ActiveEditorItem is not { } setInEdit)
            return;

        using var s = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 12f)
            .Push(ImGuiStyleVar.ChildRounding, 10f);
        using var c = ImRaii.PushColor(ImGuiCol.Button, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint())
            .Push(ImGuiCol.ChildBg, CkColor.FancyHeaderContrast.Uint());

        if (CkGui.IconButton(FAI.ArrowLeft))
            _manager.StopEditing();

        // Create a child that spans the remaining region.
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / 2 - CkGui.IconButtonSize(FAI.ArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X);
        var curLabel = setInEdit.Label;
        if (ImGui.InputTextWithHint("##EditorNameField", "Enter Name...", ref curLabel, 48))
            setInEdit.Label = curLabel;

        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        var remainingWidth = ImGui.GetContentRegionAvail().X;

        // now we must draw out the right side.
        var childGroupSize = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemInnerSpacing.X, ImGui.GetFrameHeight());
        var itemSpacing = (remainingWidth - CkGui.IconButtonSize(FAI.Save).X - (childGroupSize.X * 4)) / 6;

        // Cast a child group for the drawer.
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + itemSpacing);
        using (ImRaii.Child("HelmetMetaGroup", childGroupSize))
        {
            ImGui.AlignTextToFramePadding();
            if (HelmetCheckbox.Draw("##MetaHelmet", setInEdit.HeadgearState, out var newHelmValue))
                setInEdit.HeadgearState = newHelmValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.HatCowboySide);
            CkGui.AttachToolTip("The locked helmet state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("VisorMetaGroup", childGroupSize))
        {
            if (VisorCheckbox.Draw("##MetaVisor", setInEdit.VisorState, out var newVisorValue))
                setInEdit.VisorState = newVisorValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Glasses);
            CkGui.AttachToolTip("The locked visor state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("WeaponMetaGroup", childGroupSize))
        {
            if (WeaponCheckbox.Draw("##MetaWeapon", setInEdit.WeaponState, out var newWeaponValue))
                setInEdit.WeaponState = newWeaponValue;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Explosion);
            CkGui.AttachToolTip("The locked weapon state while active.--SEP--Note: conflicts prioritize ON over OFF.");
        }

        ImGui.SameLine(0, itemSpacing);
        using (ImRaii.Child("RedrawMetaGroup", childGroupSize))
        {
            var doRedraw = setInEdit.DoRedraw;
            if (ImGui.Checkbox("##MetaRedraw", ref doRedraw))
                setInEdit.DoRedraw = doRedraw;

            ImUtf8.SameLineInner();
            CkGui.IconText(FAI.Repeat);
            CkGui.AttachToolTip("If you redraw after application.");
        }

        // beside this, enhances the font scale to 1.5x, draw the save icon, then restore the font scale.
        ImGui.SameLine(0, itemSpacing);
        if (CkGui.IconButton(FAI.Save))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes to this Restraint Set.");

    }
}
