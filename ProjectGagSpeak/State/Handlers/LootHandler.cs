using CkCommons;
using CkCommons.Helpers;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;

namespace GagSpeak.State.Handlers;

/// <summary> 
///     Handles what happens to cursed loot when found, and provides helpers for object interaction.
/// </summary> 
public sealed class LootHandler
{
    private readonly ILogger<LootHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly CursedLootManager _manager;
    private readonly VisualStateListener _visuals;
    private readonly MainConfig _config;
    private readonly DistributorService _dds;

    /// <summary> Stores last interacted chestId so we dont keep spam opening the same chest. </summary>
    /// <remarks> This is static so we can send it to mediator calls and update it. </remarks>
    private uint _prevOpenedLootObjectId = 0;
    private Task? _openLootTask = null;

    public LootHandler(ILogger<LootHandler> logger, GagspeakMediator mediator, GagRestrictionManager gags,
        RestrictionManager restrictions, CursedLootManager manager, VisualStateListener visuals,
        MainConfig config, DistributorService dds)
    {
        _logger = logger;
        _mediator = mediator;
        _gags = gags;
        _restrictions = restrictions;
        _manager = manager;
        _visuals = visuals;
        _config = config;
        _dds = dds;
    }

    public bool LootTaskRunning => _openLootTask is not null && !_openLootTask.IsCompleted;

    /// <summary> If any cursed loot can even be applied at the moment. </summary>
    public bool CanApplyAnyLoot
        => _config.Current.CursedLootUI && MainHub.IsServerAlive && _manager.Storage.ActiveUnappliedLoot.Any();

    /// <summary> If the GameObject is a deep dungeon coffer or a treasure chest. </summary>
    public unsafe bool IsAnyTreasure(GameObject* obj)
        => obj->ObjectKind is ObjectKind.Treasure || IsDeepDungeonCoffer(obj);

    public unsafe bool IsObjectLastOpenedLoot(GameObject* obj)
        => obj->GetGameObjectId().ObjectId == _prevOpenedLootObjectId;

    public unsafe bool ObjectInLootInstance(uint gameObjId)
        => PlayerData.InSoloParty || Loot.Instance()->Items.ToArray().Any(x => x.ChestObjectId == gameObjId);


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
    ///     Handles opening a loot item to apply cursed loot!. <para />
    ///     Expects that all of the above helper's returned true (where nessisary).
    /// </summary>
    /// <remarks> Calling this during a loot opening task running will fail. </remarks>
    public unsafe void OpenLootItem(GameObject* obj)
    {
        if (LootTaskRunning)
        {
            _logger.LogTrace("Loot task already running, skipping open attempt.", LoggerType.CursedItems);
            return;
        }

        // Handle Deep Dungeon Coffers. (or i suppose any non-standard (non-treasure type) chests.
        if (IsDeepDungeonCoffer(obj))
        {
            _logger.LogTrace("Attempting to open deep dungeon coffer", LoggerType.CursedItems);
            _openLootTask = CheckDeepDungeonCoffers(obj->GetGameObjectId().ObjectId);
        }
        // Handle normal coffers.
        else
        {
            _logger.LogTrace("Attempting to open treasure chest.", LoggerType.CursedItems);
            var objId = obj->GetGameObjectId().ObjectId;
            // If in a party with other players, make sure we are the first to open it.
            if (ObjectInLootInstance(objId))
            {
                _logger.LogTrace("Chest was already opened by someone else! Skipping.", LoggerType.CursedItems);
                return;
            }

            _logger.LogTrace("we just attempted to open a dungeon chest.", LoggerType.CursedItems);
            _prevOpenedLootObjectId = objId;
            _openLootTask = ApplyCursedLoot();
        }
    }

    private async Task CheckDeepDungeonCoffers(uint interactedObjectId)
    {
        _logger.LogTrace("we just attempted to open a deep dungeon chest.", LoggerType.CursedItems);
        _prevOpenedLootObjectId = interactedObjectId;
        await ApplyCursedLoot().ConfigureAwait(false);
    }

    private async Task ApplyCursedLoot()
    {
        // run our first roll, return if not in range.
        var roll = new Random().Next(1, 101); // 0,101 will return 0 to 100 inclusive, so 1,101 is what you want for 1-100 inclusive (and for a config value of 5% to not actually be 5.9%[6/101] chance)
        _logger.LogDebug($"Cursed Loot Roll: {roll} vs Chance: {_manager.LockChance}", LoggerType.CursedItems);
        if (roll > _manager.LockChance)
            return;

        // Return if there is nothing to apply.to.
        var validItems = _manager.Storage.ActiveUnappliedLoot;
        if (validItems.Count <= 0)
            return;

        // Select a random item index to apply.
        var chosenIdx = new Random().Next(0, validItems.Count);
        var chosen = validItems[chosenIdx];

        // Calculate the timespan to apply the lock for.
        var lockTime = Generators.GetRandomTimeSpan(_manager.LockRangeLower, _manager.LockRangeUpper);

        // If the chosen item is a gag, and there is space to apply one, apply the gag item.
        // (This will fail if it is a restriction item that was selected).
        if (chosen is CursedGagItem cg && _gags.ServerGagData?.FindFirstUnused() != -1)
        {
            if (await ApplyCursedGag(cg, lockTime))
                return;
        }
        else if (chosen is CursedRestrictionItem cri)
        {
            if (await ApplyCursedRestriction(cri, lockTime))
                return;
        }
        else
        {
            _logger.LogError("Chosen cursed item was neither gag nor restriction", LoggerType.CursedItems);
            return;
        }
    }

    private AppliedItem FromGag(GarblerRestriction gag, TimeSpan lockTime)
        => new(DateTimeOffset.UtcNow.Add(lockTime), CursedLootType.Gag, null, gag.GagType);

    private AppliedItem FromRestriction(RestrictionItem restriction, TimeSpan lockTime)
        => new(DateTimeOffset.UtcNow.Add(lockTime), CursedLootType.Restriction, restriction.Identifier);

    private async Task<bool> ApplyCursedGag(CursedGagItem item, TimeSpan lockTime)
    {
        _logger.LogInformation($"Applying a cursed Gag to an open gagslot!", LoggerType.CursedItems);
        if (await _dds.PushActiveCursedLoot(_manager.Storage.AppliedLootIds.ToList(), item.Identifier, FromGag(item.RefItem, lockTime)) is not { } res)
            return false;

        // It was successful, so we can now update our visuals and inform the player.
        await _visuals.CursedGagApplied(item, res.GagLayer!.Value, DateTimeOffset.UtcNow.Add(lockTime)).ConfigureAwait(false);
        return true;
    }

    private async Task<bool> ApplyCursedRestriction(CursedRestrictionItem item, TimeSpan lockTime)
    {
        _logger.LogInformation($"Applying a cursed Item [{item.Label}] to you!", LoggerType.CursedItems);
        if (await _dds.PushActiveCursedLoot(_manager.Storage.AppliedLootIds.ToList(), item.Identifier, FromRestriction(item.RefItem, lockTime)) is not { } res)
            return false;

        // Apply the item!
        await _visuals.CursedItemApplied(item, DateTimeOffset.UtcNow.Add(lockTime)).ConfigureAwait(false);
        return true;
    }
}
