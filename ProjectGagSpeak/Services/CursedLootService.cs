using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using OtterGui;
using GameObjectKind = FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind;

namespace GagSpeak.Services;
public class CursedLootService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerData;
    private readonly CursedLootHandler _handler;
    private readonly AppearanceManager _appearance;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;

    // SHOULD fire whenever we interact with any object thing.
    internal Hook<TargetSystem.Delegates.InteractWithObject> ItemInteractedHook;

    public CursedLootService(ILogger<CursedLootService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData, CursedLootHandler handler, 
        AppearanceManager appearance, ClientMonitorService clientService, 
        OnFrameworkService frameworkUtils, IGameInteropProvider interop) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _handler = handler;
        _appearance = appearance;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;

        unsafe
        {
            ItemInteractedHook = interop.HookFromAddress<TargetSystem.Delegates.InteractWithObject>((nint)TargetSystem.MemberFunctionPointers.InteractWithObject, ItemInteractedDetour);
            ItemInteractedHook.Enable();
        }
    }

    private Task? _openTreasureTask;
    // Store the last interacted chestId so we dont keep spam opening the same chest.
    private static ulong LastOpenedTreasureId = 0;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        ItemInteractedHook?.Disable();
        ItemInteractedHook?.Dispose();
        ItemInteractedHook = null!;
    }

    private unsafe ulong ItemInteractedDetour(TargetSystem* thisPtr, GameObject* obj, bool checkLineOfSight)
    {
        try
        {
            Logger.LogTrace("Object ID: " + obj->GetGameObjectId().ObjectId);
            Logger.LogTrace("Object Kind: " + obj->ObjectKind);
            Logger.LogTrace("Object SubKind: " + obj->SubKind);
            Logger.LogTrace("Object Name: " + obj->NameString.ToString());
            if (obj->EventHandler is not null)
            {
                Logger.LogTrace("Object EventHandler ID: " + obj->EventHandler->Info.EventId.Id);
                Logger.LogTrace("Object EventHandler Entry ID: " + obj->EventHandler->Info.EventId.EntryId);
                Logger.LogTrace("Object EventHandler Content Id: " + obj->EventHandler->Info.EventId.ContentId);
            }

            // dont bother if cursed dungeon loot isnt enabled, or if there are no inactive items in the pool.
            if (!_clientConfigs.GagspeakConfig.CursedDungeonLoot || !_handler.InactiveItemsInPool.Any() || MainHub.IsOnUnregistered || !MainHub.IsConnected)
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);

            // if the object is not a treasure of event object dont worry about processing it.
            if (obj->ObjectKind is not GameObjectKind.Treasure && AchievementHelpers.IsDeepDungeonCoffer(obj) is false)
            {
                Logger.LogTrace("Interacted with GameObject that was not a Treasure Chest or Deep Dungeon Coffer.", LoggerType.CursedLoot);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            // if we the item interacted with is the same as the last opened chest, return.
            if (obj->GetGameObjectId().ObjectId == LastOpenedTreasureId)
            {
                Logger.LogTrace("Interacted with GameObject that was the last opened chest.", LoggerType.CursedLoot);
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
            }

            // Dont process if our current treasure task is running
            if (_openTreasureTask != null && !_openTreasureTask.IsCompleted)
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);

            // The chest is a valid chest at this point, but we need to determine what type it is.
            if (AchievementHelpers.IsDeepDungeonCoffer(obj))
            {
                // its a Deep Dungeon Coffer.
                Logger.LogTrace("Attempting to open Deep Dungeon coffer, checking on next second", LoggerType.CursedLoot);
                _openTreasureTask = CheckDeepDungeonCoffers(obj->GetGameObjectId().ObjectId);
            }
            else
            {
                // It's a normal Coffer.
                // Make sure we are opening it. If we were not the first, it will exist in here.
                if (_clientService.PartySize is not 1)
                {
                    foreach (var item in Loot.Instance()->Items)
                    {
                        // Perform an early return if not valid.
                        if (item.ChestObjectId == obj->GetGameObjectId().ObjectId)
                        {
                            Logger.LogTrace("This treasure was already opened!", LoggerType.CursedLoot);
                            return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
                        }
                    }
                }

                Logger.LogTrace("Attempting to open coffer, checking on next second", LoggerType.CursedLoot);
                _openTreasureTask = CheckLootTables(obj->GetGameObjectId().ObjectId);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to log object information.");
        }
        return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
    }

    private async Task CheckLootTables(ulong objectInteractedWith)
    {
        try
        {
            await Task.Delay(1000);
            Logger.LogInformation("Checking tables!", LoggerType.CursedLoot);
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                unsafe
                {
                    bool valid = _clientService.InSoloParty ? true : Loot.Instance()->Items.ToArray().Any(x => x.ChestObjectId == objectInteractedWith);
                    if (valid)
                    {
                        Logger.LogTrace("we satisfy valid condition.", LoggerType.CursedLoot);
                        if (objectInteractedWith != LastOpenedTreasureId)
                        {
                            Logger.LogTrace("we just attempted to open a dungeon chest.", LoggerType.CursedLoot);
                            LastOpenedTreasureId = objectInteractedWith;
                            ApplyCursedLoot().ConfigureAwait(false);
                            return;
                        }
                    }
                    else
                    {
                        Logger.LogTrace("No loot items are the nearest treasure", LoggerType.CursedLoot);
                    }
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _openTreasureTask = null;
        }
    }

    private async Task CheckDeepDungeonCoffers(ulong objectInteractedWith)
    {
        try
        {
            await Task.Delay(1000);
            Logger.LogInformation("Checking tables!", LoggerType.CursedLoot);
            await _frameworkUtils.RunOnFrameworkThread(() =>
            {
                unsafe
                {
                    if (objectInteractedWith != LastOpenedTreasureId)
                    {
                        Logger.LogTrace("we just attempted to open a deep dungeon chest.", LoggerType.CursedLoot);
                        LastOpenedTreasureId = objectInteractedWith;
                        ApplyCursedLoot().ConfigureAwait(false);
                        return;
                    }
                }
            }).ConfigureAwait(false);
        }
        finally
        {
            _openTreasureTask = null;
        }
    }

    /// <summary>
    /// Fired whenever we open a chest in a dungeon.
    /// </summary>
    private async Task ApplyCursedLoot()
    {
        // get the percent change to apply
        var percentChange = _handler.LockChance;

        if (percentChange is 0)
        {
            Logger.LogDebug("No Point in rolling with 0% chance, skipping!", LoggerType.CursedLoot);
            return;
        }

        // Calculate if we should apply it or not. If we fail to roll a success, return.
        Random random = new Random();
        int randomValue = random.Next(0, 101);
        if (randomValue > percentChange) return;

        // Obtain a randomly selected cursed item from the inactive items in the pool.
        var enabledPoolCount = _handler.InactiveItemsInPool.Count;
        if (enabledPoolCount <= 0)
        {
            Logger.LogWarning("No Cursed Items are available to apply. Skipping.", LoggerType.CursedLoot);
            return;
        }

        Guid selectedLootId = Guid.Empty;
        var randomIndex = random.Next(0, enabledPoolCount);
        Logger.LogDebug("Randomly selected index [" + randomIndex + "] (between 0 and " + enabledPoolCount + ") for (" + _handler.InactiveItemsInPool[randomIndex].Name + ")", LoggerType.CursedLoot);
        // if the item is a gag, handle the special condition for this.
        if (_handler.InactiveItemsInPool[randomIndex].IsGag)
        {
            var availableSlot = _playerData.AppearanceData!.GagSlots.IndexOf(x => x.GagType.ToGagType() is GagType.None);
            // If no slot is available, make a new list that is without any items marked as IsGag, and roll again.
            if (availableSlot is not -1)
            {
                Logger.LogDebug("A Gag Slot is available to apply and lock. Doing so now!", LoggerType.CursedLoot);
                selectedLootId = _handler.InactiveItemsInPool[randomIndex].LootId;

                // Notify the client of their impending fate~
                var item = new SeStringBuilder().AddItalics("As the coffer opens, cursed loot spills " +
                    "forth, silencing your mouth with a Gag now strapped on tight!").BuiltString;
                Mediator.Publish(new NotifyChatMessage(item, NotificationType.Error));

                // generate the length they will be locked for:
                var lockTimeGag = GetRandomTimeSpan(_handler.LowerLockLimit, _handler.UpperLockLimit, random);

                // fire a cursedGagApplied call to auto gag and lock it.
                await _handler.ActivateCursedGag(selectedLootId, (GagLayer)availableSlot, DateTimeOffset.UtcNow.Add(lockTimeGag));
                Logger.LogInformation($"Cursed Loot Applied & Locked!", LoggerType.CursedLoot);

                if (_playerData.GlobalPerms is not null && _playerData.GlobalPerms.LiveChatGarblerActive)
                {
                    Mediator.Publish(new NotificationMessage("Chat Garbler", "LiveChatGarbler Is Active and you were just Gagged! " +
                        "Be cautious of chatting around strangers!", NotificationType.Warning));
                }

                return;
            }
            else
            {
                Logger.LogWarning("No Gag Slots Available, Rolling Again.");
                var inactiveSetsWithoutGags = _handler.InactiveItemsInPool.Where(x => !x.IsGag).ToList();
                // if there are no other items, return.
                if (inactiveSetsWithoutGags.Count <= 0)
                {
                    Logger.LogWarning("No Non-Gag Items are available to apply. Skipping.");
                    return;
                }

                var randomIndexNoGag = random.Next(0, inactiveSetsWithoutGags.Count);
                Logger.LogDebug("Selected Index: " + randomIndexNoGag + " (" + inactiveSetsWithoutGags[randomIndexNoGag].Name + ")", LoggerType.CursedLoot);
                selectedLootId = inactiveSetsWithoutGags[randomIndexNoGag].LootId;

                // Notify the client of their impending fate~
                Mediator.Publish(new NotifyChatMessage(new SeStringBuilder().AddItalics("As the coffer opens, cursed loot spills " +
                    "forth, binding you tightly in an inescapable snare of restraints!").BuiltString, NotificationType.Error));

                // generate the length they will be locked for:
                var lockTime = GetRandomTimeSpan(_handler.LowerLockLimit, _handler.UpperLockLimit, random);

                // Activate the cursed loot item.
                await _handler.ActivateCursedItem(selectedLootId, DateTimeOffset.UtcNow.Add(lockTime));
                Logger.LogInformation($"Cursed Loot Applied!", LoggerType.CursedLoot);

                // send event that we are having cursed loot applied.
                UnlocksEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
                return;
            }
        }
        else
        {
            selectedLootId = _handler.InactiveItemsInPool[randomIndex].LootId;

            // Notify the client of their impending fate~
            Mediator.Publish(new NotifyChatMessage(new SeStringBuilder().AddItalics("As the coffer opens, cursed loot spills " +
                "forth, binding you tightly in an inescapable snare of restraints!").BuiltString, NotificationType.Error));

            // generate the length they will be locked for:
            var lockTime = GetRandomTimeSpan(_handler.LowerLockLimit, _handler.UpperLockLimit, random);

            // Activate the cursed loot item.
            await _handler.ActivateCursedItem(selectedLootId, DateTimeOffset.UtcNow.Add(lockTime));
            Logger.LogInformation($"Cursed Loot Applied!", LoggerType.CursedLoot);

            // send event that we are having cursed loot applied.
            UnlocksEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
            return;
        }
    }

    public static TimeSpan GetRandomTimeSpan(TimeSpan min, TimeSpan max, Random random)
    {
        // if the min is greater than the max, make the timespan 1 second and return.
        if (min > max) return TimeSpan.FromSeconds(5);

        double minSeconds = min.TotalSeconds;
        double maxSeconds = max.TotalSeconds;
        double randomSeconds = random.NextDouble() * (maxSeconds - minSeconds) + minSeconds;
        return TimeSpan.FromSeconds(randomSeconds);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Cursed Dungeon Loot Service Started!");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Cursed Dungeon Loot Service Stopped!");
        return Task.CompletedTask;
    }
}
