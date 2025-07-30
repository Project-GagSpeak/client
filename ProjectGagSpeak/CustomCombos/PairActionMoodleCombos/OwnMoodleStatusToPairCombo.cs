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
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodleStatusToPairCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    private Action PostButtonPress;
    public OwnMoodleStatusToPairCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale, Action postButtonPress)
        : base(log, hub, kinkster, scale, () => [.. MoodleCache.IpcData.Statuses.Values.OrderBy(x => x.Title)])
    {
        PostButtonPress = postButtonPress;
    }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou);

    protected override string ToString(MoodlesStatusInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawApplyStatuses(string id, float width, string buttonTT, Action? onButtonSuccess = null)
    {
        InnerWidth = width + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Status.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, true, buttonTT, onButtonSuccess);
    }

    public bool DrawRemoveStatuses(string id, float width, string buttonTT, Action? onButtonSuccess = null)
    {
        InnerWidth = width + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Status.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, false, buttonTT, onButtonSuccess);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var titleSpace = size.X - IconSize.X;
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable("##" + moodleStatus.Title, selected, ImGuiSelectableFlags.None, size);

        ImGui.SameLine(titleSpace);
        MoodleDisplay.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
        DrawItemTooltip(moodleStatus);

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var pos = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(pos + (size.Y - SelectableTextHeight) * 0.5f);
        using (UiFontService.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodleStatus.Title);
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
            PostButtonPress?.Invoke();
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to apply moodle status {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
            PostButtonPress?.Invoke();
            return false;
        }
    }
}
