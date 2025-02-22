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

public sealed class OwnMoodleStatusToPairCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    private readonly CharaIPCData _ownMoodles;
    public OwnMoodleStatusToPairCombo(float iconScale, MoodleStatusMonitor monitor, CharaIPCData data,
        Pair pair, MainHub hub, ILogger log, UiSharedService ui, string bText, string bTT)
        : base(iconScale, monitor, pair, hub, log, ui, bText, bTT, () =>
        [
            ..data.MoodlesStatuses.OrderBy(x => x.Title),
        ])
    {
        _ownMoodles = data;
    }

    protected override bool DisableCondition()
        => _pairRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou) is false;

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
            // get the dispelable moodle if any.
            var status = moodleStatus.StatusOnDispell.IsEmptyGuid()
                ? "Unknown"
                : Items.FirstOrDefault(x => x.GUID == moodleStatus.StatusOnDispell).Title ?? "Unknown";

            DrawItemTooltip(moodleStatus, status);
        }

        return ret;
    }
    protected override bool CanDoAction(MoodlesStatusInfo item)
        => _statuses.CanApplyPairStatus(_pairRef.PairPerms, new[] { item });

    protected override void DoAction(MoodlesStatusInfo item)
    {
        var dto = new ApplyMoodlesByGuidDto(_pairRef.UserData, new[] { item.GUID }, MoodleType.Status);
        _ = _mainHub.UserApplyMoodlesByGuid(dto);
    }
}
