using CkCommons;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.PlayerControl;
using GagSpeak.Services;

namespace GagSpeak;

/// <summary>
///     Essential Task Operations used in Hardcore Actions.
/// </summary>
public static unsafe class HcCommonTaskFuncs
{
    public static bool TargetNode(Func<IGameObject> getObj)
    {
        // if the player is not interactable return false.
        if (!PlayerData.Interactable)
            return false;

        // if this object is null, we should throw an exception.
        if (getObj() is not { } objToTarget)
            throw new InvalidOperationException($"TargetObject was null during function call!");

        if (!objToTarget.IsTarget())
        {
            if (NodeThrottler.Throttle("HcTaskFunc.SetTarget", 200))
            {
                Svc.Targets.Target = objToTarget;
                return false; // still have not targeted the object, so ret false.
            }
        }
        // ret true, as we have successfully targeted the object.
        return true;
    }


    public static bool ApproachNode(Func<IGameObject> getObj, float minDistance = 4f)
    {
        var _prevPos = Vector3.Zero;
        // obtain the object from the function caller.
        var obj = getObj();
        // if the object is null, or the player is not interactable, return false.
        if (obj is null || !PlayerData.Interactable)
            return false;

        // if the player is moving (as in automoving), throttle it to disable when we reach our threshold distance.
        if (AgentMap.Instance()->IsPlayerMoving)
        {
            var minSpeedAllowed = Control.Instance()->IsWalking ? 0.015f : 0.05f;
            // Svc.Logger.Information($"Speed is {PlayerData.DistanceTo(_prevPos)} yalm/s");
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
    }

    public static bool InteractWithNode(Func<IGameObject> objFunc, bool checkLineOfSight)
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
    }

    // Forcibly puts you into a spesific Emote State.
    public static bool PerformExpectedEmote(ushort expectedEmoteId, byte expectedCPose)
    {
        if (PlayerData.IsAnimationLocked || !PlayerData.Interactable)
            return false;

        // Obtain our current emote ID.
        var currentEmote = EmoteService.CurrentEmoteId(PlayerData.ObjectAddress);

        // if the expected emoteId is 50 or 52, handle forced sitting.
        if (expectedEmoteId is 50 or 52)
        {
            // RETURN TRUE IF: both current emote and current cycle byte are the same.
            var curCyclePose = EmoteService.CurrentCyclePose(PlayerData.ObjectAddress);
            if (currentEmote == expectedEmoteId && curCyclePose == expectedCPose)
                return true;

            // Otherwise, it needs to be enforced!
            if (!EmoteService.IsSittingAny(currentEmote))
            {
                Svc.Logger.Verbose($"Forcing Emote: {(expectedEmoteId is 50 ? "/SIT" : "/GROUNDSIT")}. (Current was: {currentEmote}).");
                EmoteService.ExecuteEmote(expectedEmoteId); // Perform the sit emote.
                return false; // not valid state!
            }

            // Have to validate the correct Cycle Pose.
            if (curCyclePose != expectedCPose)
            {
                Svc.Logger.Verbose($"Your CyclePose ({curCyclePose}) isnt the expected ({expectedCPose})");
                // the cycle pose emote, which will change our sitting/standing pose.
                EmoteService.ExecuteEmote(90);
            }
            // Will still be false by this point.
            return false;
        }
        // Otherwise, handle generic emote state enforcement.
        else
        {
            // RETURN TRUE IF: current emote is the expected emote ID.
            if (currentEmote == expectedEmoteId)
                return true;

            // if we are currently sitting, perform a stand.
            if (!EmoteService.IsSittingAny(currentEmote))
            {
                Svc.Logger.Verbose("Enforcing stand to perform the correct emote.");
                EmoteService.ExecuteEmote(51); // 51 is the stand emote.
                return false; // Still not in expected state.
            }

            // Otherwise, attempt to perform the desired emote.
            if (!EmoteService.CanUseEmote(expectedEmoteId))
                EmoteService.ExecuteEmote(expectedEmoteId);

            return false;
        }
    }

    public static bool WaitForPlayerLoading()
        => PlayerData.Interactable && HcTaskUtils.IsScreenReady();

}
