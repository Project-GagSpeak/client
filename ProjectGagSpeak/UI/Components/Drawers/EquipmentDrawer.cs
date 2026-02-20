using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Inventory;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.CustomCombos.Glamourer;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using OtterGui.Extensions;
using OtterGui.Text;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Gui.Components;

/// <summary> Helper for all displays and editors that work with Equip & Stains </summary>
/// <remarks> Can be used for hover tooltips as well and other checks involving Equip & Stains. </remarks>
public class EquipmentDrawer
{
    internal readonly record struct CachedSlotItemData(EquipItem Item);

    private static IconCheckboxEx GlamourFlagCheckbox = new(FAI.Vest, CkCol.IconOn.Uint(), CkCol.IconOff.Uint());
    private static IconCheckboxEx ModFlagCheckbox = new(FAI.FileArchive, CkCol.IconOn.Uint(), CkCol.IconOff.Uint());
    private static IconCheckboxEx MoodleFlagCheckbox = new(FAI.TheaterMasks, CkCol.IconOn.Uint(), CkCol.IconOff.Uint());
    private static IconCheckboxEx HardcoreTraitsCheckbox = new(FAI.Handcuffs, CkCol.IconOn.Uint(), CkCol.IconOff.Uint());

    private readonly GameItemCombo[] _itemCombos;
    private readonly BonusItemCombo[] _bonusCombos;
    private readonly GameStainCombo _stainCombo;
    private readonly RestrictionCombo _restrictionCombo;

    private readonly ILogger _logger;
    private readonly IpcCallerGlamourer _ipcGlamourer;
    private readonly RestrictionManager _restrictions;
    private readonly TextureService _textures;
    private readonly TutorialService _guides;

    public EquipmentDrawer(ILogger<EquipmentDrawer> logger, GagspeakMediator mediator,
        IpcCallerGlamourer glamourer, RestrictionManager restrictions, FavoritesConfig favorites,
        TextureService textures, TutorialService guides)
    {
        _logger = logger;
        _ipcGlamourer = glamourer;
        _restrictions = restrictions;
        _textures = textures;
        // Preassign these 10 itemCombo slots. They will be consistant throughout the plugins usage.
        _itemCombos = EquipSlotExtensions.EqdpSlots.Select(e => new GameItemCombo(e, logger)).ToArray();
        _bonusCombos = BonusExtensions.AllFlags.Select(f => new BonusItemCombo(f, logger)).ToArray();
        _stainCombo = new GameStainCombo(logger);
        _guides = guides;
        _restrictionCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => FavoritesConfig.Restrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
    }

    // Method for Drawing the Associated Glamour Item (Singular)
    public void DrawAssociatedGlamour(string id, GlamourSlot item, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var height = CkStyle.GetFrameRowsHeight(3);
        using (var c = CkRaii.HeaderChild("Associated Glamour", new Vector2(width, height), HeaderFlags.AddPaddingToHeight))
        {
            // get the inner width after the padding is applied.
            item.GameItem.DrawIcon(_textures, new Vector2(height), item.Slot);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                // Begin by drawing out the slot enum dropdown that spans the remaining content region.
                var barWidth = c.InnerRegion.X - height - ImGui.GetStyle().ItemInnerSpacing.X;

                if (CkGuiUtils.EnumCombo($"##{id}-slot", barWidth, item.Slot, out var newSlot, EquipSlotExtensions.EqdpSlots, slot => slot.ToName()))
                {
                    item.Slot = newSlot;
                    item.GameItem = ItemSvc.NothingItem(item.Slot);
                }
                DrawItem(item, barWidth);
                DrawStains(item, barWidth);
            }
        }
    }

    // Method for Drawing a RestraintSlot Item.
    public void DrawRestraintSlot(Dictionary<EquipSlot, IRestraintSlot> slots, EquipSlot focus, float fullWidth)
    {
        if (!slots.TryGetValue(focus, out var restraintSlot))
        {
            CkGui.ColorText("ERROR", ImGuiColors.DalamudRed);
            ImGui.Dummy(new Vector2(fullWidth, ImGui.GetFrameHeight()));
            return;
        }

        // Inner width to account for the swapper.
        var innerWidth = fullWidth - CkGui.IconButtonSize(FAI.ArrowRightArrowLeft).X - ImGui.GetStyle().ItemInnerSpacing.X;

        // Determine what we are drawing based on the type of slot.
        if (restraintSlot is RestraintSlotBasic basicSlot)
        {
            DrawRestraintSlotBasic(basicSlot, innerWidth);
            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.ArrowsLeftRight, CkStyle.TwoRowHeight(), basicSlot.EquipSlot + "Swapper"))
            {
                var slot = basicSlot.EquipSlot;
                _logger.LogTrace($"Swapping {basicSlot.EquipSlot} from Basic to Advanced.");
                slots[focus] = RestraintSlotAdvanced.GetEmpty(slot, basicSlot.Stains);
            }
        }
        else if (restraintSlot is RestraintSlotAdvanced advSlot)
        {
            DrawRestrictionRef(advSlot, focus.ToName(), innerWidth);
            ImUtf8.SameLineInner();
            if (CkGui.IconButton(FAI.ArrowsLeftRight, CkStyle.TwoRowHeight(), advSlot.EquipSlot + "Swapper"))
            {
                _logger.LogTrace($"Swapping {advSlot.EquipSlot} from Advanced to Basic.");
                var prevStains = advSlot.CustomStains;
                var newBasic = new RestraintSlotBasic(focus);
                newBasic.Glamour.GameStain = prevStains;
                slots[focus] = newBasic;
            }
        }
    }

    /// <summary> Draws out the information for a basic restraint slot. </summary>
    /// <returns> True if the item was swapped between a basic to advanced, or vise versa. </returns>
    /// <param name="basicSlot"></param>
    /// <param name="width"></param>
    public void DrawRestraintSlotBasic(RestraintSlotBasic basicSlot, float width)
    {
        using var group = ImRaii.Group();
        // Draw out the icon firstly.
        basicSlot.EquipItem.DrawIcon(_textures, new Vector2(CkStyle.TwoRowHeight()), basicSlot.EquipSlot);
        ImGui.SameLine(0, 3);
        width -= 3f + CkStyle.TwoRowHeight();

        // Get the width for the combo stuff.
        var comboWidth = width - CkGui.IconButtonSize(FAI.EyeSlash).X - ImGui.GetStyle().ItemInnerSpacing.X;
        using (ImRaii.Group())
        {
            DrawItem(basicSlot.Glamour, comboWidth);
            DrawStains(basicSlot.Glamour, comboWidth);
        }

        ImUtf8.SameLineInner();

        var overlayState = basicSlot.ApplyFlags.HasAny(RestraintFlags.IsOverlay);
        using (ImRaii.PushColor(ImGuiCol.Button, CkCol.CurvedHeaderFade.Uint()))
            if (CkGui.IconButton(overlayState ? FAI.EyeSlash : FAI.Eye, CkStyle.TwoRowHeight(), basicSlot.EquipSlot + "Overlay"))
                basicSlot.ApplyFlags ^= RestraintFlags.IsOverlay;
        CkGui.AttachToolTip(overlayState ? "This slot won't be applied if it's empty." : "Always apply this slot, even if empty.");
        if (basicSlot.EquipSlot == EquipSlot.Body)
        {
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.Overlay, WardrobeUI.LastPos, WardrobeUI.LastSize,
                () => basicSlot.ApplyFlags ^= RestraintFlags.IsOverlay);
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.OverlayBuffer, WardrobeUI.LastPos, WardrobeUI.LastSize,
                () => FancyTabBar.SelectTab("RS_EditBar", Wardrobe.RestraintsPanel.EditorTabs[2], Wardrobe.RestraintsPanel.EditorTabs));
        }

        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkCol.CurvedHeaderFade.Uint(), ImGui.GetStyle().FrameRounding);
    }

    public void DrawRestrictionRef<T>(T restriction, string id, float width) where T : IRestrictionRef
    {
        using var group = ImRaii.Group();

        // If it has a thumbnail, we should use that.
        if (!restriction.Ref.ThumbnailPath.IsNullOrWhitespace())
        {
            if (TextureManagerEx.GetMetadataPath(ImageDataType.Restrictions, restriction.Ref.ThumbnailPath) is { } thumbnail)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(thumbnail, pos, new Vector2(CkStyle.TwoRowHeight()), ImGui.GetStyle().FrameRounding);
                ImGui.Dummy(new Vector2(CkStyle.TwoRowHeight()));
            }
        }
        else
        {
            // Placeholder frame display.
            ItemSvc.NothingItem(restriction.Ref.Glamour.Slot).DrawIcon(_textures, new Vector2(CkStyle.TwoRowHeight()), restriction.Ref.Glamour.Slot);
        }

        ImGui.SameLine(0, 3);
        width -= 3f + CkStyle.TwoRowHeight();

        // restriction selection and custom combos.
        var comboWidth = width - CkStyle.TwoRowHeight() - ImGui.GetStyle().ItemInnerSpacing.X;
        using (ImRaii.Group())
        {
            var change = _restrictionCombo.Draw($"##AdvSelector{id}", restriction.Ref.Identifier, comboWidth, flags: CFlags.NoArrowButton);
            if (change && !restriction.Ref.Identifier.Equals(_restrictionCombo.Current?.Identifier))
            {
                _logger.LogTrace($"Item changed to {_restrictionCombo.Current?.Identifier} " +
                    $"[{_restrictionCombo.Current?.Label}] from {restriction.Ref.Identifier} [{restriction.Ref.Label}]");
                // Get the actual reference to the restrictions item.
                if (_restrictions.Storage.TryGetRestriction(_restrictionCombo.Current?.Identifier ?? Guid.Empty, out var match))
                    restriction.Ref = match;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace($"Item Cleared and set to the Default. [{restriction.Ref.Label}]");
                restriction.Ref = new RestrictionItem() { Identifier = Guid.Empty };
            }
            DrawCustomStains(restriction, id, comboWidth);
        }

        // Beside this, draw the restraint flag editor.
        ImUtf8.SameLineInner();
        DrawAdvancedSlotFlags(restriction, id);
        ImUtf8.SameLineInner();
    }

    public void DrawGlassesSlot(GlamourBonusSlot glasses, float width)
    {
        var totalHeight = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        using var group = ImRaii.Group();
        // Draw out the icon firstly.
        glasses.GameItem.DrawIcon(_textures, new Vector2(totalHeight), glasses.Slot);
        ImGui.SameLine(0, 3);

        DrawBonusCombo(glasses, BonusItemFlag.Glasses, ImGui.GetContentRegionAvail().X);
    }

    public bool TryImportCurrentGear(out Dictionary<EquipSlot, RestraintSlotBasic> curGear)
        => _ipcGlamourer.TryObtainActorGear(true, out curGear);

    public bool TryImportCurrentAccessories(out Dictionary<EquipSlot, RestraintSlotBasic> curAccessories)
        => _ipcGlamourer.TryObtainActorAccessories(true, out curAccessories);

    public bool TryImportCurrentCustomizations(out JObject customizations, out JObject parameters)
        => _ipcGlamourer.TryObtainActorCustomization(out customizations, out parameters);

    public bool TryImportCurrentAdvancedMaterials(out JObject materials)
        => _ipcGlamourer.TryObtainMaterials(out materials);


    private void DrawAdvancedSlotFlags<T>(T restrictionRef, string id) where T : IRestrictionRef
    {
        var spacing = new Vector2(ImGuiHelpers.GlobalScale);
        var rounding = ImGui.GetStyle().FrameRounding;
        var region = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Child($"##AdvSlotFlags{id}", region))
        {
            var curGlam = restrictionRef.ApplyFlags.HasAny(RestraintFlags.Glamour);
            var curMod = restrictionRef.ApplyFlags.HasAny(RestraintFlags.Mod);
            var curMoodle = restrictionRef.ApplyFlags.HasAny(RestraintFlags.Moodle);
            var curHcTrait = restrictionRef.ApplyFlags.HasAny(RestraintFlags.Trait);

            if (GlamourFlagCheckbox.Draw($"##GlamFlag{id}", curGlam, out var newGlam) && curGlam != newGlam)
                restrictionRef.ApplyFlags ^= RestraintFlags.Glamour;
            CkGui.AttachToolTip(curGlam ? "The Glamour from this Restriction Item will be applied." : "Glamour application is ignored.");

            ImUtf8.SameLineInner();
            if (ModFlagCheckbox.Draw($"##ModFlag{id}", curMod, out var newMod) && curMod != newMod)
                restrictionRef.ApplyFlags ^= RestraintFlags.Mod;
            CkGui.AttachToolTip(curMod ? "Mods from this Restriction Item will be applied." : "Mods are ignored.");

            // Next Line.
            if (MoodleFlagCheckbox.Draw($"##MoodleFlag{id}", curMoodle, out var newMoodle) && curMoodle != newMoodle)
                restrictionRef.ApplyFlags ^= RestraintFlags.Moodle;
            CkGui.AttachToolTip(curMoodle ? "Moodles from this Restriction Item will be applied." : "Moodles are ignored.");

            ImUtf8.SameLineInner();
            if (HardcoreTraitsCheckbox.Draw($"##TraitFlag{id}", curHcTrait, out var newHcTrait) && curHcTrait != newHcTrait)
                restrictionRef.ApplyFlags ^= RestraintFlags.Trait;
            CkGui.AttachToolTip(curHcTrait ? "Hardcore Traits from this Restriction Item will be applied." : "Hardcore Traits are ignored.");
        }

        // Draw a bordered rect around this.
        var min = ImGui.GetItemRectMin() - spacing;
        var max = ImGui.GetItemRectMax() + spacing;
        ImGui.GetWindowDrawList().AddRectFilled(min, max, CkCol.CurvedHeaderFade.Uint(), rounding);
        ImGui.GetWindowDrawList().AddRect(min, max, CkCol.CurvedHeaderFade.Uint(), rounding);
    }

    public void DrawItem(GlamourSlot item, float width)
        => DrawItem(item, width, 1.25f);

    private void DrawItem(GlamourSlot item, float width, float innerWidthScaler)
    {
        // draw the item itemCombo.
        var itemCombo = _itemCombos[item.Slot.ToIndex()];

        var change = itemCombo.Draw(item.GameItem.Name, item.GameItem.ItemId, width, width * innerWidthScaler);

        if (change && !item.GameItem.Equals(itemCombo.Current))
        {
            _logger.LogTrace($"Item changed from {itemCombo.Current} " +
                $"[{itemCombo.Current.ItemId}] to {item.GameItem} [{item.GameItem.ItemId}]");
            item.GameItem = itemCombo.Current;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace($"Item changed to {ItemSvc.NothingItem(item.Slot)} " +
                $"[{ItemSvc.NothingItem(item.Slot).ItemId}] from {item.GameItem} [{item.GameItem.ItemId}]");
            item.GameItem = ItemSvc.NothingItem(item.Slot);
        }
    }

    public void DrawBonusCombo(GlamourBonusSlot item, BonusItemFlag flag, float width)
    {
        // Assuming _bonusItemCombo is similar to ItemCombos but for bonus items
        var itemCombo = _bonusCombos[item.Slot.ToIndex()];

        var change = itemCombo.Draw(item.GameItem.Name, item.GameItem.Id.BonusItem, width, width * 1.3f);

        if (change && !item.GameItem.Equals(itemCombo.Current))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {itemCombo.Current} [{itemCombo.Current.PrimaryId}] " +
                $"to {item.GameItem} [{item.GameItem.PrimaryId}]");
            // change
            item.GameItem = itemCombo.Current;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // Assuming a method to handle item reset or clear, similar to your DrawItem method
            _logger.LogTrace($"Item reset to default for slot {flag}");
            item.GameItem = EquipItem.BonusItemNothing(flag);
        }
    }

    public void DrawStains(GlamourSlot item, float width)
        => DrawStains(item, width, 1.75f);

    private void DrawStains(GlamourSlot item, float width, float innerWidthScaler)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (item.GameStain.Count - 1)) / item.GameStain.Count;

        foreach (var (stainId, index) in item.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = ItemSvc.Stains.TryGetValue(stainId, out var stain);
            // draw the stain itemCombo.
            var change = _stainCombo.Draw($"##stain{item.Slot}", widthStains * innerWidthScaler, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss);
            if (index < item.GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (ItemSvc.Stains.TryGetValue(_stainCombo.Current.Key, out stain))
                {
                    // if changed, change it.
                    item.GameStain = item.GameStain.With(index, stain.RowIndex);
                }
                else if (_stainCombo.Current.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    item.GameStain = item.GameStain.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                item.GameStain = item.GameStain.With(index, Stain.None.RowIndex);
            }
        }
    }

    public bool DrawStains(string id, ref byte dye1, ref byte dye2, float width)
        => DrawStains(id, ref dye1, ref dye2, width, 1.75f);


    public bool DrawStains(string id, ref byte dye1, ref byte dye2, float width, float innerWidthScaler)
    {
        // fetch the correct stain from the stain data
        bool changed = false;
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X) / 2;
        using (ImUtf8.PushId(1))
        {
            var found = ItemSvc.Stains.TryGetValue(dye1, out var stain);
            // draw the stain itemCombo.
            if (_stainCombo.Draw($"##byteStain{id}", widthStains * innerWidthScaler, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss))
            {
                if (ItemSvc.Stains.TryGetValue(_stainCombo.Current.Key, out stain))
                {
                    dye1 = stain.RowIndex.Id;
                    changed = true;
                }
                else if (_stainCombo.Current.Key == Stain.None.RowIndex)
                {
                    dye1 = Stain.None.RowIndex.Id;
                    changed = true;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                dye1 = Stain.None.RowIndex.Id;
                changed = true;
            }
        }

        ImUtf8.SameLineInner();
        using (ImUtf8.PushId(2))
        {
            var found = ItemSvc.Stains.TryGetValue(dye2, out var stain);
            // draw the stain itemCombo.
            if (_stainCombo.Draw($"##byteStain{id}", widthStains * innerWidthScaler, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss))
            {
                if (ItemSvc.Stains.TryGetValue(_stainCombo.Current.Key, out stain))
                {
                    dye2 = stain.RowIndex.Id;
                    changed = true;
                }
                else if (_stainCombo.Current.Key == Stain.None.RowIndex)
                {
                    dye2 = Stain.None.RowIndex.Id;
                    changed = true;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                dye2 = Stain.None.RowIndex.Id;
                changed = true;
            }
        }
        return changed;
    }


    private void DrawCustomStains<T>(T item, string id, float width) where T : IRestrictionRef
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (item.CustomStains.Count - 1)) / item.CustomStains.Count;

        foreach (var (stainId, index) in item.CustomStains.WithIndex())
        {
            using var _ = ImUtf8.PushId(index);
            var found = ItemSvc.Stains.TryGetValue(stainId, out var stain);
            // draw the stain itemCombo.
            var change = _stainCombo.Draw($"##customStain{id}", width, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss);
            if (index < item.CustomStains.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (ItemSvc.Stains.TryGetValue(_stainCombo.Current.Key, out stain))
                {
                    // if changed, change it.
                    item.CustomStains = item.CustomStains.With(index, stain.RowIndex);
                }
                else if (_stainCombo.Current.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    item.CustomStains = item.CustomStains.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                item.CustomStains = item.CustomStains.With(index, Stain.None.RowIndex);
            }
        }
    }
}
