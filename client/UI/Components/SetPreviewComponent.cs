using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Handlers;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Numerics;

namespace GagSpeak.UI.Components;

public class SetPreviewComponent
{
    private readonly ILogger<SetPreviewComponent> _logger;
    private readonly GameItemStainHandler _textureHandler;
    public SetPreviewComponent(ILogger<SetPreviewComponent> logger, GameItemStainHandler textureHandler)
    {
        _logger = logger;
        _textureHandler = textureHandler;

        GameIconSize = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
        ItemCombos = _textureHandler.ObtainItemCombos();
        StainColorCombos = _textureHandler.ObtainStainCombos(175);
    }

    private Vector2 GameIconSize;
    private readonly GameItemCombo[] ItemCombos;
    private readonly StainColorCombo StainColorCombos;

    public void DrawRestraintSetPreviewCentered(RestraintSet set, Vector2 contentRegion)
    {
        // We should use the content region space to define how to center the content that we will draw.
        var columnWidth = GameIconSize.X + ImGui.GetFrameHeight();

        // Determine the total width of the table.
        var totalTableWidth = columnWidth * 2 + ImGui.GetStyle().ItemSpacing.X;
        var totalTableHeight = GameIconSize.Y * 6 + 10f;

        // Calculate the offset to center the table within the content region.
        var offsetX = (contentRegion.X - totalTableWidth) / 2;
        var offsetY = (contentRegion.Y - totalTableHeight) / 2;

        // Apply the offset to center the table.
        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + offsetX, ImGui.GetCursorPosY() + offsetY));

        DrawRestraintSetDisplay(set);
    }

    public void DrawRestraintSetPreviewTooltip(RestraintSet set)
    {
        ImGui.BeginTooltip();
        DrawRestraintSetDisplay(set);
        ImGui.EndTooltip();
    }

    public void DrawEquipSlotPreview(GagDrawData refData, float totalLength)
    {
        refData.GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, refData.Slot);
        ImGui.SameLine(0, 3);
        using (var groupDraw = ImRaii.Group()) DrawStain(refData);
    }

    private void DrawRestraintSetDisplay(RestraintSet set)
    {
        // Draw the table.
        using (var equipIconsTable = ImRaii.Table("equipIconsTable", 2, ImGuiTableFlags.RowBg))
        {
            if (!equipIconsTable) return;
            // Create the headers for the table
            var width = GameIconSize.X + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X;
            // setup the columns
            ImGui.TableSetupColumn("EquipmentSlots", ImGuiTableColumnFlags.WidthFixed, width);
            ImGui.TableSetupColumn("AccessorySlots", ImGuiTableColumnFlags.WidthStretch);

            // draw out the equipment slots
            ImGui.TableNextRow(); ImGui.TableNextColumn();

            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
            {
                set.DrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
                ImGui.SameLine(0, 3);
                using (var groupDraw = ImRaii.Group())
                {
                    DrawStain(set, slot);
                }
            }
            foreach (var slot in BonusExtensions.AllFlags)
            {
                set.BonusDrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
            }

            ImGui.TableNextColumn();

            //draw out the accessory slots
            foreach (var slot in EquipSlotExtensions.AccessorySlots)
            {
                set.DrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
                ImGui.SameLine(0, 3);
                using (var groupDraw = ImRaii.Group())
                {
                    DrawStain(set, slot);
                }
            }
        }
    }

    private void DrawStain(RestraintSet refSet, EquipSlot slot)
    {

        // draw the stain combo for each of the 2 dyes (or just one)
        foreach (var (stainId, index) in refSet.DrawData[slot].GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _textureHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo, but dont make it hoverable
            using (var disabled = ImRaii.Disabled(true))
            {
                StainColorCombos.Draw($"##stain{refSet.DrawData[slot].Slot}",
                    stain.RgbaColor, stain.Name, found, stain.Gloss, MouseWheelType.None);
            }
        }
    }

    private void DrawStain(GagDrawData refEquipItem)
    {

        // draw the stain combo for each of the 2 dyes (or just one)
        foreach (var (stainId, index) in refEquipItem.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _textureHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo, but dont make it hoverable
            using (var disabled = ImRaii.Disabled(true))
            {
                StainColorCombos.Draw($"##EquipStainPreview{refEquipItem.Slot}",
                    stain.RgbaColor, stain.Name, found, stain.Gloss, MouseWheelType.None);
            }
        }
    }

    public void DrawEquipDataDetailedSlot(EquipDrawData refData, float totalLength)
    {
        var iconSize = new Vector2(3 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2);

        refData.GameItem.DrawIcon(_textureHandler.IconData, iconSize, refData.Slot);
        // if we right click the icon, clear it
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            refData.GameItem = ItemIdVars.NothingItem(refData.Slot);

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            var refValue = (int)refData.Slot.ToIndex();
            ImGui.SetNextItemWidth((totalLength - ImGui.GetStyle().ItemInnerSpacing.X - iconSize.X));
            if (ImGui.Combo("##DetailedSlotEquip", ref refValue, EquipSlotExtensions.EqdpSlots.Select(slot => slot.ToName()).ToArray(), EquipSlotExtensions.EqdpSlots.Count))
            {
                refData.Slot = EquipSlotExtensions.EqdpSlots[refValue];
                refData.GameItem = ItemIdVars.NothingItem(refData.Slot);
            }

            DrawEquipDataSlot(refData, (totalLength - ImGui.GetStyle().ItemInnerSpacing.X - iconSize.X), false);
        }
    }

    private void DrawEquipDataSlot(EquipDrawData refData, float totalLength, bool allowMouse)
    {
        using var id = ImRaii.PushId((int)refData.Slot);
        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

        using var group = ImRaii.Group();

        DrawEditableItem(refData, right, left, totalLength, allowMouse);
        DrawEditableStain(refData, totalLength);
    }

    private void DrawEditableItem(EquipDrawData refData, bool clear, bool open, float width, bool allowMouse)
    {
        // draw the item combo.
        var combo = ItemCombos[refData.Slot.ToIndex()];
        if (open)
        {
            GenericHelpers.OpenCombo($"##WardrobeCreateNewSetItem-{refData.Slot}");
            _logger.LogTrace($"{combo.Label} Toggled");
        }
        // draw the combo
        var change = combo.Draw(refData.GameItem.Name, refData.GameItem.ItemId, width, width * 1.3f, allowMouseWheel: allowMouse);

        // if we changed something
        if (change && !refData.GameItem.Equals(combo.CurrentSelection))
        {
            // log full details.
            _logger.LogTrace($"Item changed from {combo.CurrentSelection} [{combo.CurrentSelection.ItemId}] " +
                $"to {refData.GameItem} [{refData.GameItem.ItemId}]");
            // update the item to the new selection.
            refData.GameItem = combo.CurrentSelection;
        }

        // if we right clicked
        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            // if we right click the item, clear it.
            _logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(refData.Slot)} " +
                $"[{ItemIdVars.NothingItem(refData.Slot).ItemId}] " +
                $"from {refData.GameItem} [{refData.GameItem.ItemId}]");
            // clear the item.
            refData.GameItem = ItemIdVars.NothingItem(refData.Slot);
        }
    }

    private void DrawEditableStain(EquipDrawData refData, float width)
    {
        // fetch the correct stain from the stain data
        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (refData.GameStain.Count - 1)) / refData.GameStain.Count;

        // draw the stain combo for each of the 2 dyes (or just one)
        foreach (var (stainId, index) in refData.GameStain.WithIndex())
        {
            using var id = ImUtf8.PushId(index);
            var found = _textureHandler.TryGetStain(stainId, out var stain);
            // draw the stain combo.
            var change = StainColorCombos.Draw($"##cursedStain{refData.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
            if (index < refData.GameStain.Count - 1)
                ImUtf8.SameLineInner(); // instantly go to draw the next one if there are two stains

            // if we had a change made, update the stain data.
            if (change)
            {
                if (_textureHandler.TryGetStain(StainColorCombos.CurrentSelection.Key, out stain))
                {
                    // if changed, change it.
                    refData.GameStain = refData.GameStain.With(index, stain.RowIndex);
                }
                else if (StainColorCombos.CurrentSelection.Key == Stain.None.RowIndex)
                {
                    // if set to none, reset it to default
                    refData.GameStain = refData.GameStain.With(index, Stain.None.RowIndex);
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                // reset the stain to default
                refData.GameStain = refData.GameStain.With(index, Stain.None.RowIndex);
            }
        }
    }
}
