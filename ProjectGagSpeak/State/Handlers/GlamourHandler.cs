using CkCommons.Classes;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;
using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Handlers;
public class GlamourHandler
{
    private readonly ILogger<GlamourHandler> _logger;
    private readonly IpcCallerGlamourer _ipc;
    private readonly GlamourCache _cache;
    private readonly ItemService _items;
    private readonly OnFrameworkService _frameworkUtils;

    private SemaphoreSlim _applySlim = new SemaphoreSlim(1, 1);
    private IpcBlockReason _ipcBlocker = IpcBlockReason.None;

    public GlamourHandler(ILogger<GlamourHandler> logger, IpcCallerGlamourer ipc,
        GlamourCache cache, ItemService items, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _ipc = ipc;
        _cache = cache;
        _items = items;
        _frameworkUtils = frameworkUtils;
    }

    public IpcBlockReason BlockIpcCalls => _ipcBlocker;
    private bool ActorCacheIsEmpty => _cache.LastUnboundState.Equals(GlamourActorState.Empty);

    // Invoked by the EquipGearsetInternal detour.
    public void OnEquipGearsetInternal(int gearsetId, byte glamourPlateId)
    {
        _logger.LogDebug($"EquipGearsetInternal for gearsetId {gearsetId} with plateId {glamourPlateId} occured!" +
            "Blocking any further OnStateChanged calls until gearset application finishes!");
        _ipcBlocker |= IpcBlockReason.Gearset;
    }

    // Invoked when the OnStateFinailization type is Gearset.
    public void OnEquipGearsetFinalized()
        => _ipcBlocker &= ~IpcBlockReason.Gearset;

    /// <summary> Add a single GlamourSlot to the GlamourCache for the key. </summary>
    public bool TryAddGlamourToCache(CombinedCacheKey key, GlamourSlot? glamour)
    {
        if (glamour is null)
            return false;
        return _cache.AddGlamour(key, glamour);
    }

    /// <summary> Add Multiple GlamourSlots to the GlamourCache for the key. </summary>
    public bool TryAddGlamourToCache(CombinedCacheKey key, IEnumerable<GlamourSlot> glamours)
    {
        if (glamours is null || !glamours.Any())
            return false;
        return _cache.AddGlamour(key, glamours);
    }

    /// <summary> Remove a single key from the GlamourCache. </summary>
    public bool TryRemGlamourFromCache(CombinedCacheKey key)
        => _cache.RemoveGlamour(key);

    /// <summary> Remove Multiple keys from the GlamourCache. </summary>
    public bool TryRemGlamourFromCache(IEnumerable<CombinedCacheKey> keys)
        => _cache.RemoveGlamour(keys);

    /// <summary>
    ///     For the appropriate <paramref name="metaIdx"/> metaState, add a key-value
    ///     pair with <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    public bool TryAddMetaToCache(CombinedCacheKey key, MetaIndex metaIdx, TriStateBool value)
        => _cache.AddMeta(key, metaIdx, value);

    /// <summary>
    ///     Adds <paramref name="meta"/>'s <see cref="TriStateBool"/>'s to all metaState caches,
    ///     adding the key-value pair at key <paramref name="key"/>.
    /// </summary>
    public bool TryAddMetaToCache(CombinedCacheKey key, MetaDataStruct meta)
        => _cache.AddMeta(key, meta);

    /// <summary> Removes a key from the Meta Caches. </summary>
    public bool TryRemMetaFromCache(CombinedCacheKey key)
        => _cache.RemoveMeta(key);

    /// <summary> Removes multiple keys from the Meta Caches. </summary>
    public bool TryRemMetaFromCache(IEnumerable<CombinedCacheKey> keys)
        => _cache.RemoveMeta(keys);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing Glamour Cache.");
        _cache.ClearCaches();
        await UpdateCaches();
    }

    /// <summary> Use this as your go-to update method for everything outside of IPC calls. </summary>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task UpdateCaches()
    {
        _logger.LogDebug("Updating Glamourer Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Run both operations in parallel.
            await Task.WhenAll(
                UpdateGlamourInternal(false, true),
                UpdateMetaInternal(true)
            );
            _logger.LogInformation($"Processed Cache Updates Successfully!");
        });
    }


    /// <summary> Use this after any glamour Finalization type occurs. </summary>
    /// <param name="storeProfile"> If true, will force the cache to be applied before updating. </param>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task ReapplyCaches(bool storeProfile = true)
    {
        _logger.LogDebug("Reapplying Glamourer Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Run both operations in parallel.
            await Task.WhenAll(
                ApplyGlamourCache(storeProfile),
                ApplyMetaCache()
            );
            _logger.LogInformation($"Reapplied Cache Updates Successfully!");
        });
    }

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateGlamourCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateGlamourInternal(false, reapply));

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateMetaCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateMetaInternal(reapply));

    /// <summary>
    ///     Updates the Final Glamour Cache, and then applies the visual updates.
    /// </summary>
    private async Task UpdateGlamourInternal(bool forceCacheCall, bool reapply)
    {
        // Update the final cache. `removedSlots` contains slots that are no longer restricted after the change.
        if (_cache.UpdateFinalGlamourCache(out var removedSlots))
        {
            _logger.LogDebug($"Final Glamour Cache was updated!", LoggerType.VisualCache);
            if (removedSlots.Any())
                await RestoreAndReapply(forceCacheCall, removedSlots);
            else
                await ApplyGlamourCache(forceCacheCall);
            return;
        }
        else if (reapply)
        {
            _logger.LogDebug("Reapplying Glamour Cache", LoggerType.VisualCache);
            await ApplyGlamourCache(forceCacheCall);
            return;
        }
        // No Change
        _logger.LogTrace("No change in Final Glamour Cache.", LoggerType.VisualCache);
    }

    /// <summary>
    ///     Updates the Final Meta Cache, and then applies the visual updates.
    /// </summary>
    private async Task UpdateMetaInternal(bool reapply)
    {
        // Update the final cache. `removedSlots` contains slots that are no longer restricted after the change.
        if (_cache.UpdateFinalMetaCache())
        {
            _logger.LogDebug($"Final MetaState Cache was updated!", LoggerType.VisualCache);
            await ApplyMetaCache();
            return;
        }
        else if (reapply)
        {
            _logger.LogDebug("Reapplying MetaState Cache", LoggerType.VisualCache);
            await ApplyMetaCache();
            return;
        }
        // No Change
        _logger.LogTrace("No change in Final MetaState Cache.", LoggerType.VisualCache);
    }

    /// <summary> 
    ///     Restore slots no longer present in _finalGlamour from <see cref="_cache"/>, then reapplies what is still active.
    /// </summary>
    private async Task RestoreAndReapply(bool forceCacheCall, IEnumerable<EquipSlot> slotsToRestore)
    {
        await Task.WhenAll(slotsToRestore
            .Select(slot =>
            {
                if (_cache.LastUnboundState.RecoverSlot(slot, out var itemId, out var stain, out var stain2))
                    return _ipc.SetClientItemSlot((ApiEquipSlot)slot, itemId, [stain, stain2], 0);
                else
                {
                    _logger.LogWarning($"Failed to restore slot {slot}, no data found in Glamourer cache.", LoggerType.IpcGlamourer);
                    return Task.CompletedTask;
                }
            }));
        _logger.LogDebug($"Restored Glamourer Slots to last applied base value.", LoggerType.IpcGlamourer);

        // Now reapply the cache.
        _logger.LogDebug("Reapplying Glamourer Cache", LoggerType.IpcGlamourer);
        await ApplyGlamourCache(forceCacheCall);
    }

    /// <summary> 
    ///     Apples the FinalGlamour from <see cref="_cache"/> to the Client. 
    /// </summary>
    private async Task ApplyGlamourCache(bool cacheBeforeApply)
    {
        if (cacheBeforeApply || ActorCacheIsEmpty)
            CacheActorState();

        // configure the tasks to run asynchronously.
        await Task.WhenAll(_cache.FinalGlamour
            .Select(slot =>
            {
                var equipSlot = (ApiEquipSlot)slot.Key;
                var gameItem = slot.Value.GameItem;
                var gameStain1 = slot.Value.GameStain.Stain1;
                var gameStain2 = slot.Value.GameStain.Stain2;
                // The whole 'Overlay Mode' logic was already handled in the listener, so dont worry about it here and just set.
                _logger.LogTrace($"Correcting slot {equipSlot} to ensure helplessness.", LoggerType.IpcGlamourer);
                return _ipc.SetClientItemSlot(equipSlot, gameItem.Id.Id, [gameStain1.Id, gameStain2.Id], 0);
            }));
        _logger.LogTrace("Applied Active Slots to Glamour", LoggerType.IpcGlamourer);
    }

    /// <summary>
    ///     Apples the _finalMeta from the <see cref="_cache"/> Cache to the Client.
    /// </summary>
    private async Task ApplyMetaCache()
    {
        await _ipc.SetMetaStates(_cache.FinalMeta.OnFlags(), true);
        await _ipc.SetMetaStates(_cache.FinalMeta.OffFlags(), false);
        //_logger.LogDebug("Updated Meta States", LoggerType.IpcGlamourer);
    }

    /// <summary>
    ///     Caches the latest state from Glamourer IPC to store the latest unbound state.
    /// </summary>
    public void CacheActorState()
    {
        _logger.LogWarning("Caching latest state from Glamourer IPC.", LoggerType.IpcGlamourer);
        var latestState = _ipc.GetClientGlamourerState();
        if (latestState != null)
        {
            var latestUnboundCopy = GlamourActorState.Clone(_cache.LastUnboundState);
            latestUnboundCopy.UpdateEquipment(latestState, _items, _cache.FinalGlamour.ToDictionary(x => x.Key, x => x.Value.GameItem));
            _cache.CacheUnboundState(latestUnboundCopy);
        }
        else
        {
            _logger.LogDebug("Failed to cache Glamourer state, latest state was null.", LoggerType.IpcGlamourer);
            _cache.CacheUnboundState(new GlamourActorState(latestState));
        }
    }

    /// <summary>
    ///     Ensures that all other calls from Glamourer are blocked during a execution.
    /// </summary>
    /// <remarks> This is nessisary to avoid deadlocks and infinite looping calls.</remarks>
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        // First, acquire the semaphore.
        await _applySlim.WaitAsync();

        // Now that we've acquired it, update block reason.
        _ipcBlocker |= IpcBlockReason.SemaphoreTask;
        _logger.LogDebug($"Now running Semaphore. Blockers: {_ipcBlocker}", LoggerType.IpcGlamourer);

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            // Schedule the re-enabling of glamour change events using RunOnFrameworkTickDelayed to offset Glamourer.
            await _frameworkUtils.RunOnFrameworkTickDelayed(() =>
            {
                _ipcBlocker &= ~IpcBlockReason.SemaphoreTask;
                _logger.LogDebug($"Releasing Semaphore Wait, Remaining Blockers: {_ipcBlocker.ToString()}", LoggerType.IpcGlamourer);
            }, 1);
            // Release the slim, allowing further execution.
            _applySlim.Release();
        }
    }
}
