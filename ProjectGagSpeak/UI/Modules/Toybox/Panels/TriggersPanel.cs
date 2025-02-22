using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Tutorial;
using GagSpeak.Triggers;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.UI.UiToybox;

public partial class TriggersPanel
{
    private readonly ILogger<TriggersPanel> _logger;
    private readonly TriggerFileSelector _selector;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly TriggerManager _manager;
    private readonly VisualApplierMoodles _moodles;
    private readonly UiSharedService _ui;
    private readonly TutorialService _guides;

    // Custom Combo's:
    private MoodleStatusCombo _statusCombo;
    private MoodlePresetCombo _presetCombo;
    // others...

    public TriggersPanel(
        ILogger<TriggersPanel> logger,
        TriggerFileSelector selector,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        TriggerManager manager,
        VisualApplierMoodles moodles,
        UiSharedService ui,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _ui = ui;
        _guides = guides;
    }

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
        if (_manager.EnabledTriggers is not { } activeItems)
            return;
        ImGui.Text("Active Triggers:");
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
