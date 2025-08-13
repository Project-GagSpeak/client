using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Helpers;
using CkCommons.Raii;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Localization;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using GagspeakAPI.Util;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;
public partial class CursedLootPanel : DisposableMediatorSubscriberBase
{
    private void DrawSelectedInner(float rightOffset)
    {
        using var innerGroup = ImRaii.Group();
        // Draw what we should, based on the editor state.
        bool isItemInEditor = _manager.ItemInEditor is not null && _manager.ItemInEditor.Identifier.Equals(_selector.Selected?.Identifier);
        if (isItemInEditor)
            DrawEditorView(rightOffset);
        else
            DrawPreviewView(rightOffset);
    }

    private void DrawEditorView(float rightOffset)
    {
        var item = _manager.ItemInEditor!;
        var comboWidthMax = (ImGui.GetContentRegionAvail().X - rightOffset) * .6f;
        // Draw the item selector.
        if (item.RestrictionRef is GarblerRestriction gagItem)
        {
            var change = _gagItemCombo.Draw("##CursedItemGagSelector", gagItem.GagType, comboWidthMax);
            if (change && !gagItem.GagType.Equals(_gagItemCombo.Current?.GagType))
            {
                Logger.LogTrace($"Item changed to {_gagItemCombo.Current?.GagType} " +
                    $"[{_gagItemCombo.Current?.GagType.GagName()}] from {gagItem.GagType} [{gagItem.GagType.GagName()}]");
                item.RestrictionRef = _gagItemCombo.Current ?? _gags.Storage.Values.First();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace($"Item Cleared and set to the First Item. (BallGag) from {gagItem.GagType} [{gagItem.GagType.GagName()}]");
                item.RestrictionRef = _gags.Storage.Values.First();
            }
            CkGui.AttachToolTip("Change the cursed Gag item.--SEP--Can also Right-Click to clear.");
        }
        else if (item.RestrictionRef is RestrictionItem restriction)
        {
            var change = _restrictionItemCombo.Draw("##CursedItemSelector", restriction.Identifier, comboWidthMax);
            if (change && !restriction.Identifier.Equals(_restrictionItemCombo.Current?.Identifier))
            {
                Logger.LogTrace($"Item changed to {_restrictionItemCombo.Current?.Identifier} " +
                    $"[{_restrictionItemCombo.Current?.Label}] from {restriction.Identifier} [{restriction.Label}]");
                item.RestrictionRef = _restrictionItemCombo.Current ?? _restrictions.Storage.First();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace($"Item Cleared and set to the First Item. [{restriction.Label}]");
                item.RestrictionRef = _restrictions.Storage.First();
            }
            CkGui.AttachToolTip("Change the cursed Restriction item.--SEP--Can also Right-Click to clear.");
        }
        ImUtf8.SameLineInner();
        // Draw the type toggle switch button, that will switch the items kind.
        if (CkGui.IconButton(FAI.ArrowsLeftRight, disabled: !KeyMonitor.ShiftPressed() || _restrictions.Storage.Count <= 0))
        {
            try
            {
                if (item.RestrictionRef is GarblerRestriction)
                    item.RestrictionRef = _restrictions.Storage.First();
                else
                    item.RestrictionRef = _gags.Storage.Values.First();
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
        if (CkGuiUtils.EnumCombo("##Precedence", ImGui.CalcTextSize("Highestm").X, item.Precedence, out var newLevel, (i) => i.ToName()))
            item.Precedence = newLevel;
        CkGui.AttachToolTip("Change the precedence level of this item, which determines how it interacts with other items in the same slot.--NL--" +
            "Higher precedence items will override lower precedence items in the same slot.");
        ImUtf8.SameLineInner();
        ImGui.TextUnformatted("Priority");

        var curVal = item.CanOverride;
        ImGui.SameLine();
        if (ImGui.Checkbox("Override", ref curVal))
            item.CanOverride = curVal;

    }

    private void DrawPreviewView(float rightOffset)
    {
        var item = _selector.Selected!;

        // Draw an icon based on the type the height of the current frame on the left.
        ImGui.AlignTextToFramePadding();
        var pos = ImGui.GetCursorScreenPos() + ImGui.GetStyle().FramePadding;
        var imgSize = CkGui.IconSize(FAI.LayerGroup);
        if (item.RestrictionRef is GarblerRestriction gagItem)
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], pos, imgSize, ImGuiColors.ParsedGold);
                ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                CkGui.TextFrameAlignedInline("Cursed Mimic Gag  ");
            }
            CkGui.AttachToolTip($"Item applies a --COL--{gagItem.GagType.GagName()}--COL--.", color: ImGuiColors.ParsedGold);
        }
        else if (item.RestrictionRef is RestrictionItem restriction)
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs], pos, imgSize, ImGuiColors.ParsedGold);
                ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                CkGui.TextFrameAlignedInline("Cursed Bondage Loot  ");
            }
            CkGui.AttachToolTip($"Item applies a --COL--{restriction.Label}--COL--.", color: ImGuiColors.ParsedGold);
        }

        // next line, draw out the priority
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.FramedIconText(FAI.LayerGroup, ImGuiColors.ParsedGold);
            CkGui.TextFrameAlignedInline("Priority  ");
        }
        CkGui.AttachToolTip($"Item has --COL--{item.Precedence.ToName()}--COL-- precedence.", color: ImGuiColors.ParsedGold);

        ImGui.SameLine();
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.BooleanToColoredIcon(item.CanOverride);
            CkGui.TextFrameAlignedInline("Overrides  ");
        }
        CkGui.AttachToolTip(item.CanOverride
            ? "Can apply over items in the same slot with the same precedence level."
            : "Cannot override items in the same slot with the same precedence level.");
    }
}
