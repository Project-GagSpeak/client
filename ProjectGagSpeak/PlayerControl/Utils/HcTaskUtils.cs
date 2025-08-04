using CkCommons;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services;
using GagSpeak.Utils;
using Lumina.Excel.Sheets;
using System.Runtime.CompilerServices;
using GameObject = FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject;
namespace GagSpeak;

public static unsafe class HcTaskUtils
{
    public static GameObject* ToStruct(this IGameObject obj)
        => (GameObject*)obj.Address;

    public static bool IsTarget(this IGameObject obj)
        => Svc.Targets.Target != null && Svc.Targets.Target.Address == obj.Address;


    // Make sure that our screen is read to interact with. Can be a tad expensive, so use sparingly.
    public static bool IsScreenReady()
    {
        if (TryGetAddonByName<AtkUnitBase>("NowLoading", out var a) && a->IsVisible) return false;
        if (TryGetAddonByName<AtkUnitBase>("FadeMiddle", out var b) && b->IsVisible) return false;
        if (TryGetAddonByName<AtkUnitBase>("FadeBack", out var c) && c->IsVisible) return false;
        return true;
    }

    public static bool IsOutside()
        => HousingManager.Instance()->IsOutside();

    public static bool? FollowTarget()
    {
        if (!PlayerData.Available)
            return false;
        if (Svc.Targets.Target != null && NodeThrottler.Throttle("Follow", 200))
        {
            ChatService.SendCommand("follow <T>");
            return true;
        }
        return false;
    }

    public static bool? LockOnToTarget()
    {
        if (!PlayerData.Available)
            return false;
        if (Svc.Targets.Target != null && NodeThrottler.Throttle("LockOn", 200))
        {
            ChatService.SendCommand("lockon");
            return true;
        }
        return false;
    }

    public static bool? EnableAutoMove()
    {
        if (!PlayerData.Available)
            return false;
        if (NodeThrottler.Throttle("EnableAutoMove", 200))
        {
            ChatService.SendCommand("automove on");
            return true;
        }
        return false;
    }

    public static bool? DisableAutoMove()
    {
        if (!PlayerData.Available)
            return false;
        if (NodeThrottler.Throttle("DisableAutoMove", 200))
        {
            ChatService.SendCommand("automove off");
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAddonReady(AtkUnitBase* addon)
        => addon->IsVisible && addon->UldManager.LoadedState == AtkLoadState.Loaded && addon->IsFullyLoaded();

    public static bool IsReady(this AtkUnitBase addon)
        => addon.IsVisible && addon.UldManager.LoadedState == AtkLoadState.Loaded && addon.IsFullyLoaded();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsComponentReady(AtkComponentNode* addon)
        => addon->AtkResNode.IsVisible() && addon->Component->UldManager.LoadedState == AtkLoadState.Loaded;

    /// <summary> Obtain the unit base of an AddonArgs instance. </summary>
    public static AtkUnitBase* Base(this AddonArgs args) => (AtkUnitBase*)args.Addon;

    // locate the parent node of a child node by recursively iterating backwards in the linked list.
    public static unsafe AtkResNode* GetRootParentNode(AtkResNode* node)
    {
        // obtain the parent from the linked list.
        var parent = node->ParentNode;
        // if the parent is null, we found it, and can return it, otherwise, return a recursive call to this function.
        return parent == null ? node : GetRootParentNode(parent);
    }

    /// <summary> Obtain the entries from a select string node. </summary>
    public static List<string> GetEntries(AddonSelectString* addon)
    {
        var list = new List<string>();
        // do not iterate through with a foreach, as we are indexing a pointer node with its own this[] method.
        for (var i = 0; i < addon->PopupMenu.PopupMenu.EntryCount; i++)
            list.Add(MemoryHelper.ReadSeStringNullTerminated((nint)addon->PopupMenu.PopupMenu.EntryNames[i].Value).ExtractText().Trim());
        return list;
    }

    internal static bool TrySelectSpesificEntry(string entry, Func<bool> throttleSelect)
        => TrySelectSpesificEntry([ entry ], throttleSelect);

    internal static bool TrySelectSpesificEntry(IEnumerable<string> entries, Func<bool> throttleSelect)
    {
        if (!TryGetAddonByName<AddonSelectString>("SelectString", out var addon) || !IsAddonReady(&addon->AtkUnitBase))
            return false;

        // obtain the entries from the addon.
        var addonEntries = GetEntries(addon);

        // if there are no entries, we cannot select anything.
        if (addonEntries.FirstOrDefault(x => entries.Any(e => e.Equals(x))) is not { } match)
            return false;

        // the entry does exist, so try and select it. (if our throttle allows it)
        var index = addonEntries.IndexOf(match);
        if (index >= 0 && throttleSelect())
        {
            Svc.Logger.Debug($"[HcTaskUtils] SelectSpesificEntry: selecting {match}/{index} requested from [{string.Join(',', entries)}].");
            StaticDetours.FireCallback((AtkUnitBase*)addon, true, index);
            return true;
        }
        return false;
    }

    internal static AtkUnitBase* GetSpesificYesNo(params string[] possibleNames)
        => GetSpesificYesNo(false, possibleNames);

    /// <summary> Obtain the spesific addon for a yes/no prompt. </summary>
    /// <param name="contains"> if we should perform a match based on Equals() or Contains(). </param>
    /// <param name="possibleNames"> all possible names the node text can have. </param>
    /// <returns> the AtkUnitBase pointer for the yesno node. </returns>
    internal static AtkUnitBase* GetSpesificYesNo(bool contains, params string[] possibleNames)
    {
        // attempt to locate the addon by all possible indexes that matches the selection.
        // once one is found, break out of the loop and return the addon.
        // certainly a 'brute force' method, but workable, and not too expensive.
        try
        {
            // if the addon is null, return null.
            if (!TryGetAddonByName<AtkUnitBase>("SelectYesno", out var addon))
                return null;

            // if the addon is not readon, continue.
            if (!IsAddonReady(addon))
                return null;

            Svc.Logger.Verbose($"SelectYesNo checking addon for {string.Join(", ", possibleNames)}");
            // obtain the text node from the addon.
            var txtNode = addon->UldManager.NodeList[15]->GetAsAtkTextNode();
            var rawTxtInNode = &txtNode->NodeText;
            var finalStr = MemoryHelper.ReadSeString(rawTxtInNode).ExtractText().Replace(" ", "");
            // Dictate if this addon is the right one, based on the contains paramater.
            var options = possibleNames.Select(n => n.Replace(" ", ""));

            if (contains ? options.Any(finalStr.Contains) : options.Any(finalStr.Equals))
                return addon;
        }
        catch (Bagagwa ex)
        {
            Svc.Logger.Error($"Error obtaining SelectYesNo: {ex}");
            return null;
        }
        return null;
    }

    /// <summary> Obtain an addon* by its name alone. If it is not found, returns false. </summary>
    public static bool TryGetAddonByName<T>(string addon, out T* addonPtr) where T : unmanaged
    {
        // we can use a more direct approach now that we have access to static classes.
        var a = Svc.GameGui.GetAddonByName(addon, 1);
        if (a == IntPtr.Zero)
        {
            addonPtr = null;
            return false;
        }
        else
        {
            addonPtr = (T*)a;
            return true;
        }
    }
}
