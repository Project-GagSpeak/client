using Dalamud.Hooking;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GagSpeak.Interop.Ipc;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using Glamourer.Api.IpcSubscribers;
using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

// Must be partial to avoid circular dependancy. The time it would take to make this
// not have circular dependancy is not worth looking into how to split it.
public sealed partial class GlamourHandler : IDisposable
{
    private readonly ILogger<GlamourHandler> _logger;
    private readonly IpcCallerGlamourer _ipc;
    private readonly ClientMonitor _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;

    private CancellationTokenSource _applyCts = new();
    private SemaphoreSlim _applySlim = new SemaphoreSlim(1, 1);
    private bool _blockIpcChanges = false;

    public GlamourHandler(
        ILogger<GlamourHandler> logger,
        IpcCallerGlamourer ipc,
        ClientMonitor clientMonitor,
        OnFrameworkService frameworkUtils,
        IDalamudPluginInterface pi,
        IGameInteropProvider gip)
    {
        _logger = logger;
        _ipc = ipc;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;

        _ipc.StateWasChanged = StateChangedWithType.Subscriber(pi, (addr, type) => _ = OnStateChanged(addr, type));
        _ipc.StateWasFinalized = StateFinalized.Subscriber(pi, (addr, type) => _ = OnStateFinalized(addr, type));
        _ipc.StateWasChanged.Enable();
        _ipc.StateWasFinalized.Enable();

        unsafe
        {
            _equipGearsetHook = gip.HookFromAddress<EquipGearsetDelegate>(
                (nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearsetInternal, GearSetDetour);
            _equipGearsetHook.Enable();
        }
    }

    public void Dispose()
    {
        _ipc.StateWasChanged.Disable();
        _ipc.StateWasChanged?.Dispose();
        _ipc.StateWasFinalized.Disable();
        _ipc.StateWasFinalized?.Dispose();
        _equipGearsetHook?.Disable();
        _equipGearsetHook?.Dispose();
    }

    /// <summary> When ANY Glamourer state change occurs for ANY given actor, this is fired. </summary>
    /// <param name="address">The address of the actor that was changed.</param>
    /// <param name="changeType">The type of change that occurred.</param>
    /// <remarks> This is primarily used to cache the state of the Client. Discarded for other players. </remarks>
    private async Task OnStateChanged(nint address, StateChangeType changeType)
    {
        if (address != _clientMonitor.Address)
            return;

        if (changeType is not (StateChangeType.Equip or StateChangeType.Stains or StateChangeType.Other))
            return;

        if (_blockIpcChanges)
        {
            //_logger.LogWarning($">> Glamourer OnStateChanged Blocked [{changeType}], Update Still Processing! << ", LoggerType.IpcGlamourer);
            return;
        }


        // Handle individual equipment slots.
        if (changeType is StateChangeType.Equip or StateChangeType.Stains)
        {
            if(_finalGlamour.Keys.Count <= 0)
                return;

            await ExecuteWithSemaphore(async () =>
            {
                _logger.LogDebug($"Glamourer OnStateChanged: {changeType} - Applying Glamour Cache", LoggerType.IpcGlamourer);
                await ApplyGlamourCache();
            });
        }
        else if (changeType is StateChangeType.Other)
        {
            if (!_finalMeta.AnySet())
                return;

            await ExecuteWithSemaphore(async () =>
            {
                _logger.LogDebug($"Glamourer OnStateChanged: {changeType} - Applying Meta Cache", LoggerType.IpcGlamourer);
                await ApplyMetaCache();
            });
        }
    }

    /// <summary> Any any primary Glamourer Operation has completed, StateFinalized will fire. (This IPC Call is a Godsend). </summary>
    /// <param name="address">The address of the actor that was finalized.</param>
    /// <param name="finalizationType">The type of finalization that occurred.</param>
    /// <remarks> This is primarily used to cache the state of the player after a glamour operation has completed. </remarks>
    private async Task OnStateFinalized(nint address, StateFinalizationType finalizationType)
    {
        if (address != _clientMonitor.Address)
            return;

        _logger.LogError($"[OnStateFinalized] Finalization Type: {finalizationType}", LoggerType.IpcGlamourer);
        
        // If the blocked trait is marked here something should be able to unmark it,
        // but it shouldnt happen. If it does, its a turbo edge case *concern*
        await ExecuteWithSemaphore(async () =>
        {
            _logger.LogError("[OnStateFinalized] Caching Latest Unbound Glamourer State", LoggerType.IpcGlamourer);
            var latestState = _ipc.GetClientGlamourerState();
            _latestUnboundState = new GlamourActorCache(latestState);
            
            _logger.LogDebug("[OnStateFinalized] Applying Glamourer Cache", LoggerType.IpcGlamourer);
            await ApplyGlamourCache();
        });
    }

    /// <summary> Restore slots no longer present in the active Cache, then reapply the cache. </summary>
    private async Task RestoreAndReapply(IEnumerable<EquipSlot> slotsToRestore)
    {
        await Task.WhenAll(slotsToRestore
            .Select(slot =>
            {
                if (_latestUnboundState.RecoverSlot(slot, out var itemId, out var stain, out var stain2))
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
        await ApplyGlamourCache();
    }

    /// <summary> Apples the <see cref="_finalGlamour"/> to the Client. </summary>
    /// <remarks> Must be used in <see cref="ExecuteWithSemaphore(Func{Task})"/></remarks>
    private async Task ApplyGlamourCache()
    {
        // If the actorGlamourCache is empty, we must cache it.
        if (_latestUnboundState.Equals(GlamourActorCache.Empty))
        {
            _logger.LogWarning("Glamourer Cache is empty, caching latest state from Glamourer IPC.");
            var latestState = _ipc.GetClientGlamourerState();
            _latestUnboundState = new GlamourActorCache(latestState);
        }

        // configure the tasks to run asynchronously.
        await Task.WhenAll(_finalGlamour
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

    /// <summary> Apples the <see cref="_finalMeta"/> Cache to the Client. </summary>
    /// <remarks> Must be used in <see cref="ExecuteWithSemaphore(Func{Task})"/></remarks>
    private async Task ApplyMetaCache()
    {
        await _ipc.SetMetaStates(_finalMeta.OnFlags(), true);
        await _ipc.SetMetaStates(_finalMeta.OffFlags(), false);
        _logger.LogDebug("Updated Meta States", LoggerType.IpcGlamourer);
    }

    /// <summary> Apples the Customize Cacheto the Client. </summary>
    /// <remarks> Must be used in <see cref="ExecuteWithSemaphore(Func{Task})"/></remarks>
    private async Task ApplyCustomizeCache()
    {
        if (_latestUnboundState.Equals(GlamourActorCache.Empty))
        {
            _logger.LogWarning("Glamourer Cache is empty, caching latest state from Glamourer IPC.");
            var latestState = _ipc.GetClientGlamourerState();
            _latestUnboundState = new GlamourActorCache(latestState);
        }

        _logger.LogError("Not yet implmented");
/*        if (RestrictedCustomize.Customize is not null && RestrictedCustomize.Parameters is not null)
            await _ipc.SetCustomize(RestrictedCustomize.Customize, RestrictedCustomize.Parameters);
        _logger.LogDebug("Applied Active Customizations", LoggerType.ClientPlayerData);*/

    }

    /// <summary> Ensures that all other calls from Glamourer are blocked during a execution.</summary>
    /// <remarks> This is nessisary to avoid deadlocks and infinite looping calls.</remarks>
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        _applyCts.Cancel();
        _blockIpcChanges = true;
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
                _blockIpcChanges = false;
            }, 1);
        }
    }

    private unsafe delegate nint EquipGearsetDelegate(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId);
    private readonly Hook<EquipGearsetDelegate> _equipGearsetHook = null!;
    private unsafe nint GearSetDetour(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId)
    {
        _blockIpcChanges = true;
        return _equipGearsetHook.Original(module, gearsetId, glamourPlateId);
    }
}
