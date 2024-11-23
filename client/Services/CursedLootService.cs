using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using OtterGui;
using System.Numerics;
using System.Security.Cryptography;

namespace GagSpeak.Services;
public class CursedLootService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly GagManager _gagManager;
    private readonly PlayerCharacterData _playerData;
    private readonly CursedLootHandler _handler;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;

    // SHOULD fire whenever we interact with any object thing.
    internal Hook<TargetSystem.Delegates.InteractWithObject> ItemInteractedHook;

    public CursedLootService(ILogger<CursedLootService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, GagManager gagManager,
        PlayerCharacterData playerData, CursedLootHandler handler, ClientMonitorService clientService, 
        OnFrameworkService frameworkUtils, IGameInteropProvider interop) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _gagManager = gagManager;
        _playerData = playerData;
        _handler = handler;
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
            if(obj->EventHandler is not null)
            {
                Logger.LogTrace("Object EventHandler ID: " + obj->EventHandler->Info.EventId.Id);
                Logger.LogTrace("Object EventHandler Entry ID: " + obj->EventHandler->Info.EventId.EntryId);
                Logger.LogTrace("Object EventHandler Content Id: " + obj->EventHandler->Info.EventId.ContentId);
            }

            // dont bother if cursed dungeon loot isnt enabled, or if there are no inactive items in the pool.
            if (!_clientConfigs.GagspeakConfig.CursedDungeonLoot || !_handler.InactiveItemsInPool.Any() || MainHub.IsOnUnregistered)
                return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);

            // if we are forced to stay, we should block any interactions with objects.
            if (obj->ObjectKind is not FFXIVClientStructs.FFXIV.Client.Game.Object.ObjectKind.Treasure)
            {
                Logger.LogTrace("Interacted with GameObject that was not a Treasure Chest.", LoggerType.CursedLoot);
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

            // Make sure we are opening it. If we were not the first, it will exist in here.
            if (_clientService.PartySize is not 1)
            {
                foreach (var item in Loot.Instance()->Items)
                {
                    // Perform an early return if not valie.
                    if (item.ChestObjectId == obj->GetGameObjectId().ObjectId)
                    {
                        Logger.LogTrace("This treasure was already opened!", LoggerType.CursedLoot);
                        return ItemInteractedHook.Original(thisPtr, obj, checkLineOfSight);
                    }
                }
            }

            // This is a valid new chest, so open it.
            Logger.LogTrace("Attempting to open coffer, checking loot instance on next second", LoggerType.CursedLoot);
            _openTreasureTask = CheckLootTables(obj->GetGameObjectId().ObjectId);
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
                    bool valid = _clientService.PartySize is 1 ? true : Loot.Instance()->Items.ToArray().Any(x => x.ChestObjectId == objectInteractedWith);
                    if (valid && objectInteractedWith != LastOpenedTreasureId)
                    {
                        Logger.LogTrace("One of the loot items is the nearest treasure and we just previously attempted to open one.", LoggerType.CursedLoot);
                        LastOpenedTreasureId = objectInteractedWith;
                        ApplyCursedLoot().ConfigureAwait(false);
                    }
                    else
                    {
                        Logger.LogTrace("No loot items are the nearest treasure, or we have already opened this chest.", LoggerType.CursedLoot);
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

        // Calculate if we should apply it or not. If we fail to roll a success, return.
        Random random = new Random();
        int randomValue = random.Next(0, 101);
        if (randomValue > percentChange) return;

        // Obtain a randomly selected cursed item from the inactive items in the pool.
        var enabledPoolCount = _handler.InactiveItemsInPool.Count;
        if(enabledPoolCount <= 0)
        {
            Logger.LogWarning("No Cursed Items are available to apply. Skipping.", LoggerType.CursedLoot);
            return;
        }

        Guid selectedLootId = Guid.Empty;
        var randomIndex = random.Next(0, enabledPoolCount);
        Logger.LogDebug("Randomly selected index ["+randomIndex+"] (between 0 and " + enabledPoolCount + ") for (" + _handler.InactiveItemsInPool[randomIndex].Name + ")", LoggerType.CursedLoot);
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
                // apply the gag via the gag manager at the available slot we found.
                await _handler.ActivateCursedItem(selectedLootId, DateTimeOffset.UtcNow.Add(lockTimeGag), (GagLayer)availableSlot);
                // Add a small delay to avoid race conditions (idk a better way to failsafe this yet)
                await Task.Delay(500);
                // lock the gag.
                var padlockData = new PadlockData()
                {
                    Layer = (GagLayer)availableSlot,
                    PadlockType = Padlocks.MimicPadlock,
                    Timer = DateTimeOffset.UtcNow.Add(lockTimeGag),
                    Assigner = MainHub.UID
                };
                _gagManager.PublishLockApplied((GagLayer)availableSlot, Padlocks.MimicPadlock, "", DateTimeOffset.UtcNow.Add(lockTimeGag), MainHub.UID);
                Logger.LogInformation($"Cursed Loot Applied & Locked!", LoggerType.CursedLoot);
                // send event that we are having cursed loot applied.
                UnlocksEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);

                if (!_playerData.CoreDataNull && _playerData.GlobalPerms!.LiveChatGarblerActive)
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

    // Encase we ever want to implement the Gag Stuff i guess.
    public static string GenerateRandomString(int length, string? allowableChars = null)
    {
        if (string.IsNullOrEmpty(allowableChars))
            allowableChars = @"ABCDEFGHJKLMNPQRSTUVWXYZ0123456789";

        // Generate random data
        var rnd = RandomNumberGenerator.GetBytes(length);

        // Generate the output string
        var allowable = allowableChars.ToCharArray();
        var l = allowable.Length;
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = allowable[rnd[i] % l];

        return new string(chars);
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
