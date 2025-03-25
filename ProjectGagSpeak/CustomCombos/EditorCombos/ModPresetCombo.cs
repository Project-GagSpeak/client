using Dalamud.Interface.Utility;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class ModPresetCombo : CkFilterComboCache<(string PresetName, ModSettings CustomSettings)>
{
    private string _currentItem;
    public ModPresetCombo(ILogger log, Func<IReadOnlyList<(string PresetName, ModSettings CustomSettings)>> generator)
    : base(generator, log)
    {
        SearchByParts = false;
    }

    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            CurrentSelection = Items[NewSelection.Value];
    }

    protected override int UpdateCurrentSelected(int currentSelected)
    {
        if (CurrentSelection.PresetName == _currentItem)
            return currentSelected;

        CurrentSelectionIdx = Items.IndexOf(i => i.PresetName == _currentItem);
        CurrentSelection = CurrentSelectionIdx >= 0 ? Items[CurrentSelectionIdx] : default;
        return base.UpdateCurrentSelected(CurrentSelectionIdx);
    }

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, string currentPresetName, float width, float innerWidthScaler)
    {
        InnerWidth = width * innerWidthScaler;
        _currentItem = currentPresetName;
        var previewLabel = Items.FirstOrDefault(i => i.PresetName == _currentItem).PresetName ?? "Select a Setting Preset...";
        var result = Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
        if(Items.Count <= 0)
            CkGui.AttachToolTip("You will not be able to open this dropdown until you have selected a mod from the mod list above!");
        return result;
    }

    protected override string ToString((string PresetName, ModSettings CustomSettings) obj)
        => obj.PresetName;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var (presetName, settings) = Items[globalIdx];
        bool ret = ImGui.Selectable(presetName, selected);

        // draws a fancy box when the preset is hovered giving you the details about the mod.
        if (ImGui.IsItemHovered())
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale);
            using var tt = ImRaii.Tooltip();
            ImGui.Dummy(new Vector2(300 * ImGuiHelpers.GlobalScale, 0));
            using (var group = ImRaii.Group())
            {
                ImGui.TextUnformatted("Enabled");
                ImGui.TextUnformatted("Priority");
                DrawSettingsLeft(settings);
            }

            ImGui.SameLine(Math.Max(ImGui.GetItemRectSize().X + 3 * ImGui.GetStyle().ItemSpacing.X, 150 * ImGuiHelpers.GlobalScale));
            using (var group = ImRaii.Group())
            {
                ImGui.TextUnformatted(settings.Enabled.ToString());
                ImGui.TextUnformatted(settings.Priority.ToString());
                DrawSettingsRight(settings);
            }
        }

        return ret;
    }

    public static void DrawSettingsLeft(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            ImGui.TextUnformatted(setting.Key);
            for (var i = 1; i < setting.Value.Count; ++i)
                ImGui.NewLine();
        }
    }

    public static void DrawSettingsRight(ModSettings settings)
    {
        foreach (var setting in settings.Settings)
        {
            if (setting.Value.Count == 0)
                ImGui.TextUnformatted("<None Enabled>");
            else
                foreach (var option in setting.Value)
                    ImGui.TextUnformatted(option);
        }
    }
}
