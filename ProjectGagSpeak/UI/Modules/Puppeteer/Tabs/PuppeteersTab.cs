using CkCommons;
using CkCommons.DrawSystem;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.DrawSystem;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class PuppeteersTab : IFancyTab
{
    private readonly ILogger<PuppeteersTab> _logger;
    private readonly PuppeteersDrawer _drawer;
    private readonly PuppeteerManager _manager;
    private readonly TutorialService _guides;

    private Kinkster? _selected => _drawer.Selected;
    
    // may need to move elsewhere
    private bool _editingPerms = false;

    public PuppeteersTab(ILogger<PuppeteersTab> logger, GagspeakMediator mediator,
        PuppeteersDrawer drawer, PuppeteerManager manager, TutorialService guides)
    {
        _logger = logger;
        _drawer = drawer;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Puppeteers";
    public string   Tooltip     => "Manage how others can puppeteer you, distinct for each person.";
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, ImUtf8.FramePadding - new Vector2(0,1));

        var leftWidth = width * 0.6f;
        var rounding = FancyTabBar.BarHeight * .4f;
        DrawPuppeteers(leftWidth, rounding);

        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            DrawSelectedPuppeteer(CkStyle.GetFrameRowsHeight(5), rounding);
            DrawMarionetteStats(rounding);
        }
    }

    private void DrawPuppeteers(float leftWidth, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedWH("list", new Vector2(leftWidth, ImGui.GetContentRegionAvail().Y), 0, GsCol.VibrantPink.Uint(), rounding);

        _drawer.DrawFilterRow(_.InnerRegion.X, 40);
        _drawer.DrawContents(flags: DynamicFlags.SelectableLeaves);
    }

    private void DrawSelectedPuppeteer(float innerHeight, float rounding)
    {
        using var _ = CkRaii.FramedChildPaddedW("Sel", ImGui.GetContentRegionAvail().X, innerHeight, 0, GsCol.VibrantPink.Uint(), rounding);
        // Draw editor if editing
        if (_editingPerms)
            DrawMarionetteEditor(_.InnerRegion, rounding);
        else
            DrawMarionetteView(_.InnerRegion, rounding);
    }

    private void DrawMarionetteView(Vector2 region, float rounding)
    {
        if (_selected is not { } puppeteer)
            return;

        ImGui.Text("WAH!");
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

    // Just editing our generic permissions for this kinkster. Nothing other than the kinkster is nessisary here.
    private void DrawMarionetteEditor(Vector2 region, float rounding)
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

    private void DrawMarionetteStats(float rounding)
    {
        var region = ImGui.GetContentRegionAvail();
        using var _ = CkRaii.FramedChildPaddedWH("Stats", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), rounding);

        if (_selected is not { } puppeteer)
            return;

        CkGui.FontTextCentered("Marionette Stats", UiFontService.UidFont);
        CkGui.Separator(GsCol.VibrantPink.Uint());

        // Fallback in the case that this puppeteer is not yet tracked for us.
        if (!_manager.Puppeteers.TryGetValue(puppeteer.UserData.UID, out var data))
        {
            CkGui.ColorTextCentered("No Puppeteer Data Found", ImGuiColors.DalamudRed);
            return;
        }

        // Otherwise display the outcome
        ImGui.Text("Puppeteered:");
        CkGui.ColorTextInline($"{data.OrdersRecieved} Times", ImGuiColors.TankBlue);

        ImGui.Text("Sit Reactions:");
        CkGui.ColorTextInline($"{data.SitOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Emote Reactions:");
        CkGui.ColorTextInline($"{data.EmoteOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Alias Reactions:");
        CkGui.ColorTextInline($"{data.AliasOrders}", ImGuiColors.TankBlue);

        ImGui.Text("Other Reactions:");
        CkGui.ColorTextInline($"{data.OtherOrders}", ImGuiColors.TankBlue);
    }
}
