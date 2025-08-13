//using Dalamud.Interface.Utility.Raii;
//using GagSpeak.State;
//using GagSpeak.Gui.Components;
//using GagSpeak.Gui.Handlers;
//using GagSpeak.Utils;
//using GagspeakAPI.Data;
//using Dalamud.Bindings.ImGui;
//using OtterGui;
//using OtterGui.Text;
//using OtterGui.Widgets;
//using Penumbra.GameData.Enums;
//using Penumbra.GameData.Structs;

//namespace GagSpeak.Gui.Components;

//public class SetPreviewComponent
//{
//    private readonly ILogger<SetPreviewComponent> _logger;
//    private readonly GameItemStainHandler _textureHandler;
//    public SetPreviewComponent(ILogger<SetPreviewComponent> logger, GameItemStainHandler textureHandler)
//    {
//        _logger = logger;
//        _textureHandler = textureHandler;

//        GameIconSize = new Vector2(2 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y);
//        ItemCombos = _textureHandler.ObtainItemCombos();
//        StainColorCombos = _textureHandler.ObtainStainCombos(175);
//    }

//    private Vector2 GameIconSize;
//    private readonly GameItemCombo[] ItemCombos;
//    private readonly StainColorCombo StainColorCombos;
//    // A temp storage container for the currently previewed restraint set.
//    // Useful for loading in light restraint data without constantly resolving the item on every drawFrame.
//    private (Guid Id, Dictionary<EquipSlot, EquipItem> CachedRestraint) CachedPreview = (Guid.Empty, new Dictionary<EquipSlot, EquipItem>());
//    private (ulong CustomItemId, EquipItem SlotEquipItem) CachedSlotItem = (ulong.MaxValue, ItemIdVars.NothingItem(EquipSlot.Head));

//    public void DrawRestraintSetPreviewCentered(RestraintSet set, Vector2 contentRegion)
//    {
//        // We should use the content region space to define how to center the content that we will draw.
//        var columnWidth = GameIconSize.X + ImGui.GetFrameHeight();

//        // Determine the total width of the table.
//        var totalTableWidth = columnWidth * 2 + ImGui.GetStyle().ItemSpacing.X;
//        var totalTableHeight = GameIconSize.Y * 6 + 5f;

//        // Calculate the offset to center the table within the content region.
//        var offsetX = (contentRegion.X - totalTableWidth) / 2;
//        var offsetY = (contentRegion.Y - totalTableHeight) / 2;

//        // Apply the offset to center the table.
//        ImGui.SetCursorPos(new Vector2(ImGui.GetCursorPosX() + offsetX, ImGui.GetCursorPosY() + offsetY));

//        DrawRestraintSetDisplay(set);
//    }

//    public void DrawAppliedSlot(AppliedSlot appliedSlot)
//    {
//        if(CachedSlotItem.CustomItemId != appliedSlot.CustomItemId)
//        {
//            // update the cached slot item.
//            CachedSlotItem.CustomItemId = appliedSlot.CustomItemId;
//            CachedSlotItem.SlotEquipItem = ItemIdVars.Resolve((EquipSlot)appliedSlot.Slot, appliedSlot.CustomItemId);
//            _logger.LogInformation($"Updated CachedSlotItem with new CustomItemId: {appliedSlot.CustomItemId}");
//        }
//        CachedSlotItem.SlotEquipItem.DrawIcon(_textureHandler.IconData, GameIconSize, (EquipSlot)appliedSlot.Slot);
//    }

//    public void DrawRestraintOnHover(RestraintSet set)
//    {
//        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
//        {
//            ImGui.BeginTooltip();
//            DrawRestraintSetDisplay(set);
//            ImGui.EndTooltip();
//        }
//    }

//    public void DrawLightRestraintOnHover(LightRestraintData lightSet)
//    {
//        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
//        {
//            ImGui.BeginTooltip();
//            DrawRestraintSetDisplay(lightSet);
//            ImGui.EndTooltip();
//        }
//    }

//    public void DrawEquipSlotPreview(GlamourSlot refGlamourSlot, float totalLength)
//    {
//        refGlamourSlot.GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, refGlamourSlot.Slot);
//        ImGui.SameLine(0, 3);
//        using (var groupDraw = ImRaii.Group()) DrawStain(refGlamourSlot);
//    }

//    // Reapproach this later.
//    private void DrawRestraintSetDisplay(RestraintSet set)
//    {
//        // Draw the table.
//*//*        using (var equipIconsTable = ImRaii.Table("equipIconsTable", 2, ImGuiTableFlags.RowBg))
//        {
//            if (!equipIconsTable) return;
//            // Create the headers for the table
//            var width = GameIconSize.X + ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.X;
//            // setup the columns
//            ImGui.TableSetupColumn("EquipmentSlots", ImGuiTableColumnFlags.WidthFixed, width);
//            ImGui.TableSetupColumn("AccessorySlots", ImGuiTableColumnFlags.WidthStretch);

//            // draw out the equipment slots
//            ImGui.TableNextRow(); ImGui.TableNextColumn();

//            foreach (var slot in EquipSlotExtensions.EquipmentSlots)
//            {
//                set.DrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
//                ImGui.SameLine(0, 3);
//                using (var groupDraw = ImRaii.Group())
//                {
//                    DrawStain(set, slot);
//                }
//            }
//            foreach (var slot in BonusExtensions.AllFlags)
//            {
//                set.BonusDrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
//            }

//            ImGui.TableNextColumn();

//            //draw out the accessory slots
//            foreach (var slot in EquipSlotExtensions.AccessorySlots)
//            {
//                set.DrawData[slot].GameItem.DrawIcon(_textureHandler.IconData, GameIconSize, slot);
//                ImGui.SameLine(0, 3);
//                using (var groupDraw = ImRaii.Group())
//                {
//                    DrawStain(set, slot);
//                }
//            }
//        }*//*
//    }
//    private void DrawRestraintSetDisplay(LightRestraintData lightSet)
//    {
//        if (CachedPreview.Id != lightSet.Identifier)
//        {
//            ImGui.Text("Loading...");
//            LoadCacheFromLightSet(lightSet);
//            return;
//        }

//        using var equipIconsTable = ImRaii.Table("equipIconsTable", 2, ImGuiTableFlags.RowBg);
//        if (!equipIconsTable) return;

//        var width = GameIconSize.X;
//        ImGui.TableSetupColumn("EquipmentSlots", ImGuiTableColumnFlags.WidthFixed, width);
//        ImGui.TableSetupColumn("AccessorySlots", ImGuiTableColumnFlags.WidthStretch);
//        ImGui.TableNextRow(); ImGui.TableNextColumn();

//        // draw out the equipment slots (maybe add support for stains but idk lol.)
//        foreach (var slot in EquipSlotExtensions.EquipmentSlots)
//            CachedPreview.CachedRestraint[slot].DrawIcon(_textureHandler.IconData, GameIconSize, slot);
//        ImGui.TableNextColumn();
//        // draw out the accessory slots (maybe add support for stains but idk lol.)
//        foreach (var slot in EquipSlotExtensions.AccessorySlots)
//            CachedPreview.CachedRestraint[slot].DrawIcon(_textureHandler.IconData, GameIconSize, slot);
//    }

//    private void LoadCacheFromLightSet(LightRestraintData lightSet)
//    {
//        CachedPreview.CachedRestraint.Clear();
//        CachedPreview.Id = lightSet.Identifier;
//        foreach (var slot in EquipSlotExtensions.EqdpSlots)
//        {
//            var customIdForSlot = lightSet.AffectedSlots.FirstOrDefault(x => x.Slot == (int)slot)?.CustomItemId ?? ulong.MaxValue;
//            CachedPreview.CachedRestraint[slot] = ItemIdVars.Resolve(slot, customIdForSlot);
//        }
//    }

//    private void DrawStain(RestraintSet refSet, EquipSlot slot)
//    {
//        // THIS IS A LOT MORE IN DEPTH TO LOGIC NOW.
//        // Maybe make it reference the RestrictionCache instead.


//*//*      // draw the stain combo for each of the 2 dyes (or just one)
//        foreach (var (stainId, index) in refSet.RestraintSlots)
//        {
//            // continue if slot is not basic and has no glamour flag.
//            if (slot != EquipSlot.Head && slot != EquipSlot.Body && slot != EquipSlot.Legs && slot != EquipSlot.Feet)
//                continue;
//            using var id = ImUtf8.PushId(index);
//            var found = _textureHandler.TryGetStain(stainId, out var stain);
//            // draw the stain combo, but dont make it hoverable
//            using (ImRaii.Disabled(true))
//                StainColorCombos.Draw($"##stain{refSet.DrawData[slot].Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, MouseWheelType.None);
//        }*//*
//    }

//    private void DrawStain(GlamourSlot refGlamourSlot)
//    {

//        // draw the stain combo for each of the 2 dyes (or just one)
//        foreach (var (stainId, index) in refGlamourSlot.GameStain.WithIndex())
//        {
//            using var id = ImUtf8.PushId(index);
//            var found = _textureHandler.TryGetStain(stainId, out var stain);
//            // draw the stain combo, but dont make it hoverable
//            using (ImRaii.Disabled(true))
//                StainColorCombos.Draw($"##EquipStainPreview{refGlamourSlot.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, MouseWheelType.None);
//        }
//    }

//    public void DrawEquipDataDetailedSlot(GlamourSlot refGlamourSlot, float totalLength)
//    {
//        var iconSize = new Vector2(3 * ImGui.GetFrameHeight() + ImGui.GetStyle().ItemSpacing.Y * 2);

//        refGlamourSlot.GameItem.DrawIcon(_textureHandler.IconData, iconSize, refGlamourSlot.Slot);
//        // if we right click the icon, clear it
//        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
//            refGlamourSlot.GameItem = ItemIdVars.NothingItem(refGlamourSlot.Slot);

//        ImUtf8.SameLineInner();
//        using (ImRaii.Group())
//        {
//            var refValue = (int)refGlamourSlot.Slot.ToIndex();
//            ImGui.SetNextItemWidth((totalLength - ImGui.GetStyle().ItemInnerSpacing.X - iconSize.X));
//            if (ImGui.Combo("##DetailedSlotEquip", ref refValue, EquipSlotExtensions.EqdpSlots.Select(slot => slot.ToName()).ToArray(), EquipSlotExtensions.EqdpSlots.Count))
//            {
//                refGlamourSlot.Slot = EquipSlotExtensions.EqdpSlots[refValue];
//                refGlamourSlot.GameItem = ItemIdVars.NothingItem(refGlamourSlot.Slot);
//            }

//            DrawEquipDataSlot(refGlamourSlot, (totalLength - ImGui.GetStyle().ItemInnerSpacing.X - iconSize.X), false);
//        }
//    }

//    private void DrawEquipDataSlot(GlamourSlot refGlamourSlot, float totalLength, bool allowMouse)
//    {
//        using var id = ImRaii.PushId((int)refGlamourSlot.Slot);
//        var spacing = ImGui.GetStyle().ItemInnerSpacing with { Y = ImGui.GetStyle().ItemSpacing.Y };
//        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing);

//        var right = ImGui.IsItemClicked(ImGuiMouseButton.Right);
//        var left = ImGui.IsItemClicked(ImGuiMouseButton.Left);

//        using var group = ImRaii.Group();

//        DrawEditableItem(refGlamourSlot, right, left, totalLength, allowMouse);
//        DrawEditableStain(refGlamourSlot, totalLength);
//    }

//    private void DrawEditableItem(GlamourSlot refGlamourSlot, bool clear, bool open, float width, bool allowMouse)
//    {
//        // draw the item combo.
//        var combo = ItemCombos[refGlamourSlot.Slot.ToIndex()];
//        if (open)
//        {
//            GenericHelpers.OpenCombo($"##WardrobeCreateNewSetItem-{refGlamourSlot.Slot}");
//            _logger.LogTrace($"{combo.Label} Toggled");
//        }
//        // draw the combo
//        var change = combo.Draw(refGlamourSlot.GameItem.Name, refGlamourSlot.GameItem.ItemId, width, width * 1.3f, allowMouseWheel: allowMouse);

//        // if we changed something
//        if (change && !refGlamourSlot.GameItem.Equals(combo.Current))
//        {
//            // log full details.
//            _logger.LogTrace($"Item changed from {combo.Current} [{combo.Current.ItemId}] " +
//                $"to {refGlamourSlot.GameItem} [{refGlamourSlot.GameItem.ItemId}]");
//            // update the item to the new selection.
//            refGlamourSlot.GameItem = combo.Current;
//        }

//        // if we right clicked
//        if (clear || ImGui.IsItemClicked(ImGuiMouseButton.Right))
//        {
//            // if we right click the item, clear it.
//            _logger.LogTrace($"Item changed to {ItemIdVars.NothingItem(refGlamourSlot.Slot)} " +
//                $"[{ItemIdVars.NothingItem(refGlamourSlot.Slot).ItemId}] " +
//                $"from {refGlamourSlot.GameItem} [{refGlamourSlot.GameItem.ItemId}]");
//            // clear the item.
//            refGlamourSlot.GameItem = ItemIdVars.NothingItem(refGlamourSlot.Slot);
//        }
//    }

//    private void DrawEditableStain(GlamourSlot refGlamourSlot, float width)
//    {
//        // fetch the correct stain from the stain data
//        var widthStains = (width - ImUtf8.ItemInnerSpacing.X * (refGlamourSlot.GameStain.Count - 1)) / refGlamourSlot.GameStain.Count;

//        // draw the stain combo for each of the 2 dyes (or just one)
//        foreach (var (stainId, index) in refGlamourSlot.GameStain.WithIndex())
//        {
//            using var id = ImUtf8.PushId(index);
//            var found = _textureHandler.TryGetStain(stainId, out var stain);
//            // draw the stain combo.
//            var change = StainColorCombos.Draw($"##cursedStain{refGlamourSlot.Slot}", stain.RgbaColor, stain.Name, found, stain.Gloss, widthStains);
//            if (index < refGlamourSlot.GameStain.Count - 1)
//                ImUtf8.SameLineInner(); // instantly go to draw the next one if there are two stains

//            // if we had a change made, update the stain data.
//            if (change)
//            {
//                if (_textureHandler.TryGetStain(StainColorCombos.Current.Key, out stain))
//                {
//                    // if changed, change it.
//                    refGlamourSlot.GameStain = refGlamourSlot.GameStain.With(index, stain.RowIndex);
//                }
//                else if (StainColorCombos.Current.Key == Stain.None.RowIndex)
//                {
//                    // if set to none, reset it to default
//                    refGlamourSlot.GameStain = refGlamourSlot.GameStain.With(index, Stain.None.RowIndex);
//                }
//            }
//            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
//            {
//                // reset the stain to default
//                refGlamourSlot.GameStain = refGlamourSlot.GameStain.With(index, Stain.None.RowIndex);
//            }
//        }
//    }
//}

