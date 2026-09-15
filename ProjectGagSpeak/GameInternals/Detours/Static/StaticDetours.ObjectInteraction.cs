using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace GagSpeak.GameInternals.Detours;
public partial class StaticDetours
{
    /// <summary>
    ///   SHOULD fire whenever we interact with any object thing.
    /// </summary>
    internal Hook<TargetSystem.Delegates.InteractWithObject> ItemInteractedHook;

    private unsafe ulong ItemInteractedDetour(TargetSystem* thisPtr, GameObject* obj, bool checkLineOfSight)
    {
        try
        {
            DebugGameObject(obj);

            // Return if we cannot apply loot anyways.
            if (!_lootHandler.CanApplyAnyLoot)
            {
                Logger.LogTrace("Cannot apply loot currently.", LogFilter.CursedItems);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            if (!_lootHandler.IsAnyTreasure(obj))
            {
                Logger.LogTrace("Interacted with GameObject that was not a Treasure Chest or Deep Dungeon Coffer.", LogFilter.CursedItems);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            if (_lootHandler.IsObjectLastOpenedLoot(obj))
            {
                Logger.LogTrace("Interacted with GameObject that was the last opened chest.", LogFilter.CursedItems);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            // Open the Loot Item
            _lootHandler.OpenLootItem(obj);
        }
        catch (Bagagwa e)
        {
            Logger.LogError(e, "Failed to log object information.");
        }

        return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
    }

    private unsafe void DebugGameObject(GameObject* obj)
    {
        Logger.LogTrace("Object ID: " + obj->GetGameObjectId().ObjectId, LogFilter.CursedItems);
        Logger.LogTrace("Object Kind: " + obj->ObjectKind, LogFilter.CursedItems);
        Logger.LogTrace("Object SubKind: " + obj->SubKind, LogFilter.CursedItems);
        Logger.LogTrace("Object Name: " + obj->NameString.ToString(), LogFilter.CursedItems);
        if (obj->EventHandler is not null)
        {
            Logger.LogTrace("Object EventHandler ID: " + obj->EventHandler->Info.EventId.Id, LogFilter.CursedItems);
            Logger.LogTrace("Object EventHandler Entry ID: " + obj->EventHandler->Info.EventId.EntryId, LogFilter.CursedItems);
            Logger.LogTrace("Object EventHandler Content Id: " + obj->EventHandler->Info.EventId.ContentId, LogFilter.CursedItems);
        }
    }
}
