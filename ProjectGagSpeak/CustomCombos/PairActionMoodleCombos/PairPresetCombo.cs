using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairPresetCombo : MoodleComboBase<LociPresetInfo>
{
    private int _maxPresetCount => _kinksterRef.LociData.Presets.Count > 0 ? _kinksterRef.LociData.PresetList.Max(x => x.Statuses.Count) : 0;
    private float _iconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;

    public PairPresetCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [.. kinkster.LociData.PresetList.OrderBy(x => x.Title)])
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.LociAccess.HasAny(LociAccess.AllowOwn);
    protected override string ToString(LociPresetInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawPresets(string id, float width, string buttonTT)
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

        // Push the font first so the height is correct.
        using var _ = Fonts.Default150Percent.Push();

        if (moodlePreset.Statuses.Count > 0)
        {
            ImGui.SameLine(titleSpace);
            for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
            {
                var status = moodlePreset.Statuses[i];
                if (!_kinksterRef.LociData.Statuses.TryGetValue(status, out var info))
                {
                    ImGui.SameLine(0, _iconWithPadding);
                    continue;
                }

                LociIcon.Draw(info.IconID, info.Stacks, IconSize);
                LociEx.AttachTooltip(info, _kinksterRef.LociData);

                if (++iconsDrawn < moodlePreset.Statuses.Count)
                    ImUtf8.SameLineInner();
            }
        }

        ImGui.SameLine(ImUtf8.ItemInnerSpacing.X);
        var adjust = (size.Y - ImUtf8.TextHeight) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + adjust);
        CkRichText.Text(titleSpace, moodlePreset.Title);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - adjust);
        return ret;
    }

    protected override bool CanDoAction(LociPresetInfo item)
    {
        var toCheck = new List<LociStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
            if (_kinksterRef.LociData.Statuses.TryGetValue(guid, out var info))
                toCheck.Add(info);

        return LociEx.CanApply(_kinksterRef.PairPerms, toCheck);
    }

    protected override void OnApplyButton(LociPresetInfo item)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _mainHub.UserApplyLociData(new(_kinksterRef.UserData, item.Statuses, true, false));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
