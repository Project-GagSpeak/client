using Dalamud.Interface.ImGuiFileDialog;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorEquipment : ICkTab
{
    private readonly ILogger<RestraintEditorEquipment> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorEquipment(ILogger<RestraintEditorEquipment> logger, 
        RestraintSetFileSelector selector, EquipmentDrawer equipDrawer, 
        RestraintManager manager, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Equipment";
    public string   Tooltip     => "Configure what Equipment and Customizations are applied to you.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        ImGui.Text("HELLO THERE");
    }
}
