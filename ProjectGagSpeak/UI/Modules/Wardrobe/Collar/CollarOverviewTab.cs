using CkCommons;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using GagSpeak.Gui.Components;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;

namespace GagSpeak.Gui.Wardrobe;

public class CollarOverviewTab : IFancyTab
{
    private readonly ILogger<CollarOverviewTab> _logger;
    private readonly CollarManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public CollarOverviewTab(ILogger<CollarOverviewTab> logger, CollarManager manager,
        EquipmentDrawer equipDrawer, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _equipDrawer = equipDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Collar Overview";
    public string   Tooltip     => string.Empty;
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        // Calculate the size of the left and right windows by the region - spacing of window padding for equal distribution.
        var subWindowSize = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / 2, ImGui.GetContentRegionAvail().Y);

        CkGui.FontText("Charles", UiFontService.GagspeakTitleFont, CkColor.CkMistressColor.Uint());
    }
}
