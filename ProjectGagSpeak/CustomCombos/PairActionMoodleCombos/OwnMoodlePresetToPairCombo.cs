using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.IPC;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodlePresetToPairCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private readonly CharaIPCData _ownMoodles;
    private int longestPresetCount => _pairRef.LastIpcData.MoodlesPresets.Max(x => x.Statuses.Count);
    public OwnMoodlePresetToPairCombo(float iconScale, MoodleStatusMonitor monitor, CharaIPCData data,
        Pair pair, MainHub hub, ILogger log, UiSharedService ui, string bText, string bTT)
        : base(iconScale, monitor, pair, hub, log, ui, bText, bTT, () =>
        [
            ..data.MoodlesPresets.OrderBy(x => x.Title),
        ])
    {
        _ownMoodles = data;
    }

    private float ComboBoxWidth => IconSize.X * (longestPresetCount - 1);

    protected override bool DisableCondition()
        => _pairRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou) is false;

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
            var info = _ownMoodles.MoodlesStatuses.FirstOrDefault(x => x.GUID == status);

            if (EqualityComparer<MoodlesStatusInfo>.Default.Equals(info, default))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + IconSize.X);
                continue;
            }

            _statuses.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            // get the dispelable moodle if any.
            var title = info.StatusOnDispell.IsEmptyGuid()
                ? "Unknown"
                : _ownMoodles.MoodlesStatuses.FirstOrDefault(x => x.GUID == info.StatusOnDispell).Title ?? "Unknown";

            DrawItemTooltip(info, title);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImGui.SameLine();
        }

        return ret;
    }

    protected override bool CanDoAction(MoodlePresetInfo item)
        => _statuses.CanApplyPairStatus(_pairRef.PairPerms, item.Statuses.Select(
            x => _ownMoodles.MoodlesStatuses.FirstOrDefault(y => y.GUID == x)).ToList());

    protected override void DoAction(MoodlePresetInfo item)
    {
        var statusInfos = item.Statuses
            .Select(x => _pairRef.LastIpcData.MoodlesStatuses.FirstOrDefault(y => y.GUID == x))
            .ToArray();

        var dto = new ApplyMoodlesByGuidDto(_pairRef.UserData, statusInfos.Select(x => x.GUID).ToArray(), MoodleType.Preset);
        _ = _mainHub.UserApplyMoodlesByGuid(dto);
    }
}
