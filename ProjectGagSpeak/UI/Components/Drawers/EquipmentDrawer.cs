using Dalamud.Plugin.Services;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CustomCombos.Glamourer;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.UI.Components;

/// <summary> Helper for all displays and editors that work with Equip & Stains </summary>
/// <remarks> Can be used for hover tooltips as well and other checks involving Equip & Stains. </remarks>
public class EquipmentDrawer
{
    /// <summary> An internal struct used to represent the latest cached item data on hover. </summary>
    internal readonly struct CachedSlotItemData(EquipItem item)
    {
        public readonly EquipItem Item = item;
        public readonly CustomItemId ItemId => Item.Id;
    }

    private readonly ILogger _logger;
    private readonly DictStain _stains;
    private readonly ItemService _items;
    public readonly TextureService ItemIcons;
    public readonly GameItemCombo[] ItemCombos;
    public readonly BonusItemCombo[] BonusCombo;

    public EquipmentDrawer(ILogger<EquipmentDrawer> logger, ItemService items, TextureService textures, IDataManager data)
    {
        _logger = logger;
        _items = items;
        _stains = items.Stains;
        ItemIcons = textures;
        // Preassign these 10 combo slots. They will be consistant throughout the plugins usage.
        ItemCombos = EquipSlotExtensions.EqdpSlots.Select(e => new GameItemCombo(data, e, items.ItemData, logger)).ToArray();
        BonusCombo = BonusExtensions.AllFlags.Select(f => new BonusItemCombo(items, data, f, logger)).ToArray();
        GameIconSize = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
    }

    // Temporary Cached Storage holding the currently resolved item from the latest hover.
    private CachedSlotItemData LastCachedItem;
    public readonly Vector2 GameIconSize;

    /// <summary> Public provider for stain color combo data. </summary>
    /// <param name="comboWidth"> The Width of the color stain combo. </param>
    /// <returns> A GameStainCombo object. </returns>
    /// <remarks> We obtain instead of create so we can define the width of the combo. </remarks>
    public GameStainCombo ObtainStainCombos(float comboWidth)
        => new GameStainCombo(comboWidth - 20, _stains, _logger);

    /// <summary> Attempts to get the stain data for the stainId passed in. </summary>
    /// <param name="stain">StainId to check</param>
    /// <param name="data">Stain Data to output.</param>
    /// <returns> True if in the stain dictionary, false otherwise.</returns>
    public bool TryGetStain(StainId stain, out Stain data)
        => _stains.TryGetValue(stain, out data);


    public void DrawAppliedSlot(AppliedSlot appliedSlot)
    {
        // Resolve the item again if it is different from the cached item.
        if (LastCachedItem.ItemId != appliedSlot.CustomItemId)
        {
            LastCachedItem = new CachedSlotItemData(_items.Resolve((EquipSlot)appliedSlot.Slot, appliedSlot.CustomItemId));
            _logger.LogInformation($"Updated CachedSlotItem with new CustomItemId: {appliedSlot.CustomItemId}");
        }

        // display the item.
        LastCachedItem.Item.DrawIcon(ItemIcons, GameIconSize, (EquipSlot)appliedSlot.Slot);
    }

/*
    public void DrawEquip(ref RestraintSet refRestraintSet, EquipSlot slot, float _comboLength)
    {
        using var id = ImRaii.PushId((int)refRestraintSet.DrawData[slot].Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        var ItemWidth = _comboLength - _uiShared.GetIconButtonSize(FontAwesomeIcon.EyeSlash).X - ImUtf8.ItemInnerSpacing.X;

        using var group = ImRaii.Group();
        DrawItem(ref refRestraintSet, out var label, right, left, slot, ItemWidth);
        ImUtf8.SameLineInner();
        FontAwesomeIcon icon = refRestraintSet.DrawData[slot].IsEnabled ? FontAwesomeIcon.Eye : FontAwesomeIcon.EyeSlash;
        if (_uiShared.IconButton(icon))
        {
            refRestraintSet.DrawData[slot].IsEnabled = !refRestraintSet.DrawData[slot].IsEnabled;
        }
        UiSharedService.AttachToolTip("Toggles Apply Style of Item." +
            Environment.NewLine + "EYE Icon (Apply Mode) applies regardless of selected item. (nothing slots make the slot nothing)" +
            Environment.NewLine + "EYE SLASH Icon (Overlay Mode) means that it only will apply the item if it is NOT an nothing slot.");
        DrawStain(ref refRestraintSet, slot, _comboLength);
    }

    private void DrawItem(ref RestraintSet refRestraintSet, out string label, bool clear, bool open, EquipSlot slot, float width)
    {
        // draw the item combo.
        var combo = ItemCombos[refRestraintSet.DrawData[slot].Slot.ToIndex()];
        label = combo.Label;
        if (open)
        {
            GenericHelpers.OpenCombo($"##WardrobeCreateNewSetItem-{slot}");
            _logger.LogTrace($"{combo.Label} Toggled");
        }
        // draw the combo
        var change = combo.Draw(refRestraintSet.DrawData[slot].GameItem.Name,
            refRestraintSet.DrawData[slot].GameItem.ItemId, width, ComboWidth * 1.3f);

        // if we changed something
        if (change && !refRestraintSet.DrawData[slot].GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.ItemId}] " +
                $"to {refRestraintSet.DrawData[slot].GameItem} [{refRestraintSet.DrawData[slot].GameItem.ItemId}]");
            // update the item to the new selection.
            refRestraintSet.DrawData[slot].GameItem = combo.CurrentSelection;
        }

        // if we right clicked
        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // if we right click the item, clear it.
            _logger.LogTrace($"Item changed to {ItemService.NothingItem(refRestraintSet.DrawData[slot].Slot)} " +
                $"[{ItemService.NothingItem(refRestraintSet.DrawData[slot].Slot).ItemId}] " +
                $"from {refRestraintSet.DrawData[slot].GameItem} [{refRestraintSet.DrawData[slot].GameItem.ItemId}]");
            // clear the item.
            refRestraintSet.DrawData[slot].GameItem = ItemService.NothingItem(refRestraintSet.DrawData[slot].Slot);
        }
    }

    private void DrawBonusItem(ref RestraintSet refRestraintSet, BonusItemFlag flag, float width)
    {
        using var id = ImRaii.PushId((int)refRestraintSet.BonusDrawData[flag].Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        bool clear = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        bool open = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        // Assuming _bonusItemCombo is similar to ItemCombos but for bonus items
        var combo = BonusItemCombos[refRestraintSet.BonusDrawData[flag].Slot.ToIndex()];

        if (open)
            ImGui.OpenPopup($"##{combo.Label}");

        var change = combo.Draw(refRestraintSet.BonusDrawData[flag].GameItem.Name,
            refRestraintSet.BonusDrawData[flag].GameItem.Id.BonusItem,
            width, ComboWidth * 1.3f);

        if (change && !refRestraintSet.BonusDrawData[flag].GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.PrimaryId}] " +
                $"to {refRestraintSet.BonusDrawData[flag].GameItem} [{refRestraintSet.BonusDrawData[flag].GameItem.PrimaryId}]");
            // change
            refRestraintSet.BonusDrawData[flag].GameItem = combo.CurrentSelection;
        }

        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // Assuming a method to handle item reset or clear, similar to your DrawItem method
            _logger.LogTrace($"Item reset to default for slot {flag}");
            refRestraintSet.BonusDrawData[flag].GameItem = EquipItem.BonusItemNothing(flag);
        }
    }

    private void DrawStain(ref RestraintSet refRestraintSet, EquipSlot slot, float width)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X *
            (refRestraintSet.DrawData[slot].GameStain.Count - 1)) / refRestraintSet.DrawData[slot].GameStain.Count;

        // draw the stain combo for each of the 2 dyes (or just one)
        foreach (var (stainId, index) in refRestraintSet.DrawData[slot].GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _itemStainHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo.
            var change = GameStainCombos.Draw($"##stain{refRestraintSet.DrawData[slot].Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
            if (index < refRestraintSet.DrawData[slot].GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one if there are two stains

            // if we had a change made, update the stain data.
            if (change)
            {
                if (_itemStainHandler.TryGetStain(GameStainCombos.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, stain.RowIndex);
                }
                else if (GameStainCombos.CurrentSelection.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                refRestraintSet.DrawData[slot].GameStain = refRestraintSet.DrawData[slot].GameStain.With(index, Stain.None.RowIndex);
            }
        }
    }*/
}
