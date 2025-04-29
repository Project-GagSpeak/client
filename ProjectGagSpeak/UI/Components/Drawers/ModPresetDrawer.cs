using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.Raii;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using ImGuiNET;
using OtterGui.Text;
using OtterGui.Widgets;
using Penumbra.Api.Enums;

namespace GagSpeak.CkCommons.Gui.Components;
// This class will automate the drawing of checkboxes, buttons, sliders and more used across the various UI elements through a modular approach.
public sealed class ModPresetDrawer
{
    private readonly ILogger<ModPresetDrawer> _logger;
    private readonly ModSettingPresetManager _manager;

    public ModPresetDrawer(ILogger<ModPresetDrawer> logger, ModSettingPresetManager manager)
    {
        _logger = logger;
        _manager = manager;
    }

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
        // construct a child object here.
        var pos = ImGui.GetCursorScreenPos();
        var style = ImGui.GetStyle();
        var dropdownH = ImGui.GetFrameHeight()*2 + style.ItemSpacing.Y;
        var previewH = 9 * ImGui.GetFrameHeightWithSpacing();
        var winSize = new Vector2(width, previewH);

        // Can migrate to usings later but for now am lazy.
        using (CkRaii.HeaderChild("Associated Mod", winSize))
        {
            var widthInner = ImGui.GetContentRegionAvail().X;

            using (ImRaii.Group())
            {
                // The Mod Selection.
                var change = _manager.ModCombo.Draw("AMP-ModCombo-" + id, item.Mod.Container.DirectoryPath, widthInner, 1.4f);
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

                // The Mod Preset Selection.
                var presetChange = _manager.PresetCombo.Draw("AMP-ModPresetCombo-" + id, item.Mod.Label, widthInner, 1f);
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
            }

            // now we need to draw a preview display, this should be 10x ImGui.GetFrameHeightWithSpacing() in size, spanning the same width.
            DrawPresetPreview(item.Mod);
        }
    }

    public void DrawPresetTooltip(ModSettingsPreset modPreset)
    {
        using var disabled = ImRaii.Disabled(true);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.Alpha, .95f);
        using var tt = ImRaii.Tooltip();

        DrawPresetPreviewInner(modPreset);
    }

    public void DrawPresetPreview(ModSettingsPreset preset)
    {
        var outerRegion = ImGui.GetContentRegionAvail();
        using (CkRaii.FramedChildPadded("MP-Preview" + preset.Container.DirectoryPath, ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint()))
        {
            using (UiFontService.GagspeakLabelFont.Push())
            {
                var offset = (outerRegion.X - ImGui.CalcTextSize("Previewing Settings").X) / 2;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
                CkGui.OutlinedFont("Previewing Settings", CkGui.Color(ImGuiColors.DalamudRed), 0xFF000000, 2);
            }

            DrawPresetPreviewInner(preset);
        }
    }

    public void DrawPresetPreviewInner(ModSettingsPreset preset)
    {
        var allSettings = _manager.GetModInfo(preset.Container.DirectoryPath)?.AllSettings;
        if (allSettings is null)
            return;

        using var disabled = ImRaii.Disabled(true);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.Alpha, .95f);

        foreach (var (groupName, groupInfo) in allSettings)
        {
            if (groupName.IsNullOrEmpty())
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
        using (CkRaii.FramedChildPadded("MP-EditorWindow", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint()))
        {
            if (_manager.ItemInEditor is not { } activeEditor)
                return;

            if (_manager.GetModInfo(_manager.ItemInEditor.Container.DirectoryPath)?.AllSettings is not { } allSettings)
                return;

            foreach (var (groupName, groupInfo) in allSettings)
            {
                if (groupName.IsNullOrEmpty())
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
