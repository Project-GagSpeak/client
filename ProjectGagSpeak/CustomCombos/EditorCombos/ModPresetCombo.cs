using GagSpeak.PlayerData.Storage;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class ModPresetCombo : CkFilterComboCache<ModSettingsPreset>
{
    private string _currentItem;
    public ModPresetCombo(ILogger log, Func<IReadOnlyList<ModSettingsPreset>> generator)
    : base(generator, log)
    {
        SearchByParts = false;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            Current = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (Current?.Label == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.Label == _currentItem);
        Current = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, string currentPreset, float width, float innerWidthScaler)
    {
        InnerWidth = width * innerWidthScaler;
        _currentItem = currentPreset;
        var previewLabel = Items.FirstOrDefault(i => i.Label == _currentItem)?.Label ?? "Select a Setting Preset...";
        var result = Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
        if(Items.Count <= 0)
            CkGui.AttachToolTip("You will not be able to open this dropdown until you have selected a mod from the mod list above!");
        return result;
    }

    protected override string ToString(ModSettingsPreset obj)
        => obj.Label;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var presetSettings = Items[globalIdx];
        var ret = ImGui.Selectable(presetSettings.Label, selected);
        return ret;
    }
}
