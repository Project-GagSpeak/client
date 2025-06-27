using CkCommons;
using CkCommons.Gui;
using CkCommons.Raii;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using ImGuiNET;

namespace GagSpeak.Gui.Wardrobe;

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
        var headerName = _selector.SelectedContainer.ModName.IsNullOrEmpty() ? "Select a Mod to view its Presets" : _selector.SelectedContainer.ModName;
        using (CkRaii.HeaderChild(headerName, new Vector2(ImGui.GetContentRegionAvail().X, presetSelectorH), HeaderFlags.AddPaddingToHeight))
            DrawPresetListForSelected();

        if (!_selector.SelectedContainer.ModPresets.Any(p => p.Label == _selectedPreset))
            _selectedPreset = string.Empty;

        var editingPreset = _manager.ItemInEditor is not null;
        var icon = editingPreset ? FAI.Save : FAI.Edit;

        if (_selectedPreset.IsNullOrEmpty())
        {
            using (CkRaii.HeaderChild("Customize Settings", ImGui.GetContentRegionAvail(), HeaderFlags.SizeIncludesHeader))
                _modDrawer.DrawPresetEditor();
        }
        else
        {
            using (CkRaii.IconButtonHeaderChild("Settings Preset Editor", icon, ImGui.GetContentRegionAvail(), ToggleEditState, CkStyle.HeaderRounding(), HeaderFlags.SizeIncludesHeader))
            {
                if (editingPreset)
                    _modDrawer.DrawPresetEditor();
                else
                {
                    var modPreset = _selector.SelectedContainer.ModPresets.FirstOrDefault(p => p.Label == _selectedPreset) ?? new ModSettingsPreset(_selector.SelectedContainer);
                    _modDrawer.DrawPresetPreview(modPreset);
                }
            }
        }
    }

    private void ToggleEditState()
    {
        if (_manager.ItemInEditor is not null)
            _manager.ExitEditingAndSave();
        else
            _manager.StartEditingCustomPreset(_selector.SelectedContainer.DirectoryPath, _selectedPreset);
    }

    private void DrawPresetListForSelected()
    {
        using var col = ImRaii.PushColor(ImGuiCol.FrameBg, CkColor.FancyHeaderContrast.Uint());
        using (CkRaii.FramedChild("PresetList", ImGui.GetContentRegionAvail(), CkColor.FancyHeaderContrast.Uint(), CkStyle.ChildRounding(),
            2 * ImGuiHelpers.GlobalScale, wFlags: WFlags.AlwaysUseWindowPadding))
        {
            // return if the size of the keys is 0.
            if (_selector.SelectedContainer.ModName.IsNullOrEmpty())
                return;
            // We have a mod, so we should grab the presets from it.
            var itemSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight());
            foreach (var preset in _selector.SelectedContainer.ModPresets)
            {
                var selected = _selectedPreset == preset.Label;
                if (DrawItemListingChild(preset, selected, itemSize, () => _selectedPreset = selected ? string.Empty : preset.Label))
                    break;
                CkGui.AttachToolTip("Hold CTRL and click to rename!");
            }
            // Finally, draw a framed child for creating a new preset.
            DrawNewItemChild(itemSize);
        }
    }

    private bool DrawItemListingChild(ModSettingsPreset modPreset, bool selected, Vector2 size, Action onClick)
    {
        var renamingChild = _renamingPresetName == modPreset.Label;
        using (ImRaii.Child("Preset-" + modPreset.Label, size))
        {
            ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
            if (renamingChild)
            {
                var newName = modPreset.Label;
                if (ImGui.InputText("##RenamePreset", ref newName, 255, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (newName != modPreset.Label)
                    {
                        modPreset.Label = newName;
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
                ImGui.TextUnformatted(modPreset.Label);
            }
            ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - ImGui.GetStyle().ItemInnerSpacing.X);
            if (CkGui.IconButton(FAI.Eraser, inPopup: true))
            {
                _manager.RemovePreset(modPreset);
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
                _renamingPresetName = _renamingPresetName == modPreset.Label ? string.Empty : modPreset.Label;
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
            _manager.TryAddModPreset(_selector.SelectedContainer, _newPresetName);
    }
}
