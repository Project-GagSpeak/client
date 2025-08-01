using CkCommons;
using CkCommons.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.Kinksters;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.State.Handlers;

/// <summary> 
///     Handles what happens to cursed loot when found, and provides helpers for object interaction.
/// </summary> 
public sealed class LootHandler
{
    private readonly ILogger<LootHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly KinksterManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly MainConfig _config;
    private readonly OnFrameworkService _frameworkUtils;

    /// <summary> Stores last interacted chestId so we dont keep spam opening the same chest. </summary>
    /// <remarks> This is static so we can send it to mediator calls and update it. </remarks>
    private uint _prevOpenedLootObjectId = 0;
    private Task? _openLootTask = null;

    public LootHandler(ILogger<LootHandler> logger, GagspeakMediator mediator, MainHub hub,
        KinksterManager pairs, GagRestrictionManager gags, RestrictionManager restrictions,
        CursedLootManager manager, MainConfig config, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _pairs = pairs;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _config = config;
        _frameworkUtils = frameworkUtils;
    }

    public bool LootTaskRunning => _openLootTask is not null && !_openLootTask.IsCompleted;

    /// <summary> If any cursed loot can even be applied at the moment. </summary>
    public bool CanApplyAnyLoot 
        => _config.Current.CursedLootPanel && MainHub.IsServerAlive && _manager.Storage.InactiveItemsInPool.Any();

    /// <summary> If the GameObject is a deep dungeon coffer or a treasure chest. </summary>
    public unsafe bool IsAnyTreasure(GameObject* obj)
        => obj->ObjectKind is ObjectKind.Treasure || IsDeepDungeonCoffer(obj);

    public unsafe bool IsObjectLastOpenedLoot(GameObject* obj)
        => obj->GetGameObjectId().ObjectId == _prevOpenedLootObjectId;

    public unsafe bool ObjectInLootInstance(uint gameObjId)
        => PlayerData.InSoloParty ? true : Loot.Instance()->Items.ToArray().Any(x => x.ChestObjectId == gameObjId);


    /// <summary>
    ///     Bronzes are already categorized as "Treasure" and need no changes to function with cursed loot.
    ///     Silver and gold chests across every deep dungeon and every language share all attributes aside from name.
    /// </summary>
    public unsafe bool IsDeepDungeonCoffer(GameObject* obj)
        => obj->ObjectKind is ObjectKind.EventObj
        && obj->SubKind is 0
        && obj->EventHandler->Info.EventId.Id is 983600
        && obj->EventHandler->Info.EventId.EntryId is 560
        && obj->EventHandler->Info.EventId.ContentId is EventHandlerContent.GimmickAccessor
        && NodeStringLang.DeepDungeonCoffer.Any(n => n.Equals(obj->NameString.ToString()));
    
    /// <summary> 
    ///     Handles opening a loot item to apply cursed loot!.
    /// </summary>
    /// <remarks> Calling this during a loot opening task running will fail. </remarks>
    public unsafe void OpenLootItem(GameObject* obj)
    {
        if (LootTaskRunning)
            return;

        // Handle Deep Dungeon Coffers.
        if (IsDeepDungeonCoffer(obj))
            _openLootTask = CheckDeepDungeonCoffers(obj->GetGameObjectId().ObjectId);
        else
        {
            // If in a party with other players, make sure we are the first to open it.
            if (PlayerData.PartySize is not 1)
            {
                foreach (var item in Loot.Instance()->Items)
                    if (item.ChestObjectId == obj->GetGameObjectId().ObjectId)
                    {
                        _logger.LogTrace("This treasure was already opened!", LoggerType.CursedItems);
                        return; // Early return to avoid assigning the task.
                    }
            }

            // We can open it, so do that.
            _logger.LogTrace("Attempting to open coffer, checking on next second", LoggerType.CursedItems);
            _openLootTask = CheckLootTables(obj->GetGameObjectId().ObjectId);
        }
    }

    private async Task CheckDeepDungeonCoffers(uint interactedObjectId)
    {
        await Task.Delay(1000);
        _logger.LogInformation("Checking tables!", LoggerType.CursedItems);
        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            if (interactedObjectId != _prevOpenedLootObjectId)
            {
                _logger.LogTrace("we just attempted to open a deep dungeon chest.", LoggerType.CursedItems);
                _prevOpenedLootObjectId = interactedObjectId;
                ApplyCursedLoot().ConfigureAwait(false);
            }
        }).ConfigureAwait(false);
    }

    public async Task CheckLootTables(uint interactedObjectId)
    {
        await Task.Delay(1000);
        _logger.LogInformation("Checking tables!", LoggerType.CursedItems);
        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            if (ObjectInLootInstance(interactedObjectId))
            {
                if (interactedObjectId != _prevOpenedLootObjectId)
                {
                    _logger.LogTrace("we just attempted to open a dungeon chest.", LoggerType.CursedItems);
                    _prevOpenedLootObjectId = interactedObjectId;
                    ApplyCursedLoot().ConfigureAwait(false);
                    return;
                }
            }
        }).ConfigureAwait(false);
    }

    private async Task ApplyCursedLoot()
    {
        // get the percent change to apply
        var percentChange = _manager.LockChance;
        var randomValue = new Random().Next(0, 101);
        if (_manager.LockChance <= 0 || randomValue > percentChange)
            return;

        // aquire the items we can apply to.
        var inactiveInPoolCnt = _manager.Storage.InactiveItemsInPool.Count;
        if (inactiveInPoolCnt <= 0)
            return;

        // Attempt a first application with gags in account.
        var randomIndex = new Random().Next(0, inactiveInPoolCnt);
        var chosenItem = _manager.Storage.InactiveItemsInPool[randomIndex];
        var lockTimeGag = Generators.GetRandomTimeSpan(_manager.LockRangeLower, _manager.LockRangeUpper);
        if (await HandleGagApplication(chosenItem, lockTimeGag))
        {
            _manager.ActivateCursedItem(chosenItem, DateTimeOffset.UtcNow.Add(lockTimeGag));
            _logger.LogInformation("Cursed Loot Applied!", LoggerType.CursedItems);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
            return;
        }

        // gag failed to apply, now we need to apply a valid restriction. Restriction must be unoccupied.
        // This currently allows for collar restrictions, dont let this happen.
        var itemsToRoll = _manager.Storage.InactiveItemsInPool
            .Where(item => item.RestrictionRef is IRestrictionItem r 
                && !_restrictions.ActiveItemsAll.ContainsKey(r.Identifier));
        // If no items, abort.
        if (!itemsToRoll.Any())
        {
            _logger.LogWarning("No items to apply restrictions to. Skipping.");
            return;
        }

        // Roll for the chosen item.
        randomIndex = new Random().Next(0, itemsToRoll.Count());
        chosenItem = itemsToRoll.ElementAt(randomIndex);
        if (await HandleRestrictionApplication(chosenItem, lockTimeGag))
        {
            _manager.ActivateCursedItem(chosenItem, DateTimeOffset.UtcNow.Add(lockTimeGag));
            _logger.LogInformation("Cursed Loot Applied!", LoggerType.CursedItems);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.CursedDungeonLootFound);
            return;
        }
    }

    private async Task<bool> HandleGagApplication(CursedItem item, TimeSpan lockTime)
    {
        if (_gags.ServerGagData is not { } gagData)
            return false;

        var Idx = gagData.FindFirstUnused();
        if (Idx is -1 || item.RestrictionRef is not GarblerRestriction gag)
            return false;

        // Apply the gag restriction to that player.
        _logger.LogInformation("Applying a cursed Gag Item (" + gag.GagType + ") to layer " + Idx, LoggerType.CursedItems);
        var itemToSet = new AppliedItem(DateTimeOffset.UtcNow.Add(lockTime), CursedLootType.Gag, null, gag.GagType);
        var result = await _hub.UserPushActiveLoot(new(_pairs.GetOnlineUserDatas(), _manager.Storage.ActiveIds.ToList(), item.Identifier, itemToSet));
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            _logger.LogInformation($"Cursed Loot Applied & Locked!", LoggerType.CursedItems);
            var message = new SeStringBuilder().AddItalics("As the coffer opens, cursed loot spills forth, silencing your mouth with a Gag now strapped on tight!").BuiltString;
            Svc.Chat.PrintError(message);

            if (OwnGlobals.Perms?.ChatGarblerActive ?? false)
                _mediator.Publish(new NotificationMessage("Chat Garbler", "LiveChatGarbler Is Active and you were just Gagged! Be cautious of chatting around strangers!", NotificationType.Warning));

            // Update the cursed items offset time.
            if (_manager.Storage.TryGetLoot(item.Identifier, out var loot))
                loot.ReleaseTime = DateTimeOffset.UtcNow.Add(lockTime);

            return true;
        }
        else
        {
            _logger.LogError($"Failed to apply gag restriction to player. [{result.ErrorCode}]");
            return false;
        }
    }

    private async Task<bool> HandleRestrictionApplication(CursedItem cursedItem, TimeSpan lockTime)
    {
        if (_restrictions.ActiveItems is not { } restrictionData)
            return false;

        // If the attached restriction item is already in the container of active restrictions, return false.
        if (restrictionData.Values.Any(x => x.Identifier == cursedItem.Identifier))
            return false;

        // Get the first unused restriction index.
        if (cursedItem.RestrictionRef is not IRestrictionItem restriction)
            return false;

        // Apply the restriction to that player.
        _logger.LogInformation($"Applying a cursed Item [{cursedItem.Label}] to you!", LoggerType.CursedItems);
        var itemToSet = new AppliedItem(DateTimeOffset.UtcNow.Add(lockTime), CursedLootType.Restriction, restriction.Identifier);
        var newInfo = new PushClientActiveLoot(_pairs.GetOnlineUserDatas(), _manager.Storage.ActiveIds.ToList(), cursedItem.Identifier, itemToSet);
        var result = await _hub.UserPushActiveLoot(newInfo);
        if (await _hub.UserPushActiveLoot(newInfo) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            _logger.LogError("Failed to apply restriction to player. Error Code: " + result.ErrorCode);
            return false;
        }
        else
        {
            Svc.Chat.PrintError(new SeStringBuilder()
                .AddItalics("As the coffer opens, cursed loot spills forth, binding you in an inescapable restraint!")
                .BuiltString);

            // Update the items release time if successful.
            if (_manager.Storage.TryGetLoot(cursedItem.Identifier, out var loot))
                loot.ReleaseTime = DateTimeOffset.UtcNow.Add(lockTime);
            return true;
        }
    }
}
