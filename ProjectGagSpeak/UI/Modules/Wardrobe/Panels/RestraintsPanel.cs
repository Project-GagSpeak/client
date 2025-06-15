using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

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
    public bool IsEditing => _manager.ItemInEditor != null;
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
        EditorTabs = new IFancyTab[]
        {
            editorInfo,
            editorEquipment,
            editorLayers,
            editorModsMoodles,
        };

        Mediator.Subscribe<TooltipSetItemToEditorMessage>(this, (msg) =>
        {
            if (_manager.ItemInEditor?.RestraintSlots[msg.Slot] is RestraintSlotBasic basicSlot)
            {
                basicSlot.Glamour.GameItem = msg.Item;
                Logger.LogDebug($"Set [" + msg.Slot + "] to [" + msg.Item.Name + "] on edited set " + "[" + _manager.ItemInEditor.Label + "]", LoggerType.Restraints);
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
    public IFancyTab[] EditorTabs;

    /// <summary> All Content in here is grouped. Can draw either editor or overview left panel. </summary>
    public void DrawEditorContents(CkHeader.DrawRegion topRegion, CkHeader.DrawRegion botRegion)
    {
        ImGui.SetCursorScreenPos(topRegion.Pos);
        using (ImRaii.Child("RestraintEditorTop", topRegion.Size))
            DrawEditorHeader();

        ImGui.SetCursorScreenPos(botRegion.Pos);
        using (ImRaii.Child("RestraintEditorBot", botRegion.Size, false, WFlags.AlwaysUseWindowPadding))
        {
            // Draw out the tab bar, and the items respective contents.
            using (CkRaii.TabBarChild("AllowanceTabBars", WFlags.AlwaysUseWindowPadding, out var selected, EditorTabs))
                selected?.DrawContents(ImGui.GetContentRegionAvail().X);
        }
    }

    public void DrawContents(CkHeader.QuadDrawRegions drawRegions, float curveSize, WardrobeTabs tabMenu)
    {
        ImGui.SetCursorScreenPos(drawRegions.TopLeft.Pos);
        using (ImRaii.Child("RestraintsTopLeft", drawRegions.TopLeft.Size))
            _selector.DrawFilterRow(drawRegions.TopLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.BotLeft.Pos);
        using (ImRaii.Child("RestraintsBottomLeft", drawRegions.BotLeft.Size, false, WFlags.NoScrollbar))
            _selector.DrawList(drawRegions.BotLeft.SizeX);

        ImGui.SetCursorScreenPos(drawRegions.TopRight.Pos);
        using (ImRaii.Child("RestraintsTopRight", drawRegions.TopRight.Size))
            tabMenu.Draw(drawRegions.TopRight.Size);

        // Draw the active item at the position. (This draws as a child)
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos);
        DrawInfoUpper(drawRegions.BotRight, curveSize);

        var verticalShift = new Vector2(0, ImGui.GetItemRectSize().Y + ImGui.GetStyle().WindowPadding.Y);
        ImGui.SetCursorScreenPos(drawRegions.BotRight.Pos + verticalShift);
        DrawActiveItemInfo(drawRegions.BotRight.Size - verticalShift);
    }

    private void DrawInfoUpper(CkHeader.DrawRegion drawRegion, float rounding)
    {
        using var _ = ImRaii.Group();

        // Define the height of the selection child.
        var style = ImGui.GetStyle();
        var size = new Vector2(drawRegion.Size.X, WardrobeUI.SelectedRestraintH());
        var thumbnailRegion = new Vector2(size.Y / 1.2f, size.Y) - style.WindowPadding * 2;
        var leftWidth = size.X - style.WindowPadding.X - style.ItemSpacing.X - thumbnailRegion.X;

        var buttonSize = new Vector2(leftWidth, ImGui.GetFrameHeight());
        var buttonWrapSize = buttonSize + style.WindowPadding / 2;
        var btnHovered = ImGui.IsMouseHoveringRect(drawRegion.Pos, drawRegion.Pos + buttonSize);
        var descriptionSize = new Vector2(buttonWrapSize.X, ImGui.GetFrameHeight() + ImGui.GetTextLineHeightWithSpacing() * 4);

        // Split the channels to draw our components.
        var cursorPos = ImGui.GetCursorPos();
        var wdl = ImGui.GetWindowDrawList();

        // Draw the background.
        wdl.AddRectFilled(drawRegion.Pos - new Vector2(style.WindowPadding.X, 0), drawRegion.Pos + size, CkColor.FancyHeader.Uint(), rounding, ImDrawFlags.RoundCornersRight);
        wdl.AddRectFilled(drawRegion.Pos - new Vector2(style.WindowPadding.X, 0), drawRegion.Pos + new Vector2(0, size.Y), CkGui.Color(ImGuiColors.DalamudGrey));

        // Draw out the description BG.
        wdl.AddRectFilled(drawRegion.Pos, drawRegion.Pos + descriptionSize, CkColor.FancyHeaderContrast.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);

        // Draw out the Label Button.
        var col = btnHovered ? CkColor.VibrantPinkHovered.Uint() : CkColor.VibrantPink.Uint();
        wdl.AddRectFilled(drawRegion.Pos, drawRegion.Pos + buttonWrapSize, CkColor.SideButton.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);
        wdl.AddRectFilled(drawRegion.Pos, drawRegion.Pos + buttonSize, col, rounding, ImDrawFlags.RoundCornersBottomRight);
        
        ImGui.SetCursorPosX(cursorPos.X + style.WindowPadding.X);
        ImUtf8.TextFrameAligned(_selector.Selected!.Label);

        if (btnHovered)
        {
            if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                _manager.StartEditing(_selector.Selected!);
            CkGui.AttachToolTip("Double-Click me to open the editor!");
        }

        ImGui.SetCursorPosY(cursorPos.Y + ImGui.GetFrameHeight());
        using (ImRaii.Child("DescriptionChild", new Vector2(leftWidth, ImGui.GetTextLineHeightWithSpacing() * 4), false, WFlags.AlwaysUseWindowPadding))
            CkGui.TextWrapped(_selector.Selected!.Description);

        // Draw the IconsRow.
        var trueCol = 0xFFFFFFFF;
        var falseCol = CkColor.FancyHeaderContrast.Uint();
        var helmAttribute = _selector.Selected.HeadgearState != OptionalBool.Null;
        var visorAttribute = _selector.Selected.VisorState != OptionalBool.Null;
        var weaponAttribute = _selector.Selected.WeaponState != OptionalBool.Null;
        var redrawAttribute = _selector.Selected.DoRedraw;
        var layersAttribute = _selector.Selected.Layers.Count > 0;
        var modsAttribute = _selector.Selected.RestraintMods.Count > 0;
        var moodleAttribute = _selector.Selected.RestraintMoodles.Count > 0;

        // Get the remaining Y content region, and set our cursorPos to be center of it.
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(FAI.HatCowboy, helmAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(helmAttribute ? "Headgear is set" : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Glasses, visorAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(visorAttribute ? "Visor is set" : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Shield, weaponAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(weaponAttribute ? "Weapon is set" : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Repeat, redrawAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(redrawAttribute ? "Redraws character on application and removal." : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.LayerGroup, layersAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(layersAttribute ? "Restraint Set has Layers" : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.PersonRays, modsAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(modsAttribute ? "Restraint Set has Attached Mods" : string.Empty);

            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.TheaterMasks, moodleAttribute ? trueCol : falseCol);
            CkGui.AttachToolTip(moodleAttribute ? "Restraint Set has Moodles" : string.Empty);
        }

        // Draw the Thumbnail Image.
        var imgPos = drawRegion.Pos + size - style.WindowPadding - thumbnailRegion;
        var hoveringImg = ImGui.IsMouseHoveringRect(imgPos, imgPos + thumbnailRegion);
        var imgCol = hoveringImg ? CkColor.FancyHeader.Uint() : CkColor.FancyHeaderContrast.Uint();
        ImGui.SetCursorScreenPos(imgPos);
        ImGui.GetWindowDrawList().AddRectFilled(imgPos, imgPos + thumbnailRegion, imgCol, rounding);
        _activeDrawer.DrawFramedImage(_selector.Selected!, thumbnailRegion, rounding, true);
        if (hoveringImg)
        {
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            {
                var metaData = new ImageMetadataGS(ImageDataType.Restraints, new Vector2(120, 120f * 1.2f), _selector.Selected!.Identifier);
                Mediator.Publish(new OpenThumbnailBrowser(metaData));
            }
            CkGui.AttachToolTip("The Thumbnail for this item.--SEP--Double Click to change the image.");
        }
    }

    private void DrawActiveItemInfo(Vector2 region)
    {
        using var _ = ImRaii.Child("ActiveRestraintItems", region, false, WFlags.AlwaysUseWindowPadding);
        if (_manager.ServerRestraintData is not { } activeData)
            return;

        // get the size of the child for this centered header.
        var activeSetHeight = ImGui.GetFrameHeightWithSpacing() * 5 + ImGui.GetStyle().ItemSpacing.Y * 4;
        var activeSetSize = new Vector2(ImGui.GetContentRegionAvail().X, activeSetHeight);
        if (_manager.AppliedRestraint is null)
        {
            using var inactive = CkRaii.HeaderChild("No Restraint Set Is Active", activeSetSize, HeaderFlags.AddPaddingToHeight);
        }
        else
        {
            using (CkRaii.HeaderChild("Active Restraint Set", activeSetSize, CkStyle.HeaderRounding(), HeaderFlags.AddPaddingToHeight))
            {
                // Draw the restraint set equipment, grouped.
                _activeDrawer.DrawRestraintSlots(_manager.AppliedRestraint, new Vector2(ImGui.GetFrameHeightWithSpacing()));

                // i wont really be able to debug this until we have proper server interaction lol.

                // Sameline, beside it, draw the padlock management state.

                // Under this create a secondary group.
                // On the left of this group, show the attached moodles of the base.
                // Then the attached traits of the base.
                // On the right, draw the thumbnail.
            }
        }
    }

    private void DrawEditorHeader()
    {
        // Dont draw anything if the editor is not active.
        if (_manager.ItemInEditor is not { } setInEdit)
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
