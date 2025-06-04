using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.MainWindow;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.CustomCombos.Padlockable;
using GagSpeak.CustomCombos.PairActions;
using GagSpeak.CustomCombos.Moodles;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using OtterGui.Text;
using Dalamud.Utility;
using GagspeakAPI.Enums;

namespace GagSpeak.CkCommons.Gui.Permissions;

// For Pair Component Draws.
public partial class PairStickyUI
{
    public void DrawPermRowPair(float width, SPPID perm, bool curState, bool editAccess)
    {
        DrawPermRowPairCommon(width, perm, curState, editAccess, () => !curState);
    }

    public void DrawPermRowPair(float width, SPPID perm, PuppetPerms curState, PuppetPerms editAccess, PuppetPerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowPairCommon(width, perm, isFlagSet, editAccess.HasAny(editFlag), () => curState ^ editFlag);
    }

    public void DrawPermRowPair(float width, SPPID perm, MoodlePerms curState, MoodlePerms editAccess, MoodlePerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowPairCommon(width, perm, isFlagSet, editAccess.HasAny(editFlag), () => curState ^ editFlag);
    }

    public void DrawPermRowPair(float width, SPPID perm, TimeSpan curState, bool editAccess)
    {
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : curState.ToGsRemainingTime();
        var data = PairPermData[perm];
        

        if (CkGui.IconInputText("##" + perm, data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !editAccess))
        {
            if (str != curState.ToGsRemainingTime() && PadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var res = perm.ToPermValue();

                if (res.name.IsNullOrEmpty() || res.type is not PermissionType.UniquePairPerm)
                    return;

                UiTask = PermissionHelper.ChangeOtherUnique(_hub, SPair.UserData, SPair.PairPerms, res.name, (ulong)newTime.Ticks);
            }
            _timespanCache.Remove(perm);
        }
        CkGui.AttachToolTip($"The Max Duration {DisplayName} can lock for.");

        CkGui.BooleanToColoredIcon(editAccess, true, FAI.Unlock, FAI.Lock);
        CkGui.AttachToolTip(editAccess 
            ? $"{SPair.GetNickAliasOrUid()} allows you to change this permission at will."
            : $"{SPair.GetNickAliasOrUid()} is preventing you from changing this permission. Only they can update it.");
    }

    public void DrawPermRowPairCommon<T>(float width, SPPID perm, bool curState, bool editAccess, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, editAccess ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = PairPermData[perm];

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            CkGui.FramedIconText(curState ? data.IconOn : data.IconOff);
            CkGui.TextFrameAlignedInline(data.TextPrefix(curState));
            ImGui.SameLine();
            CkGui.ColorTextBool(curState ? data.CondTrue : data.CondFalse, curState);
            CkGui.TextFrameAlignedInline(data.TextSuffix(curState));
        }
        CkGui.AttachToolTip(data.ToggleTextTT);

        CkGui.BooleanToColoredIcon(editAccess, true, FAI.Unlock, FAI.Lock);
        CkGui.AttachToolTip(editAccess
            ? $"{SPair.GetNickAliasOrUid()} allows you to change this permission at will."
            : $"{SPair.GetNickAliasOrUid()} is preventing you from changing this permission. Only they can update it.");

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;

            UiTask = res.type switch
            {
                PermissionType.Global => PermissionHelper.ChangeOtherGlobal(_hub, SPair.UserData, SPair.PairGlobals, res.name, newState),
                PermissionType.UniquePairPerm => PermissionHelper.ChangeOwnUnique(_hub, SPair.UserData, SPair.PairPerms, res.name, newState),
                _ => Task.Run(() => _logger.LogWarning($"Cannot Update a Pairs Edit Access!"))
            };
        }
    }
}
