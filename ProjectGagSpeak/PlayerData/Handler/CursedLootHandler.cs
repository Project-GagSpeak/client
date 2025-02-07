using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.Utils;

namespace GagSpeak.PlayerData.Handlers;

public class CursedLootHandler : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerData;
    private readonly AppearanceManager _appearanceHandler;

    public CursedLootHandler(ILogger<CursedLootHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ClientData playerData,
        AppearanceManager handler) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _appearanceHandler = handler;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckLockedItems());
    }

    private CursedLootStorage Data => _clientConfigs.CursedLootConfig.CursedLootStorage;

    public List<CursedItem> CursedItems => Data.CursedItems;
    public TimeSpan LowerLockLimit => Data.LockRangeLower;
    public TimeSpan UpperLockLimit => Data.LockRangeUpper;
    public int LockChance => Data.LockChance;


    public List<CursedItem> ActiveItems => Data.CursedItems
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .OrderBy(x => x.AppliedTime)
        .ToList();

    public List<CursedItem> ActiveItemsDecending => Data.CursedItems
    .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
    .OrderByDescending(x => x.AppliedTime)
    .ToList();

    public List<CursedItem> ItemsInPool => Data.CursedItems
        .Where(x => x.InPool)
        .ToList();

    // have a public accessor to return the list of all items in the pool that are not activeItems.
    public List<CursedItem> InactiveItemsInPool => Data.CursedItems
        .Where(x => x.InPool && x.AppliedTime == DateTimeOffset.MinValue)
        .ToList();

    public List<CursedItem> ItemsNotInPool => Data.CursedItems
        .Where(x => !x.InPool)
        .ToList();

    public void SetLowerLimit(TimeSpan time)
    {
        Data.LockRangeLower = time;
        Save();
    }

    public void SetUpperLimit(TimeSpan time)
    {
        Data.LockRangeUpper = time;
        Save();
    }

    public void SetLockChance(int chance)
    {
        Data.LockChance = chance;
        Save();
    }

    public string MakeUniqueName(string originalName)
        => _clientConfigs.EnsureUniqueName(originalName, CursedItems, item => item.Name);

    public void AddItem(CursedItem item)
        => _clientConfigs.AddCursedItem(item);

    public void RemoveItem(Guid idToRemove)
        => _clientConfigs.RemoveCursedItem(idToRemove);

    public async Task ActivateCursedItem(Guid idToActivate, DateTimeOffset releaseTimeUTC)
    {
        // activate it,
        _clientConfigs.ActivateCursedItem(idToActivate, releaseTimeUTC);
        // then apply it if valid.
        if (CursedItems.TryGetItem(x => x.LootId == idToActivate, out var cursedItem))
            await _appearanceHandler.CursedItemApplied(cursedItem);
    }

    public async Task ActivateCursedGag(Guid idToActivate, GagLayer gagLayer, DateTimeOffset releaseTimeUTC)
    {
        // activate it,
        _clientConfigs.ActivateCursedItem(idToActivate, releaseTimeUTC);
        // then apply it if valid.
        if (CursedItems.TryGetItem(x => x.LootId == idToActivate, out var cursedItem))
            await _appearanceHandler.CursedGagApplied(cursedItem, gagLayer, releaseTimeUTC);
    }

    public async Task DeactivateCursedItem(Guid idToDeactivate)
    {
        // deactivate it.
        _clientConfigs.DeactivateCursedItem(idToDeactivate);
        // then remove it if valid.
        if (CursedItems.TryGetItem(x => x.LootId == idToDeactivate, out var cursedItem))
            await _appearanceHandler.CursedItemRemoved(cursedItem, true);
    }

    public void Save() => _clientConfigs.SaveCursedLoot();

    private void CheckLockedItems()
    {
        if (ActiveItems.Count == 0) return;

        foreach (var item in ActiveItems)
        {
            if (item.ReleaseTime - DateTimeOffset.UtcNow <= TimeSpan.Zero)
                DeactivateCursedItem(item.LootId).ConfigureAwait(false);
        }
    }
}
