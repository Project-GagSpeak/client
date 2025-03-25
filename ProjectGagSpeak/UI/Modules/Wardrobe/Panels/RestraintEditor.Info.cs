using Dalamud.Interface.ImGuiFileDialog;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorInfo : ICkTab
{
    private readonly ILogger<RestraintEditorInfo> _logger;
    private readonly FileDialogManager _fileDialog = new();
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorInfo(ILogger<RestraintEditorInfo> logger, RestraintSetFileSelector selector,
        EquipmentDrawer equipDrawer, ModPresetDrawer modDrawer, MoodleDrawer moodleDrawer,
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

    public string   Label       => "Info & Traits";
    public string   Tooltip     => "View and edit the traits and information of the selected item.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        ImGui.Text("HELLO THERE");
    }
}
