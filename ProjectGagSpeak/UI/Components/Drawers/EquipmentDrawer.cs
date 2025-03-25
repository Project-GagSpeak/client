using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Classes;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Glamourer;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.UI.Components;

/// <summary> Helper for all displays and editors that work with Equip & Stains </summary>
/// <remarks> Can be used for hover tooltips as well and other checks involving Equip & Stains. </remarks>
public class EquipmentDrawer
{
    internal readonly record struct CachedSlotItemData(EquipItem Item);
    
    private static IconCheckboxEx GlamourFlagCheckbox = new(FAI.Vest, CkColor.IconCheckOn.Uint(), CkColor.IconCheckOn.Uint());
    private static IconCheckboxEx ModFlagCheckbox = new(FAI.Eye, CkColor.IconCheckOn.Uint(), CkColor.IconCheckOn.Uint());
    private static IconCheckboxEx MoodleFlagCheckbox = new(FAI.Tint, CkColor.IconCheckOn.Uint(), CkColor.IconCheckOn.Uint());
    private static IconCheckboxEx HardcoreTraitsCheckbox = new(FAI.Gift, CkColor.IconCheckOn.Uint(), CkColor.IconCheckOn.Uint());
    
    private readonly GameItemCombo[] _itemCombos;
    private readonly BonusItemCombo[] _bonusCombos;
    private readonly GameStainCombo _stainCombo;
    private readonly RestrictionCombo _restrictionCombo;


    private readonly ILogger _logger;
    private readonly ItemService _items;
    private readonly TextureService _textures;
    private readonly CosmeticService _cosmetics;
    public EquipmentDrawer(ILogger<EquipmentDrawer> logger, RestrictionManager restrictions,
        FavoritesManager favorites, ItemService items, TextureService textures,
        CosmeticService cosmetics, IDataManager data)
    {
        _logger = logger;
        _items = items;
        _cosmetics = cosmetics;
        _textures = textures;
        // Preassign these 10 itemCombo slots. They will be consistant throughout the plugins usage.
        _itemCombos = EquipSlotExtensions.EqdpSlots.Select(e => new GameItemCombo(data, e, items.ItemData, logger)).ToArray();
        _bonusCombos = BonusExtensions.AllFlags.Select(f => new BonusItemCombo(items, data, f, logger)).ToArray();
        _stainCombo = new GameStainCombo(_items.Stains, logger);
        _restrictionCombo = new RestrictionCombo(logger, favorites, () => 
        [ 
            ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
        ]);
        GameIconSize = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
    }

    // Temporary Cached Storage holding the currently resolved item from the latest hover.
    private CachedSlotItemData LastCachedItem;
    public readonly Vector2 GameIconSize;

    /// <summary> Attempts to get the stain data for the stainId passed in. </summary>
    /// <returns> True if in the stain dictionary, false otherwise.</returns>
    public bool TryGetStain(StainId stain, out Stain data)
        => _items.Stains.TryGetValue(stain, out data);

    /// <summary> Draws a slot provided by a paired Kinkster's SlotCache </summary>
    /// <remarks> Intended for lightweight usage. </remarks>
    public void DrawAppliedSlot(AppliedSlot appliedSlot)
    {
        if (LastCachedItem.Item.Id != appliedSlot.CustomItemId)
            LastCachedItem = new CachedSlotItemData(_items.Resolve((EquipSlot)appliedSlot.Slot, appliedSlot.CustomItemId));

        LastCachedItem.Item.DrawIcon(_textures, GameIconSize, (EquipSlot)appliedSlot.Slot);
    }

    // Method for Drawing the Associated Glamour Item (Singular)
    public void DrawAssociatedGlamour(string id, GlamourSlot item, float width)
    {
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var iconH = ImGui.GetFrameHeight() * 3 + style.ItemSpacing.Y * 2;
        var winSize = new Vector2(width, iconH);
        using (CkComponents.CenterHeaderChild("AssociatedGlamour" + id, "Associated Glamour", winSize, WFlags.AlwaysUseWindowPadding))
        {
            // get the innder width after the padding is applied.
            var widthInner = ImGui.GetContentRegionAvail().X;
            item.GameItem.DrawIcon(_textures, new Vector2(iconH), item.Slot);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                // Begin by drawing out the slot enum dropdown that spans the remaining content region.
                var barWidth = widthInner - iconH - ImGui.GetStyle().ItemInnerSpacing.X;

                if (CkGuiUtils.EnumCombo("##" + id + "slot itemCombo", barWidth, item.Slot, out var newSlot,
                    EquipSlotExtensions.EqdpSlots, (slot) => slot.ToName(), "Select Slot..."))
                {
                    item.Slot = newSlot;
                    item.GameItem = ItemService.NothingItem(item.Slot);
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

        // Determine what we are drawing based on the type of slot.
        if (restraintSlot is RestraintSlotBasic basicSlot)
        {
            DrawRestraintSlotBasic(basicSlot, fullWidth, out bool swapped);
            if (swapped)
                slots[focus] = new RestraintSlotAdvanced() { CustomStains = basicSlot.Stains };
        }
        else if (restraintSlot is RestraintSlotAdvanced advSlot)
        {
            DrawRestraintSlotAdvanced(advSlot, fullWidth, out bool swapped);
            if (swapped)
            {
                var prevStains = advSlot.CustomStains;
                var newBasic = new RestraintSlotBasic();
                newBasic.Glamour.GameStain = prevStains;
                slots[focus] = newBasic;
            }
        }
    }

    public void DrawRestraintSlotBasic(RestraintSlotBasic basicSlot, float width, out bool swapped)
    {
        swapped = false;
        var totalHeight = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        using var group = ImRaii.Group();
        // Draw out the icon firstly.
        basicSlot.EquipItem.DrawIcon(_textures, new Vector2(totalHeight), basicSlot.EquipSlot);
        ImUtf8.SameLineInner();

        var rightEndWidth = totalHeight;
        using (ImRaii.Group())
        {
            // First Row.
            if (CkGui.IconButton(FAI.ArrowsLeftRight))
            {
                swapped = true;
                return;
            }
            ImUtf8.SameLineInner();
            DrawItem(basicSlot.Glamour, ImGui.GetContentRegionAvail().X - rightEndWidth);

            // Second Row.
            var overlayState = basicSlot.ApplyFlags.HasAny(RestraintFlags.IsOverlay);
            if (CkGui.IconButton(overlayState ? FAI.Eye : FAI.EyeSlash))
                basicSlot.ApplyFlags ^= RestraintFlags.IsOverlay;
            ImUtf8.SameLineInner();
            DrawStains(basicSlot.Glamour, ImGui.GetContentRegionAvail().X - rightEndWidth);
        }
    }

    public void DrawRestraintSlotAdvanced(RestraintSlotAdvanced advSlot, float width, out bool swapped)
    {
        swapped = false;
        var totalHeight = ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;
        using var group = ImRaii.Group();
        // Determine what should be drawn as the image. If it has a thumbnail, we should use that.
        if(!advSlot.Ref.ThumbnailPath.IsNullOrWhitespace())
        {
            var thumbnailImage = _cosmetics.GetImageFromThumbnailPath(advSlot.Ref.ThumbnailPath);
            if (thumbnailImage is { } wrap)
            {
                var pos = ImGui.GetCursorScreenPos();
                ImGui.GetWindowDrawList().AddDalamudImageRounded(wrap, pos, new Vector2(totalHeight), ImGui.GetStyle().FrameRounding);
                ImGui.Dummy(new Vector2(totalHeight));
            }
            else
            {
                advSlot.EquipItem.DrawIcon(_textures, new Vector2(totalHeight), advSlot.EquipSlot);
            }
        }

        ImUtf8.SameLineInner();
        var rightEndWidth = totalHeight;
        using (ImRaii.Group())
        {
            // First Row.
            if (CkGui.IconButton(FAI.ArrowsLeftRight))
            {
                swapped = true;
                return;
            }
            ImUtf8.SameLineInner();
            var comboWidth = ImGui.GetContentRegionAvail().X - rightEndWidth;
            _restrictionCombo.Draw("##AdvancedSlotSelector" + advSlot.EquipSlot, advSlot.Ref.Identifier, comboWidth);

            // Second Row.
            var overlayState = advSlot.ApplyFlags.HasAny(RestraintFlags.IsOverlay);
            if (CkGui.IconButton(overlayState ? FAI.Eye : FAI.EyeSlash))
                advSlot.ApplyFlags ^= RestraintFlags.IsOverlay;
            ImUtf8.SameLineInner();
            DrawCustomStains(advSlot, comboWidth);
        }
        // Beside this, draw the restraint flag editor.
        ImGui.SameLine();
        DrawAdvancedSlotFlags(advSlot);
    }

    private void DrawAdvancedSlotFlags(RestraintSlotAdvanced advSlot)
    {
        var spacing = ImGui.GetStyle().ItemInnerSpacing;
        var rounding = ImGui.GetStyle().FrameRounding;
        var region = new Vector2(ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Child("##AdvSlotFlags" + advSlot.EquipSlot, region, false))
        {
            var curGlam = advSlot.ApplyFlags.HasAny(RestraintFlags.Glamour);
            var curMod = advSlot.ApplyFlags.HasAny(RestraintFlags.Mod);
            var curMoodle = advSlot.ApplyFlags.HasAny(RestraintFlags.Moodle);
            var curHcTrait = advSlot.ApplyFlags.HasAny(RestraintFlags.Trait);

            if (GlamourFlagCheckbox.Draw("##GlamFlagToggle" + advSlot.EquipSlot, curGlam, out var newGlam) && curGlam != newGlam)
                advSlot.ApplyFlags ^= RestraintFlags.Glamour;
            ImUtf8.SameLineInner();
            if (ModFlagCheckbox.Draw("##ModFlagToggle" + advSlot.EquipSlot, curMod, out var newMod) && curMod != newMod)
                advSlot.ApplyFlags ^= RestraintFlags.Mod;
            // Next Line.
            if (MoodleFlagCheckbox.Draw("##MoodleFlagToggle" + advSlot.EquipSlot, curMoodle, out var newMoodle) && curMoodle != newMoodle)
                advSlot.ApplyFlags ^= RestraintFlags.Moodle;
            ImUtf8.SameLineInner();
            if (HardcoreTraitsCheckbox.Draw("##TraitFlagToggle" + advSlot.EquipSlot, curHcTrait, out var newHcTrait) && curHcTrait != newHcTrait)
                advSlot.ApplyFlags ^= RestraintFlags.Trait;
        }
        // Draw a bordered rect around this.
        var min = ImGui.GetItemRectMin() - spacing;
        var max = ImGui.GetItemRectMax() + spacing;
        ImGui.GetWindowDrawList().AddRectFilled(min, max, CkColor.FancyHeaderContrast.Uint(), rounding);
        ImGui.GetWindowDrawList().AddRect(min, max, CkColor.FancyHeaderContrast.Uint(), rounding);
    }

    private void DrawItem(GlamourSlot item, float width)
    {
        // draw the item itemCombo.
        var itemCombo = _itemCombos[item.Slot.ToIndex()];

        var change = itemCombo.Draw(item.GameItem.Name, item.GameItem.ItemId, width, width * 1.25f);

        if (change && !item.GameItem.Equals(itemCombo.CurrentSelection))
        {
            _logger.LogTrace($"Item changed from {itemCombo.CurrentSelection} " +
                $"[{itemCombo.CurrentSelection.ItemId}] to {item.GameItem} [{item.GameItem.ItemId}]");
            item.GameItem = itemCombo.CurrentSelection;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace($"Item changed to {ItemService.NothingItem(item.Slot)} " +
                $"[{ItemService.NothingItem(item.Slot).ItemId}] from {item.GameItem} [{item.GameItem.ItemId}]");
            item.GameItem = ItemService.NothingItem(item.Slot);
        }
    }

    private void DrawStains(GlamourSlot item, float width)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (item.GameStain.Count - 1)) / item.GameStain.Count;

        foreach (var (stainId, index) in item.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = TryGetStain(stainId, out var stain);
            // draw the stain itemCombo.
            var change = _stainCombo.Draw($"##stain{item.Slot}", widthStains * 1.5f, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss);
            if (index < item.GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (TryGetStain(_stainCombo.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    item.GameStain = item.GameStain.With(index, stain.RowIndex);
                }
                else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
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

    private void DrawCustomStains(RestraintSlotAdvanced item, float width)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (item.CustomStains.Count - 1)) / item.CustomStains.Count;

        foreach (var (stainId, index) in item.CustomStains.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = TryGetStain(stainId, out var stain);
            // draw the stain itemCombo.
            var change = _stainCombo.Draw($"##customStain{item.EquipSlot}", widthStains * 1.5f, widthStains, stain.RgbaColor, stain.Name, found, stain.Gloss);
            if (index < item.CustomStains.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one.

            // if we had a change made, update the stain data.
            if (change)
            {
                if (TryGetStain(_stainCombo.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    item.CustomStains = item.CustomStains.With(index, stain.RowIndex);
                }
                else if (_stainCombo.CurrentSelection.Key == Stain.None.RowIndex)
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

    /*    private void DrawBonusItem(ref RestraintSet refRestraintSet, BonusItemFlag flag, float width)
        {
            using var id = ImRaii.PushId((int)refRestraintSet.BonusDrawData[flag].Slot);
            var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

            bool clear = ImGui.IsItemClicked(ImGuiMouseButton.Right);
            bool open = ImGui.IsItemClicked(ImGuiMouseButton.Left);

            // Assuming _bonusItemCombo is similar to ItemCombos but for bonus items
            var itemCombo = BonusItemCombos[refRestraintSet.BonusDrawData[flag].Slot.ToIndex()];

            if (open)
                ImGui.OpenPopup($"##{itemCombo.Label}");

            var change = itemCombo.Draw(refRestraintSet.BonusDrawData[flag].GameItem.Name,
                refRestraintSet.BonusDrawData[flag].GameItem.Id.BonusItem,
                width, ComboWidth * 1.3f);

            if (change && !refRestraintSet.BonusDrawData[flag].GameItem.Equals(itemCombo.CurrentSelection))
            {
                // log full details.
                _logger.LogTrace($"Item changed from {itemCombo.CurrentSelection} [{itemCombo.CurrentSelection.PrimaryId}] " +
                    $"to {refRestraintSet.BonusDrawData[flag].GameItem} [{refRestraintSet.BonusDrawData[flag].GameItem.PrimaryId}]");
                // change
                refRestraintSet.BonusDrawData[flag].GameItem = itemCombo.CurrentSelection;
            }

            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // Assuming a method to handle item reset or clear, similar to your DrawItem method
                _logger.LogTrace($"Item reset to default for slot {flag}");
                refRestraintSet.BonusDrawData[flag].GameItem = EquipItem.BonusItemNothing(flag);
            }
        }*/
}
