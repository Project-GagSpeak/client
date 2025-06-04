using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairMoodleStatusCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    public PairMoodleStatusCombo(float iconScale, IconDisplayer monitor, Pair pair, MainHub hub, ILogger log)
        : base(iconScale, monitor, pair, hub, log, () => [ ..pair.LastIpcData.MoodlesStatuses.OrderBy(x => x.Title)])
    { }

    public PairMoodleStatusCombo(float iconScale, IconDisplayer monitor, Pair pair, MainHub hub, ILogger log,
        Func<IReadOnlyList<MoodlesStatusInfo>> generator) : base(iconScale, monitor, pair, hub, log, generator)
    { }

    protected override bool DisableCondition()
        => _pairRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) is false;

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
            var title = moodleStatus.StatusOnDispell.IsEmptyGuid()
                ? "Unknown"
                : Items.FirstOrDefault(x => x.GUID == moodleStatus.StatusOnDispell).Title ?? "Unknown";
            DrawItemTooltip(moodleStatus, title);
        }

        return ret;
    }

    protected override bool CanDoAction(MoodlesStatusInfo item)
        => IconDisplayer.CanApplyPairStatus(_pairRef.PairPerms, [ item ]);

    protected override async Task<bool> OnApplyButton(MoodlesStatusInfo item)
    {
        var dto = new MoodlesApplierById(_pairRef.UserData, [item.GUID], MoodleType.Status);
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

    protected override async Task<bool> OnRemoveButton(MoodlesStatusInfo item)
    {
        var dto = new MoodlesRemoval(_pairRef.UserData, [item.GUID]);
        HubResponse res = await _mainHub.UserRemoveMoodles(dto);
        if (res.ErrorCode is GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Removing moodle status {item.Title} from {_pairRef.GetNickAliasOrUid()}", LoggerType.Permissions);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to remove moodle status {item.Title} from {_pairRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.Permissions);
            return false;

        }
    }
}
