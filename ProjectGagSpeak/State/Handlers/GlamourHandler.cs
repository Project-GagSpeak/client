 using CkCommons.Classes;
using GagSpeak.Interop;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;
using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.State.Handlers;

public class GlamourHandler
{
    private readonly ILogger<GlamourHandler> _logger;
    private readonly IpcCallerGlamourer _ipc;
    private readonly GlamourCache _cache;

    private SemaphoreSlim _applySlim = new SemaphoreSlim(1, 1);
    private IpcBlockReason _ipcBlocker = IpcBlockReason.None;

    public GlamourHandler(ILogger<GlamourHandler> logger, IpcCallerGlamourer ipc, GlamourCache cache)
    {
        _logger = logger;
        _ipc = ipc;
        _cache = cache;
    }

    public IpcBlockReason BlockIpcCalls => _ipcBlocker;
    public bool ActorCacheIsEmpty => _cache.LastUnboundState.IsEmpty;

    // Invoked by the EquipGearsetInternal detour.
    public void OnEquipGearsetInternal(int gearsetId, byte glamourPlateId)
    {
        _logger.LogDebug($"EquipGearsetInternal for gearsetId {gearsetId} with plateId {glamourPlateId} occured!" +
            "Blocking any further OnStateChanged calls until gearset application finishes!");
        _ipcBlocker |= IpcBlockReason.Gearset;
    }

    public void OnAppliedGlamourPlate(uint glamourPlateIdx)
    {
        _logger.LogDebug($"Applied Glamour Plate with index {glamourPlateIdx}.");
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

    public bool TryUpdateGlamourDyes(CombinedCacheKey key, EquipSlot slot, byte dye1, byte dye2)
        => _cache.UpdateGlamourDyes(key, slot, new(dye1, dye2));

    public bool TryUpdateGlamourDyes(CombinedCacheKey key, EquipSlot slot, StainIds newDyes)
        => _cache.UpdateGlamourDyes(key, slot, newDyes);

    /// <summary> Remove a single key from the GlamourCache. </summary>
    public bool TryRemGlamourFromCache(CombinedCacheKey key)
        => _cache.RemoveGlamour(key);

    /// <summary> Remove Multiple keys from the GlamourCache. </summary>
    public bool TryRemGlamourFromCache(IEnumerable<CombinedCacheKey> keys)
        => _cache.RemoveGlamour(keys);

    /// <summary>
    ///   For the appropriate <paramref name="metaIdx"/> metaState, add a key-value
    ///   pair with <paramref name="key"/> and <paramref name="value"/>.
    /// </summary>
    public bool TryAddMetaToCache(CombinedCacheKey key, MetaIndex metaIdx, TriStateBool value)
        => _cache.AddMeta(key, metaIdx, value);

    /// <summary>
    ///   Adds <paramref name="meta"/>'s <see cref="TriStateBool"/>'s to all metaState caches,
    ///   adding the key-value pair at key <paramref name="key"/>.
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
        // After we clear out the cache and update them to their recovered state we should clear the unbound cache out
        _cache.CacheUnboundState(GlamourActorState.Empty);
    }

    /// <summary> Use this as your go-to update method for everything outside of IPC calls. </summary>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task UpdateCaches()
    {
        _logger.LogDebug("Updating Glamourer Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Cached once up front, so neither calls write the unbound state.
            var live = CacheActorState(false);
            // Run both operations in parallel.
            await Task.WhenAll(
                UpdateMetaInternal(live, true),
                UpdateGlamourInternal(live, true)
            );
            _logger.LogInformation($"Processed Cache Updates Successfully!");
        });
        _logger.LogDebug("Finished Updating Glamourer Caches.");
    }


    /// <summary> Use this after any glamour Finalization type occurs. </summary>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task ReapplyAllCaches()
    {
        _logger.LogDebug("Reapplying Glamourer Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            var live = CacheActorState(true);
            // Run both operations in parallel.
            await Task.WhenAll(
                ApplyGlamourCache(live),
                ApplyMetaCache(live)
            );
            _logger.LogInformation($"Reapplied Cache Updates Successfully!");
        });
    }

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateGlamourCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateGlamourInternal(CacheActorState(false), reapply));

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateMetaCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateMetaInternal(CacheActorState(true), reapply));

    /// <summary>
    ///   Updates the Final Glamour Cache, and then applies the visual updates.
    /// </summary>
    private async Task UpdateGlamourInternal(JObject? live, bool reapply)
    {
        // Update the final cache. `removedSlots` contains slots that are no longer restricted after the change.
        if (_cache.UpdateFinalGlamourCache(out var removedSlots, out var changedSlots))
        {
            _logger.LogDebug($"Final Glamour Cache was updated!", LoggerType.VisualCache);
            if (removedSlots.Any())
                await RestoreAndReapply(live, removedSlots, changedSlots);
            else
                await ApplyGlamourCache(live, changedSlots);
            return;
        }
        else if (reapply)
        {
            _logger.LogDebug("Reapplying Glamour Cache", LoggerType.VisualCache);
            await ApplyGlamourCache(live, changedSlots);
            return;
        }
        // No Change
        _logger.LogTrace("No change in Final Glamour Cache.", LoggerType.VisualCache);
    }

    /// <summary>
    ///   Updates the Final Meta Cache, and then applies the visual updates.
    /// </summary>
    private async Task UpdateMetaInternal(JObject? live, bool reapply)
    {
        // What we were enforcing before the update, so we only apply states we are enforcing.
        var wasForcing = _cache.FinalMeta;
        // Update the final cache. `noHat`/`noVisor`/`noWeapon` mean nothing binds that state anymore.
        if (_cache.UpdateFinalMetaCache(out var noHat, out var noVisor, out var noWeapon, out var changedFlags))
        {
            _logger.LogDebug($"Final MetaState Cache was updated!", LoggerType.VisualCache);
            var releasedHat = noHat && wasForcing.Headgear.HasValue;
            var releasedVisor = noVisor && wasForcing.Visor.HasValue;
            var releasedWeapon = noWeapon && wasForcing.Weapon.HasValue;
            if (releasedHat || releasedVisor || releasedWeapon)
                await RestoreMetaAndReapply(live, releasedHat, releasedVisor, releasedWeapon, changedFlags);
            else
                await ApplyMetaCache(live, changedFlags);
            return;
        }
        else if (reapply)
        {
            _logger.LogDebug("Reapplying MetaState Cache", LoggerType.VisualCache);
            await ApplyMetaCache(live, changedFlags);
            return;
        }
        // No Change
        _logger.LogTrace("No change in Final MetaState Cache.", LoggerType.VisualCache);
    }

    /// <summary> 
    ///   Restore slots no longer present in _finalGlamour from <see cref="_cache"/>, then reapplies what is still active.
    /// </summary>
    private async Task RestoreAndReapply(JObject? live, IEnumerable<EquipSlot> slotsToRestore, IReadOnlySet<EquipSlot> changedSlots)
    {
        await Task.WhenAll(slotsToRestore
            .Select(slot =>
            {
                if (!_cache.LastUnboundState.RecoverSlot(slot, out var itemId, out var stain, out var stain2))
                {
                    _logger.LogWarning($"Failed to restore slot {slot}, no data found in Glamourer cache.", LoggerType.IpcGlamourer);
                    return Task.CompletedTask;
                }
                // if already showing the unbound value, no need to set it again.
                if (SlotMatchesLive(live, slot, itemId, stain, stain2))
                    return Task.CompletedTask;

                return _ipc.SetClientItemSlot((ApiEquipSlot)slot, itemId, [stain, stain2], 0);
            }));
        _logger.LogDebug($"Restored Glamourer Slots to last applied base value.", LoggerType.IpcGlamourer);
        // Now reapply the cache.
        _logger.LogDebug("Reapplying Glamourer Cache", LoggerType.IpcGlamourer);
        await ApplyGlamourCache(live, changedSlots);
    }

    /// <summary> 
    ///   Apples the FinalGlamour from <see cref="_cache"/> to the Client. 
    /// </summary>
    /// <remarks> Only differences are set. </remarks>
    private async Task ApplyGlamourCache(JObject? live, IReadOnlySet<EquipSlot>? changedSlots = null)
    {
        // configure the tasks to run asynchronously.
        await Task.WhenAll(_cache.FinalGlamour
            .Where(slot => (changedSlots?.Contains(slot.Key) ?? false) || !SlotMatchesLive(live, slot.Key, slot.Value))
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

    /// <summary> Restores the metastates we were forcing back to their last unbound value. </summary>
    private async Task RestoreMetaAndReapply(JObject? live, bool restoreHat, bool restoreVisor, bool restoreWeapon, MetaFlag changedFlags)
    {
        GlamourActorState.TryReadMeta(live, out var liveMeta);
        var unbound = _cache.LastUnboundState.MetaStates;

        // If we are restoring the hat, visor or weapon, we need to restore the slots.
        if (restoreHat && (bool?)unbound.Headgear is { } newVal && !liveMeta.Headgear.Equals(unbound.Headgear))
            await _ipc.SetMetaStates(MetaFlag.HatState, newVal);
        if (restoreVisor && (bool?)unbound.Visor is { } newVal2 && !liveMeta.Visor.Equals(unbound.Visor))
            await _ipc.SetMetaStates(MetaFlag.VisorState, newVal2);
        if (restoreWeapon && (bool?)unbound.Weapon is { } newVal3 && !liveMeta.Weapon.Equals(unbound.Weapon))
            await _ipc.SetMetaStates(MetaFlag.WeaponState, newVal3);

        _logger.LogDebug($"Restored Meta Slots to last applied base value.", LoggerType.IpcGlamourer);

        // Now reapply the states
        _logger.LogDebug("Reapplying Meta Cache", LoggerType.IpcGlamourer);
        await ApplyMetaCache(live, changedFlags);
    }

    /// <summary>
    ///   Apples the _finalMeta from the <see cref="_cache"/> Cache to the Client.
    /// </summary>
    /// <remarks> Only differences are pushed. </remarks>
    private async Task ApplyMetaCache(JObject? live, MetaFlag changedFlags = 0)
    {
        var final = _cache.FinalMeta;
        // Without a live state to compare against we cannot tell what is already correct, so push everything.
        var toWrite = GlamourActorState.TryReadMeta(live, out var liveMeta)
            ? changedFlags | StaleFlags(final, liveMeta)
            : changedFlags | final.OnFlags() | final.OffFlags();

        var writeOn = final.OnFlags() & toWrite;
        var writeOff = final.OffFlags() & toWrite;

        if (writeOn is not 0)
            await _ipc.SetMetaStates(writeOn, true);
        if (writeOff is not 0)
            await _ipc.SetMetaStates(writeOff, false);
        
        // re-assert headgear, Glamourer drops it out of sync when the visor toggles
        if (((writeOn | writeOff) & MetaFlag.VisorState) is not 0 && final.Headgear.Value is { } hatVal)
        {
            await Task.Delay(1);
            await _ipc.SetMetaStates(MetaFlag.HatState, hatVal);
        }
    }

    /// <summary> The MetaStates we are enforcing that Glamourer is not currently holding. </summary>
    private static MetaFlag StaleFlags(MetaDataStruct final, MetaDataStruct live)
    {
        MetaFlag flags = 0;
        if (final.Headgear.HasValue && !final.Headgear.Equals(live.Headgear)) flags |= MetaFlag.HatState;
        if (final.Visor.HasValue && !final.Visor.Equals(live.Visor)) flags |= MetaFlag.VisorState;
        if (final.Weapon.HasValue && !final.Weapon.Equals(live.Weapon)) flags |= MetaFlag.WeaponState;
        return flags;
    }

    /// <summary> True when Glamourer is already showing <paramref name="slot"/> at the given appearance. </summary>
    private static bool SlotMatchesLive(JObject? live, EquipSlot slot, ulong itemId, byte stain, byte stain2)
        => live is not null
        && GlamourActorState.TryReadSlot(live, slot, out var liveItem, out var liveStain, out var liveStain2)
        && liveItem == itemId && liveStain == stain && liveStain2 == stain2;

    private static bool SlotMatchesLive(JObject? live, EquipSlot slot, GlamourSlot desired)
        => SlotMatchesLive(live, slot, desired.GameItem.Id.Id, desired.GameStain.Stain1.Id, desired.GameStain.Stain2.Id);

    /// <summary>
    ///   Caches the actor once for the whole pass, and hands back the state it read. <para />
    ///   When <paramref name="storeAsUnbound"/> (or we have nothing cached yet) it also refreshes the
    ///   unbound state that released slots restore back to.
    /// </summary>
    /// <returns> Glamourer's live state, so applies can skip values it already holds. </returns>
    private JObject? CacheActorState(bool storeAsUnbound)
    {
        var latest = _ipc.GetActorState();
        if (!storeAsUnbound && !ActorCacheIsEmpty)
            return latest;

        if (latest is null)
        {
            _logger.LogDebug("Failed to cache Glamourer state, latest state was null.", LoggerType.IpcGlamourer);
            _cache.CacheUnboundState(new GlamourActorState(null));
            return null;
        }

        _logger.LogTrace("Caching latest state from Glamourer IPC.", LoggerType.IpcGlamourer);
        // create a clone of our latest unbound state to avoid modifying the original.
        var latestUnboundCopy = GlamourActorState.Clone(_cache.LastUnboundState);
        latestUnboundCopy.UpdateEquipment(latest, _cache.FinalGlamour.ToDictionary(x => x.Key, x => x.Value.GameItem));
        latestUnboundCopy.UpdateMetaCheckBinds(latest, _cache.AnyHatMeta, _cache.AnyVisorMeta, _cache.AnyWeaponMeta);
        _cache.CacheUnboundState(latestUnboundCopy);
        return latest;
    }

    /// <summary>
    ///   Caches the latest state from Glamourer IPC to store the latest unbound state. <para />
    ///   To anyone reviewing this code, I am so sorry you have to try and understand this clusterfuck of a method to adapt with 
    ///   Glamourer's MetaState handling. <para />
    ///   Idealy we could set these by detouring the direct detours from the game's virtual table, but it would also
    ///   mean falling out of sync with glamourer's internal state, which may not reflect the game state (it does this sometimes). <para />
    ///   This is the best I could get it. Hopefully it improves down the line.
    /// </summary>
    public void CacheActorMeta(bool flagFromLatest)
    {
        _logger.LogTrace("Caching latest state from Glamourer IPC.", LoggerType.IpcGlamourer);
        var latestState = _ipc.GetActorState();
        if (latestState != null)
        {
            // must clone since the struct contains an internal dictionary.
            var latestUnboundCopy = GlamourActorState.Clone(_cache.LastUnboundState);
            if (flagFromLatest)
                latestUnboundCopy.UpdateMetaWithLatest(latestState);
            else
                latestUnboundCopy.UpdateMetaCheckBinds(latestState, _cache.AnyHatMeta, _cache.AnyVisorMeta, _cache.AnyWeaponMeta);
            // finalize the state.
            _cache.CacheUnboundState(latestUnboundCopy);
        }
        else
        {
            _logger.LogDebug("Failed to cache Glamourer state, latest state was null.", LoggerType.IpcGlamourer);
            _cache.CacheUnboundState(new GlamourActorState(latestState));
        }
    }

    public void CacheActorFromLatest()
    {
        _logger.LogTrace("Caching Actor from Latest State from Glamourer IPC.", LoggerType.IpcGlamourer);
        var latestState = _ipc.GetActorState();
        if (latestState != null)
        {
            // must clone since the struct contains an internal dictionary.
            var latestUnboundCopy = GlamourActorState.Clone(_cache.LastUnboundState);
            latestUnboundCopy.UpdateEquipment(latestState, _cache.FinalGlamour.ToDictionary(x => x.Key, x => x.Value.GameItem));
            latestUnboundCopy.UpdateMetaWithLatest(latestState);
            // finalize the state.
            _cache.CacheUnboundState(latestUnboundCopy);
        }
        else
        {
            _logger.LogDebug("Failed to cache Glamourer state, latest state was null.", LoggerType.IpcGlamourer);
            _cache.CacheUnboundState(new GlamourActorState(latestState));
        }
    }

    /// <summary>
    ///   Ensures that all other calls from Glamourer are blocked during a execution.
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
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            // Schedule the re-enabling of glamour change events using RunOnFrameworkTickDelayed to offset Glamourer.)
            try
            {
                await Svc.Framework.RunOnTick(() =>
                {
                    _ipcBlocker &= ~IpcBlockReason.SemaphoreTask;
                    _logger.LogDebug($"Releasing Semaphore Wait, Remaining Blockers: {_ipcBlocker.ToString()}", LoggerType.IpcGlamourer);
                }, delayTicks: 1);
            }
            catch (TaskCanceledException) { /* CONSUME */ }

            // Release the slim, allowing further execution.
            _applySlim.Release();
        }
    }

    public void PrintLatestCache()
    {
        var latest = _cache.LastUnboundState;
        // get the jobject string to print.
        _logger.LogInformation($"Latest Unbound State: {latest.State?.ToString() ?? string.Empty}, " +
            $"Meta: Hat: {latest.MetaStates.Headgear}, Visor: {latest.MetaStates.Visor}, Weapon: {latest.MetaStates.Weapon}");
    }
}
