using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairMoodlePresetCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private int _maxPresetCount => _kinksterRef.MoodleData.Presets.Count > 0 ? _kinksterRef.MoodleData.PresetList.Max(x => x.Statuses.Count) : 0;
    private float _iconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;

    public PairMoodlePresetCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [.. kinkster.MoodleData.PresetList.OrderBy(x => x.Title)])
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodleAccess.HasAny(MoodleAccess.AllowOwn);
    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawApplyPresets(string id, float width, string buttonTT)
    {
        InnerWidth = width + _iconWithPadding * _maxPresetCount;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Preset.." : Current.Title.StripColorTags();
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
                if (!_kinksterRef.MoodleData.Statuses.TryGetValue(status, out var info))
                {
                    ImGui.SameLine(0, _iconWithPadding);
                    continue;
                }

                MoodleIcon.DrawMoodleIcon(info.IconID, info.Stacks, IconSize);
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
        var statusesToCheck = new List<MoodlesStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
        {
            if (_kinksterRef.MoodleData.Statuses.TryGetValue(guid, out var info))
                statusesToCheck.Add(info);
        }

        return PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, statusesToCheck);
    }

    protected override void OnApplyButton(MoodlePresetInfo item)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _mainHub.UserApplyMoodlesByGuid(new(_kinksterRef.UserData, item.Statuses, true, false));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
