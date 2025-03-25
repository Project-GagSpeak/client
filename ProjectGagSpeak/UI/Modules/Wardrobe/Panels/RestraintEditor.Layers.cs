using Dalamud.Interface.ImGuiFileDialog;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorLayers : ICkTab
{
    private readonly ILogger<RestraintEditorLayers> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorLayers(ILogger<RestraintEditorLayers> logger,
        RestraintSetFileSelector selector, EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer, MoodleDrawer moodleDrawer,
        RestraintManager manager, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Layers";
    public string   Tooltip     => "Define the layers that can be applied to the restraint set." +
        "--SEP--Restraint Layers can be toggled while a restraint set is locked.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        ImGui.Text("I am Restraint Layers");
    }
}
