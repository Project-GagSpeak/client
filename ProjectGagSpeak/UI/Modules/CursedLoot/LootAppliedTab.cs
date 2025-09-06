using CkCommons;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;

namespace GagSpeak.Gui.Wardrobe;

public class LootAppliedTab : IFancyTab
{
    private readonly ILogger<LootAppliedTab> _logger;
    private readonly CursedLootFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItems;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    public LootAppliedTab(ILogger<LootAppliedTab> logger, CursedLootFileSelector selector,
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

    public string   Label       => "Applied Loot";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        // if we are editing any item, we should draw out the editor at the top. Otherwise, we draw out the File System.

        // some future conditional here.


        // draw out the rest of the items here and things.

        // Calculate the size of the left and right windows by the region - spacing of window padding for equal distribution.
        var subWindowSize = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / 2, ImGui.GetContentRegionAvail().Y);

        CkGui.FontText("Applied Items", UiFontService.GagspeakTitleFont, CkColor.CkMistressColor.Uint());
    }
}
