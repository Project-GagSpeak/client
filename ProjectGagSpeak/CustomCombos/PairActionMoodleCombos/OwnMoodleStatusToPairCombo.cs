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
using Dalamud.Bindings.ImGui;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class OwnMoodleStatusToPairCombo : CkMoodleComboButtonBase<MoodlesStatusInfo>
{
    public OwnMoodleStatusToPairCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [.. MoodleCache.IpcData.Statuses.Values.OrderBy(x => x.Title)])
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou);

    protected override string ToString(MoodlesStatusInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawApplyStatuses(string id, float width, string buttonTT)
    {
        InnerWidth = width + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Status.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, true, buttonTT);
    }

    public bool DrawRemoveStatuses(string id, float width, string buttonTT)
    {
        InnerWidth = width + IconSize.X + ImGui.GetStyle().ItemInnerSpacing.X;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Status.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, false, buttonTT);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var titleSpace = size.X - IconSize.X;
        var moodleStatus = Items[globalIdx];
        var ret = ImGui.Selectable("##" + moodleStatus.Title, selected, ImGuiSelectableFlags.None, size);

        ImGui.SameLine(titleSpace);
        MoodleIcon.DrawMoodleIcon(moodleStatus.IconID, moodleStatus.Stacks, IconSize);
        DrawItemTooltip(moodleStatus);

        ImGui.SameLine(ImGui.GetStyle().ItemInnerSpacing.X);
        var adjust = (size.Y - SelectableTextHeight) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + adjust);
        using (UiFontService.Default150Percent.Push())
            CkRichText.Text(titleSpace, moodleStatus.Title);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - adjust);

        return ret;
    }

    protected override bool CanDoAction(MoodlesStatusInfo item)
        => PermissionHelper.CanApplyPairStatus(_kinksterRef.PairPerms, [ item ]);

    protected override void OnApplyButton(MoodlesStatusInfo item)
    {
        UiService.SetUITask(async () =>
        {
            var dto = new MoodlesApplierByStatus(_kinksterRef.UserData, [item], MoodleType.Status);
            var res = await _mainHub.UserApplyMoodlesByStatus(dto);
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle status {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
