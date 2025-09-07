using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using OtterGui.Text.Widget.Editors;
using static CkCommons.Widgets.CkHeader;
using static FFXIVClientStructs.FFXIV.Client.UI.Misc.GroupPoseModule;

namespace GagSpeak.Gui.Wardrobe;

public class LootItemsTab : IFancyTab
{
    private readonly ILogger<LootItemsTab> _logger;
    private readonly CursedLootFileSelector _selector;
    private readonly ActiveItemsDrawer _activeItems;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly TutorialService _guides;
    public LootItemsTab(ILogger<LootItemsTab> logger, GagspeakMediator mediator,
        CursedLootFileSelector selector, ActiveItemsDrawer activeItems, GagRestrictionManager gags, 
        RestrictionManager restrictions, CursedLootManager manager, FavoritesManager favorites,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _activeItems = activeItems;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _guides = guides;

        _gagItemCombo = new RestrictionGagCombo(logger, favorites, () => [
        ..gags.Storage.Values.OrderByDescending(p => favorites._favoriteGags.Contains(p.GagType)).ThenBy(p => p.GagType)
        ]);
        _restrictionItemCombo = new RestrictionCombo(logger, mediator, favorites, () => [
            ..restrictions.Storage.OrderByDescending(p => favorites._favoriteRestrictions.Contains(p.Identifier)).ThenBy(p => p.Label)
            ]);
    }

    public string   Label       => "Cursed Items";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;

    private RestrictionGagCombo _gagItemCombo;
    private RestrictionCombo    _restrictionItemCombo;

    // should be very similar to drawing out the list of items, except this will have a unique flavor to it.
    public void DrawContents(float width)
    {
        // if we are editing any item, we should draw out the editor at the top. Otherwise, we draw out the File System.
        if (_manager.ItemInEditor is not null)
            DrawItemEditor(width);

        // Draw out the remaining items here.
        DrawLootItemList(width);
    }

    private void DrawItemEditor(float width)
    {


    }

    private void DrawLootItemList(float width)
    {
        using var c = CkRaii.FramedChildPaddedW("LootItems", width, ImGui.GetContentRegionAvail().Y, 0, CkColor.VibrantPink.Uint(), DFlags.RoundCornersAll);

        _selector.DrawList(width);
    }

    public void DrawSearchFilter(float width)
        => _selector.DrawFilterRow(width);

}
