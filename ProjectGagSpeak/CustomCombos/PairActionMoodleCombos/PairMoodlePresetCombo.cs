using GagSpeak.CkCommons.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairMoodlePresetCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private int MaxPresetCount => _kinksterRef.LastIpcData.Presets.Values.Max(x => x.Statuses.Count);
    public PairMoodlePresetCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [ ..kinkster.LastIpcData.Presets.Values.OrderBy(x => x.Title)])
    { }

    protected override bool DisableCondition()
        => _kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou) is false;

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
            if (!_kinksterRef.LastIpcData.Statuses.TryGetValue(status, out var info))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + IconSize.X);
                continue;
            }

            MoodleIcons.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            // get the dispelable moodle if any.
            var title = _kinksterRef.LastIpcData.Statuses.GetValueOrDefault(info.StatusOnDispell).Title ?? "Unknown";
            DrawItemTooltip(info, title);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImGui.SameLine();
        }

        return ret;
    }

    protected override bool CanDoAction(MoodlePresetInfo item)
    {
        var statusesToCheck = new List<MoodlesStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
        {
            if (_kinksterRef.LastIpcData.Statuses.TryGetValue(guid, out var info))
                statusesToCheck.Add(info);
        }

        return PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, statusesToCheck);
    }

    protected override async Task<bool> OnApplyButton(MoodlePresetInfo item)
    {
        var dto = new MoodlesApplierById(_kinksterRef.UserData, item.Statuses, MoodleType.Preset);
        HubResponse res = await _mainHub.UserApplyMoodlesByGuid(dto);
        if (res.ErrorCode is GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Applying moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}", LoggerType.StickyUI);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
            return false;
        }
    }

    protected override async Task<bool> OnRemoveButton(MoodlePresetInfo item)
        => await Task.FromResult(false);
}
