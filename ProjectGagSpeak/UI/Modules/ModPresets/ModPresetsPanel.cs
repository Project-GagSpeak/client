using CkCommons;
using CkCommons.Raii;
using CkCommons.Widgets;
using Dalamud.Interface.Colors;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Components;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Tutorial;
using GagSpeak.State.Managers;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.Gui.Wardrobe;

public class ModPresetsPanel : DisposableMediatorSubscriberBase
{
    private readonly ModPresetFileSelector _selector;
    private readonly ModPresetManager _manager;
    private readonly ModPresetDrawer _modDrawer;
    private readonly TutorialService _guides;

    private ItemSelectorBox<ModSettingsPreset> _presetSelector;
    private ModSettingsPreset? _selectedRef = null;
    private string _renamingPresetName = string.Empty;
    private string _newPresetName = string.Empty;

    public ModPresetsPanel(ILogger<ModPresetsPanel> logger, GagspeakMediator mediator,
        ModPresetFileSelector selector, ModPresetManager manager, ModPresetDrawer modDrawer,
        TutorialService guides) : base(logger, mediator)
    {
        _selector = selector;
        _manager = manager;
        _modDrawer = modDrawer;
        _guides = guides;

        _presetSelector = new ItemSelectorBoxBuilder<ModSettingsPreset>()
            .Create(FAI.Plus, "New Preset")
            .WithColSelected(CkCol.LChild.Uint())
            .WithColHovered(ImGui.GetColorU32(ImGuiCol.FrameBgHovered))
            .WithBgCol(CkCol.CurvedHeaderFade.Uint())
            .OnAdd(name => _manager.TryCreatePreset(_selector.Selected, name))
            .OnSelect(newSel => _selectedRef = newSel)
            .OnRename((oldItem, newName) => _manager.RenamePreset(oldItem, newName))
            .OnRemove(item => _manager.RemovePreset(item))
            .Build();

        Mediator.Subscribe<SelectedModContainerChanged>(this, _ 
            => _selectedRef = _selector.Selected?.ModPresets.FirstOrDefault());
    }

    public void DrawModuleTitle()
    {
        // get the text of the mod we are currently editing.
        var text = "Mod Preset Manager";
        if (_selector.Selected is not null)
            text = $"Preset Manager for {_selector.Selected.ModName}";
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImUtf8.TextFrameAligned(text);
    }

    public void DrawPresetEditor(Vector2 region)
    {
        try
        {
            if (_selectedRef != null && _selector.Selected is { } selected)
            {
                var editingPreset = _manager.ItemInEditor is not null;
                var headerText = $"{(editingPreset ? "Editing" : "Inspecting")} ({_selectedRef.Label})";
                var icon = editingPreset ? FAI.Save : FAI.Edit;
                using (var c = CkRaii.IconButtonHeaderChild(headerText, icon, region, ToggleEditState, CkStyle.HeaderRounding(), HeaderFlags.SizeIncludesHeader))
                {
                    DrawPresetListForSelected(c.InnerRegion);
                    if (_manager.ItemInEditor is null)
                        _modDrawer.DrawPresetPreview(_selectedRef);
                    else
                        _modDrawer.DrawPresetEditor();
                }
            }
            else
            {
                var text = _selector.Selected is not null ? $"No Preset Selected" : "No Mod Selected";
                using (var c = CkRaii.HeaderChild(text, region, CkStyle.HeaderRounding(), HeaderFlags.SizeIncludesHeader))
                {
                    DrawPresetListForSelected(c.InnerRegion);
                    _modDrawer.DrawPresetPreview(_selectedRef);
                }
            }
        }
        catch (Bagagwa e)
        {
            Logger.LogError("Error while drawing the Mod Presets Panel:" + e);
            ImGui.TextColored(ImGuiColors.DalamudRed, "An error occurred while drawing the Mod Presets Panel. Check the logs for details.");
        }
    }

    private void ToggleEditState()
    {
        if (_manager.ItemInEditor is not null)
            _manager.ExitEditingAndSave();
        else if (_selector.Selected is not null && _selectedRef is not null)
            _manager.StartEditingCustomPreset(_selector.Selected.DirectoryPath, _selectedRef.Label);
    }

    private void DrawPresetListForSelected(Vector2 region)
    {
        var col = CkCol.CurvedHeaderFade.Uint();
        using (var child = CkRaii.FramedChildPaddedW("PresetList", region.X, CkStyle.GetFrameRowsHeight(3), col, col, DFlags.RoundCornersAll))
        {
            // return early if nothing is selected.
            if (_selector.Selected is not { } selected)
                return;

            _presetSelector.DrawSelectorChildBox("PresetListBox", child.InnerRegion, true, selected.ModPresets, _selectedRef, _ => _.Label, GsCol.VibrantPink.Vec4());
        }
    }
}
