using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Extensions;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class LootPoolTab : IFancyTab
{
    private readonly ILogger<LootPoolTab> _logger;
    private readonly CursedLootFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItems;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    public LootPoolTab(ILogger<LootPoolTab> logger, CursedLootFileSelector selector,
        ActiveItemsDrawer activeItems, GagRestrictionManager gags, RestrictionManager restrictions, 
        CursedLootManager manager, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _activeItems = activeItems;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _guides = guides;
    }

    public string   Label       => "Active Item Pool";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        CkGui.FontText("Active Loot Pool", UiFontService.GagspeakTitleFont, CkColor.CkMistressColor.Uint());

        // draw out the rest of the items here and things.
        var allItemsInPool = _manager.Storage.AllItemsInPoolByActive;
        if (allItemsInPool.Count <= 0)
            return;
        
        foreach (var item in allItemsInPool)
            DrawLootPoolItem(item);
    }

    private void DrawLootPoolItem(CursedItem item)
    {
        var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());

        using (CkRaii.FramedChild(item.Identifier.ToString(), itemSize, CkColor.FancyHeaderContrast.Uint(), 0))
        {
            var active = item.AppliedTime != DateTimeOffset.MinValue;
            if (active)
            {
                CkGui.FramedIconText(FAI.Stopwatch);
                CkGui.AttachToolTip("Item is currently applied!");
            }
            else
            {
                if (CkGui.IconButton(FAI.ArrowLeft, inPopup: true))
                    _manager.TogglePoolState(item);
                CkGui.AttachToolTip("Remove this Item from the Cursed Loot Pool.");
            }

            // Draw out the text label.
            ImUtf8.SameLineInner();
            ImUtf8.TextFrameAligned(item.Label);

            if (active)
            {
                // Draw out the release time right aligned.
                ImUtf8.SameLineInner();
                var timerText = item.ReleaseTime.ToGsRemainingTimeFancy();
                var offset = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().FramePadding.X - ImGui.CalcTextSize(timerText).X;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
                CkGui.ColorText(timerText, ImGuiColors.HealerGreen);
            }
        }
    }
}
