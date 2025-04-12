using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.UI.Components;
using ImGuiNET;

namespace GagSpeak.UI.Wardrobe;

public class ModPresetsPanel
{
    private readonly ILogger<ModPresetsPanel> _logger;
    private readonly ModPresetSelector _selector;
    private readonly ModSettingPresetManager _manager;
    private readonly ModPresetDrawer _modDrawer;
    private readonly CosmeticService _cosmetics;
    private readonly TutorialService _guides;
    public ModPresetsPanel(
        ILogger<ModPresetsPanel> logger,
        ModPresetSelector selector,
        ModSettingPresetManager manager,
        ModPresetDrawer modDrawer,
        CosmeticService cosmetics,
        TutorialService guides)
    {
        _logger = logger;
        _selector = selector;
        _manager = manager;
        _modDrawer = modDrawer;
        _cosmetics = cosmetics;
        _guides = guides;
    }

    private IReadOnlyDictionary<string, ModSettings> _presets
        => _manager.GetModPresets(_selector.SelectedMod.DirectoryName);
    private string _selectedPreset = string.Empty;
    private string _renamingPresetName = string.Empty;
    private string _newPresetName = string.Empty;

    public void DrawModuleTitle()
    {
        //using var font = UiFontService.GagspeakLabelFont.Push();
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("Mod Preset Manager").X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted("Mod Preset Manager");
    }

    public void DrawPresetEditor()
    {
        using var group = ImRaii.Group();
        // Create two sub components here, a preset selector and a preset editor.
        var presetSelectorH = ImGui.GetFrameHeightWithSpacing() * 3;
        var headerName = _selector.SelectedMod.Name.IsNullOrEmpty() ? "Select a Mod to view its Presets" : _selector.SelectedMod.Name;
        using (CkComponents.CenterHeaderChild("MP-Selector", headerName, new Vector2(ImGui.GetContentRegionAvail().X, presetSelectorH), WFlags.AlwaysUseWindowPadding))
            DrawPresetListForSelected();

        if (!_presets.ContainsKey(_selectedPreset))
            _selectedPreset = string.Empty;

        var editingPreset = _manager.CurrentEditCache is not null;
        var icon = editingPreset ? FAI.Save : FAI.Edit;

        if(_selectedPreset.IsNullOrEmpty())
        {
            using (CkComponents.CenterHeaderChild("MP-EditorFallback", "Customize Settings", ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding))
                _modDrawer.DrawPresetPreview(_selector.SelectedMod, _selectedPreset);
        }
        else
        {
            using (CkComponents.ButtonHeaderChild("MP-Editor", "Settings Preset Editor", ImGui.GetContentRegionAvail(), CkComponents.DefaultHeaderRounding,
                WFlags.AlwaysUseWindowPadding, icon, ToggleEditState))
            {
                if(editingPreset)
                    _modDrawer.DrawPresetEditor();
                else
                    _modDrawer.DrawPresetPreview(_selector.SelectedMod, _selectedPreset);
            }
        }
    }

    private void ToggleEditState()
    {
        if (_manager.CurrentEditCache is not null)
            _manager.ExitEditingAndSave();
        else
            _manager.StartEditingCustomPreset(_selector.SelectedMod, _selectedPreset);
    }

    private void DrawPresetListForSelected()
    {
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());
        using (CkComponents.FramedChild("PresetList", CkColor.FancyHeaderContrast.Uint(), ImGui.GetContentRegionAvail(), WFlags.AlwaysUseWindowPadding))
        {
            // return if the size of the keys is 0.
            if (_selector.SelectedMod.Name.IsNullOrEmpty())
                return;
            // We have a mod, so we should grab the presets from it.
            var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
            foreach (var preset in _presets.Keys)
            {
                var selected = _selectedPreset == preset;
                if (DrawItemListingChild(preset, selected, itemSize, () => _selectedPreset = selected ? string.Empty : preset))
                    break;
                CkGui.AttachToolTip("Hold CTRL and click to rename!");
            }
            // Finally, draw a framed child for creating a new preset.
            DrawNewItemChild(itemSize);
        }
    }

    private bool DrawItemListingChild(string presetName, bool selected, Vector2 size, Action onClick)
    {
        var renamingChild = _renamingPresetName == presetName;
        using (ImRaii.Child("Preset-" + presetName, size))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            if (renamingChild)
            {
                var newName = presetName;
                if (ImGui.InputText("##RenamePreset", ref newName, 255, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (newName != presetName)
                    {
                        _manager.RenameModPreset(_selector.SelectedMod, presetName, newName);
                        _renamingPresetName = string.Empty;
                        return true;
                    }
                }
                if(ImGui.IsItemDeactivated())
                    _renamingPresetName = string.Empty;
            }
            else
            {
                ImGui.AlignTextToFramePadding();
                ImGui.TextUnformatted(presetName);
            }
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemInnerSpacing.X);
            if (CkGui.IconButton(FAI.Eraser, inPopup: true))
            {
                _manager.RemoveSettingPreset(_selector.SelectedMod.DirectoryName, presetName);
                return true;
            }
        }

        var hovering = ImGui.IsItemHovered();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var color = selected ? CkColor.ElementBG.Uint() : hovering ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        ImGui.GetWindowDrawList().AddRectFilled(min, max, color, ImGui.GetStyle().FrameRounding * 1.25f, ImDrawFlags.RoundCornersAll);
        ImGui.GetWindowDrawList().AddRect(min, max, color, ImGui.GetStyle().FrameRounding * 1.25f, ImDrawFlags.None, 2);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && hovering)
        {
            if (ImGui.GetIO().KeyCtrl)
                _renamingPresetName = _renamingPresetName == presetName ? string.Empty : presetName;
            else
                onClick();
        }
        return false;
    }

    private void DrawNewItemChild(Vector2 size)
    {
        using (ImRaii.Child("NewPresetButton", size))
        {
            var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize("New Preset").X) / 2;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            ImGui.TextUnformatted("New Preset");
        }
        var hovered = ImGui.IsItemHovered();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        var color = hovered ? ImGui.GetColorU32(ImGuiCol.FrameBgHovered) : CkColor.FancyHeaderContrast.Uint();
        ImGui.GetWindowDrawList().AddRectFilled(min, max, color, ImGui.GetStyle().FrameRounding * 2f);
        ImGui.GetWindowDrawList().AddRect(min, max, color, ImGui.GetStyle().FrameRounding * 2f);
        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && hovered)
            _manager.AddModPreset(_selector.SelectedMod, "New Preset");
    }
}
