using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

// Attributions & References:
// --------------------------
// - Control Hotbar Lock Visibility.
//   https://github.com/Caraxi/SimpleTweaksPlugin/blob/15a3ac835ece1f54e41af24d133aba9fef476e30/Tweaks/Chat/HideChat.cs#L73

 namespace GagSpeak.GameInternals.Addons;
public unsafe static class AddonHotbar
{
    private static AddonActionBarBase* _hotbar => (ForceStayUtils.TryGetAddonByName<AtkUnitBase>("_ActionBar", out var a) && ForceStayUtils.IsAddonReady(a))
        ? (AddonActionBarBase*)a : (AddonActionBarBase*)null;
    private static AtkUnitBase* _hotbarBase => (AtkUnitBase*)_hotbar;

    // Exposed Properties.
    public static bool IsHotbarLocked
    {
        get
        {
            if (_hotbar is null)
                return false;
            // see if the node is locked or not.
            return _hotbar->IsLocked;
        }
    }

    // Exposed Methods
    public static void LockHotbar()
    {
        if (!IsHotbarLocked) AtkHelper.GenerateCallback(_hotbarBase, 9, 3, 51u, 0u, true);
    }

    public static void LockAndHide()
    {
        if (_hotbarBase is null)
            return;

        LockHotbar();

        var lockNode = _hotbarBase->GetNodeById(21);
        if (lockNode is null)
            return;

        var componentNode = lockNode->GetAsAtkComponentNode();
        if (componentNode is null)
            return;

        componentNode->AtkResNode.ToggleVisibility(false);
    }

    public static void UnlockHotbar()
    {
        if (IsHotbarLocked) AtkHelper.GenerateCallback(_hotbarBase, 9, 3, 51u, 0u, false);
    }

    public static void UnlockAndShow()
    {
        if (_hotbarBase is null)
            return;

        UnlockHotbar();

        var lockNode = _hotbarBase->GetNodeById(21);
        if (lockNode is null)
            return;

        var componentNode = lockNode->GetAsAtkComponentNode();
        if (componentNode is null)
            return;

        componentNode->AtkResNode.ToggleVisibility(true);
    }
}
