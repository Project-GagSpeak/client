using CkCommons;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
namespace GagSpeak;

public static unsafe class HcStayHousingEntrance
{
    public static bool ConfirmHouseEntranceAndEnter()
    {
        var addon = HcTaskUtils.GetSpesificYesNo(NodeStringLang.ConfirmHouseEntrance);
        if (addon is null)
            return false;
        // Addon valid, throttle the yesno selection, if possible.
        if (HcTaskUtils.IsAddonReady(addon) && NodeThrottler.Throttle("SelectYesNo"))
        {
            var yesno = (AddonSelectYesno*)addon;
            // if addon is ready, check for validation to hit the yes button prior to pressing it.
            if (yesno->YesButton is not null && !yesno->YesButton->IsEnabled)
            {
                // forcibly enable the yes button through node flag manipulation.
                Svc.Logger.Verbose($"{nameof(AddonSelectYesno)}: Force enabling [Yes]");
                var flagsPtr = (ushort*)&yesno->YesButton->AtkComponentBase.OwnerNode->AtkResNode.NodeFlags;
                *flagsPtr ^= 1 << 5; // Toggle the 5th bit to enable the button.
            }
            return ClickButtonIfEnabled(addon, yesno->YesButton);
        }
        // failed
        return false;
    }

    public static bool ClickButtonIfEnabled(AtkUnitBase* nodeBase, AtkComponentButton* buttonToPress)
    {
        //if the button is enabled and its resolution node is visible, try interacting with it.
        if (buttonToPress->IsEnabled && buttonToPress->AtkResNode->IsVisible())
        {
            buttonToPress->ClickAddonButton(nodeBase);
            return true;
        }
        return false;
    }
    public static void ClickAddonButton(this AtkComponentButton target, AtkUnitBase* addon)
    {
        var buttonResNode = target.AtkComponentBase.OwnerNode->AtkResNode;
        var buttonEvent = (AtkEvent*)buttonResNode.AtkEventManager.Event;
        addon->ReceiveEvent(buttonEvent->State.EventType, (int)buttonEvent->Param, buttonResNode.AtkEventManager.Event);
    }

    // retrieves the nearest housing entrance that is within travel range.
    public static IGameObject GetNearestHousingEntrance(out float distance)
    {
        var nearestDist = float.MaxValue;
        IGameObject nearestNode = null!;

        var validNames = NodeStringLang.Entrance.Concat(NodeStringLang.EnterApartment);

        // iterate through the current objects in the table.
        foreach (var o in Svc.Objects)
        {
            // continue if the object is not an event object.
            if (o.ObjectKind != ObjectKind.EventObj)
                continue;

            if (!o.IsTargetable || !validNames.Any(n => n.Equals(o.Name.ToString())))
                continue;

            // If the name is valid, and it is targetable, consider it a valid entrance and calculate its distance.
            var objDist = Vector3.Distance(PlayerData.Position, o.Position);
            if (objDist < nearestDist)
            {
                nearestDist = objDist;
                nearestNode = o;
            }
        }
        // we should be now have the valid nearest node from the object table.
        distance = nearestDist;
        return nearestNode;
    }
}
