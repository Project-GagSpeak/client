using CkCommons;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.Services;
using System;

namespace GagSpeak.PlayerControl;

/// <summary>
///     Essential Task Operations used in Hardcore Actions.
/// </summary>
public static unsafe class HcCommonTasks
{
    public static HardcoreTask ApproachNode(Func<IGameObject> getObj, float minDistance = 4f, HcTaskConfiguration? config = null)
    {
        var _prevPos = Vector3.Zero;
        return new HardcoreTask(() =>
        {
            // obtain the object from the function caller.
            var obj = getObj();
            // if the object is null, or the player is not interactable, return false.
            if (obj is null || !PlayerData.Interactable)
                return false;

            // if the player is moving (as in automoving), throttle it to disable when we reach our threshold distance.
            if (AgentMap.Instance()->IsPlayerMoving)
            {
                var minSpeedAllowed = Control.Instance()->IsWalking ? 0.015f : 0.05f;
                Svc.Logger.Information($"Speed is {PlayerData.DistanceTo(_prevPos)} yalm/s");
                if (PlayerData.DistanceTo(obj) < minDistance && NodeThrottler.Throttle("HcTaskFunc.AutoMoveOff", 200))
                    ChatService.SendCommand("automove off");

                // If something is potentially obstructing our movement, and we have slowed down, and not jumped in .75s, try jumping.
                else if (HcTaskManager.ElapsedTime > 500 && !PlayerData.IsJumping)
                {
                    // try to jump if our speed is slow enough.
                    if (PlayerData.DistanceTo(_prevPos) < minSpeedAllowed && NodeThrottler.Throttle("HcTaskFunc.Jump", 1250))
                    {
                        ChatService.SendGeneralActionCommand(2); // Jumping!
                        Svc.Logger.Verbose("Jumping to try and get unstuck.");
                    }
                }

                _prevPos = PlayerData.Object.Position;
                // return false, as we are still moving.
                return false;
            }
            else
            {
                // if the object is our target, perform a lock on and begin automove.
                if (obj.IsTarget())
                {
                    // otherwise, check if the distance is less than the minimum distance.
                    if (PlayerData.DistanceTo(obj) < minDistance)
                        return true; // ret true, as we reached the object and are no longer moving.

                    // target and begin automove.
                    if (NodeThrottler.Throttle("HcTaskFunc.LockOn"))
                    {
                        ChatService.SendCommand("lockon on");
                        ChatService.SendCommand("automove on");
                        return false; // still have not reached the object, so ret false.
                    }
                }
                else
                {
                    // the object is not targeted, so set the target!.
                    if (obj.IsTargetable && NodeThrottler.Throttle("HcTaskFunc.SetTarget"))
                    {
                        Svc.Targets.Target = obj;
                        return false;
                    }
                }
            }
            return false;
        }, $"ApproachNode({getObj()}, {minDistance})", config);
    }

    public static HardcoreTask InteractWithNode(Func<IGameObject> objFunc, bool checkLineOfSight, HcTaskConfiguration? config = null)
    {
        return new HardcoreTask(() =>
        {
            var obj = objFunc();
            if (obj is null || PlayerData.IsAnimationLocked || !PlayerData.Interactable)
                return false;

            // object and player was valid, so ensure it is the target, and if not, set the target.
            if (!obj.IsTarget())
            {
                if (NodeThrottler.Throttle("HcTaskFunc.SetTarget", 200))
                {
                    Svc.Targets.Target = obj;
                    return false;
                }
            }
            else
            {
                if (NodeThrottler.Throttle("HcTaskFunc.Interact"))
                {
                    TargetSystem.Instance()->InteractWithObject(obj.ToStruct(), checkLineOfSight);
                    return true;
                }
            }
            // generic FAILURE.
            return false;
        }, $"InteractWithNode({objFunc}, {checkLineOfSight})", config);
    }

    public static HardcoreTask WaitForPlayerLoading(HcTaskConfiguration? config = null)
        => new HardcoreTask(() => PlayerData.Interactable && ForceStayUtils.IsScreenReady(), "Wait for screen fadeout completion", config);

}
