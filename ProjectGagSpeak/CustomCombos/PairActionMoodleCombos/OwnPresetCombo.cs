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
using Dalamud.Bindings.ImGui;
using OtterGui.Text;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnPresetCombo : MoodleComboBase<LociPresetInfo>
{
    private int _maxPresetCount => LociCache.Data.PresetList.Max(x => x.Statuses.Count);
    private float _iconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
    public OwnPresetCombo(ILogger log, MainHub hub, Kinkster pair, float scale)
        : base(log, hub, pair, scale, () => [ ..LociCache.Data.PresetList.OrderBy(x => x.Title) ])
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.LociAccess.HasAny(LociAccess.AllowOther);

    protected override string ToString(LociPresetInfo obj)
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

        // Push the font first so the height is correct.
        using var _ = Fonts.Default150Percent.Push();

        var ret = ImGui.Selectable($"##{moodlePreset.Title}", selected, ImGuiSelectableFlags.None, size);

        if (moodlePreset.Statuses.Count > 0)
        {
            ImGui.SameLine(titleSpace);
            for (int i = 0, iconsDrawn = 0; i < moodlePreset.Statuses.Count; i++)
            {
                var status = moodlePreset.Statuses[i];
                if (!LociCache.Data.Statuses.TryGetValue(status, out var info))
                {
                    ImGui.SameLine(0, _iconWithPadding);
                    continue;
                }

                LociIcon.Draw(info.IconID, info.Stacks, IconSize);
                LociEx.AttachTooltip(info, LociCache.Data);

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
        var statuses = new List<LociStatusInfo>(item.Statuses.Count);
        foreach (var guid in item.Statuses)
            if (LociCache.Data.Statuses.TryGetValue(guid, out var s))
                statuses.Add(s);
        // Check application.
        return PermHelper.CanApplyPairStatus(_kinksterRef.PairPerms, statuses);
    }

    protected override void OnApplyButton(LociPresetInfo item)
    {
        if (!CanDoAction(item))
            return;

        UiService.SetUITask(async () =>
        {
            var statuses = new List<LociStatusInfo>();
            foreach (var guid in item.Statuses)
                if (LociCache.Data.Statuses.TryGetValue(guid, out var s))
                    statuses.Add(s);

            var res = await _mainHub.UserApplyLociStatusTuples(new(_kinksterRef.UserData, statuses, false));
            if (res.ErrorCode is GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
