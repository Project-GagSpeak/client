using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class LootPoolTab : IFancyTab
{
    private readonly ILogger<LootPoolTab> _logger;
    private readonly CursedLootFileSelector _selector;
    private readonly ActiveItemsDrawer _drawer;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    public LootPoolTab(ILogger<LootPoolTab> logger, CursedLootFileSelector selector,
        ActiveItemsDrawer drawer, GagRestrictionManager gags, RestrictionManager restrictions, 
        CursedLootManager manager, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _drawer = drawer;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Cursed Item Pool";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        var componentWidth = (width - ImUtf8.ItemSpacing.X) / 2f;
        var componentSize = new Vector2(componentWidth, ImGui.GetContentRegionAvail().Y);

        DrawInactiveList(componentSize);
        ImGui.SameLine();
        DrawActiveList(componentSize);
    }

    private void DrawInactiveList(Vector2 region)
    {
        using var c = CkRaii.HeaderChild("Inactive Loot", region, CkRaii.HeaderChildColors.Default, FancyTabBar.RoundingInner, HeaderFlags.SizeIncludesHeader);

        var itemsOutOfPool = _manager.Storage.InactiveLoot;
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 7f);
        using (var _ = CkRaii.FramedChildPaddedWH("InactiveItems", c.InnerRegion, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint()))
        {
            if (itemsOutOfPool.Count is 0)
                return;

            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(ImUtf8.ItemInnerSpacing.X * .5f, 0));
            foreach (var inactiveLoot in itemsOutOfPool)
                DrawLootItem(inactiveLoot, _.InnerRegion.X);
        }
    }

    private void DrawActiveList(Vector2 region)
    {
        using var c = CkRaii.HeaderChild("Active Loot Pool", region, CkRaii.HeaderChildColors.Default, FancyTabBar.RoundingInner, HeaderFlags.SizeIncludesHeader);

        var itemsInPool = _manager.Storage.ActiveUnappliedLoot;
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 7f);
        using (var _ = CkRaii.FramedChildPaddedWH("ActiveItems", c.InnerRegion, CkColor.FancyHeaderContrast.Uint(), CkColor.FancyHeaderContrast.Uint()))
        {
            if (itemsInPool.Count is 0)
                return;

            using var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(ImUtf8.ItemInnerSpacing.X * .5f, 0));
            foreach (var inactiveLoot in itemsInPool)
                DrawLootItem(inactiveLoot, _.InnerRegion.X);
        }
    }

    private void DrawLootItem(CursedItem item, float width)
    {
        using var _ = CkRaii.ChildPaddedW(item.Identifier.ToString(), width, ImUtf8.FrameHeight, CkColor.FancyHeaderContrast.Uint());

        var iconSize = new Vector2(_.InnerRegion.Y);
        var imgPadding = iconSize.Y * .1f;
        var rounding = ImGui.GetStyle().FrameRounding;

        // Draw out the icon based on the type, or a dummy if nothing.
        if (item is CursedGagItem gag)
            ActiveItemsDrawer.DrawImagePadded(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged], iconSize, 0, imgPadding);
        else if (item is CursedRestrictionItem item2)
            ActiveItemsDrawer.DrawImagePadded(CosmeticService.CoreTextures.Cache[CoreTexture.Weighty], iconSize, 0, imgPadding);
        else
            CkGui.FramedIconText(FAI.QuestionCircle, ImGuiColors.DalamudYellow);
        CkGui.AttachToolTip($"Item has --COL--[{item.Precedence.ToName()}]--COL-- precedence.", item.Precedence.ToColor());

        // The cursed item name.
        CkGui.TextFrameAlignedInline(item.Label);

        ImGui.SameLine(0,0);
        CkGui.FramedHoverIconText(FAI.InfoCircle, ImGuiColors.TankBlue.ToUint());
        if (ImGui.IsItemHovered())
            DrawLootTooltip(item);

        // Shift to the far right and draw out the arrow to go into the pool and such.
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImUtf8.FrameHeight);
        if (CkGui.IconButton(item.InPool ? FAI.ArrowLeft : FAI.ArrowRight, disabled: item.IsActive(), inPopup: true))
            _manager.TogglePoolState(item);
        CkGui.AttachToolTip(item.InPool ? "Remove item from the pool." : "Add item to the pool.");
    }

    private void DrawLootTooltip(CursedItem item)
    {
        using var s = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.One * 6f)
            .Push(ImGuiStyleVar.WindowRounding, 8f)
            .Push(ImGuiStyleVar.PopupBorderSize, 1f);
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedPink);

        ImGui.BeginTooltip();
        var imgSize = new Vector2(ImGui.GetTextLineHeight() + ImGui.GetTextLineHeightWithSpacing() * 2);
        // Draw it out.
        if (item is CursedGagItem gag && TextureManagerEx.GagImage(gag.RefItem.GagType) is { } img)
            _drawer.DrawFramedImage(gag.RefItem.GagType, imgSize.Y, CkStyle.ChildRounding());
        else if (item is CursedRestrictionItem restr)
            _drawer.DrawRestrictionImage(restr.RefItem, imgSize.Y, CkStyle.ChildRounding());
        else
            ImGui.Dummy(imgSize);
        
        ImUtf8.SameLineInner();
        using (ImRaii.Group())
        {
            CkGui.ColorText("Loot Name:", ImGuiColors.ParsedGold);
            CkGui.TextInline(item.Label);
            CkGui.ColorText("Loot Kind:", ImGuiColors.ParsedGold);
            CkGui.TextInline(item.Type.ToString());
            CkGui.ColorText("Applies:", ImGuiColors.ParsedGold);
            CkGui.ColorTextInline($"[{item.RefLabel}]", ImGuiColors.DalamudGrey3);
        }

        CkGui.ColorText("Precedence:", ImGuiColors.ParsedGold);
        CkGui.ColorTextInline(item.Precedence.ToName(), item.Precedence.ToColor());

        CkGui.ColorText("Set Traits:", ImGuiColors.ParsedGold);
        CkGui.BooleanToColoredIcon(item.ApplyTraits);

        CkGui.ColorText("Applies At:", ImGuiColors.ParsedGold);
        CkGui.TextInline(item.AppliedTime == DateTimeOffset.MinValue ? "<Not Applied>" : item.AppliedTime.ToLocalTime().ToString("G"));

        CkGui.ColorText("Unlocks At:", ImGuiColors.ParsedGold);
        CkGui.TextInline(item.ReleaseTime == DateTimeOffset.MinValue ? "<Not Applied>" : item.ReleaseTime.ToLocalTime().ToString("G"));

        ImGui.EndTooltip();
    }
}
