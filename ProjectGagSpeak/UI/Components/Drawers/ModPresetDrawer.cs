using CkCommons;
using CkCommons.Gui;
using CkCommons.Gui.Utility;
using CkCommons.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Wardrobe;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.Api.Enums;
using TerraFX.Interop.Windows;

namespace GagSpeak.Gui.Components;
// This class will automate the drawing of checkboxes, buttons, sliders and more used across the various UI elements through a modular approach.
public sealed class ModPresetDrawer
{
    private readonly ILogger<ModPresetDrawer> _logger;
    private readonly ModPresetManager _manager;
    private readonly TutorialService _guides;

    public ModPresetDrawer(ILogger<ModPresetDrawer> logger, ModPresetManager manager,  TutorialService guides)
    {
        _logger = logger;
        _manager = manager;
        _guides = guides;
    }

    // this needs a refactor as it sucks at properly updating currently when we have everything refernecing the same 2 combos.
    public void DrawModPresetCombos(string id, IModPreset modItem, float width)
    {
        using var _ = ImRaii.Group();

        var change = _manager.ModCombo.Draw($"##MP-ModCombo-{id}", modItem.Mod.Container.DirectoryPath, width, 1.4f);
        if (change && !modItem.Mod.Container.DirectoryPath.Equals(_manager.ModCombo.Current?.DirPath))
        {
            if (_manager.ModPresetStorage.FirstOrDefault(mps => mps.DirectoryPath == _manager.ModCombo.Current!.DirPath) is { } match)
            {
                _logger.LogTrace($"Associated Mod changed to {_manager.ModCombo.Current!.Name} [{_manager.ModCombo.Current!.DirPath}] from {modItem.Mod.Container.ModName}");
                modItem.Mod = match.ModPresets.First(); // Let this crash you if it happens, because it means something has gone horribly wrong.
            }
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Associated Mod was cleared. and is now Empty");
            modItem.Mod = new ModSettingsPreset(new ModPresetContainer());
        }

        // The Mod Preset Selection.
        var presetChange = _manager.PresetCombo.Draw($"##MP-PresetCombo-{id}", modItem.Mod.Label, width, 1f);
        if (presetChange && !modItem.Mod.Label.Equals(_manager.PresetCombo.Current?.Label))
        {
            if (modItem.Mod.Container.ModPresets.FirstOrDefault(mp => mp.Label == _manager.PresetCombo.Current!.Label) is { } match)
            {
                _logger.LogTrace($"Associated Mod Preset changed to {_manager.PresetCombo.Current!.Label} [{_manager.PresetCombo.Current.Label}] from {modItem.Mod.Label}");
                modItem.Mod = match;
            }
        }
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            _logger.LogTrace("Associated Mod Preset was cleared. and is now Empty");
            var curContainer = modItem.Mod.Container;
            modItem.Mod = new ModSettingsPreset(curContainer);
        }
    }

    // Method for Drawing the Associated Glamour Item (Singular)
    public void DrawModPresetBox(string id, IRestriction item, float width)
    {
        var winSize = new Vector2(width, CkStyle.GetFrameRowsHeight(2) + (9 * ImGui.GetFrameHeightWithSpacing()));
        using var c = CkRaii.HeaderChild("Associated Mod", winSize, HeaderFlags.AddPaddingToHeight);
        using (ImRaii.Group())
        {
            // The Mod Selection.
            var change = _manager.ModCombo.Draw("AMP-ModCombo-" + id, item.Mod.Container.DirectoryPath, c.InnerRegion.X, 1.4f);
            if (change && !item.Mod.Container.DirectoryPath.Equals(_manager.ModCombo.Current?.DirPath))
            {
                // retrieve and set the new container reference.
                if (_manager.ModPresetStorage.FirstOrDefault(mps => mps.DirectoryPath == _manager.ModCombo.Current!.DirPath) is { } match)
                {
                    _logger.LogTrace($"Associated Mod changed to {_manager.ModCombo.Current!.Name} [{_manager.ModCombo.Current!.DirPath}] from {item.Mod.Container.ModName}");
                    item.Mod = match.ModPresets.First(); // Let this crash you if it happens, because it means something has gone horribly wrong.
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Associated Mod was cleared. and is now Empty");
                item.Mod = new ModSettingsPreset(new ModPresetContainer());
            }
            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectingMod, WardrobeUI.LastPos, WardrobeUI.LastSize);

            // The Mod Preset Selection.
            var presetChange = _manager.PresetCombo.Draw("AMP-ModPresetCombo-" + id, item.Mod.Label, c.InnerRegion.X, 1f);
            if (presetChange && !item.Mod.Label.Equals(_manager.PresetCombo.Current?.Label))
            {
                // recreate the same modsettingPreset, but at the new label.
                if (item.Mod.Container.ModPresets.FirstOrDefault(mp => mp.Label == _manager.PresetCombo.Current!.Label) is { } match)
                {
                    _logger.LogTrace($"Associated Mod Preset changed to {_manager.PresetCombo.Current!.Label} " +
                                     $"[{_manager.PresetCombo.Current.Label}] from {item.Mod.Label}");
                    item.Mod = match;
                }
            }
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                _logger.LogTrace("Associated Mod Preset was cleared. and is now Empty");
                var curContainer = item.Mod.Container;
                item.Mod = new ModSettingsPreset(curContainer);
            }

            _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.SelectingPreset, WardrobeUI.LastPos, WardrobeUI.LastSize);
        }
        
        DrawPresetPreview(item.Mod);
        _guides.OpenTutorial(TutorialType.Restrictions, StepsRestrictions.PresetPreview, WardrobeUI.LastPos, WardrobeUI.LastSize);
    }

    public void DrawPresetTooltip(ModSettingsPreset modPreset)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
        using var tt = ImRaii.Tooltip();
        
        var allSettings = _manager.GetModInfo(modPreset.Container.DirectoryPath)?.AllSettings;
        if (allSettings is null)
            ImGui.Text("Mod is Null!");
        else
        {
            if (allSettings.Keys.Count is 0)
                ImGui.Text("No options in selected Mod.");
            else
                DrawPresetPreviewInner(modPreset, allSettings);
        }
    }

    public void DrawPresetPreview(ModSettingsPreset? preset)
    {
        var region = ImGui.GetContentRegionAvail();
        var id = $"MP-Preview-{(preset?.Container.DirectoryPath ?? "INVALID")}";
        using (CkRaii.FramedChildPaddedWH(id, region, CkCol.CurvedHeaderFade.Uint(), CkCol.CurvedHeaderFade.Uint()))
        {
            if (preset is null)
                return;

            using (Fonts.GagspeakLabelFont.Push())
            {
                var offset = (region.X - ImGui.CalcTextSize("Previewing").X) / 2;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
                CkGui.OutlinedFont("Previewing", CkGui.Color(ImGuiColors.DalamudRed), 0xFF000000, 2);
            }

            var allSettings = _manager.GetModInfo(preset.Container.DirectoryPath)?.AllSettings;
            if (allSettings is null)
                CkGui.ColorTextCentered("No Options", ImGuiColors.DalamudRed);
            else
                DrawPresetPreviewInner(preset, allSettings);
        }
    }

    public void DrawPresetPreviewInner(ModSettingsPreset preset, Dictionary<string, (string[] Options, GroupType GroupType)> settings)
    {
        using var disabled = ImRaii.Disabled(true);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .95f);

        foreach (var (groupName, groupInfo) in settings)
        {
            if (string.IsNullOrEmpty(groupName))
                continue;

            var optionType = groupInfo.GroupType;
            // draw the output based on what the type is.
            switch (optionType)
            {
                case GroupType.Single when groupInfo.Options.Length <= 2:
                    CkGuiUtils.DrawSingleGroupRadio(groupName, groupInfo.Options, preset.SelectedOption(groupName));
                    break;
                case GroupType.Single:
                    CkGuiUtils.DrawSingleGroupCombo(groupName, groupInfo.Options, preset.SelectedOption(groupName));
                    break;
                case GroupType.Multi:
                case GroupType.Imc:
                case GroupType.Combining:
                    CkGuiUtils.DrawMultiGroup(groupName, groupInfo.Options, preset.SelectedOptions(groupName));
                    break;
                default:
                    _logger.LogWarning($"Unknown ModSettingGroupType {optionType} for group {groupName}");
                    break;
            }
        }
    }

    public void DrawPresetEditor()
    {
        var outerRegion = ImGui.GetContentRegionAvail();
        using (CkRaii.FramedChild("MP-EditorWindow", ImGui.GetContentRegionAvail(), CkCol.CurvedHeaderFade.Uint(), CkCol.CurvedHeaderFade.Uint(),
            CkStyle.ChildRounding(), CkStyle.ThinThickness(), wFlags: WFlags.AlwaysUseWindowPadding))
        {
            if (_manager.ItemInEditor is not { } activeEditor)
                return;

            if (_manager.GetModInfo(_manager.ItemInEditor.Container.DirectoryPath)?.AllSettings is not { } allSettings)
                return;

            foreach (var (groupName, groupInfo) in allSettings)
            {
                if (string.IsNullOrEmpty(groupName))
                    continue;

                var optionType = groupInfo.GroupType;
                // draw the output based on what the type is.
                switch (optionType)
                {
                    case GroupType.Single when groupInfo.Options.Length <= 2:
                        DrawSingleGroupRadio(activeEditor, groupName, groupInfo.Options);
                        break;
                    case GroupType.Single:
                        DrawSingleGroupCombo(activeEditor, groupName, groupInfo.Options);
                        break;
                    case GroupType.Multi:
                    case GroupType.Imc:
                    case GroupType.Combining:
                        DrawMultiGroup(activeEditor, groupName, groupInfo.Options);
                        break;
                    default:
                        _logger.LogWarning($"Unknown ModSettingGroupType {optionType} for group {groupName}");
                        break;
                }
            }
        }
    }

    /// <summary> Draw a single group selector as a combo box.(For Editing) </summary>
    private void DrawSingleGroupCombo(ModSettingsPreset cache, string groupName, string[] options)
    {
        var current = cache.SelectedOption(groupName);
        var comboWidth = ImGui.GetContentRegionAvail().X / 2;
        if(CkGuiUtils.StringCombo(groupName, comboWidth, current, out var newSelection, options.AsEnumerable(), "None Selected..."))
            cache.ModSettings[groupName] = new List<string>() { newSelection };
    }

    /// <summary> Draw a single group selector as a set of radio buttons. (for Editing) </summary>
    private void DrawSingleGroupRadio(ModSettingsPreset cache, string groupName, string[] options)
    {
        var current = cache.SelectedOption(groupName);
        using var id = ImUtf8.PushId(groupName);
        var minWidth = Widget.BeginFramedGroup(groupName);

        using (ImRaii.Disabled(false))
        {
            for (var idx = 0; idx < options.Length; ++idx)
            {
                using var i = ImUtf8.PushId(idx);
                var option = options[idx];
                if (ImUtf8.RadioButton(option, current == option))
                    cache.ModSettings[groupName] = new List<string>() { option };
            }
        }
        Widget.EndFramedGroup();
    }

    /// <summary> Draw a multi group selector as a bordered set of checkboxes. (for Editing) </summary>
    private void DrawMultiGroup(ModSettingsPreset cache, string groupName, string[] options)
    {
        var current = cache.SelectedOptions(groupName);
        using var id = ImUtf8.PushId(groupName);
        var minWidth = Widget.BeginFramedGroup(groupName);

        using (ImRaii.Disabled(false))
        {
            for (var idx = 0; idx < options.Length; ++idx)
            {
                using var i = ImUtf8.PushId(idx);
                var option = options[idx];
                var isSelected = current.Contains(option);
                if(ImUtf8.Checkbox(option, ref isSelected))
                {
                    var newOptions = (isSelected ? current.Append(option) : current.Where(x => x != option)).ToArray();
                    cache.ModSettings[groupName] = newOptions.ToList();
                }
            }
        }
        Widget.EndFramedGroup();
    }
}
