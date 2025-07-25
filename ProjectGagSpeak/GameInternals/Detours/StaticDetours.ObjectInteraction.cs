using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace GagSpeak.GameInternals.Detours;
public partial class StaticDetours
{
    /// <summary>
    ///     SHOULD fire whenever we interact with any object thing.
    /// </summary>
    internal Hook<TargetSystem.Delegates.InteractWithObject> ItemInteractedHook;

    private unsafe ulong ItemInteractedDetour(TargetSystem* thisPtr, GameObject* obj, bool checkLineOfSight)
    {
        try
        {
            DebugGameObject(obj);

            // Return if we cannot apply loot anyways.
            if (!_lootHandler.CanApplyAnyLoot)
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);

            if (!_lootHandler.IsAnyTreasure(obj))
            {
                Logger.LogTrace("Interacted with GameObject that was not a Treasure Chest or Deep Dungeon Coffer.", LoggerType.CursedItems);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            if (_lootHandler.IsObjectLastOpenedLoot(obj))
            {
                Logger.LogTrace("Interacted with GameObject that was the last opened chest.", LoggerType.CursedItems);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            if (_lootHandler.LootTaskRunning)
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);

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
        Logger.LogTrace("Object ID: " + obj->GetGameObjectId().ObjectId, LoggerType.CursedItems);
        Logger.LogTrace("Object Kind: " + obj->ObjectKind, LoggerType.CursedItems);
        Logger.LogTrace("Object SubKind: " + obj->SubKind, LoggerType.CursedItems);
        Logger.LogTrace("Object Name: " + obj->NameString.ToString(), LoggerType.CursedItems);
        if (obj->EventHandler is not null)
        {
            Logger.LogTrace("Object EventHandler ID: " + obj->EventHandler->Info.EventId.Id, LoggerType.CursedItems);
            Logger.LogTrace("Object EventHandler Entry ID: " + obj->EventHandler->Info.EventId.EntryId, LoggerType.CursedItems);
            Logger.LogTrace("Object EventHandler Content Id: " + obj->EventHandler->Info.EventId.ContentId, LoggerType.CursedItems);
        }
    }
}
