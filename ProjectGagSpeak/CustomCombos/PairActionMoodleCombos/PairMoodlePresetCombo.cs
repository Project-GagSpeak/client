using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairMoodlePresetCombo : CkMoodleComboButtonBase<MoodlePresetInfo>
{
    private Action PostButtonPress;
    private int _maxPresetCount => _kinksterRef.LastIpcData.Presets.Values.Max(x => x.Statuses.Count);
    private float _iconWithPadding => IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;

    public PairMoodlePresetCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale, Action postButtonPress)
        : base(log, hub, kinkster, scale, () => [.. kinkster.LastIpcData.Presets.Values.OrderBy(x => x.Title)])
    {
        PostButtonPress = postButtonPress;
    }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou);
    protected override string ToString(MoodlePresetInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawApplyPresets(string id, float width, string buttonTT, Action? onButtonSuccess = null)
    {
        InnerWidth = width + _iconWithPadding * _maxPresetCount;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Preset.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, true, buttonTT, onButtonSuccess);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var moodlePreset = Items[globalIdx];
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var iconsSpace = (_iconWithPadding * moodlePreset.Statuses.Count);
        var titleSpace = size.X - iconsSpace;
        var ret = ImGui.Selectable($"##{moodlePreset.Title}", selected, ImGuiSelectableFlags.None, size);

        if (moodlePreset.Statuses.Count <= 0)
            return ret;

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

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var pos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(pos + (size.Y - SelectableTextHeight) * 0.5f);
        using (UiFontService.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodlePreset.Title);
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
            PostButtonPress?.Invoke();
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to apply moodle preset {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return false;
        }
    }

    protected override async Task<bool> OnRemoveButton(MoodlePresetInfo item)
        => await Task.FromResult(false);
}
