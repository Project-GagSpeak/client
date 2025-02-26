using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.UI.Components;

/// <summary> An assister for drawing out the various permissions in the pair action window. </summary>
/// <remarks> This helps by using predefined data to avoid high calculations each draw frame. </remarks>
public partial class PermissionsDrawer : IMediatorSubscriber, IDisposable
{
    public GagspeakMediator Mediator { get; }
    private readonly MainHub _hub;
    private readonly PermissionData _pad;
    private readonly PiShockProvider _shockies;
    private readonly UiSharedService _uiShared;
    private Dictionary<SPPID, string> _timespanCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    public PermissionsDrawer(GagspeakMediator mediator, MainHub hub, PermissionData permData, 
        PiShockProvider shockies, UiSharedService uiShared)
    {
        Mediator = mediator;
        _hub = hub;
        _pad = permData;
        _shockies = shockies;
        _uiShared = uiShared;

        Mediator.Subscribe<StickyPairWindowCreated>(this, _ =>
        {
            // cancel any tasks and clear the cached information.
            _timespanCache.Clear();
            _lastRefresh = DateTime.MinValue;
        });
    }

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
    }

    private Task? UiBlockingTask = null;
    public bool DisableActions => !(UiBlockingTask?.IsCompleted ?? true);

    public void DrawPermRowPair(float width, SPPID perm, bool curState, bool editAccess)
    {
        DrawPermRowPairCommon(width, perm, curState, editAccess, () => !curState);
    }

    public void DrawPermRowPair(float width, SPPID perm, PuppetPerms curState, bool editAccess, PuppetPerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowPairCommon(width, perm, isFlagSet, editAccess, () => curState ^ editFlag);
    }

    public void DrawPermRowPair(float width, SPPID perm, MoodlePerms curState, bool editAccess, MoodlePerms editFlag)
    {
        var isFlagSet = (curState & editFlag) == editFlag;
        DrawPermRowPairCommon(width, perm, isFlagSet, editAccess, () => curState ^ editFlag);
    }

    public void DrawPermRowPair(float width, SPPID perm, TimeSpan curState, bool editAccess)
    {
        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var str = _timespanCache.TryGetValue(perm, out var value) ? value : curState.ToGsRemainingTime();
        var data = _pad.PairPermData[perm];

        if (_uiShared.IconInputText("##" + perm, data.IconOn, data.Text, "0d0h0m0s", ref str, 32, buttonW, true, !editAccess))
        {
            if (str != curState.ToGsRemainingTime() && GsPadlockEx.TryParseTimeSpan(str, out var newTime))
            {
                var res = perm.ToPermValue();

                if(!res.name.IsNullOrEmpty())
                    UiBlockingTask = AssignBlockingTask(res.name, (ulong)newTime.Ticks, res.type, UpdateDir.Other);
            }
            _timespanCache.Remove(perm);
        }
        UiSharedService.AttachToolTip("The Max Duration " + PermissionData.DispName + "Can Lock for.");

        _uiShared.BooleanToColoredIcon(editAccess, true, FontAwesomeIcon.Unlock, FontAwesomeIcon.Lock);
        UiSharedService.AttachToolTip(editAccess ? PermissionData.PairAccessYesTT : PermissionData.PairAccessNoTT);
    }

    private void DrawPermRowPairCommon<T>(float width, SPPID perm, bool curState, bool editAccess, Func<T> newStateFunc)
    {
        using var dis = ImRaii.PushStyle(ImGuiStyleVar.Alpha, editAccess ? 1f : 0.5f);
        using var butt = ImRaii.PushColor(ImGuiCol.Button, new Vector4(1.0f, 1.0f, 1.0f, 0.0f));
        var data = _pad.PairPermData[perm].TextInfo(curState);

        var buttonW = width - ImGui.GetFrameHeightWithSpacing();
        var pos = ImGui.GetCursorScreenPos();
        var button = ImGui.Button("##pair" + perm, new Vector2(buttonW, ImGui.GetFrameHeight()));
        ImGui.SetCursorScreenPos(pos);
        using (ImRaii.Group())
        {
            _uiShared.IconText(data.icon);
            ImUtf8.SameLineInner();
            ImGui.Text(data.prefix);
            ImGui.SameLine();
            UiSharedService.ColorTextBool(data.condText, curState);
            ImGui.SameLine();
            ImGui.Text(data.suffix);
        }
        UiSharedService.AttachToolTip(data.tt);

        _uiShared.BooleanToColoredIcon(editAccess, true, FontAwesomeIcon.Unlock, FontAwesomeIcon.Lock);
        UiSharedService.AttachToolTip(editAccess ? PermissionData.PairAccessYesTT : PermissionData.PairAccessNoTT);

        if (button)
        {
            var res = perm.ToPermValue();
            var newState = newStateFunc();
            if (newState is null || res.name.IsNullOrEmpty())
                return;
            
            UiBlockingTask = AssignBlockingTask(res.name, newState, res.type, UpdateDir.Other);
        }
    }

    /// <summary> Attempts to run a task, this can and will throw if the task is not valid. </summary>
    /// <param name="name"> the permission name. </param>
    /// <param name="thing"> the new value. </param>
    /// <param name="type"> the permission type. </param>
    /// <param name="dir"> if it is for the client or a pair. </param>
    /// <returns> the task to assign to UiBlockingComputation. </returns>
    /// <exception cref="ArgumentOutOfRangeException"> If the paramaters are not valid for the set. </exception>
    private Task AssignBlockingTask(string name, object thing, PermissionType type, UpdateDir dir)
    {
        try
        {
            return type switch
            {
                PermissionType.Global => dir switch
                {
                    UpdateDir.Own => Task.Run(async () => await _hub.UserUpdateOwnGlobalPerm(new(_pad.PairUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(name, thing), dir))),
                    UpdateDir.Other => Task.Run(async () => await _hub.UserUpdateOtherGlobalPerm(new(_pad.PairUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(name, thing), dir))),
                    _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
                },
                PermissionType.UniquePairPerm => dir switch
                {
                    UpdateDir.Own => Task.Run(async () => await _hub.UserUpdateOwnPairPerm(new(_pad.PairUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(name, thing), dir))),
                    UpdateDir.Other => Task.Run(async () => await _hub.UserUpdateOtherPairPerm(new(_pad.PairUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(name, thing), dir))),
                    _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
                },
                PermissionType.UniquePairPermEditAccess => dir switch
                {
                    UpdateDir.Own => Task.Run(async () => await _hub.UserUpdateOwnPairPermAccess(new(_pad.PairUserData, MainHub.PlayerUserData, new KeyValuePair<string, object>(name, thing), dir))),
                    _ => throw new ArgumentOutOfRangeException(nameof(dir), dir, null),
                },
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogCritical("Failed to assign blocking task." + e);
            throw;
        }
    }
}
