using Dalamud.Utility;
using GagSpeak.CkCommons.Text;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using ImGuiNET;
using Penumbra.GameData.Files.ShaderStructs;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class MoodlePresetCombo : CkMoodleComboBase<MoodlePresetInfo>
{
    private int longestPresetCount => _moodleData.MoodlesPresets.Max(x => x.Statuses.Count);
    private float ComboBoxWidth => IconSize.X * (longestPresetCount - 1);
    public MoodlePresetCombo(float iconScale, CharaIPCData data, MoodleStatusMonitor monitor, ILogger log)
        : base(iconScale, data, monitor, log, () => [ ..data.MoodlesPresets.OrderBy(x => x.Title)])
    {
        SearchByParts = false;
    }

    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodlePreset = Items[globalIdx];
        var ret = ImGui.Selectable(moodlePreset.Title.StripColorTags(), selected);

        if (moodlePreset.Statuses.Count <= 0)
            return ret;

        ImGui.SameLine();
        var offset = ImGui.GetContentRegionAvail().X - IconSize.X * moodlePreset.Statuses.Count;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);

        for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
        {
            var status = moodlePreset.Statuses[i];
            var info = _moodleData.MoodlesStatuses.FirstOrDefault(x => x.GUID == status);

            if (EqualityComparer<MoodlesStatusInfo>.Default.Equals(info, default))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + IconSize.X);
                continue;
            }

            _statuses.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            DrawItemTooltip(info);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImGui.SameLine();
        }
        return ret;
    }

    public void Draw(float width)
    {
        // update the inner width.
        InnerWidth = IconSize.X * (longestPresetCount - 1);
        // Begin Draw.
        var name = CurrentSelection.Title;
        var label = name.IsNullOrWhitespace() ? "Select a Preset..." : name;
        Draw("##PresetMoodle", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }
}
