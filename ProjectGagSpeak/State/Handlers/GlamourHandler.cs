using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Interop.Ipc;
using GagSpeak.State.Caches;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Handlers;
public class GlamourHandler
{
    private readonly ILogger<GlamourHandler> _logger;
    private readonly IpcCallerGlamourer _ipc;
    private readonly GlamourCache _cache;
    private readonly OnFrameworkService _frameworkUtils;

    private CancellationTokenSource _applyCts = new();
    private SemaphoreSlim _applySlim = new SemaphoreSlim(1, 1);
    private IpcBlockReason _ipcBlocker = IpcBlockReason.None;

    public GlamourHandler(
        ILogger<GlamourHandler> logger,
        IpcCallerGlamourer ipc,
        GlamourCache cache,
        PlayerData clientMonitor,
        OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi,
        IGameInteropProvider gip)
    {
        _logger = logger;
        _ipc = ipc;
        _cache = cache;
        _frameworkUtils = frameworkUtils;
    }

    public IpcBlockReason BlockIpcCalls => _ipcBlocker;
    private bool ActorCacheIsEmpty => _cache.LastUnboundState.Equals(GlamourActorState.Empty);

    /// <summary>
    ///     Called upon by the EquipGearsetInternalDetour.
    /// </summary>
    public void OnEquipGearsetInternal(int gearsetId, byte glamourPlateId)
    {
        // This is called when a gearset is equipped, so we need to ensure that the glamour cache is applied.
        _logger.LogDebug($"EquipGearsetInternal for gearsetId {gearsetId} with plateId {glamourPlateId} occured!" +
            $"Blocking any further OnStateChanged calls until gearset application finishes!");
        _ipcBlocker |= IpcBlockReason.Gearset;
    }

    /// <summary>
    ///     Handles the apply process for the GlamourCache on both Glamour and Meta.
    /// </summary>
    public async Task ApplySemaphore(bool applyGlamour, bool applyMeta, bool forceCacheCall)
    {
        if (!applyGlamour && !applyMeta)
            return;

        await ExecuteWithSemaphore(async () =>
        {
            var tasks = new List<Task>();
            if (applyGlamour)
                tasks.Add(ApplyGlamourCache(forceCacheCall));

            if (applyMeta)
                tasks.Add(ApplyMetaCache());

            _logger.LogInformation($"Processing ApplySemaphore");
            await Task.WhenAll(tasks);
            _logger.LogInformation($"Processed ApplySemaphore Successfully!");
        });
    }

    /// <summary>
    ///     Handles the removal process for the GlamourCache on both Glamour and Meta.
    /// </summary>
    public async Task RemoveSemaphore(bool removeGlamour, bool removeMeta, bool forceCacheCall, List<EquipSlot>? removedSlots = null)
    {
        if (!removeGlamour && !removeMeta)
            return;

        if (removeGlamour && removedSlots is null)
        {
            _logger.LogError("Cannot remove GlamourCache without removedSlots provided!");
            return;
        }

        await ExecuteWithSemaphore(async () =>
        {
            var tasks = new List<Task>();
            if (removeGlamour)
                tasks.Add(RestoreAndReapply(forceCacheCall, removedSlots!));

            if (removeMeta)
                tasks.Add(ApplyMetaCache());

            _logger.LogInformation($"Processing RemoveSemaphore");
            await Task.WhenAll(tasks);
            _logger.LogInformation($"Processed RemoveSemaphore Successfully!");
        });
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
    /// <remarks> Must be used in <see cref="ExecuteWithSemaphore(Func{Task})"/></remarks>
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
    /// <remarks> Must be used in <see cref="ExecuteWithSemaphore(Func{Task})"/></remarks>
    private async Task ApplyMetaCache()
    {
        await _ipc.SetMetaStates(_cache.FinalMeta.OnFlags(), true);
        await _ipc.SetMetaStates(_cache.FinalMeta.OffFlags(), false);
        _logger.LogDebug("Updated Meta States", LoggerType.IpcGlamourer);
    }

    /// <summary>
    ///     Caches the latest state from Glamourer IPC to store the latest unbound state.
    /// </summary>
    private void CacheActorState()
    {
        _logger.LogError("Caching latest state from Glamourer IPC.", LoggerType.IpcGlamourer);
        var latestState = _ipc.GetClientGlamourerState();
        _cache.CacheUnboundState(new GlamourActorState(latestState));
    }

    /// <summary>
    ///     Ensures that all other calls from Glamourer are blocked during a execution.
    /// </summary>
    /// <remarks> This is nessisary to avoid deadlocks and infinite looping calls.</remarks>
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        _applyCts.Cancel();
        _ipcBlocker |= IpcBlockReason.SemaphoreTask;
        await _applySlim.WaitAsync();
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
            _applySlim.Release();
            // Schedule the re-enabling of glamour change events using RunOnFrameworkTickDelayed to offset Glamourer.
            await _frameworkUtils.RunOnFrameworkTickDelayed(() =>
            {
                _logger.LogWarning("Re-Allowing Glamour Change Event", LoggerType.IpcGlamourer);
                _ipcBlocker &= ~IpcBlockReason.SemaphoreTask;
            }, 1);
        }
    }
}
