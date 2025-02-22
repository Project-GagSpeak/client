using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Restrictions;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;
public partial class RestrictionsPanel
{
    private readonly ILogger<RestrictionsPanel> _logger;
    private readonly FileDialogManager _fileDialog = new();
    private readonly RestrictionFileSelector _selector;
    private readonly EquipmentDrawer _equipDrawer;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly RestrictionManager _manager;
    private readonly PairManager _pairs;
    private readonly UiSharedService _ui;
    private readonly TutorialService _guides;

    public RestrictionsPanel(
        ILogger<RestrictionsPanel> logger,
        RestrictionFileSelector selector,
        EquipmentDrawer equipDrawer,
        ModPresetDrawer modDrawer,
        MoodleDrawer moodleDrawer,
        RestrictionManager manager,
        PairManager pairs,
        UiSharedService ui,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _equipDrawer = equipDrawer;
        _modDrawer = modDrawer;
        _moodleDrawer = moodleDrawer;
        _manager = manager;
        _pairs = pairs;
        _ui = ui;
        _guides = guides;
    }

    // Handles drawing the Padlock interface for client restrictions. (handle this later)
    // private PadlockRestraintsClient _restraintPadlock;

    public void DrawPanel(Vector2 remainingRegion, float selectorSize)
    {
        using var group = ImRaii.Group();

        // within this group, if we are editing an item, draw the editor.
        if (_manager.ActiveEditorItem is not null)
        {
            DrawEditor(remainingRegion);
            return;
        }
        else
        {
            _selector.Draw(selectorSize);
            ImGui.SameLine();
            using (ImRaii.Group())
            {
                DrawActiveItemInfo();
                DrawSelectedItemInfo();
            }
        }
    }

    private void DrawActiveItemInfo()
    {
        if (_manager.ActiveRestrictionsData is not { } activeData)
            return;

        if (_manager.ActiveRestrictions is not { } activeItems)
            return;

    }

    private void DrawSelectedItemInfo()
    {
        // Draws additional information about the selected item. Uses the Selector for reference.
        if (_selector.Selected is null)
            return;

        ImGui.Text("Selected Item:" + _selector.Selected.Label);

        if (ImGui.Button("Begin Editing"))
            _manager.StartEditing(_selector.Selected);
    }
}
