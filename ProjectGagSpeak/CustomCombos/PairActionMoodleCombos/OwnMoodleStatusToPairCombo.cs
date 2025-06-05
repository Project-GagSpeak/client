using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Visual;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using ProjectGagSpeak.Utils.Enums;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodleStatusToPairCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    public OwnMoodleStatusToPairCombo(float scale, IconDisplayer disp, Pair pair, MainHub hub, ILogger log)
        : base(scale, disp, pair, hub, log, () => [ ..VisualApplierMoodles.LatestIpcData.MoodlesStatuses.OrderBy(x => x.Title)])
    { }

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
        => IconDisplayer.CanApplyPairStatus(_pairRef.PairPerms, new[] { item });

    protected override async Task<bool> OnApplyButton(MoodlesStatusInfo item)
    {
        var dto = new MoodlesApplierById(_pairRef.UserData, new[] { item.GUID }, MoodleType.Status);
        HubResponse res = await _mainHub.UserApplyMoodlesByGuid(dto);
        if (res.ErrorCode is GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Applying moodle status {item.Title} on {_pairRef.GetNickAliasOrUid()}", LoggerType.Permissions);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to apply moodle status {item.Title} on {_pairRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.Permissions);
            return false;
        }
    }
}
