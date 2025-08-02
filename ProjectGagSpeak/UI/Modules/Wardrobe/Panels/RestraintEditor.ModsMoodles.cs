using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using CkCommons.Helpers;
using CkCommons.Raii;
using CkCommons.Widgets;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using ImGuiNET;
using OtterGui.Text;
using CkCommons;
using CkCommons.Gui;

namespace GagSpeak.Gui.Wardrobe;

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
        using (CkRaii.HeaderChild("Associated Mods", panelSize, FancyTabBar.RoundingInner, HeaderFlags.SizeIncludesHeader))
        {
            DrawModSelector();
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingMods, ImGui.GetWindowPos(), ImGui.GetWindowSize());
            DrawModsList();
        }

        // Then the one for the moodles and such.
        ImGui.SameLine(0, ImGui.GetStyle().WindowPadding.X);
        using (CkRaii.HeaderChild("Associated Moodles", panelSize, FancyTabBar.RoundingInner, HeaderFlags.SizeIncludesHeader))
        {
            DrawMoodleSelector();
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.AddingMoodles, ImGui.GetWindowPos(), ImGui.GetWindowSize());
            DrawMoodlesList();
            // Draw out the moodle icon row.
            _moodleDrawer.ShowStatusIconsFramed("AssociatedMoodles", _manager.ItemInEditor!.RestraintMoodles, 
                ImGui.GetContentRegionAvail().X, CkStyle.ChildRoundingLarge(), rows: 2);
            _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.MoodlePreview, ImGui.GetWindowPos(), ImGui.GetWindowSize());
        }
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.ModsMoodles, ImGui.GetWindowPos(), ImGui.GetWindowSize());
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
        {
            if (!_manager.ItemInEditor!.RestraintMods.Any(m => m.Equals(_selectedPreset.Label)))
                _manager.ItemInEditor!.RestraintMods.Add(new ModSettingsPreset(_selectedPreset));
        }

        void ModCombo()
        {
            var change = _modPresets.ModCombo.Draw("##ModSelector", _selectedPreset.Container.DirectoryPath, comboWidth, 1.4f, CFlags.NoArrowButton);
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
            var presetChange = _modPresets.PresetCombo.Draw("##ModPresetSelector", _selectedPreset.Label, comboWidth, 1f, CFlags.NoArrowButton);
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
        _guides.OpenTutorial(TutorialType.Restraints, StepsRestraints.SwapMoodleTypes, ImGui.GetWindowPos(), ImGui.GetWindowSize(),
            () => { }/* attach a moodle here */ );

        ImUtf8.SameLineInner();
        var comboWidth = ImGui.GetContentRegionAvail().X - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;
        _moodleDrawer.DrawMoodleCombo(_selectedMoodle, comboWidth, CFlags.NoArrowButton);

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
        var innerRegion = ImGui.GetContentRegionAvail().WithoutWinPadding();
        using var _ = CkRaii.FrameChildPadded("MoodlesList", innerRegion, CkColor.FancyHeaderContrast.Uint(), 0, CkStyle.ChildRoundingLarge());

        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);
        foreach (var mod in _manager.ItemInEditor!.RestraintMods.ToList())
        {
            var itemLabel = mod.Label;
            var itemSource = mod.Container.ModName;

            using (CkRaii.FramedChildPaddedW(mod.Container.DirectoryPath + mod.Label, ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2, CkColor.FancyHeaderContrast.Uint(), 0))
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
        var size = ImGui.GetContentRegionAvail();
        var height = size.Y - (MoodleDrawer.FramedIconDisplayHeight(2).AddWinPadY() + ImGui.GetStyle().ItemSpacing.Y);

        using var _ = CkRaii.FramedChildPaddedWH("MoodlesList", new Vector2(size.X, height), CkColor.FancyHeaderContrast.Uint(), 0, CkStyle.ChildRoundingLarge());
        
        var buttonSize = CkGui.IconButtonSize(FAI.Eraser);

        foreach (var moodle in _manager.ItemInEditor!.RestraintMoodles.ToList())
        {
            var itemLabel = moodle is MoodlePreset p
                ? MoodleCache.IpcData.Presets.GetValueOrDefault(p.Id).Title.StripColorTags() ?? "<invalid preset>"
                : MoodleCache.IpcData.Statuses.GetValueOrDefault(moodle.Id).Title.StripColorTags() ?? "<invalid status>";
            var typeText = moodle is MoodlePreset ? "Moodle Preset Item" : "Moodle Status Item";

            using (CkRaii.FramedChildPaddedW(moodle.Id.ToString(), ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 2, CkColor.FancyHeaderContrast.Uint(), 0))
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
