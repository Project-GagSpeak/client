using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class LootAppliedTab : IFancyTab
{
    private readonly ActiveItemsDrawer _drawer;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    public LootAppliedTab(ActiveItemsDrawer drawer, RestrictionManager restrictions, 
        CursedLootManager manager, TutorialService guides)
    {
        _drawer = drawer;
        _restrictions = restrictions;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Applied Cursed Loot";
    public string   Tooltip     => string.Empty;
    // Should maybe not do this if we want increased performance but for now just do it.
    public bool     Disabled    => false; // _restrictions.LootItems.Count is 0;

    public void DrawContents(float width)
    {
        // Could maybe add some kind of topbar here for filters and sort orders and such.

        using var style = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var _ = CkRaii.FramedChildPaddedWH("AppliedItems", ImGui.GetContentRegionAvail(), 0, GsCol.VibrantPink.Uint(), FancyTabBar.RoundingInner);

        // draw out the list of active, applied items.
        var appliedLoot = _manager.Storage.ActiveAppliedLoot;
        if (appliedLoot.Count is 0)
            return;

        foreach (var item in appliedLoot)
            DrawAppliedItem(item, _.InnerRegion.X);
    }

    private void DrawAppliedItem(CursedItem item, float width)
    {
        using var _ = CkRaii.ChildPaddedW(item.Identifier.ToString(), width, CkStyle.TwoRowHeight());

        var wdl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        var rounding = CkStyle.ChildRounding();
        var iconSize = new Vector2(_.InnerRegion.Y);
        wdl.AddRectFilled(pos, pos + iconSize, ImGui.GetColorU32(ImGuiCol.FrameBg), rounding);
        if (item is CursedGagItem gag)
        {
            _drawer.DrawFramedImage(gag.RefItem.GagType, iconSize.Y, rounding, 0);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Gagged].Handle, new Vector2(ImGui.GetTextLineHeight()));
                CkGui.TextInline(gag.Label);
                ImGui.SameLine();
                CkGui.TagLabelText(gag.Precedence.ToName(), gag.Precedence.ToColor(), 3 * ImGuiHelpers.GlobalScale);

                CkGui.ColorText($"Applies: [{gag.RefItem.GagType.GagName()}]", ImGuiColors.DalamudGrey3);
            }
        }
        else if (item is CursedRestrictionItem restr)
        {
            _drawer.DrawRestrictionImage(restr.RefItem, iconSize.Y, rounding, false);
            ImUtf8.SameLineInner();
            using (ImRaii.Group())
            {
                ImGui.Image(CosmeticService.CoreTextures.Cache[CoreTexture.Weighty].Handle, new Vector2(ImGui.GetTextLineHeight()));
                CkGui.TextInline(restr.Label);
                ImGui.SameLine();
                CkGui.TagLabelText(restr.Precedence.ToName(), restr.Precedence.ToColor(), 3 * ImGuiHelpers.GlobalScale);
                
                CkGui.ColorText($"Applies: [{restr.RefItem.Label}]", ImGuiColors.DalamudGrey3);
            }
        }
        else
            ImGui.Dummy(iconSize);

        // To the end, we should draw out the time remaining, we can do this by determining the space to draw first.
        var timeText = item.ReleaseTime.ToGsRemainingTimeFancy();
        var timeTextWidth = ImGui.CalcTextSize(timeText).X;
        var timeOffsetX = ImGui.GetContentRegionAvail().X - ImUtf8.FrameHeight - timeTextWidth - ImUtf8.ItemInnerSpacing.X;

        ImGui.SameLine(timeOffsetX);
        using (ImRaii.Group())
        {
            CkGui.ColorText(timeText, GsCol.VibrantPink.Uint());
            ImUtf8.SameLineInner();
            CkGui.FramedIconText(FAI.Stopwatch);
        }
        CkGui.AttachToolTip("The time remaining until the cursed item is removed.");
    }
}
