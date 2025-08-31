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
        if (item is CursedGagItem gagItem)
        {
            var change = _gagItemCombo.Draw("##CursedItemGagSelector", gagItem.RefItem.GagType, comboWidthMax);
            if (change && !gagItem.RefItem.GagType.Equals(_gagItemCombo.Current?.GagType))
            {
                Logger.LogTrace($"Item changed to {_gagItemCombo.Current?.GagType} " +
                    $"[{_gagItemCombo.Current?.GagType.GagName()}] from {gagItem.RefItem.GagType} [{gagItem.RefItem.GagType.GagName()}]");
                gagItem.RefItem = _gagItemCombo.Current ?? _gags.Storage.Values.First();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace($"Item Cleared and set to the First Item. (BallGag) from {gagItem.RefItem.GagType} [{gagItem.RefItem.GagType.GagName()}]");
                gagItem.RefItem = _gags.Storage.Values.First();
            }
            CkGui.AttachToolTip("Change the cursed Gag item.--SEP--Can also Right-Click to clear.");
        }
        else if (item is CursedRestrictionItem bindItem)
        {
            var change = _restrictionItemCombo.Draw("##CursedItemSelector", bindItem.RefItem.Identifier, comboWidthMax);
            if (change && !bindItem.RefItem.Identifier.Equals(_restrictionItemCombo.Current?.Identifier))
            {
                Logger.LogTrace($"Item changed to {_restrictionItemCombo.Current?.Identifier} " +
                    $"[{_restrictionItemCombo.Current?.Label}] from {bindItem.RefItem.Identifier} [{bindItem.RefItem.Label}]");
                bindItem.RefItem = _restrictionItemCombo.Current ?? _restrictions.Storage.First();
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                Logger.LogTrace($"Item Cleared and set to the First Item. [{bindItem.RefItem.Label}]");
                bindItem.RefItem = _restrictions.Storage.First();
            }
            CkGui.AttachToolTip("Change the cursed Restriction item.--SEP--Can also Right-Click to clear.");
        }
        ImUtf8.SameLineInner();
        // Draw the type toggle switch button, that will switch the items kind.
        if (CkGui.IconButton(FAI.ArrowsLeftRight, disabled: !KeyMonitor.ShiftPressed() || _restrictions.Storage.Count is 0))
        {
            Generic.Safe(() =>
            {
                if (item is CursedGagItem)
                {
                    Logger.LogInformation($"Item type changed: CursedGagItem -> CursedRestrictionItem");
                    _manager.ChangeCursedLootType(item, CursedLootKind.Restriction);
                }
                else
                {
                    Logger.LogInformation($"Item type changed: CursedRestrictionItem -> CursedGagItem");
                    _manager.ChangeCursedLootType(item, CursedLootKind.Gag);
                }
            });
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
    }

    private void DrawPreviewView(float rightOffset)
    {
        var item = _selector.Selected!;

        // Draw an icon based on the type the height of the current frame on the left.
        ImGui.AlignTextToFramePadding();
        var pos = ImGui.GetCursorScreenPos() + ImGui.GetStyle().FramePadding;
        var imgSize = CkGui.IconSize(FAI.LayerGroup);
        if (item is CursedGagItem gagItem)
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], pos, imgSize, ImGuiColors.ParsedGold);
                ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                CkGui.TextFrameAlignedInline("Cursed Mimic Gag  ");
            }
            CkGui.AttachToolTip($"Item applies a --COL--{gagItem.RefItem.GagType.GagName()}--COL--.", color: ImGuiColors.ParsedGold);
        }
        else if (item is CursedRestrictionItem bindItem)
        {
            using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
            {
                ImGui.GetWindowDrawList().AddDalamudImage(CosmeticService.CoreTextures.Cache[CoreTexture.RestrainedArmsLegs], pos, imgSize, ImGuiColors.ParsedGold);
                ImGui.Dummy(new Vector2(ImGui.GetFrameHeight()));
                CkGui.TextFrameAlignedInline("Cursed Bondage Loot  ");
            }
            CkGui.AttachToolTip($"Item applies a --COL--{bindItem.RefItem.Label}--COL--.", color: ImGuiColors.ParsedGold);
        }

        // next line, draw out the priority
        using (CkRaii.Group(CkColor.FancyHeaderContrast.Uint()))
        {
            CkGui.FramedIconText(FAI.LayerGroup, ImGuiColors.ParsedGold);
            CkGui.TextFrameAlignedInline("Priority  ");
        }
        CkGui.AttachToolTip($"Item has --COL--{item.Precedence.ToName()}--COL-- precedence.", color: ImGuiColors.ParsedGold);
    }
}
