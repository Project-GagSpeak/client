using Dalamud.Interface.Utility;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using GagSpeak.CkCommons.Gui.Utility;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using ImGuiNET;
using ImGuizmoNET;
using NAudio.SoundFont;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;
using Penumbra.Api.Enums;
using static FFXIVClientStructs.FFXIV.Client.UI.RaptureAtkHistory.Delegates;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class ModPresetCombo : CkFilterComboCache<ModSettingsPreset>
{
    private ModSettingPresetManager Manager;
    private string _currentItem;
    public ModPresetCombo(ILogger log, ModSettingPresetManager manager,
        Func<IReadOnlyList<ModSettingsPreset>> generator) : base(generator, log)
    {
        Manager = manager;
        SearchByParts = false;
    }

    public void SetDirty()
    {
        _currentItem = string.Empty;
        CurrentSelectionIdx = -1;
        Current = default;
        Cleanup();
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
        => Draw(label, currentPreset, width, innerWidthScaler, ImGuiComboFlags.None);

    /// <summary> An override to the normal draw method that forces the current item to be the item passed in. </summary>
    /// <returns> True if a new item was selected, false otherwise. </returns>
    public bool Draw(string label, string currentPreset, float width, float innerWidthScaler, ImGuiComboFlags flags)
    {
        InnerWidth = width * innerWidthScaler;
        _currentItem = currentPreset;
        var previewLabel = Items.FirstOrDefault(i => i.Label == _currentItem)?.Label ?? "Select a Preset...";
        var result = Draw(label, previewLabel, string.Empty, width, ImGui.GetTextLineHeightWithSpacing(), flags);
        if(Items.Count <= 0)
            CkGui.AttachToolTip("You will not be able to open this dropdown until you have selected a mod from the mod list above!");
        return result;
    }

    protected override string ToString(ModSettingsPreset obj)
        => obj.Label;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        using var id = ImRaii.PushId(globalIdx);
        var preset = Items[globalIdx];
        var ret = ImGui.Selectable(preset.Label, selected);

        // draws a fancy box when the mod is hovered giving you the details about the mod.
        if (ImGui.IsItemHovered() && Manager.GetModInfo(preset.Container.DirectoryPath)?.AllSettings is { } allSettings)
        {
            using var disabled = ImRaii.Disabled(true);
            using var style = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale)
                .Push(ImGuiStyleVar.Alpha, .95f);
            using var tt = ImRaii.Tooltip();

            foreach (var (groupName, groupInfo) in allSettings)
            {
                if (groupName.IsNullOrEmpty())
                    continue;

                var optionType = groupInfo.GroupType;
                // draw the output based on what the type is.
                switch (optionType)
                {
                    case GroupType.Single when groupInfo.Options.Length <= Globals.MaxRadioOptionCount:
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
                        Log.LogWarning($"Unknown ModSettingGroupType {optionType} for group {groupName}");
                        break;
                }
            }
            return ret;
        }
        return ret;
    }
}
