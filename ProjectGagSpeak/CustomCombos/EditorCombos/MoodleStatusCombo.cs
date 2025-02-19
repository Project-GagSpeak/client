using Dalamud.Utility;
using GagSpeak.CkCommons.TextHelpers;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data.Character;
using ImGuiNET;

namespace GagSpeak.CustomCombos.EditorCombos;

public sealed class MoodleStatusCombo : CkMoodleComboBase<MoodlesStatusInfo>
{
    public MoodleStatusCombo(float iconScale, CharaIPCData data, MoodleStatusMonitor monitor, ILogger log)
        : base(iconScale, data, monitor, log, () => [ ..data.MoodlesStatuses.OrderBy(x => x.Title) ])
    { }

    protected override string ToString(MoodlesStatusInfo obj)
        => obj.Title;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable(moodleStatus.Title.StripColorTags(), selected);

        if (moodleStatus.IconID > 200000)
        {
            ImGui.SameLine();
            var offset = ImGui.GetContentRegionAvail().X - IconSize.X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            _statuses.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
            DrawItemTooltip(moodleStatus);
        }
        return ret;
    }

    public void Draw(float width)
    {
        var name = CurrentSelection.Title.StripColorTags();
        var label = name.IsNullOrWhitespace() ? "Select a Status..." : name;
        Draw("##StatusMoodle", name, string.Empty, width, ImGui.GetTextLineHeightWithSpacing());
    }
}
