using CkCommons;
using Dalamud.Game.ClientState.Objects.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Game.Readers;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerControl;
using GagSpeak.State;
namespace GagSpeak;

public static unsafe class HcStayApartment
{
    public static HardcoreTaskGroup GetTaskGroup(HcTaskManager hcTasks)
    {
        return hcTasks.CreateGroup("WalkToAndOpenApartmentMenu", new(HcTaskControl.LockThirdPerson | HcTaskControl.BlockAllKeys | HcTaskControl.DoConfinementPrompts))
            .Add(HcTaskUtils.IsScreenReady)
            .Add(TargetApartmentEntrance)
            .Add(HcTaskUtils.LockOnToTarget)
            .Add(HcTaskUtils.EnableAutoMove)
            .Add(() => Vector3.Distance(PlayerData.Position, Svc.Targets.Target?.Position ?? Vector3.Zero) < 3.5f)
            .Add(HcTaskUtils.DisableAutoMove)
            .Add(InteractWithApartmentEntrance)
            .AsGroup();
    }

    /// <summary> Identifies the Apartment Entrance node by ID, and targets it. </summary>
    public static bool TargetApartmentEntrance()
    {
        // 2007402 is dataId apartment building entrance 0   apartment building entrances 0   1   1   0   0
        foreach (var o in Svc.Objects.OrderBy(x => Vector3.Distance(x.Position, PlayerData.Position)))
        {
            // continue if not the data ID.
            if (o.BaseId != 2007402)
                continue;

            // if the object is not the current target, make it the target.
            if (!o.IsTarget())
            {
                // target on throttle cooldown.
                if (NodeThrottler.Throttle("TargetApartment"))
                    Svc.Targets.Target = o;
                // ret false since it was not yet the target.
                return false;
            }
            // otherwise, we are already targeting the apartment entrance.
            return true;
        }
        // if we did not find any apartment entrances, return false.
        return false;
    }

    /// <summary> Attempt to interact with the apartment entrance. </summary>
    public static unsafe bool InteractWithApartmentEntrance()
    {
        // do not interact if animation locked.
        if (PlayerData.IsAnimationLocked)
            return false;
        // if the target is not an event object and it's ID is not 2007402, then return false.
        if (Svc.Targets.Target?.ObjectKind != ObjectKind.EventObj || Svc.Targets.Target?.BaseId != 2007402)
            return false;

        // target was valid, so perform a throttled interaction with the apartment entrance.
        if (NodeThrottler.Throttle("InteractWithApartment", 5000))
        {
            // convert to struct.
            var targetStruct = Svc.Targets.Target.ToStruct();
            TargetSystem.Instance()->InteractWithObject(targetStruct, false);
            return true;
        }

        // failed to throttle, return false.
        return false;
    }


    /// <summary> Select the "Go to my Apartment" option from the apartment confirmation menu. </summary>
    public static unsafe bool GoToMyApartment()
        => HcTaskUtils.TrySelectSpesificEntry(NodeStringLang.GoToMyApartment, () => NodeThrottler.Throttle("SelectStringApartment"));

    /// <summary> Select the "Go to specified apartment"? option from the room confirmation menu. </summary>
    public static unsafe bool SelectGoToSpecifiedApartment()
        => HcTaskUtils.TrySelectSpesificEntry(NodeStringLang.GoToSpecifiedApartment, () => NodeThrottler.Throttle("SelectStringApartment"));


    /// <summary> Select a spesific apartment index. </summary>
    public static unsafe bool? SelectApartment(int apartmentNum)
    {
        // make sure we get the apartment selection by the page entry. (15 per page)
        var desiredSection = (int)((apartmentNum) / 15);
        // if we cannot locate the addon, return false.
        if (!HcTaskUtils.TryGetAddonByName<AtkUnitBase>("MansionSelectRoom", out var addon) || !HcTaskUtils.IsAddonReady(addon))
            return false;

        // obtain the mansionSelectRoom reader for easy assistance.
        var reader = new AtkMansionSelectRoomReader(addon);
        if (!reader.IsLoaded)
            return false;

        // if the reader's section is the section we are looking for, then select the apartment.
        if (reader.Section == desiredSection)
        {
            // if we can throttle the room selection, do so.
            if (!NodeThrottler.Throttle("ApartmentSelectRoom", 3000))
                return false;

            // we throttled the selection, so perform the action.
            var targetRoom = apartmentNum - desiredSection * 15;
            // ensure the target is within reasonable bounds.
            if (targetRoom < 0 || targetRoom > 14)
                throw new InvalidOperationException($"Apartment number out of range. (R:{targetRoom} | Sect:{desiredSection})");
            // if the target room is more than the maximum rooms for the section, return null and log error.
            if (targetRoom >= reader.SectionRoomsCount)
            {
                Svc.Logger.Error($"[HcTaskUtils] Couldn't find Apartment # {apartmentNum + 1} ({targetRoom} in section {desiredSection})");
                return null;
            }
            // get the room info.
            var roomInfo = reader.Rooms.SafelySelect(targetRoom);
            // if the room owner is blank, or the room access state is vacent, log error and return null.
            if (string.IsNullOrEmpty(roomInfo.RoomOwner) || roomInfo.AccessState == 1)
            {
                Svc.Logger.Error($"[HcTaskUtils] Apartment#{apartmentNum + 1} is vacent. Cannot enter!");
                return null;
            }
            // fire the callback to select the room! we did it! Yippee we made it through hell!
            StaticDetours.FireCallback(addon, true, 0, targetRoom);
            return true;
        }
        else
        {
            // we need to change the section to the one with the room we are looking for.
            if (desiredSection < 0 || desiredSection >= reader.ExistingSectionsCount)
            {
                Svc.Logger.Error($"[HcTaskUtils] SelectApartment: Invalid section {desiredSection} for apartment {apartmentNum}.");
                return null;
            }
            // if we can throttle the selection to enter the apartment, do so.
            if (NodeThrottler.Throttle("EnterApartmentRoom", 3000))
            {
                // fire the callback to pick the correct section.
                StaticDetours.FireCallback(addon, true, 1, desiredSection);
                return false;
            }
        }
        // failed, return false.
        return false;
    }
}
