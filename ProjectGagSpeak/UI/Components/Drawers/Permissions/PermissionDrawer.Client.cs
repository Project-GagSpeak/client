using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Components;

public partial class PermissionsDrawer
{
    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, bool curState, bool editAccess)
    {
        DrawPermRowClientCommon(width, perm, inHardcore, curState, editAccess, () => !curState);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, PuppetPerms curState, bool editAccess, PuppetPerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowClientCommon(width, perm, inHardcore, isFlagSet, editAccess, () => curState ^ editFlag);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, MoodlePerms curState, bool editAccess, MoodlePerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowClientCommon(width, perm, inHardcore, isFlagSet, editAccess, () => curState ^ editFlag);
    }

    public void DrawPermRowClient(float width, SPPID perm, bool inHardcore, TimeSpan curState, bool editAccess)
    {
        using var disabled = ImRaii.Disabled(inHardcore);

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : curState.ToGsRemainingTime();
        var data = _pad.ClientPermData[perm];

        if (CkGui.IconInputText("##" + perm, data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !editAccess))
        {
            if (str != curState.ToGsRemainingTime() && GsPadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var ticks = (ulong)newTime.Ticks;
                var res = perm.ToPermValue();
                if (!res.name.IsNullOrEmpty() && res.type is PermissionType.UniquePairPerm)
                    UiBlockingTask = AssignBlockingTask(res.name, ticks, PermissionType.UniquePairPerm, UpdateDir.Own);
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip("The Max Duration " + PermissionData.DispName + "Can Lock for.");

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiBlockingTask = AssignBlockingTask(perm.ToPermAccessValue(), refVar, PermissionType.UniquePairPermEditAccess, UpdateDir.Own);
        CkGui.AttachToolTip(editAccess ? PermissionData.ClientAccessYesTT : PermissionData.ClientAccessNoTT);
    }

    private void DrawPermRowClientCommon<T>(float width, SPPID perm, bool inHardcore, bool curState, bool editAccess, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, editAccess ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        using var disabled = ImRaii.Disabled(inHardcore);

        var data = _pad.ClientPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            CkGui.IconText(curState ? data.IconOn : data.IconOff);
            ImUtf8.SameLineInner();
            ImGui.Text(data.TextPrefix);
            ImGui.SameLine();
            CkGui.ColorTextBool(curState ? data.CondTrue : data.CondFalse, curState);
            ImGui.SameLine();
            ImGui.Text(data.TextSuffix);
        }
        CkGui.AttachToolTip(data.Tooltip(curState));

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiBlockingTask = AssignBlockingTask(res.name, newState, res.type, UpdateDir.Own);
        }

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiBlockingTask = AssignBlockingTask(perm.ToPermAccessValue(), refVar, PermissionType.UniquePairPermEditAccess, UpdateDir.Own);
        CkGui.AttachToolTip(editAccess ? PermissionData.ClientAccessYesTT : PermissionData.ClientAccessNoTT);
    }

    public void DrawHardcorePermRowClient(float width, SPPID perm, bool inHardcore, string curState, bool permAllowed)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, inHardcore ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = _pad.ClientPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        bool isActive = !curState.IsNullOrWhitespace();

        using (ImRaii.Disabled(inHardcore))
        {
            var button = ImGui.Button("##client" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
            ImGui.SetCursorScreenPos(pos);
            using (ImRaii.Group())
            {
                CkGui.IconText(isActive ? data.IconOn : data.IconOff);
                ImUtf8.SameLineInner();
                ImGui.Text(data.TextPrefix);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                ImGui.SameLine();
                ImGui.Text(data.TextSuffix);
            }
            CkGui.AttachToolTip(data.Tooltip(isActive));

            if (button)
            {
                var res = perm.ToPermValue();
                if (res.name.IsNullOrEmpty())
                    return;

                UiBlockingTask = AssignBlockingTask(res.name, !permAllowed, res.type, UpdateDir.Own);
            }
        }

        ImGui.SameLine(buttonW);
        var refVar = permAllowed;
        if (ImGui.Checkbox("##" + perm + "edit", ref refVar))
            UiBlockingTask = AssignBlockingTask(perm.ToPermAccessValue(), refVar, PermissionType.UniquePairPermEditAccess, UpdateDir.Own);
        CkGui.AttachToolTip(permAllowed ? PermissionData.ClientAccessYesTT : PermissionData.ClientAccessNoTT);
    }

    public void DrawHardcorePermRowClient(float width, bool inHardcore, string curState, bool editAccess, bool editAccess2)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, inHardcore ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = _pad.ClientPermData[SPPID.ForcedEmoteState];

        bool isActive = !curState.IsNullOrWhitespace();

        var buttonW = width - (2 * ImGui.GetFrameHeightWithSpacing());
        using (ImRaii.Disabled(inHardcore))
        {
            using (ImRaii.Group())
            {
                CkGui.IconText(isActive ? data.IconOn : data.IconOff);
                ImUtf8.SameLineInner();
                ImGui.Text(data.TextPrefix);
                ImGui.SameLine();
                CkGui.ColorTextBool(isActive ? data.CondTrue : data.CondFalse, isActive);
                ImGui.SameLine();
                ImGui.Text(data.TextSuffix);
            }
            CkGui.AttachToolTip(data.Tooltip(isActive));
        }

        ImGui.SameLine(buttonW);
        var refVar = editAccess;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit", ref refVar))
            UiBlockingTask = AssignBlockingTask(SPPID.ForcedEmoteState.ToPermAccessValue(), refVar, PermissionType.UniquePairPermEditAccess, UpdateDir.Own);
        CkGui.AttachToolTip("Limit " + PermissionData.DispName + " to only force GroundSit, Sit, and CyclePose.");

        ImUtf8.SameLineInner();
        var refVar2 = editAccess2;
        if (ImGui.Checkbox("##" + SPPID.ForcedEmoteState + "edit2", ref refVar2))
            UiBlockingTask = AssignBlockingTask(SPPID.ForcedEmoteState.ToPermAccessValue(true), refVar2, PermissionType.UniquePairPermEditAccess, UpdateDir.Own);
        CkGui.AttachToolTip("Allow " + PermissionData.DispName + " to force you into any looped Emote.");
    }

    /// <summary> This function is messy because it is independant of everything else due to a bad conflict between pishock HTML and gagspeak signalR. </summary>
    public void DrawPiShockPairPerms(float width, UserPairPermissions pairPerms, UserEditAccessPermissions pairAccess)
    {
        // First row must be drawn.
        using (ImRaii.Group())
        {
            var length = width - CkGui.IconTextButtonSize(FontAwesomeIcon.Sync, "Refresh") + ImGui.GetFrameHeight();
            var refCode = pairPerms.PiShockShareCode;
            if (CkGui.IconInputText("Code" + PermissionData.DispName, FontAwesomeIcon.ShareAlt, string.Empty, "Unique Share Code",
                ref refCode, 40, width, true, false))
            {
                UiBlockingTask = AssignBlockingTask(SPPID.PiShockShareCode.ToPermValue().name, refCode, PermissionType.UniquePairPerm, UpdateDir.Own);
            }
            CkGui.AttachToolTip($"Unique Share Code for " + PermissionData.DispName + "." +
                "--SEP--This should be a separate Share Code from your Global Share Code." +
                "--SEP--A Unique Share Code can have permissions elevated higher than the Global Share Code that only " + PermissionData.DispName + " can use.");

            ImUtf8.SameLineInner();
            if (CkGui.IconTextButton(FontAwesomeIcon.Sync, "Refresh", disabled: DateTime.Now - _lastRefresh < TimeSpan.FromSeconds(15) || refCode.IsNullOrWhitespace()))
            {
                _lastRefresh = DateTime.Now;
                UiBlockingTask = Task.Run(async () =>
                {
                    var newPerms = await _shockies.GetPermissionsFromCode(pairPerms.PiShockShareCode);
                    pairPerms.AllowShocks = newPerms.AllowShocks;
                    pairPerms.AllowVibrations = newPerms.AllowVibrations;
                    pairPerms.AllowBeeps = newPerms.AllowBeeps;
                    pairPerms.MaxDuration = newPerms.MaxDuration;
                    pairPerms.MaxIntensity = newPerms.MaxIntensity;
                    await _hub.UserPushAllUniquePerms(new(_pad.PairUserData, MainHub.PlayerUserData, pairPerms, pairAccess, UpdateDir.Other));
                });
            }
        }

        // special case for this.
        float seconds = (float)pairPerms.MaxVibrateDuration.TotalMilliseconds / 1000;
        using (var group = ImRaii.Group())
        {
            if (CkGui.IconSliderFloat("##maxVibeTime" + PermissionData.DispName, FontAwesomeIcon.Stopwatch, "Max Vibe Duration",
                ref seconds, 0.1f, 15f, width * .65f, true, pairPerms.HasValidShareCode()))
            {
                pairPerms.MaxVibrateDuration = TimeSpan.FromSeconds(seconds);
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                TimeSpan timespanValue = TimeSpan.FromSeconds(seconds);
                ulong ticks = (ulong)timespanValue.Ticks;
                UiBlockingTask = AssignBlockingTask(SPPID.MaxVibrateDuration.ToPermValue().name, ticks, PermissionType.UniquePairPerm, UpdateDir.Own);
            }
            CkGui.AttachToolTip("Max duration you allow this pair to vibrate your Shock Collar for");
        }
    }
}
