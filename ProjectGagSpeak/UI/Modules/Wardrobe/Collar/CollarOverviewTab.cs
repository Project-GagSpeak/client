using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.RichText;
using CkCommons.Textures;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Caches;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui.Text;
using Penumbra.GameData.Enums;

namespace GagSpeak.Gui.Wardrobe;

public class CollarOverviewTab : IFancyTab
{
    private readonly ILogger<CollarOverviewTab> _logger;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodles;
    private readonly VisualStateListener _visuals;
    private readonly CollarManager _manager;
    private readonly KinksterManager _kinksters;
    private readonly ModPresetManager _modPresets;
    private readonly DistributorService _dds;
    private readonly UiThumbnailService _thumbnails;
    private readonly TutorialService _guides;
    public CollarOverviewTab(
        ILogger<CollarOverviewTab> logger,
        EquipmentDrawer equipDrawer, 
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        VisualStateListener visuals,
        CollarManager collar,
        KinksterManager kinksters,
        ModPresetManager modPresets,
        DistributorService dds, 
        UiThumbnailService thumbnails,
        TutorialService guides)
    {
        _logger = logger;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodles = moodleDrawer;
        _visuals = visuals;
        _manager = collar;
        _kinksters = kinksters;
        _modPresets = modPresets;
        _dds = dds;
        _thumbnails = thumbnails;
        _guides = guides;
    }

    public string   Label       => "Collar Overview";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;

    public void DrawContents(float width)
    {
        var leftWidth = width * .55f;
        var permissionH = CkStyle.HeaderHeight() + ImGui.GetFrameHeightWithSpacing() * 5;
        var topleftRegion = new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y - permissionH.AddWinPadY());
        using (ImRaii.Group())
        {
            DrawSetup(topleftRegion);
            DrawPermissionEdits(new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y));
        }
        ImUtf8.SameLineInner();
        DrawActiveState(ImGui.GetContentRegionAvail());
    }

    private void DrawSetup(Vector2 region)
    {
        if (_manager.ItemInEditor is { } collar)
            DrawSetupEditor(region, collar);
        else
            DrawSetupOverview(region);
    }

    private void DrawSetupEditor(Vector2 size, GagSpeakCollar collar)
    {
        using var child = CkRaii.FramedChildPaddedWH("Setup", size, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);
        // Precalculate essential variable sizes.
        var textH = CkGui.CalcFontTextSize("T", UiFontService.UidFont).Y;
        var iconSize = CkGui.IconButtonSize(FAI.Edit);
        var buttonOffset = (textH - iconSize.Y) / 2;
        var topleftWidth = child.InnerRegion.X - (iconSize.X * 2 + ImGui.GetStyle().ItemInnerSpacing.X);
        // Idealy this should all be to the right of the displayed image.
        CkGui.FontText("Collar Setup", UiFontService.UidFont);
        ImGui.SameLine(topleftWidth);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + buttonOffset);
        if (CkGui.IconButton(FAI.Undo, inPopup: true))
            _manager.StopEditing();
        CkGui.AttachToolTip("Discard Changes and Exit Editing.");

        ImUtf8.SameLineInner();
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + buttonOffset);
        if (CkGui.IconButton(FAI.Save, inPopup: true))
            _manager.SaveChangesAndStopEditing();
        CkGui.AttachToolTip("Save Changes and Exit Editing.");

        CkGui.Separator(CkColor.VibrantPink.Uint());

        // Collar Label edit.
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.Font);
        CkGui.AttachToolTip("The Label for this Collar.");

        ImUtf8.SameLineInner();
        var label = collar.Label;
        ImGui.InputTextWithHint("##CollarLabel", "Collar Name..", ref label, 40);
        if (ImGui.IsItemDeactivatedAfterEdit())
            collar.Label = label;

        // Glamour edit.
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.Vest);
        CkGui.AttachToolTip("The attached Glamourer item.");

        ImUtf8.SameLineInner();
        if (CkGuiUtils.EnumCombo($"##CollarES", 100f, collar.Glamour.Slot, out var nSlot, EquipSlotExtensions.EqdpSlots, _ => _.ToName(), flags: CFlags.NoArrowButton))
        {
            collar.Glamour.Slot = nSlot;
            collar.Glamour.GameItem = ItemSvc.NothingItem(nSlot);
        }
        ImUtf8.SameLineInner();
        _equipDrawer.DrawItem(collar.Glamour, ImGui.GetContentRegionAvail().X);

        // Mod Field.
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.FileDownload);
        CkGui.AttachToolTip("The Mod Preset applied to this Collar.");

        ImUtf8.SameLineInner();
        if (_modPresets.PresetCombo.Draw("##CollarMPS", collar.Mod.Label, ImGui.GetContentRegionAvail().X * .4f, 1f, CFlags.NoArrowButton))
        {
            _logger.LogInformation("Requesting Collar ModPreset change to " + _modPresets.PresetCombo.Current?.Label);
            // locate the preset in the container.
            if (collar.Mod.Container.ModPresets.FirstOrDefault(mp => mp.Label == _modPresets.PresetCombo.Current!.Label) is { } match)
            {
                _logger.LogTrace($"Associated Mod Preset changed to {_modPresets.PresetCombo.Current!.Label}");
                collar.Mod = match;
            }
        }
        // if right clicked, clear it. (retain mod)
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Associated Mod Preset was cleared. and is now Empty");
            var curContainer = collar.Mod.Container;
            collar.Mod = new ModSettingsPreset(curContainer);
        }

        // Same for Mod.
        ImUtf8.SameLineInner();
        if (_modPresets.ModCombo.Draw("##CollarMS", collar.Mod.Container.DirectoryPath, ImGui.GetContentRegionAvail().X, 1.5f))
        {
            _logger.LogInformation("Requesting Collar Mod change to " + _modPresets.ModCombo.Current?.Name);
            // locate the mod from the preset storage, and if found, update it.
            if (_modPresets.ModPresetStorage.FirstOrDefault(mps => mps.DirectoryPath == _modPresets.ModCombo.Current!.DirPath) is { } match)
            {
                _logger.LogTrace($"Associated Mod changed to {_modPresets.ModCombo.Current!.Name}");
                collar.Mod = match.ModPresets.First();
            }
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Associated Mod was cleared. and is now Empty");
            collar.Mod = new ModSettingsPreset(new ModPresetContainer());
        }
    }
    
    private void DrawSetupOverview(Vector2 size)
    {
        using var child = CkRaii.FramedChildPaddedWH("Setup", size, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);
        // Precalculate essential variable sizes.
        var pos = ImGui.GetCursorScreenPos();
        var textH = CkGui.CalcFontTextSize("T", UiFontService.UidFont).Y;
        var iconSize = CkGui.IconButtonSize(FAI.Edit);
        var thumbnailH = ImGui.GetFrameHeight() + CkGui.GetSeparatorHeight() + textH;
        var buttonOffset = (textH - iconSize.Y) / 2;
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

            CkGui.Separator(CkColor.VibrantPink.Uint(), topleftWidth);

            ImGui.Spacing();
            CkGui.FramedIconText(FAI.Font);
            CkGui.TextFrameAlignedInline(string.IsNullOrEmpty(_manager.ClientCollar.Label) ? "<No Label Set!>" : _manager.ClientCollar.Label);
            CkGui.AttachToolTip("The Label for this Collar.");
        }

        // Glamour Field.
        var validGlamour = !_manager.ClientCollar.Glamour.IsDefault();
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.Vest);
        CkGui.TextFrameAlignedInline(validGlamour ? _manager.ClientCollar.Glamour.GameItem.Name : "No Glamour Attached");

        // Mod Field.
        var validMod = _manager.ClientCollar.Mod.HasData;
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.FileDownload);
        if (validMod)
        {
            CkGui.TextFrameAlignedInline(_manager.ClientCollar.Mod.Label);
            CkGui.AttachToolTip("The Mod Preset applied to this Collar.");

            ImGui.SameLine();
            CkGui.TagLabelText(_manager.ClientCollar.Mod.Container.ModName, CkColor.FancyHeader.Uint());
            if(ImGui.IsItemHovered())
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
        ActiveItemsDrawer.DrawCollarImage(_manager.ClientCollar, thumbnailSize, FancyTabBar.RoundingInner, CkColor.ElementSplit.Uint());
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner, thickness);

        if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            _thumbnails.SetThumbnailSource(Guid.Empty, new Vector2(120 * 1.2f, 120f), ImageDataType.Collar);
        CkGui.AttachToolTip("The Thumbnail for this Collar.--SEP--Double Click to change the image.");
    }

    private void DrawPermissionEdits(Vector2 region)
    {
        var permBoxWidth = (region.X - ImGui.GetStyle().ItemInnerSpacing.X) / 2;
        var permBoxSize = new Vector2(permBoxWidth, CkStyle.GetFrameRowsHeight(5));
        using (CkRaii.HeaderChild("Your Access", permBoxSize, FancyTabBar.RoundingInner, HeaderFlags.AddPaddingToHeight))
        {
            var refVar1 = true;
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
            ImGui.Checkbox("Glam/Mod Editing", ref refVar1);
        }
        ImUtf8.SameLineInner();
        using (CkRaii.HeaderChild("Owners Access", permBoxSize, FancyTabBar.RoundingInner, HeaderFlags.AddPaddingToHeight))
        {
            var refVar2 = false;
            var refVar3 = true;
            var refVar4 = false;
            var refVar5 = false;
            ImGui.Checkbox("Toggle Visibility", ref refVar2);
            ImGui.Checkbox("Glamour Dyes", ref refVar3);
            ImGui.Checkbox("Moodle", ref refVar4);
            ImGui.Checkbox("Collar Writing", ref refVar5);
        }
    }

    private void DrawActiveState(Vector2 region)
    {
        using var child = CkRaii.FramedChildPaddedWH("Active", region, 0, CkColor.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        var textH = CkGui.CalcFontTextSize("T", UiFontService.UidFont).Y;
        var pos = ImGui.GetCursorScreenPos();
        pos.X += (child.InnerRegion.X - textH - ImUtf8.ItemSpacing.X);

        ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Collar], pos, new(textH));
        CkGui.FontText("Active State", UiFontService.UidFont);

        CkGui.Separator(CkColor.VibrantPink.Uint());
        
        if (_manager.SyncedData is not { } curState)
        {
            CkGui.CenterTextAligned("Nobody has ownership over your collar.");
            return;
        }

        var ownPerms = curState.CollaredAccess;
        var ownerPerms = curState.OwnerAccess;

        // Make sure that various areas are enabled or disabled based on access granted.
        DrawCollarVisuals(ownPerms);
        DrawCollarWriting(ownPerms);
        DrawDyes(ownPerms);
        DrawMoodle(ownPerms);

        ImGui.Spacing();
        foreach (var owner in curState.OwnerUIDs)
        {
            CkGui.FramedIconText(FAI.Heart);
            CkGui.TextFrameAlignedInline(owner);
            CkGui.AttachToolTip("An owner of this collar.");
        }
    }

    private void DrawCollarVisuals(CollarAccess ownPerms)
    {
        using var _ = ImRaii.Disabled(UiService.DisableUI || !ownPerms.HasAny(CollarAccess.Visuals));
        
        ImGui.Spacing();
        var refVisuals = _manager.SyncedData!.Visuals;
        if (ImGui.Checkbox("Collar Visuals", ref refVisuals))
        {
            _logger.LogInformation("Toggling Collar Visuals to " + refVisuals);
            var newData = _manager.SyncedData with { Visuals = refVisuals };
            SelfBondageHelper.CollarUpdateTask(newData, DataUpdateType.VisibilityChange, _dds, _visuals);
        }
    }

    private void DrawCollarWriting(CollarAccess ownPerms)
    {
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.PencilAlt);
        ImUtf8.SameLineInner();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        using (ImRaii.Disabled(UiService.DisableUI || !ownPerms.HasAny(CollarAccess.Writing)))
        {
            var writing = _manager.SyncedData!.Writing;
            ImGui.InputTextWithHint("##CollarWriting", "Written text on collar..", ref writing, 100);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                _logger.LogInformation($"Updating Collar Writing to [{writing}]");
                var newData = _manager.SyncedData with { Writing = writing };
                SelfBondageHelper.CollarUpdateTask(newData, DataUpdateType.CollarWritingChange, _dds, _visuals);
            }
        }
    }

    private void DrawDyes(CollarAccess ownPerms)
    {

        ImGui.Spacing();
        CkGui.FramedIconText(FAI.FillDrip);
        ImUtf8.SameLineInner();
        using (ImRaii.Disabled(UiService.DisableUI || !ownPerms.HasAny(CollarAccess.Dyes)))
        {
            var dye1 = _manager.SyncedData!.Dye1;
            var dye2 = _manager.SyncedData!.Dye2;
            if (_equipDrawer.DrawStains("CollarStains", ref dye1, ref dye2, ImGui.GetContentRegionAvail().X))
            {
                _logger.LogInformation($"Updating Collar Dyes to [{dye1}][{dye2}]");
                var newData = _manager.SyncedData with { Dye1 = dye1, Dye2 = dye2 };
                SelfBondageHelper.CollarUpdateTask(newData, DataUpdateType.CollarWritingChange, _dds, _visuals);
            }
        }
    }

    private void DrawMoodle(CollarAccess ownPerms)
    {
        ImGui.Spacing();
        CkGui.FramedIconText(FAI.TheaterMasks);
        ImUtf8.SameLineInner();
        var moodle = _manager.SyncedData!.Moodle;
        if (moodle.GUID == Guid.Empty)
        {
            CkGui.ColorText("<No Moodle Set!>", ImGuiColors.DalamudRed);
            return;
        }

        CkRichText.Text(ImGui.GetContentRegionAvail().X - MoodleDrawer.IconSizeFramed.X, _manager.SyncedData!.Moodle.Title);
        ImGui.SameLine();
        MoodleDisplay.DrawMoodleIcon(moodle.IconID, moodle.Stacks, MoodleDrawer.IconSizeFramed);
        GsExtensions.DrawMoodleStatusTooltip(moodle, MoodleCache.IpcData.StatusList);
    }
}
