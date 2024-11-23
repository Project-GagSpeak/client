using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Utils;

// https://github.com/Caraxi/SimpleTweaksPlugin/blob/e42253ffde1e1b7e12ef8a6e4c8bf0de910f95b6/Tweaks/AutoLockHotbar.cs#L40
// controls the state of hotbar lock
namespace GagSpeak.Hardcore.Hotbar;
public unsafe static class HotbarLocker
{
    public static NewState LockStatePriorToActivation = NewState.Disabled;

    public static void StoreCurrentLockState()
    {
        var hotbarBase = (AddonActionBarBase*)(AtkUnitBase*)AtkFuckery.GetAddonByName("_ActionBar");
        if (hotbarBase != null)
        {
            // see if the node is locked or not.
            var isLocked = hotbarBase->IsLocked;
            StaticLogger.Logger.LogDebug("Hotbar Locked prior to application?: " + isLocked);
            LockStatePriorToActivation = isLocked ? NewState.Locked : NewState.Unlocked;
        }
    }

    public static void SetHotbarLockState(NewState newState)
    {
        if (newState is NewState.Disabled or NewState.Enabled)
            return;

        // First, grab our current hotbar state and store it.
        StoreCurrentLockState();
        // if for whatever god forsaken cursed reason it is not locked or unlocked, return.
        if (LockStatePriorToActivation == NewState.Disabled)
            return;

        // Otherwise, if we want to lock, and are unlocked, or want to unlock, and are locked, we should do so.
        var actionBar = (AtkUnitBase*)AtkFuckery.GetAddonByName("_ActionBar");
        if (actionBar is null)
            return;

        // If we are trying to lock the hotbar, and it was unlocked before set activation, lock it.
        if (newState is NewState.Locked && LockStatePriorToActivation is NewState.Unlocked)
        {
            AtkFuckery.GenerateCallback(actionBar, 9, 3, 51u, 0u, true);
        }
        // if we are wishing to unlock, we should only unlock it if it was unlocked prior to activation.
        if (newState is NewState.Unlocked && LockStatePriorToActivation is NewState.Unlocked)
        {
            AtkFuckery.GenerateCallback(actionBar, 9, 3, 51u, 0u, false);
            // reset the lock state to disabled to prevent duplicate calls.
            LockStatePriorToActivation = NewState.Disabled;
        }

        // in other words, if it was locked prior to toggling on or off, it will simply hide the lock button.
        // Speaking of which, let's do that now.
        var lockNode = actionBar->GetNodeById(21);
        if (lockNode is null)
            return;

        var lockComponentNode = lockNode->GetAsAtkComponentNode();
        if (lockComponentNode is null)
            return;

        // if we are going to lock it, hide it. If we are going to unlock it, show it.
        lockComponentNode->AtkResNode.ToggleVisibility(newState is NewState.Locked ? false : true);
    }
}
