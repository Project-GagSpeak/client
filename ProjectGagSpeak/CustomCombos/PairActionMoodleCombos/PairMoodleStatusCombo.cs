using CkCommons.Helpers;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairMoodleStatusCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    public PairMoodleStatusCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [ .. kinkster.LastIpcData.Statuses.Values.OrderBy(x => x.Title)])
    { }

    public PairMoodleStatusCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale, Func<IReadOnlyList<MoodlesStatusInfo>> generator)
        : base(log, hub, kinkster, scale, generator)
    { }

    protected override bool DisableCondition()
        => _kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) is false;

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
            var title = moodleStatus.StatusOnDispell== Guid.Empty
                ? "Unknown"
                : Items.FirstOrDefault(x => x.GUID == moodleStatus.StatusOnDispell).Title ?? "Unknown";
            DrawItemTooltip(moodleStatus, title);
        }

        return ret;
    }

    protected override bool CanDoAction(MoodlesStatusInfo item)
        => PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, [ item ]);

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

    protected override async Task<bool> OnRemoveButton(MoodlesStatusInfo item)
    {
        var dto = new MoodlesRemoval(_kinksterRef.UserData, [item.GUID]);
        var res = await _mainHub.UserRemoveMoodles(dto);
        if (res.ErrorCode is GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Removing moodle status {item.Title} from {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to remove moodle status {item.Title} from {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
            return false;

        }
    }
}
