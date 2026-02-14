using CkCommons.Helpers;
using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Bindings.ImGui;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using OtterGui.Text;

namespace GagSpeak.CustomCombos.Moodles;

public sealed class PairStatusCombo : MoodleComboBase<MoodlesStatusInfo>
{
    public PairStatusCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale)
        : base(log, hub, kinkster, scale, () => [ .. kinkster.MoodleData.Statuses.Values.OrderBy(x => x.Title)])
    { }

    public PairStatusCombo(ILogger log, MainHub hub, Kinkster kinkster, float scale, Func<IReadOnlyList<MoodlesStatusInfo>> generator)
        : base(log, hub, kinkster, scale, generator)
    { }

    protected override bool DisableCondition()
        => Current.GUID == Guid.Empty || !_kinksterRef.PairPerms.MoodleAccess.HasAny(MoodleAccess.AllowOwn);

    protected override string ToString(MoodlesStatusInfo obj)
        => obj.Title.StripColorTags();

    public bool DrawStatuses(string id, float width, bool isApplying, string buttonTT)
    {
        InnerWidth = width + IconSize.X + ImUtf8.ItemInnerSpacing.X;
        var prevLabel = Current.GUID == Guid.Empty ? "Select Status.." : Current.Title.StripColorTags();
        return DrawComboButton(id, prevLabel, width, isApplying, buttonTT);
    }

    protected override bool DrawSelectable(int globalIdx, bool selected)
    {
        var size = new Vector2(GetFilterWidth(), IconSize.Y);
        var titleSpace = size.X - IconSize.X;
        var myStatus = Items[globalIdx];

        // Push the font first so the height is correct.
        using var _ = UiFontService.Default150Percent.Push();

        var ret = ImGui.Selectable("##" + myStatus.Title, selected, ImGuiSelectableFlags.None, size);

        ImGui.SameLine(titleSpace);
        MoodleIcon.DrawMoodleIcon(myStatus.IconID, myStatus.Stacks, IconSize);
        myStatus.AttachTooltip(_kinksterRef.MoodleData.StatusList);

        ImGui.SameLine(ImUtf8.ItemInnerSpacing.X);
        var adjust = (size.Y - ImUtf8.TextHeight) * 0.5f;
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + adjust);
        CkRichText.Text(titleSpace, myStatus.Title);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - adjust);
        return ret;
    }

    protected override bool CanDoAction(MoodlesStatusInfo item)
        => PermHelper.CanApplyPairStatus(_kinksterRef.PairPerms, [ item ]);

    protected override void OnApplyButton(MoodlesStatusInfo item)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _mainHub.UserApplyMoodlesByGuid(new(_kinksterRef.UserData, [item.GUID], false, false));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to apply moodle status {item.Title} on {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }

    protected override void OnRemoveButton(MoodlesStatusInfo item)
    {
        UiService.SetUITask(async () =>
        {
            var res = await _mainHub.UserRemoveMoodles(new(_kinksterRef.UserData, [item.GUID]));
            if (res.ErrorCode is not GagSpeakApiEc.Success)
                Log.LogDebug($"Failed to remove moodle status {item.Title} from {_kinksterRef.GetNickAliasOrUid()}: [{res.ErrorCode}]", LoggerType.StickyUI);
        });
    }
}
