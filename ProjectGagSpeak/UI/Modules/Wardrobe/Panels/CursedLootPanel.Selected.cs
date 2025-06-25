using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Util;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Wardrobe;
public partial class CursedLootPanel : DisposableMediatorSubscriberBase
{
    private void DrawSelectedItemInfo(Vector2 region, float rounding)
    {
        var ItemSelected = _selector.Selected is not null;

        var styler = ImGui.GetStyle();
        var wdl = ImGui.GetWindowDrawList();
        using (ImRaii.Child("SelectedItemOuter", region))
        {
            var imgSize = new Vector2(region.Y) - styler.WindowPadding * 2;
            var imgDrawPos = ImGui.GetCursorScreenPos() + new Vector2(region.X - region.Y, 0) + styler.WindowPadding;
            // Draw the left items.
            if(ItemSelected)
                DrawSelectedInner();

            // move to the cursor position and attempt to draw it.
            ImGui.GetWindowDrawList().AddRectFilled(imgDrawPos, imgDrawPos + imgSize, CkColor.FancyHeaderContrast.Uint(), rounding);
            ImGui.SetCursorScreenPos(imgDrawPos);

            // Handle the image based on the type to display.
            if(ItemSelected)
            {
                if (_selector.Selected!.RestrictionRef is GarblerRestriction gagItem)
                    _activeItemDrawer.DrawFramedImage(gagItem.GagType, imgSize.Y, rounding, true);
                else if (_selector.Selected!.RestrictionRef is BlindfoldRestriction blindfoldRestrictItem)
                    _activeItemDrawer.DrawRestrictionImage(blindfoldRestrictItem, imgSize.Y, rounding, true);
                else if (_selector.Selected!.RestrictionRef is RestrictionItem normalRestrictItem)
                    _activeItemDrawer.DrawRestrictionImage(normalRestrictItem, imgSize.Y, rounding, true);
            }
        }
        // draw the actual design element.
        var minPos = ImGui.GetItemRectMin();
        var size = ImGui.GetItemRectSize();
        wdl.AddRectFilled(minPos, minPos + size, CkColor.FancyHeader.Uint(), rounding, ImDrawFlags.RoundCornersRight);
        // Draw a secondary rect just like the first but going slightly bigger.
        wdl.AddRectFilled(minPos, minPos + new Vector2(size.X * .65f + styler.ItemInnerSpacing.Y, ImGui.GetFrameHeight() + styler.ItemInnerSpacing.Y), CkColor.SideButton.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);
        // Add a rect that spans the top row up to about .67 of the height.
        wdl.AddRectFilled(minPos, minPos + new Vector2(size.X * .65f, ImGui.GetFrameHeight()), CkColor.VibrantPink.Uint(), rounding, ImDrawFlags.RoundCornersBottomRight);
    }

    private void DrawSelectedInner()
    {
        if (_selector.Selected is not { } selectedCursedItem)
        {
            ImGui.Text("No Item Selected.");
            return;
        }

        var isEditing = _manager.ItemInEditor is not null;
        if (CkGui.IconButton(isEditing ? FAI.Save : FAI.Edit, inPopup: true))
        {
            if (isEditing) _manager.SaveChangesAndStopEditing();
            else _manager.StartEditing(selectedCursedItem);
        }
        ImGui.AlignTextToFramePadding();
        ImGui.SameLine();
        ImGui.Text(selectedCursedItem.Label);

        // Shift down.
        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetStyle().ItemInnerSpacing.Y/4));
        var comboLength = ImGui.GetItemRectSize().X - CkGui.IconButtonSize(FAI.ArrowsLeftRight).X - ImGui.GetStyle().ItemInnerSpacing.X*2;
        if (_manager.ItemInEditor is { } ActiveCursedItem)
        {
            // Draw the item selector.
            if (ActiveCursedItem.RestrictionRef is GarblerRestriction gagItem)
            {
                var change = _gagItemCombo.Draw("##CursedItemGagSelector", gagItem.GagType, comboLength);
                if (change && !gagItem.GagType.Equals(_gagItemCombo.Current?.GagType))
                {
                    Logger.LogTrace($"Item changed to {_gagItemCombo.Current?.GagType} " +
                        $"[{_gagItemCombo.Current?.GagType.GagName()}] from {gagItem.GagType} [{gagItem.GagType.GagName()}]");
                    ActiveCursedItem.RestrictionRef = _gagItemCombo.Current ?? _gags.Storage.Values.First();
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    Logger.LogTrace($"Item Cleared and set to the First Item. (BallGag) from {gagItem.GagType} [{gagItem.GagType.GagName()}]");
                    ActiveCursedItem.RestrictionRef = _gags.Storage.Values.First();
                }
            }
            else if (ActiveCursedItem.RestrictionRef is RestrictionItem restriction)
            {
                var change = _restrictionItemCombo.Draw("##CursedItemSelector", restriction.Identifier, comboLength);
                if (change && !restriction.Identifier.Equals(_restrictionItemCombo.Current?.Identifier))
                {
                    Logger.LogTrace($"Item changed to {_restrictionItemCombo.Current?.Identifier} " +
                        $"[{_restrictionItemCombo.Current?.Label}] from {restriction.Identifier} [{restriction.Label}]");
                    ActiveCursedItem.RestrictionRef = _restrictionItemCombo.Current ?? _restrictions.Storage.First();
                }

                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                {
                    Logger.LogTrace($"Item Cleared and set to the First Item. [{restriction.Label}]");
                    ActiveCursedItem.RestrictionRef = _restrictions.Storage.First();
                }
            }
            ImUtf8.SameLineInner();
            // Draw the type toggle switch button, that will switch the items kind.
            if (CkGui.IconButton(FAI.ArrowsLeftRight, disabled: !KeyMonitor.ShiftPressed() || _restrictions.Storage.Count <= 0))
            {
                try
                {
                    if (ActiveCursedItem.RestrictionRef is GarblerRestriction)
                        ActiveCursedItem.RestrictionRef = _restrictions.Storage.First();
                    else
                        ActiveCursedItem.RestrictionRef = _gags.Storage.Values.First();
                }
                catch
                {
                    Logger.LogError("Could not resolve the switch between types!");
                }
                // Perform early return.
                return;
            }
            CkGui.AttachToolTip("Switch between GagType and RestrictionItem types.--SEP--Must Hold Shift to Use.");

            // Next Row, Draw the enum combo for precedence selection.
            var width = ImGui.CalcTextSize("Highestm").X;
            if (CkGuiUtils.EnumCombo("##Precedence", width, ActiveCursedItem.Precedence, out var newLevel, (i) => i.ToName()))
                ActiveCursedItem.Precedence = newLevel;
            ImUtf8.SameLineInner();
            ImGui.TextUnformatted("Precedence");

            var curVal = ActiveCursedItem.CanOverride;
            ImUtf8.SameLineInner();
            if (ImGui.Checkbox("Override", ref curVal))
                ActiveCursedItem.CanOverride = curVal;
        }
        else
        {
            // Draw an icon based on the type the height of the current frame on the left.
            ImGui.AlignTextToFramePadding();
            var pos = ImGui.GetCursorScreenPos() + ImGui.GetStyle().FramePadding;
            var imgSize = CkGui.IconSize(FAI.LayerGroup);
            if (selectedCursedItem.RestrictionRef is GarblerRestriction gagItem)
            {
                using (ImRaii.Group())
                {
                    ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures[CoreTexture.Gagged], pos, imgSize, ImGuiColors.ParsedGold);
                    ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                    ImUtf8.SameLineInner();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(gagItem.GagType.GagName());
                }
                CkGui.AttachToolTip("Cursed Item is Referencing a --COL--" + gagItem.GagType.GagName() + "--COL--.", color: ImGuiColors.ParsedGold);
            }
            else if (selectedCursedItem.RestrictionRef is RestrictionItem restriction)
            {
                using (ImRaii.Group())
                {
                    ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures[CoreTexture.RestrainedArmsLegs], pos, imgSize, ImGuiColors.ParsedGold);
                    ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                    ImUtf8.SameLineInner();
                    ImGui.AlignTextToFramePadding();
                    ImGui.Text(restriction.Label);
                }
                CkGui.AttachToolTip("Cursed Item is Referencing a --COL--" + restriction.Label + "--COL--.", color: ImGuiColors.ParsedGold);
            }

            // next line, draw out the priority
            using (ImRaii.Group())
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetStyle().ItemInnerSpacing.X);
                ImGui.AlignTextToFramePadding();
                CkGui.IconText(FAI.LayerGroup, ImGuiColors.ParsedGold);
                ImUtf8.SameLineInner();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(selectedCursedItem.Precedence.ToName());
            }
            CkGui.AttachToolTip("This Cursed Item has --COL--" + selectedCursedItem.Precedence.ToName() + "--COL-- Precedence.", color: ImGuiColors.ParsedGold);

            ImGui.SameLine();
            using (ImRaii.Group())
            {
                ImGui.AlignTextToFramePadding();
                CkGui.BooleanToColoredIcon(selectedCursedItem.CanOverride, true);
                ImUtf8.SameLineInner();
                ImGui.AlignTextToFramePadding();
                ImGui.Text(selectedCursedItem.CanOverride ? "Can Override." : "Can't Override");
            }
            CkGui.AttachToolTip(selectedCursedItem.CanOverride
                ? "Can apply over items in the same slot with the same precedence level."
                : "Cannot override items in the same slot with the same precedence level.");
        }
    }
}
