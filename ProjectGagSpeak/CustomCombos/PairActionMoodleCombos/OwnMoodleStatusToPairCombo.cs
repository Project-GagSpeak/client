using CkCommons.Helpers;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodleStatusToPairCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    public OwnMoodleStatusToPairCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [ ..MoodleCache.IpcData.Statuses.Values.OrderBy(x => x.Title)])
    { }

    protected override bool DisableCondition()
        => _kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou) is false;

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable(moodleStatus.Title.StripColorTags(), selected);

        if (moodleStatus.IconID > 200000)
        {
            ImGui.SameLine();
            var offset = ImGui.GetContentRegionAvail().X - IconSize.X;
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
            MoodleDisplay.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
            // get the dispelable moodle if any.
            var status = moodleStatus.StatusOnDispell== Guid.Empty
                ? "Unknown"
                : Items.FirstOrDefault(x => x.GUID == moodleStatus.StatusOnDispell).Title ?? "Unknown";

            DrawItemTooltip(moodleStatus, status);
        }

        return ret;
    }
    protected override bool CanDoAction(MoodlesStatusInfo item)
        => PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, new[] { item });

    protected override async Task<bool> OnApplyButton(MoodlesStatusInfo item)
    {
        var dto = new MoodlesApplierById(_kinksterRef.UserData, [item.GUID], MoodleType.Status);
        var res = await _mainHub.UserApplyMoodlesByGuid(dto);
        if (res.ErrorCode is GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Applying moodle status {item.Title} on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to apply moodle status {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
            return false;
        }
    }
}
