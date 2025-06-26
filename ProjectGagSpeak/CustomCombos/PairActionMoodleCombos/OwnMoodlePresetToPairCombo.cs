using GagSpeak.CkCommons.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodlePresetToPairCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private int _maxPresetCount => _kinksterRef.LastIpcData.Presets.Values.Max(x => x.Statuses.Count);
    public OwnMoodlePresetToPairCombo(ILogger log, MainHub hub, Kinkster pair, float scale)
        : base(log, hub, pair, scale, () => [ ..MoodleCache.IpcData.Presets.Values.OrderBy(x => x.Title) ])
    { }

    protected override bool DisableCondition()
        => _kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou) is false;

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
            if (!MoodleCache.IpcData.Statuses.TryGetValue(status, out var info))
            {
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + IconSize.X);
                continue;
            }

            MoodleIcons.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
            // get the dispelable moodle if any.
            var title = MoodleCache.IpcData.Statuses.GetValueOrDefault(info.StatusOnDispell).Title ?? "Unknown";
            DrawItemTooltip(info, title);

            if (++iconsDrawn < moodlePreset.Statuses.Count)
                ImGui.SameLine();
        }

        return ret;
    }

    protected override bool CanDoAction(MoodlePresetInfo item)
    {
        var statuses = new List<MoodlesStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
        {
            if (MoodleCache.IpcData.Statuses.TryGetValue(guid, out var s))
                statuses.Add(s);
        }
        // Check application.
        return PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, statuses);
    }

    protected override async Task<bool> OnApplyButton(MoodlePresetInfo item)
    {
        var dto = new MoodlesApplierById(_kinksterRef.UserData, item.Statuses, MoodleType.Preset);
        var res = await _mainHub.UserApplyMoodlesByGuid(dto);
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
}
