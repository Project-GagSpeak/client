using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class AliasesTab : IFancyTab
{
    private readonly ILogger<AliasesTab> _logger;
    private readonly AliasesFileSelector _selector;
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;
    public AliasesTab(ILogger<AliasesTab> logger, GagspeakMediator mediator,
        AliasesFileSelector selector, PuppeteerManager manager, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Aliases";
    public string   Tooltip     => "Create aliases to enhance puppeteer!";
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0,1));

        var leftWidth = width * 0.6f;
        var rounding = FancyTabBar.BarHeight * .4f;
        DrawLootItemList(leftWidth, rounding);

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            DrawSelectedItem(CkStyle.GetFrameRowsHeight(5), rounding);
            DrawStatistics(rounding);
        }
    }

    private void DrawLootItemList(float leftWidth, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedWH("list", new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding);

        _selector.DrawFilterRow(_.InnerRegion.X);
        _selector.DrawList(_.InnerRegion.X);
    }

    private void DrawSelectedItem(float innerHeight, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Sel", ImGui.GetContentRegionAvail().X, innerHeight, 0, GsCol.VibrantPink.Uint(), rounding);

        // Draw editor if editing
        DrawPanel(_.InnerRegion, rounding);
    }

    // Bottom right segment (not used in this panel)
    private void DrawStatistics(float rounding)
    {
        var region = ImGui.GetContentRegionAvail();
        using var _ = CkRaii.FramedChildPaddedWH("Stats", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding);

        CkGui.FontTextCentered("Statistics", UiFontService.UidFont);
        CkGui.Separator(GsCol.VibrantPink.Uint());

        //ImGui.Text("Total Loot Found:");
        //CkGui.ColorTextInline(_manager.TotalEncounters.ToString(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Gags Found:");
        //CkGui.ColorTextInline(_manager.GagEncounters.ToString(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Restrictions Found:");
        //CkGui.ColorTextInline(_manager.BindEncounters.ToString(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Mimics Evaded:");
        //CkGui.ColorTextInline(_manager.MimicsEvaded.ToString(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Total Time Cursed:");
        //CkGui.ColorTextInline(_manager.TimeInCursedLoot.ToGsRemainingTime(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Longest Lock Time:");
        //CkGui.ColorTextInline(_manager.LongestLockTime.ToGsRemainingTime(), CkColor.VibrantPink.Uint());

        //ImGui.Text("Max Active At Once:");
        //CkGui.ColorTextInline(_manager.MaxLootActiveAtOnce.ToString(), CkColor.VibrantPink.Uint());
    }


    private void DrawPanel(Vector2 region, float rounding)
    {
        if (_selector.Selected is not { } selected)
            return;
        // Image.
        var pos = ImGui.GetCursorScreenPos();
        var thumbnailSize = new Vector2(CkStyle.TwoRowHeight());
        var iconSize = new Vector2(ImUtf8.FrameHeight);
        var padding = ImGui.GetStyle().FramePadding;
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + thumbnailSize, ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);
        ImGui.Dummy(thumbnailSize);

        ImUtf8.SameLineInner();
        //// The non-editor variant of the topright segment.
        //using (ImRaii.Group())
        //{
        //    CkGui.ColorTextFrameAligned(selected.InPool ? "In Pool" : "Not In Pool", selected.InPool ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        //    ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImUtf8.FrameHeight);
        //    if (CkGui.IconButton(FAI.Edit, inPopup: true))
        //        _manager.StartEditing(selected);

        //    // Label field.
        //    CkGui.TextFrameAligned(selected.Label);
        //}

        //// Item Label.
        //if (selected is CursedGagItem item)
        //    ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, iconSize);
        //else if (selected is CursedRestrictionItem item2)
        //    ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Restrained].Handle, iconSize);
        //else
        //    ImGui.Dummy(iconSize);

        //CkGui.TextFrameAlignedInline(selected.RefLabel);

        //// Precedence.
        //CkGui.FramedIconText(FAI.SortAmountUp);
        //CkGui.TextFrameAlignedInline(selected.Precedence.ToName());

        //// Trait Application.
        //CkGui.BooleanToColoredIcon(selected.ApplyTraits, false);
        //CkGui.TextFrameAlignedInline(selected.ApplyTraits ? "Applies traits" : "Ignores traits");
    }

    private void DrawEditorPanel(Vector2 region, AliasTrigger item, float rounding)
    {
        var rightButtons = CkStyle.GetFrameWidth(2);
        var spacing = ImUtf8.ItemInnerSpacing;
        var frameH = ImUtf8.FrameHeight;
        var buttonSize = CkGui.IconButtonSize(FAI.ArrowsLeftRight);
        var imgSize = new Vector2(CkStyle.TwoRowHeight());

        var pos = ImGui.GetCursorScreenPos();
        ImGui.GetWindowDrawList().AddRectFilled(pos, pos + imgSize, ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);
        //_drawer.DrawRestrictionImage(item.RefItem, imgSize.Y, rounding, false);
        //ImUtf8.SameLineInner();

        //using (ImRaii.Group())
        //{
        //    CkGui.ColorTextFrameAligned(item.InPool ? "In Pool" : "Not In Pool", item.InPool ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
        //    ImGui.SameLine(ImGui.GetContentRegionAvail().X - rightButtons);
        //    if (CkGui.IconButton(FAI.Undo, inPopup: true))
        //        _manager.StopEditing();
        //    ImUtf8.SameLineInner();
        //    if (CkGui.IconButton(FAI.Save, inPopup: true))
        //        _manager.SaveChangesAndStopEditing();

        //    // Label field.
        //    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        //    var label = item.Label;
        //    if (ImGui.InputTextWithHint("##Name", "Loot Name..", ref label, 64))
        //        item.Label = label;
        //    CkGui.AttachToolTip("The Cursed Loot Name");
        //}

        //ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new Vector2(frameH));
        //ImUtf8.SameLineInner();
        //var comboW = ImGui.GetContentRegionAvail().X - buttonSize.X - spacing.X;
        //if (_bindCombo.Draw("##LootRestriction", item.RefItem.Identifier, comboW))
        //    item.RefItem = _bindCombo.Current!;
        //if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        //    item.RefItem = _restrictions.Storage.FirstOrDefault()!;
        //ImUtf8.SameLineInner();
        //if (CkGui.IconButton(FAI.ArrowsLeftRight))
        //    _manager.ChangeCursedLootType(CursedLootKind.Gag);
        //CkGui.AttachToolTip("Switch between Gag & Restriction Loot Types.");

        //// Everything else here is from shared editor info.
        //CkGui.FramedIconText(FAI.SortAmountUp);
        //ImUtf8.SameLineInner();
        //if (CkGuiUtils.EnumCombo("##Precedence", ImGui.GetContentRegionAvail().X / 2, item.Precedence, out var newP, _ => _.ToName(), flags: CFlags.None))
        //    item.Precedence = newP;
        //CkGui.AttachToolTip("The priority of application when multiple applied items go on the same slot.");

        //var doTraits = item.ApplyTraits;
        //if (ImGui.Checkbox("Apply Traits", ref doTraits))
        //    item.ApplyTraits = doTraits;
        //CkGui.AttachToolTip("If the ref item's hardcore traits are applied.");
    }
}
