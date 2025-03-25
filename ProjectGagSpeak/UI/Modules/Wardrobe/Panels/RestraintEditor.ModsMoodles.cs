using Dalamud.Interface.ImGuiFileDialog;
using GagSpeak.CkCommons.Drawers;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class RestraintEditorModsMoodles : ICkTab
{
    private readonly ILogger<RestraintEditorModsMoodles> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorModsMoodles(ILogger<RestraintEditorModsMoodles> logger,
        RestraintSetFileSelector selector, ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer, RestraintManager manager, CosmeticService cosmetics,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _manager = manager;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string   Label       => "Mods & Moodles";
    public string   Tooltip     => "Set the Associated Mods & Moodles for your set." +
        "--SEP--These are applied in addition to the ones linked to layers and advanced slots.";
    public bool     Disabled    => false;


    public void DrawContents(float width)
    {
        ImGui.Text("Mods & Moodles UI.");
    }
}
