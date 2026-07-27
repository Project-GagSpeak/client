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
    
    private readonly Dictionary<EquipSlot, (List<EquipItem> Released, int Attempts)> _pendingRestores = new();
    private readonly Dictionary<MetaIndex, (TriStateBool Released, int Attempts)> _pendingMetaRestores = new();
    private const int MaxRestoreAttempts = 3;

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
    /// <remarks> Runs inside the SemaphoreSlim so the cache wipe cannot run midway through another operation. </remarks>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing Glamour Cache.");
        await ExecuteWithSemaphore(async () =>
        {
            _cache.ClearCaches();
            // The internals are called directly here, as UpdateCaches would re-enter this same semaphore.
            await Task.WhenAll(
                UpdateMetaInternal(false, true),
                UpdateGlamourInternal(false, true)
            );
            // After we clear out the cache and update them to their recovered state we should clear the unbound cache out
            _cache.CacheUnboundState(GlamourActorState.Empty);
            // Nothing left to reconcile against once the unbound snapshot is gone.
            _pendingRestores.Clear();
            _pendingMetaRestores.Clear();
        });
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
                UpdateMetaInternal(false, true),
                UpdateGlamourInternal(false, true)
            );
            _logger.LogInformation($"Processed Cache Updates Successfully!");
        });
        _logger.LogDebug("Finished Updating Glamourer Caches.");
    }


    /// <summary> Use this after any glamour Finalization type occurs. </summary>
    /// <param name="storeProfile"> If true, will force the cache to be applied before updating. </param>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task ReapplyAllCaches()
    {
        _logger.LogDebug("Reapplying Glamourer Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Run both operations in parallel.
            await Task.WhenAll(
                ApplyGlamourCache(true),
                ApplyMetaCache(true)
            );
            _logger.LogInformation($"Reapplied Cache Updates Successfully!");
        });
    }

    /// <summary>
    ///     Self-heal for stuck glamour. <para />
    ///     When a restriction is released we push the slot back to its unbound appearance, but that
    ///     IPC call can silently fail to run - and once released, the slot is gone from
    ///     <see cref="GlamourCache.FinalGlamour"/>, so nothing ever notices it is still showing
    ///     the released item. This compares <see cref="_pendingRestores"/> against Glamourer's live state
    ///     and retries any slot that is verifiably still stuck.
    /// </summary>
    /// <remarks> Intended to be called periodically (e.g. once a minute). Runs through the SemaphoreSlim. </remarks>
    public async Task ReconcileWithActualState()
    {
        await ExecuteWithSemaphore(async () =>
        {
            if (_pendingRestores.Count is 0 && _pendingMetaRestores.Count is 0)
            {
                _logger.LogTrace("Glamour reconciliation: nothing pending.", LoggerType.IpcGlamourer);
                return;
            }

            if (_ipc.GetActorState() is not { } liveObj)
            {
                _logger.LogTrace("Glamour reconciliation: actor state unavailable, skipping.", LoggerType.IpcGlamourer);
                return;
            }

            var live = new GlamourActorState(liveObj);
            if (live.ParsedEquipment.Count is 0)
            {
                _logger.LogDebug("Glamour reconciliation: live state had no equipment, skipping.", LoggerType.IpcGlamourer);
                return;
            }

            var stuckSlots = new List<EquipSlot>();
            var needsReapply = false;

            foreach (var (slot, (released, attempts)) in _pendingRestores.ToList())
            {
                if (!live.ParsedEquipment.TryGetValue(slot, out var actual))
                {
                    _logger.LogDebug($"Glamour reconciliation: slot {slot} missing from live state, still watching it.", LoggerType.IpcGlamourer);
                    continue;
                }

                if (_cache.FinalGlamour.TryGetValue(slot, out var forced))
                {
                    if (actual.Equals(forced.GameItem))
                    {
                        _logger.LogTrace($"Glamour reconciliation: slot {slot} re-forced and correct.", LoggerType.IpcGlamourer);
                        _pendingRestores.Remove(slot);
                        continue;
                    }

                    if (attempts >= MaxRestoreAttempts)
                    {
                        _logger.LogError($"Glamour reconciliation: slot {slot} still not showing forced item " +
                            $"[{forced.GameItem.Name}] after {attempts} attempts, giving up.", LoggerType.IpcGlamourer);
                        _pendingRestores.Remove(slot);
                        continue;
                    }

                    _logger.LogWarning($"Glamour reconciliation: slot {slot} should show [{forced.GameItem.Name}], " +
                        $"reapplying (attempt {attempts + 1}/{MaxRestoreAttempts}).", LoggerType.IpcGlamourer);
                    _pendingRestores[slot] = (released, attempts + 1);
                    needsReapply = true;
                    continue;
                }
                
                if (!released.Any(i => i.Equals(actual)))
                {
                    _logger.LogTrace($"Glamour reconciliation: slot {slot} restored correctly.", LoggerType.IpcGlamourer);
                    _pendingRestores.Remove(slot);
                    continue;
                }
                
                if (attempts >= MaxRestoreAttempts)
                {
                    _logger.LogError($"Glamour reconciliation: slot {slot} still stuck on [{actual.Name}] after " +
                        $"{attempts} attempts, giving up to avoid fighting the player.", LoggerType.IpcGlamourer);
                    _pendingRestores.Remove(slot);
                    continue;
                }

                _logger.LogWarning($"Glamour reconciliation: slot {slot} still stuck on [{actual.Name}], " +
                    $"retrying restore (attempt {attempts + 1}/{MaxRestoreAttempts}).", LoggerType.IpcGlamourer);
                _pendingRestores[slot] = (released, attempts + 1);
                stuckSlots.Add(slot);
            }

            foreach (var (metaIdx, (released, attempts)) in _pendingMetaRestores.ToList())
            {
                var liveVal = MetaStateFor(live.MetaStates, metaIdx);
                var baseVal = MetaStateFor(_cache.LastUnboundState.MetaStates, metaIdx);
                
                if (MetaStateFor(_cache.FinalMeta, metaIdx).HasValue || !liveVal.Equals(released))
                {
                    _pendingMetaRestores.Remove(metaIdx);
                    continue;
                }

                if (attempts >= MaxRestoreAttempts)
                {
                    _logger.LogError($"Glamour reconciliation: {metaIdx} still stuck on ({released}) after " +
                        $"{attempts} attempts, giving up.", LoggerType.IpcGlamourer);
                    _pendingMetaRestores.Remove(metaIdx);
                    continue;
                }
                
                if ((bool?)baseVal is not { } restoreTo)
                {
                    _logger.LogDebug($"Glamour reconciliation: {metaIdx} stuck on ({released}) but the unbound state " +
                        "has no value to restore to, waiting.", LoggerType.IpcGlamourer);
                    continue;
                }

                _logger.LogWarning($"Glamour reconciliation: {metaIdx} still stuck on ({released}), " +
                    $"retrying restore (attempt {attempts + 1}/{MaxRestoreAttempts}).", LoggerType.IpcGlamourer);
                _pendingMetaRestores[metaIdx] = (released, attempts + 1);
                await _ipc.SetMetaStates(MetaFlagFor(metaIdx), restoreTo);
            }

            if (stuckSlots.Count is 0 && !needsReapply)
                return;

            // Deliberately NOT cacheBeforeApply: We do not trust the live state, so
            // re-snapshotting it would bake the stuck item in as the slot's base appearance.
            if (stuckSlots.Count > 0)
                await Task.WhenAll(stuckSlots.Select(RestoreSlot));
            await ApplyGlamourCache(false);
        });
    }

    /// <summary>
    ///     Records a released meta flag so <see cref="ReconcileWithActualState"/> can verify the restore occurred.
    /// </summary>
    /// <remarks>
    ///     Skipped when nothing was actually being forced, or when the unbound base already matches the
    ///     forced value
    /// </remarks>
    private void TrackPendingMeta(MetaIndex metaIdx, TriStateBool released, TriStateBool baseValue)
    {
        if (!released.HasValue || baseValue.Equals(released))
            return;
        _pendingMetaRestores[metaIdx] = (released, 0);
    }

    private static TriStateBool MetaStateFor(MetaDataStruct meta, MetaIndex metaIdx)
        => metaIdx switch
        {
            MetaIndex.HatState => meta.Headgear,
            MetaIndex.VisorState => meta.Visor,
            MetaIndex.WeaponState => meta.Weapon,
            _ => TriStateBool.Null,
        };

    private static MetaFlag MetaFlagFor(MetaIndex metaIdx)
        => metaIdx switch
        {
            MetaIndex.HatState => MetaFlag.HatState,
            MetaIndex.VisorState => MetaFlag.VisorState,
            _ => MetaFlag.WeaponState,
        };

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateGlamourCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateGlamourInternal(false, reapply));

    /// <summary> Should only ever be used by the GlamourListener. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateMetaCacheSlim(bool reapply)
        => await ExecuteWithSemaphore(() => UpdateMetaInternal(true, reapply));

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
    private async Task UpdateMetaInternal(bool forceCacheCall, bool reapply)
    {
        // Capture what we were forcing BEFORE the update, so we can flag what must no longer be showing.
        var wasForcing = _cache.FinalMeta;
        // Update the final cache. `removedSlots` contains slots that are no longer restricted after the change.
        if (_cache.UpdateFinalMetaCache(out bool noHat, out bool noVisor, out bool noWeapon))
        {
            _logger.LogDebug($"Final MetaState Cache was updated!", LoggerType.VisualCache);
            if (noHat || noVisor || noWeapon)
                await RestoreMetaAndReapply(forceCacheCall, wasForcing, noHat, noVisor, noWeapon);
            else
                await ApplyMetaCache(forceCacheCall);
            return;
        }
        else if (reapply)
        {
            _logger.LogDebug("Reapplying MetaState Cache", LoggerType.VisualCache);
            await ApplyMetaCache(forceCacheCall);
            return;
        }
        // No Change
        _logger.LogTrace("No change in Final MetaState Cache.", LoggerType.VisualCache);
    }

    /// <summary>
    ///     Restore slots no longer present in _finalGlamour from <see cref="_cache"/>, then reapplies what is still active.
    /// </summary>
    /// <remarks>
    ///     Each released slot is recorded in <see cref="_pendingRestores"/> first, so that if this
    ///     push silently fails to reach Glamourer the slot does not stay stuck forever.
    /// </remarks>
    private async Task RestoreAndReapply(bool forceCacheCall, IReadOnlyDictionary<EquipSlot, EquipItem> slotsToRestore)
    {
        foreach (var (slot, releasedItem) in slotsToRestore)
        {
            if (_pendingRestores.TryGetValue(slot, out var pending))
            {
                // An earlier release on this slot is still unconfirmed, so keep watching for that item
                // too, and do not hand the slot a fresh attempt budget.
                if (!pending.Released.Any(i => i.Equals(releasedItem)))
                    pending.Released.Add(releasedItem);
                _pendingRestores[slot] = pending;
            }
            else
                _pendingRestores[slot] = ([releasedItem], 0);
        }

        await Task.WhenAll(slotsToRestore.Keys.Select(RestoreSlot));
        _logger.LogDebug($"Restored Glamourer Slots to last applied base value.", LoggerType.IpcGlamourer);
        // Now reapply the cache.
        _logger.LogDebug("Reapplying Glamourer Cache", LoggerType.IpcGlamourer);
        await ApplyGlamourCache(forceCacheCall);
    }

    /// <summary> Pushes a single slot back to its last known unbound appearance. </summary>
    private Task RestoreSlot(EquipSlot slot)
    {
        if (_cache.LastUnboundState.RecoverSlot(slot, out var itemId, out var stain, out var stain2))
            return _ipc.SetClientItemSlot((ApiEquipSlot)slot, itemId, [stain, stain2], 0);

        // Nothing to restore to. ReconcileWithActualState will notice the slot is still stuck and retry.
        _logger.LogWarning($"Failed to restore slot {slot}, no data found in Glamourer cache.", LoggerType.IpcGlamourer);
        return Task.CompletedTask;
    }

    /// <summary> 
    ///     Apples the FinalGlamour from <see cref="_cache"/> to the Client. 
    /// </summary>
    private async Task ApplyGlamourCache(bool cacheBeforeApply)
    {
        if (cacheBeforeApply || ActorCacheIsEmpty)
            CacheActorEquip();

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

    private async Task RestoreMetaAndReapply(bool forceCacheCall, MetaDataStruct wasForcing, bool restoreHat, bool restoreVisor, bool restoreWeapon)
    {
        if (restoreHat)
            TrackPendingMeta(MetaIndex.HatState, wasForcing.Headgear, _cache.LastUnboundState.MetaStates.Headgear);
        if (restoreVisor)
            TrackPendingMeta(MetaIndex.VisorState, wasForcing.Visor, _cache.LastUnboundState.MetaStates.Visor);
        if (restoreWeapon)
            TrackPendingMeta(MetaIndex.WeaponState, wasForcing.Weapon, _cache.LastUnboundState.MetaStates.Weapon);

        // If we are restoring the hat, visor or weapon, we need to restore the slots.
        if (restoreHat && (bool?)_cache.LastUnboundState.MetaStates.Headgear is { } newVal)
            await _ipc.SetMetaStates(MetaFlag.HatState, newVal);
        if (restoreVisor && (bool?)_cache.LastUnboundState.MetaStates.Visor is { } newVal2)
            await _ipc.SetMetaStates(MetaFlag.VisorState, newVal2);
        if (restoreWeapon && (bool?)_cache.LastUnboundState.MetaStates.Weapon is { } newVal3)
            await _ipc.SetMetaStates(MetaFlag.WeaponState, newVal3);

        _logger.LogDebug($"Restored Meta Slots to last applied base value.", LoggerType.IpcGlamourer);

        // Now reapply the states
        _logger.LogDebug("Reapplying Meta Cache", LoggerType.IpcGlamourer);
        await ApplyMetaCache(forceCacheCall);
    }

    /// <summary>
    ///     Apples the _finalMeta from the <see cref="_cache"/> Cache to the Client.
    /// </summary>
    private async Task ApplyMetaCache(bool cacheBeforeApply)
    {
        if (cacheBeforeApply || ActorCacheIsEmpty)
            CacheActorMeta(false);

        await _ipc.SetMetaStates(_cache.FinalMeta.OnFlags(), true);
        await _ipc.SetMetaStates(_cache.FinalMeta.OffFlags(), false);
        // attempt to work around glamourer's wonky issue where it fails to sync meta state updates with visor.
        // this will result in the visor 'flashing' if out of sync, but the headgear will stay in sync at least.
        if (_cache.FinalMeta.Visor.Value is true)
        {
            await Task.Delay(1);
            await _ipc.SetMetaStates(MetaFlag.HatState, true);
        }
        //_logger.LogDebug("Updated Meta States", LoggerType.IpcGlamourer);
    }

    public void CacheActorEquip()
    {
        _logger.LogTrace("Caching latest Equip from Glamourer IPC.", LoggerType.IpcGlamourer);
        var latestState = _ipc.GetActorState();
        if (latestState != null)
        {
            // create a clone of our latest unbound state to avoid modifying the original.
            var latestUnboundCopy = GlamourActorState.Clone(_cache.LastUnboundState);
            latestUnboundCopy.UpdateEquipment(latestState, _cache.FinalGlamour.ToDictionary(x => x.Key, x => x.Value.GameItem));
            _cache.CacheUnboundState(latestUnboundCopy);
        }
        else
        {
            _logger.LogDebug("Failed to cache Glamourer state, latest state was null.", LoggerType.IpcGlamourer);
            _cache.CacheUnboundState(new GlamourActorState(latestState));
        }
    }

    /// <summary>
    ///     Caches the latest state from Glamourer IPC to store the latest unbound state. <para />
    ///     To anyone reviewing this code, I am so sorry you have to try and understand this clusterfuck of a method to adapt with 
    ///     Glamourer's MetaState handling. <para />
    ///     Idealy we could set these by detouring the direct detours from the game's virtual table, but it would also
    ///     mean falling out of sync with glamourer's internal state, which may not reflect the game state (it does this sometimes). <para />
    ///     This is the best I could get it. Hopefully it improves down the line.
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
                latestUnboundCopy.UpdateMetaCheckBinds(latestState, _cache.FinalMeta, _cache.AnyHatMeta, _cache.AnyVisorMeta, _cache.AnyWeaponMeta);
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
