using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using OtterGui.Text;
using OtterGui.Text.EndObjects;
using TerraFX.Interop.Windows;
using static CkCommons.Widgets.CkHeader;
using static FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.DynamicEvent.Delegates;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace GagSpeak.Gui.Wardrobe;

public class CollarOverviewTab : IFancyTab
{
    private readonly ILogger<CollarOverviewTab> _logger;
    private readonly CollarManager _manager;
    private readonly ActiveItemsDrawer _itemDrawHelper;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly UiThumbnailService _thumbnails;
    private readonly TutorialService _guides;
    public CollarOverviewTab(ILogger<CollarOverviewTab> logger, CollarManager manager,
        ActiveItemsDrawer itemDrawHelper, EquipmentDrawer equipDrawer, ModPresetDrawer modDrawer, 
        CosmeticService cosmetics, UiThumbnailService thumbnails, TutorialService guides)
    {
        _logger = logger;
        _manager = manager;
        _itemDrawHelper = itemDrawHelper;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _cosmetics = cosmetics;
        _thumbnails = thumbnails;
        _guides = guides;
    }

    public string   Label       => "Collar Overview";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        var leftWidth = width * .55f;
        using (ImRaii.Group())
        {
            DrawSetup(leftWidth);
            DrawPermissionEdits(new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y));
        }
        ImUtf8.SameLineInner();
        DrawActiveState(ImGui.GetContentRegionAvail());
    }

    private void DrawSetup(float width)
    {
        var textH = CkGui.CalcFontTextSize("T", UiFontService.UidFont).Y;
        var iconSize = CkGui.IconButtonSize(FAI.Edit);
        var ThumbnailH = ImGui.GetFrameHeight() + CkGui.GetSeparatorHeight() + textH;
        var buttonOffset = (textH - iconSize.Y) / 2;
        var setupSize = new Vector2(width, ThumbnailH + ImGui.GetFrameHeightWithSpacing() * 2);

        if (_manager.IsEditing)
            DrawSetupEditor(setupSize, ThumbnailH, buttonOffset);
        else
            DrawSetupOverview(setupSize, ThumbnailH, buttonOffset);
    }

    private void DrawSetupEditor(Vector2 size, float thumbnailH, float buttonOffset)
    {

    }
    
    private void DrawSetupOverview(Vector2 size, float thumbnailH, float buttonOffset)
    {
        using var child = CkRaii.FramedChildPaddedW("Setup", size.X, size.Y, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);
        // Precalculate essential variable sizes.
        var pos = ImGui.GetCursorScreenPos();
        var thumbnailSize = new Vector2(thumbnailH * 1.2f, thumbnailH);
        var topleftWidth = child.InnerRegion.X - (thumbnailSize.X + ImGui.GetStyle().ItemSpacing.X);
        // Idealy this should all be to the right of the displayed image.
        using (ImRaii.Group())
        {
            CkGui.FontText("Collar Setup", UiFontService.UidFont);
            ImGui.SameLine(child.InnerRegion.X - (thumbnailSize.X + ImGui.GetStyle().ItemSpacing.X + CkGui.IconButtonSize(FAI.Edit).X));
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + buttonOffset);
            if (CkGui.IconButton(FAI.Edit, inPopup: true))
                _manager.StartEditing();

            CkGui.SeparatorColored(topleftWidth, col: CkColor.VibrantPink.Uint());

            CkGui.FramedIconText(FAI.Font);
            CkGui.TextFrameAlignedInline(string.IsNullOrEmpty(_manager.ClientCollar.Label) ? "<No Label Set!>" : _manager.ClientCollar.Label);
            CkGui.AttachToolTip("The Label for this Collar.");
        }

        // Glamour Field.
        var validGlamour = !_manager.ClientCollar.Glamour.IsDefault();
        CkGui.BooleanToColoredIcon(validGlamour, false, FAI.Vest, FAI.Vest);
        CkGui.TextFrameAlignedInline(validGlamour ? _manager.ClientCollar.Glamour.GameItem.Name : "No Glamour Attached");

        // Mod Field.
        var validMod = _manager.ClientCollar.Mod.HasData;
        CkGui.BooleanToColoredIcon(validMod, false, FAI.FileDownload, FAI.FileDownload);
        if (validMod)
        {
            CkGui.TextFrameAlignedInline(_manager.ClientCollar.Mod.Label);
            CkGui.AttachToolTip("The Mod Preset applied to this Collar.");

            ImUtf8.SameLineInner();
            CkGui.TagLabelText(_manager.ClientCollar.Mod.Container.ModName, ImGuiColors.TankBlue);
            CkGui.AttachToolTip("The Mod this Preset uses.");

            CkGui.FramedHoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
            if (ImGui.IsItemHovered())
                _modDrawer.DrawPresetTooltip(_manager.ClientCollar.Mod);
        }
        else
        {
            CkGui.TextFrameAlignedInline("No Mod Attached");
        }

        // Thumbnail area
        var thickness = CkStyle.FrameThickness();
        pos += new Vector2(child.InnerRegion.X - thumbnailSize.X - thickness, thickness);
        ImGui.SetCursorScreenPos(pos);
        _itemDrawHelper.DrawCollarImage(_manager.ClientCollar, thumbnailSize, FancyTabBar.RoundingInner, CkColor.ElementSplit.Uint());
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner, thickness);

        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            _thumbnails.SetThumbnailSource(Guid.Empty, new Vector2(120 * 1.2f, 120f), ImageDataType.Collar);
        CkGui.AttachToolTip("The Thumbnail for this Collar.--SEP--Double Click to change the image.");
    }

    private void DrawPermissionEdits(Vector2 region)
    {
        using var child = CkRaii.FramedChildPaddedWH("Perms", region, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        // Idealy this should all be to the right of the displayed image.
        CkGui.FontText("Edit Access Permissions", UiFontService.UidFont);
        CkGui.SeparatorColored(child.InnerRegion.X, col: CkColor.VibrantPink.Uint());

        // Draw out the two permission boxes on either side.
        var permBoxWidth = (child.InnerRegion.X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        var permBoxSize = new Vector2(permBoxWidth, ImGui.GetContentRegionAvail().Y);
        using (CkRaii.HeaderChild("Your Access", permBoxSize, HeaderFlags.SizeIncludesHeader))
        {
            var refVar1 = true;
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Glamour & Mod", ref refVar1);
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
        }
        ImUtf8.SameLineInner();
        using (CkRaii.HeaderChild("Owners Access", permBoxSize, HeaderFlags.SizeIncludesHeader))
        {
            var refVar1 = true;
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Glamour & Mod", ref refVar1);
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
        }
    }

    private void DrawActiveState(Vector2 region)
    {
        using var child = CkRaii.FramedChildPaddedWH("Active", region, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        CkGui.ColorText("wawawaw", CkColor.CkMistressColor.Uint());
    }
}
