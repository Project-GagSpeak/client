using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodlePresetToPairCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private int _maxPresetCount => MoodleCache.IpcData.PresetList.Max(x => x.Statuses.Count);
    private float _iconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public OwnMoodlePresetToPairCombo(ILogger log, MainHub hub, Kinkster pair, float scale)
        : base(log, hub, pair, scale, () => [ ..MoodleCache.IpcData.PresetList.OrderBy(x => x.Title) ])
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou);

    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawApplyPresets(string id, float width, string buttonTT)
    {
        InnerWidth = width + _iconWithPadding * _maxPresetCount;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Presets.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, true, buttonTT);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodlePreset = Items[globalIdx];
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var iconsSpace = (_iconWithPadding * moodlePreset.Statuses.Count);
        var titleSpace = size.X - iconsSpace;
        var ret = ImGui.Selectable($"##{moodlePreset.Title}", selected, ImGuiSelectableFlags.None, size);

        if (moodlePreset.Statuses.Count > 0)
        {
            ImGui.SameLine(titleSpace);
            for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
            {
                var status = moodlePreset.Statuses[i];
                if (!MoodleCache.IpcData.Statuses.TryGetValue(status, out var info))
                {
                    ImGui.SameLine(0, _iconWithPadding);
                    continue;
                }

                MoodleDisplay.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
                DrawItemTooltip(info);

                if (++iconsDrawn < moodlePreset.Statuses.Count)
                    ImUtf8.SameLineInner();
            }
        }

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var adjust = (size.Y - SelectableTextHeight) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + adjust);
        using (UiFontService.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodlePreset.Title);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - adjust);
        return ret;
    }

    protected override bool CanDoAction(MoodlePresetInfo item)
    {
        var statuses = new List<MoodlesStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
            if (MoodleCache.IpcData.Statuses.TryGetValue(guid, out var s))
                statuses.Add(s);
        // Check application.
        return PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, statuses);
    }

    protected override void OnApplyButton(MoodlePresetInfo item)
    {
        if (!CanDoAction(item))
            return;

        UiService.SetUITask(async () =>
        {
            var statuses = new List<MoodlesStatusInfo>();
            foreach (var guid in item.Statuses)
                if (MoodleCache.IpcData.Statuses.TryGetValue(guid, out var s))
                    statuses.Add(s);

            var dto = new MoodlesApplierByStatus(_kinksterRef.UserData, statuses, MoodleType.Preset);
            var res = await _mainHub.UserApplyMoodlesByStatus(dto);
            if (res.ErrorCode is GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
