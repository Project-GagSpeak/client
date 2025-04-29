using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.RestraintSets;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.Utils;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Wardrobe;

public class RestraintEditorModsMoodles : IFancyTab
{
    private readonly ILogger<RestraintEditorModsMoodles> _logger;
    private readonly RestraintSetFileSelector _selector;
    private readonly RestraintManager _manager;
    private readonly ModSettingPresetManager _modPresets;
    private readonly ModPresetDrawer _modDrawer;
    private readonly MoodleDrawer _moodleDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public RestraintEditorModsMoodles(ILogger<RestraintEditorModsMoodles> logger,
        RestraintSetFileSelector selector, RestraintManager manager, 
        ModSettingPresetManager modPresets, ModPresetDrawer modPresetDrawer,
        MoodleDrawer moodleDrawer, CosmeticService cosmetics, TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _modPresets = modPresets;
        _modDrawer = modPresetDrawer;
        _moodleDrawer = moodleDrawer;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    public string Label => "Mods & Moodles";
    public string Tooltip => "Set Mods & Moodles for your set." +
        "--SEP--These are applied in addition to the ones linked to layers and advanced slots.";
    public bool Disabled => false;

    // Temp Storage Variables for the selected items in our combos.
    private ModSettingsPreset _selectedPreset = new ModSettingsPreset(new ModPresetContainer());
    private Moodle _selectedMoodle = new();

    public void DrawContents(float width)
    {
        if (_manager.ItemInEditor is null)
            return;

        var panelSize = new Vector2((ImGui.GetContentRegionAvail().X - ImGui.GetStyle().WindowPadding.X) / 2, ImGui.GetContentRegionAvail().Y);

        // Draw out the associated Mods Child.
        using (CkRaii.HeaderChild("Associated Mods", panelSize, FancyTabBar.RoundingInner, CkRaii.HeaderFlags.SizeIncludesHeader))
        {
            DrawModSelector();
            DrawModsList();
        }

        // Then the one for the moodles and such.
        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        using (CkRaii.HeaderChild("Associated Moodles", panelSize, FancyTabBar.RoundingInner, CkRaii.HeaderFlags.SizeIncludesHeader))
        {
            DrawMoodleSelector();
            DrawMoodlesList();
            // Draw out the moodle icon row.
            _moodleDrawer.FramedMoodleIconDisplay(_manager.ItemInEditor!.RestraintMoodles, ImGui.GetContentRegionAvail().X, CkRaii.GetChildRoundingLarge(), 2);
        }
    }

    public void DrawModSelector()
    {
        using var _ = ImRaii.Group();

        var combosWidth = ImGui.GetContentRegionAvail().X - ImGui.GetStyle().ItemInnerSpacing.X - CkGui.IconButtonSize(FAI.Plus).X;
        var comboWidth = (combosWidth - ImGui.GetStyle().ItemInnerSpacing.X) / 2;

        ModCombo();

        ImUtf8.SameLineInner();
        PresetCombo();
        
        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Plus))
            _manager.ItemInEditor!.RestraintMods.Add(new ModSettingsPreset(_selectedPreset));

        void ModCombo()
        {
            var change = _modPresets.ModCombo.Draw("##ModSelector", _selectedPreset.Container.DirectoryPath, comboWidth, 1.4f, ImGuiComboFlags.NoArrowButton);
            if (change && !_selectedPreset.Container.DirectoryPath.Equals(_modPresets.ModCombo.Current?.DirPath))
            {
                if (_modPresets.ModPresetStorage.FirstOrDefault(mps => mps.DirectoryPath == _modPresets.ModCombo.Current!.DirPath) is { } match)
                {
                    _logger.LogTrace($"Associated Mod changed to {_modPresets.ModCombo.Current!.Name} [{_modPresets.ModCombo.Current!.DirPath}] from {_selectedPreset.Container.ModName}");
                    _selectedPreset = match.ModPresets.First(); // Let this crash you if it happens, because it means something has gone horribly wrong.
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Associated Mod was cleared. and is now Empty");
                _selectedPreset = new ModSettingsPreset(new ModPresetContainer());
            }
        }

        void PresetCombo()
        {
            var presetChange = _modPresets.PresetCombo.Draw("##ModPresetSelector", _selectedPreset.Label, comboWidth, 1f, ImGuiComboFlags.NoArrowButton);
            if (presetChange && !_selectedPreset.Label.Equals(_modPresets.PresetCombo.Current?.Label))
            {
                if (_selectedPreset.Container.ModPresets.FirstOrDefault(mp => mp.Label == _modPresets.PresetCombo.Current!.Label) is { } match)
                {
                    _logger.LogTrace($"Associated Mod Preset changed to {_modPresets.PresetCombo.Current!.Label} [{_modPresets.PresetCombo.Current.Label}] from {_selectedPreset.Label}");
                    _selectedPreset = match;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Associated Mod Preset was cleared. and is now Empty");
                var curContainer = _selectedPreset.Container;
                _selectedPreset = new ModSettingsPreset(curContainer);
            }
        }
    }

    private void DrawMoodleSelector()
    {
        using var _ = ImRaii.Group();
        var buttonWidth = CkGui.IconButtonSize(FAI.Plus).X;

        if (CkGui.IconButton(FAI.ArrowsLeftRight, disabled: !KeyMonitor.ShiftPressed()))
        {
            _selectedMoodle = _selectedMoodle switch
            {
                MoodlePreset => new Moodle(),
                Moodle => new MoodlePreset(),
                _ => throw new ArgumentOutOfRangeException(nameof(_selectedMoodle), _selectedMoodle, "Unknown Moodle Type"),
            };
        }
        CkGui.AttachToolTip(_moodleDrawer.MoodleTypeTooltip(_selectedMoodle));

        ImUtf8.SameLineInner();
        var comboWidth = ImGui.GetContentRegionAvail().X - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        _moodleDrawer.DrawMoodleCombo("AssociatedMoodleSelector", _selectedMoodle, comboWidth, ImGuiComboFlags.NoArrowButton);

        ImUtf8.SameLineInner();
        if (CkGui.IconButton(FAI.Plus))
        {
            if (_selectedMoodle is MoodlePreset p)
                _manager.ItemInEditor!.RestraintMoodles.Add(new MoodlePreset(p));
            else
                _manager.ItemInEditor!.RestraintMoodles.Add(new Moodle(_selectedMoodle));
        }
    }

    private void DrawModsList()
    {
        using var _ = CkRaii.FramedChildPadded("MoodlesList", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(), CkRaii.GetChildRoundingLarge());

        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);
        var listingSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().WindowPadding.Y * 2);

        foreach (var mod in _manager.ItemInEditor!.RestraintMods.ToList())
        {
            var itemLabel = mod.Label;
            var itemSource = mod.Container.ModName;

            using (CkRaii.FramedChildPadded(mod.Container.DirectoryPath + mod.Label, listingSize, CkColor.FancyHeaderContrast.Uint()))
            {
                using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                using (ImRaii.Group())
                {
                    CkGui.ColorText(itemLabel, ImGuiColors.DalamudGrey);
                    CkGui.ColorText(itemSource, ImGuiColors.DalamudGrey3);
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - buttonSize.X);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - buttonSize.Y) / 2);
                if (CkGui.IconButton(FAI.Eraser, inPopup: true))
                    _manager.ItemInEditor!.RestraintMods.Remove(mod);
            }
            if(ImGui.IsItemHovered())
                _modDrawer.DrawPresetTooltip(mod);
        }
    }

    private void DrawMoodlesList()
    {
        var size = ImGui.GetContentRegionAvail() - new Vector2(0, MoodleDrawer.FramedIconDisplayHeight(2) + ImGui.GetStyle().ItemSpacing.Y);

        using var _ = CkRaii.FramedChildPadded("MoodlesList", size, CkColor.FancyHeaderContrast.Uint(), CkRaii.GetChildRoundingLarge());
        
        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);
        var presetLookup = VisualApplierMoodles.LatestIpcData.MoodlesPresets.ToDictionary(p => p.GUID, p => p.Title);
        var statusLookup = VisualApplierMoodles.LatestIpcData.MoodlesStatuses.ToDictionary(s => s.GUID, s => s.Title);
        var listingSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().WindowPadding.Y * 2);

        foreach (var moodle in _manager.ItemInEditor!.RestraintMoodles.ToList())
        {
            var itemLabel = moodle is MoodlePreset
                ? presetLookup.TryGetValue(moodle.Id, out var presetTitle) ? presetTitle : "INVALID PRESET"
                : statusLookup.TryGetValue(moodle.Id, out var statusTitle) ? statusTitle : "INVALID STATUS";
            var typeText = moodle is MoodlePreset ? "Moodle Preset Item" : "Moodle Status Item";

            using (CkRaii.FramedChildPadded(moodle.Id.ToString(), listingSize, CkColor.FancyHeaderContrast.Uint()))
            {
                using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
                using (ImRaii.Group())
                {
                    CkGui.ColorText(itemLabel.StripColorTags(), ImGuiColors.DalamudGrey);
                    CkGui.ColorText(typeText, ImGuiColors.DalamudGrey3);
                }

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - buttonSize.X);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + (ImGui.GetContentRegionAvail().Y - buttonSize.Y) / 2);
                if (CkGui.IconButton(FAI.Eraser, inPopup: true))
                    _manager.ItemInEditor!.RestraintMoodles.Remove(moodle);
            }
        }
    }
}
