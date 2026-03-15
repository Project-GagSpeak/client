using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using GagSpeak.Interop.Helpers;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Loci;

public sealed class OwnPresetCombo : LociComboBase<LociPresetInfo>
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
        var lociPreset = Items[globalIdx];
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var iconsSpace = (_iconWithPadding * lociPreset.Statuses.Count);
        var titleSpace = size.X - iconsSpace;

        // Push the font first so the height is correct.
        using var _ = Fonts.Default150Percent.Push();

        var ret = ImGui.Selectable($"##{lociPreset.Title}", selected, ImGuiSelectableFlags.None, size);

        if (lociPreset.Statuses.Count > 0)
        {
            ImGui.SameLine(titleSpace);
            for (int i = 0, iconsDrawn = 0; i < lociPreset.Statuses.Count; i++)
            {
                var status = lociPreset.Statuses[i];
                if (!LociCache.Data.Statuses.TryGetValue(status, out var info))
                {
                    ImGui.SameLine(0, _iconWithPadding);
                    continue;
                }

                LociIcon.Draw(info.IconID, info.Stacks, IconSize);
                LociHelpers.AttachTooltip(info, LociCache.Data);

                if (++iconsDrawn < lociPreset.Statuses.Count)
                    ImUtf8.SameLineInner();
            }
        }

        ImGui.SameLine(ImUtf8.ItemInnerSpacing.X);
        var adjust = (size.Y - ImUtf8.TextHeight) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + adjust);
        CkRichText.Text(titleSpace, lociPreset.Title);
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
        return LociHelpers.CanApply(_kinksterRef.PairPerms, statuses);
    }

    protected override void OnApplyButton(LociPresetInfo item)
    {
        if (!CanDoAction(item))
            return;

        UiService.SetUITask(async () =>
        {
            var statuses = new List<LociStatusStruct>();
            foreach (var guid in item.Statuses)
                if (LociCache.Data.Statuses.TryGetValue(guid, out var s))
                    statuses.Add(s.ToStruct());

            var res = await _mainHub.UserApplyLociStatusTuples(new(_kinksterRef.UserData, statuses, false));
            if (res.ErrorCode is GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply loci preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
