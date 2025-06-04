using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Gui.Permissions;

public partial class PairStickyUI
{
    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, bool curState, bool editAccess)
    {
        DrawPermRowClientCommon(width, perm, inHardcore, curState, editAccess, () => !curState);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, PuppetPerms curState, PuppetPerms editAccess, PuppetPerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowClientCommon(width, perm, inHardcore, isFlagSet, editAccess.HasAny(editFlag), () => curState ^ editFlag);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, MoodlePerms curState, MoodlePerms editAccess, MoodlePerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowClientCommon(width, perm, inHardcore, isFlagSet, editAccess.HasAny(editFlag), () => curState ^ editFlag);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, TimeSpan curState, bool editAccess)
    {
        using var disabled = ImRaii.Disabled(inHardcore);

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : curState.ToGsRemainingTime();
        var data = ClientPermData[perm];

        if (CkGui.IconInputText("##" + perm, data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !editAccess))
        {
            if (str != curState.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();

                // Assign the blocking task if allowed.
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.UniquePairPerm)
                    UiTask = PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, res.name, ticks);
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Max Duration {SPair.GetNickAliasOrUid()} can Lock for.");

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiTask = PermissionHelper.ChangeOwnAccess(_hub, SPair.UserData, SPair.OwnPermAccess, perm.ToPermAccessValue(), refVar);
        CkGui.AttachToolTip(editAccess
            ? $"Grant {SPair.GetNickAliasOrUid()} control over this permission, allowing them to change the permission at any time"
            : $"Revoke {SPair.GetNickAliasOrUid()} control over this permission, preventing them from changing the permission at any time");
    }

    private void DrawPermRowClientCommon<T>(float width, SPPID perm, bool inHardcore, bool curState, bool editAccess, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, editAccess ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        using var disabled = ImRaii.Disabled(inHardcore);

        var data = ClientPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(curState ? data.IconOn : data.IconOff);
            CkGui.TextFrameAlignedInline(data.Text);
            ImGui.SameLine();
            CkGui.ColorTextBool(curState ? data.CondTrue : data.CondFalse, curState);
            CkGui.TextFrameAlignedInline(".");
        }
        CkGui.AttachToolTip(data.Tooltip(curState));

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiTask = res.type switch
            {
                PermissionType.UniquePairPermEditAccess
                    => PermissionHelper.ChangeOwnAccess(_hub, SPair.UserData, SPair.OwnPermAccess, res.name, newState),
                PermissionType.UniquePairPerm
                    => PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, res.name, newState),
                _ => Task.Run(async () =>
                {
                    if (_globals.GlobalPerms is not { } perms) return;
                    await PermissionHelper.ChangeOwnGlobal(_hub, perms, res.name, newState);
                })
            };
        }

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiTask = PermissionHelper.ChangeOwnAccess(_hub, SPair.UserData, SPair.OwnPermAccess, perm.ToPermAccessValue(), refVar);
        CkGui.AttachToolTip(editAccess
            ? $"Grant {SPair.GetNickAliasOrUid()} control over this permission, allowing them to change the permission at any time"
            : $"Revoke {SPair.GetNickAliasOrUid()} control over this permission, preventing them from changing the permission at any time");
    }

    public void DrawHardcorePermRowClient(float width, SPPID perm, bool inHardcore, string curState, bool permAllowed)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, inHardcore ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = ClientPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var isActive = !curState.IsNullOrWhitespace();

        using (ImRaii.Disabled(inHardcore))
        {
            // Be better corby... after all your UI work, you can make a better design for this...
            var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
            ImGui.SetCursorScreenPos(pos);
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(isActive ? data.IconOn : data.IconOff);
                CkGui.TextFrameAlignedInline(data.Text);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip(data.Tooltip(isActive));

            if (button)
            {
                var res = perm.ToPermValue();
                if (res.name.IsNullOrEmpty())
                    return;

                UiTask = res.type switch
                {
                    PermissionType.UniquePairPermEditAccess => PermissionHelper.ChangeOwnAccess(_hub, SPair.UserData, SPair.OwnPermAccess, res.name, !permAllowed),
                    PermissionType.UniquePairPerm => PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, res.name, !permAllowed),
                    _ => Task.Run(async () => {
                        if (_globals.GlobalPerms is not { } perms) return;
                        await PermissionHelper.ChangeOwnGlobal(_hub, perms, res.name, !permAllowed);
                    })
                };
            }
        }

        ImGui.SameLine(buttonW);
        var refVar = permAllowed;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiTask = PermissionHelper.ChangeOwnAccess(_hub, SPair.UserData, SPair.OwnPermAccess, perm.ToPermAccessValue(), refVar);
        CkGui.AttachToolTip(permAllowed
            ? $"Grant {SPair.GetNickAliasOrUid()} control over this permission, allowing them to change the permission at any time"
            : $"Revoke {SPair.GetNickAliasOrUid()} control over this permission, preventing them from changing the permission at any time");
    }

    public void DrawHardcorePermRowClient(float width, bool inHardcore, string curState, bool editAccess, bool editAccess2)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, inHardcore ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = ClientPermData[SPPID.ForcedEmoteState];

        var isActive = !curState.IsNullOrWhitespace();

        var buttonW = width - (2 * ImGui.GetFrameHeightWithSpacing());
        using (ImRaii.Disabled(inHardcore))
        {
            using (ImRaii.Group())
            {
                CkGui.FramedIconText(isActive ? data.IconOn : data.IconOff);
                CkGui.TextFrameAlignedInline(data.TextPrefix);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                CkGui.TextFrameAlignedInline(".");
            }
            CkGui.AttachToolTip(data.Tooltip(isActive));
        }

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit", ref refVar))
        {
            var propertyName = SPPID.ForcedEmoteState.ToPermAccessValue();
            UiTask = PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, propertyName, refVar);
        }
        CkGui.AttachToolTip($"Limit {SPair.GetNickAliasOrUid()} to only force GroundSit, Sit, and CyclePose.");

        ImUtf8.SameLineInner();
        var refVar2 = editAccess2;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit2", ref refVar2))
        {
            var propertyName = SPPID.ForcedEmoteState.ToPermAccessValue(true);
            UiTask = PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, propertyName, refVar);
        }
        CkGui.AttachToolTip($"Allow {SPair.GetNickAliasOrUid()} to force you into any looped Emote.");
    }

    /// <summary> This function is messy because it is independant of everything else due to a bad conflict between pishock HTML and gagspeak signalR. </summary>
    public void DrawPiShockPairPerms(float width, PairPerms pairPerms, PairPermAccess pairAccess)
    {
        // First row must be drawn.
        using (ImRaii.Group())
        {
            var length = width - CkGui.IconTextButtonSize(FAI.Sync, "Refresh") + ImGui.GetFrameHeight();
            var refCode = pairPerms.PiShockShareCode;

            CkGui.IconInputText($"Code {SPair.GetNickAliasOrUid()}", FAI.ShareAlt, string.Empty, "Unique Share Code", ref refCode, 40, width, true, false);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                UiTask = PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, SPPID.PiShockShareCode.ToPermValue().name, refCode);
            }
            CkGui.AttachToolTip($"Unique Share Code for {SPair.GetNickAliasOrUid()}." +
                "--SEP--This should be a separate Share Code from your Global Share Code." +
                $"--SEP--A Unique Share Code can have permissions elevated higher than the Global Share Code that only {SPair.GetNickAliasOrUid()} can use.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FAI.Sync, "Refresh", disabled: DateTime.Now - _lastRefresh < TimeSpan.FromSeconds(15) || refCode.IsNullOrWhitespace()))
            {
                _lastRefresh = DateTime.Now;
                UiTask = Task.Run(async () =>
                {
                    var newPerms = await _shockies.GetPermissionsFromCode(pairPerms.PiShockShareCode);
                    pairPerms.AllowShocks = newPerms.AllowShocks;
                    pairPerms.AllowVibrations = newPerms.AllowVibrations;
                    pairPerms.AllowBeeps = newPerms.AllowBeeps;
                    pairPerms.MaxDuration = newPerms.MaxDuration;
                    pairPerms.MaxIntensity = newPerms.MaxIntensity;
                    await _hub.UserBulkChangeUnique(new(SPair.UserData, pairPerms, pairAccess));
                });
            }
        }

        // special case for this.
        var seconds = (float)pairPerms.MaxVibrateDuration.TotalMilliseconds / 1000;
        using (ImRaii.Group())
        {
            if (CkGui.IconSliderFloat("##maxVibeTime" + SPair.UserData.UID, FAI.Stopwatch, "Max Vibe Duration",
                ref seconds, 0.1f, 15f, width * .65f, true, pairPerms.HasValidShareCode()))
            {
                pairPerms.MaxVibrateDuration = TimeSpan.FromSeconds(seconds);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                var timespanValue = TimeSpan.FromSeconds(seconds);
                var ticks = (ulong)timespanValue.Ticks;
                UiTask = PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.OwnPerms, SPPID.MaxVibrateDuration.ToPermValue().name, ticks);
            }
            CkGui.AttachToolTip("Max duration you allow this pair to vibrate your Shock Collar for");
        }
    }
}
