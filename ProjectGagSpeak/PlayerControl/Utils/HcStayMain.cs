using CkCommons;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.PlayerControl;
using GagSpeak.State;
namespace GagSpeak;

public static unsafe class HcApproachNearestHousing
{
    public static bool IsTargetApartment()
    {
        var tName = Svc.Targets.Target?.Name.ToString() ?? string.Empty;
        return NodeStringLang.EnterApartment.Any(n => n.Equals(tName, StringComparison.OrdinalIgnoreCase));
    }

    public static HardcoreTaskCollection GetTaskCollection(HcTaskManager hcTasks)
    {
        return hcTasks.CreateCollection("Enter Housing", new(HcTaskControl.LockThirdPerson | HcTaskControl.BlockAllKeys | HcTaskControl.DoConfinementPrompts))
            .Add(new HardcoreTask(HcCommonTaskFuncs.WaitForPlayerLoading))
            .Add(new HardcoreTask(TargetNearestHousingNode))
            .Add(hcTasks.CreateBranch(IsTargetApartment, "Approach Housing Node")
                .SetTrueTask(hcTasks.CreateGroup("Approach Apartment")
                    .Add(() => HcCommonTaskFuncs.ApproachNode(() => Svc.Targets.Target!, 3.5f))
                    .Add(HcStayApartment.InteractWithApartmentEntrance)
                    .Add(HcStayApartment.SelectGoToSpecifiedApartment)
                    .AsGroup())
                .SetFalseTask(hcTasks.CreateGroup("Approach Home")
                    .Add(() => HcCommonTaskFuncs.ApproachNode(() => Svc.Targets.Target!, 2.75f))
                    .Add(InteractWithHousingEntrance)
                    .Add(HcStayHousingEntrance.ConfirmHouseEntranceAndEnter)
                    .AsGroup())
                .AsBranch())
            .AsCollection();
    }

    // Attempts to locate the nearest housing entrance within range.
    public static unsafe bool TargetNearestHousingNode()
    {
        var node = HcStayHousingEntrance.GetNearestHousingEntrance(out var distance);
        // if the node is too far away, or the node further than the maximum yalm distance, return false.
        if (node is null || distance >= 20f)
            return false;

        // We know that we have a valid node. If we are not yet targetting it, we should target it.
        if (!node.IsTarget())
        {
            if (node.IsTargetable && NodeThrottler.Throttle("HousingEntrance.Target", 200))
            {
                Svc.Targets.Target = node;
                return false;
            }
        }
        else
        {
            // it is the target, so return true.
            return true;
        }
        // operation failed, so return false.
        return false;
    }

    public static unsafe bool InteractWithHousingEntrance()
    {
        // do not interact if animation locked.
        if (PlayerData.IsAnimationLocked)
            return false;
        // if the target is not an event object and it's ID is not 2007402, then return false.
        if (Svc.Targets.Target?.ObjectKind != ObjectKind.EventObj || Svc.Targets.Target?.DataId != 2002737)
            return false;

        // target was valid, so perform a throttled interaction with the apartment entrance.
        if (NodeThrottler.Throttle("InteractWithHouse", 1000))
        {
            TargetSystem.Instance()->InteractWithObject(Svc.Targets.Target.ToStruct(), false);
            return true; // return true regardless so we do not endlessly interact with something not in LOS.
        }
        // failed to throttle, return false.
        return false;
    }
}
