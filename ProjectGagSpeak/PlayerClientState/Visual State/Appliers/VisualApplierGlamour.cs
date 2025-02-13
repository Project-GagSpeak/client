using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using GagSpeak.Interop.Ipc;
using GagSpeak.UpdateMonitoring;
using Glamourer.Api.Enums;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Handles performing all application calls off to Glamourer from Glamour changes and updates </summary>
public class VisualApplierGlamour : IDisposable
{
    private readonly ILogger<VisualApplierGlamour> _logger;
    private readonly IpcCallerGlamourer _glamourer;
    private readonly OnFrameworkService _frameworkUtils;

    private CancellationTokenSource ApplierCts;
    private static SemaphoreSlim ApplierSlim = new SemaphoreSlim(1, 1);
    public VisualApplierGlamour(ILogger<VisualApplierGlamour> logger, IpcCallerGlamourer glamourer, 
        OnFrameworkService frameworkUtils, IGameInteropProvider interop)
    {
        _logger = logger;
        _glamourer = glamourer;
        _frameworkUtils = frameworkUtils;
        ApplierCts = new CancellationTokenSource();

        unsafe
        {
            _equipGearsetHook = interop.HookFromAddress<EquipGearsetDelegate>(
                (nint)RaptureGearsetModule.MemberFunctionPointers.EquipGearsetInternal, GearSetDetour);
            _equipGearsetHook.Enable();
        }
    }

    public bool StateChangeBlocked { get; set; } = false;
    public GlamourCache LastUnboundState { get; internal set; } = new();
    public ConcurrentDictionary<EquipSlot, GlamourSlot> RestrictedSlots { get; internal set; } = new();
    public MetaDataStruct RestrictedMeta { get; internal set; } = new();
    public (JToken? Customize, JToken? Parameters) RestrictedCustomize = (null, null);

    public void Dispose()
    {
        _equipGearsetHook?.Disable();
        _equipGearsetHook?.Dispose();
    }

    public async Task UpdateAllActiveSlots(Dictionary<EquipSlot, GlamourSlot> items)
    {
        await ExecuteWithSemaphore(async () =>
        {
            var removedSlots = RestrictedSlots
                .Where(kv => !items.ContainsKey(kv.Key))
                .Select(kv => kv.Value);
            // update the slots.
            RestrictedSlots = new ConcurrentDictionary<EquipSlot, GlamourSlot>(items);
            // remove and restore the slots that were restricted prior.
            foreach (var slot in removedSlots)
            {
                LastUnboundState.RecoverSlot(slot.Slot, out var customItemId, out var stain, out var stain2);
                await _glamourer.SetClientItemSlot((ApiEquipSlot)slot.Slot, customItemId, [stain, stain2], 0);
            }
            // Apply the new active slots.
            await ApplyActiveSlots();
            _logger.LogDebug("Active Restricted Slots updated to new collection. Removed slots restored.", LoggerType.IpcGlamourer);
        });
    }

    public async Task UpdateActiveSlot(GlamourSlot item)
    {
        await ExecuteWithSemaphore(async () =>
        {
            RestrictedSlots[item.Slot] = item;
            await _glamourer.SetClientItemSlot((ApiEquipSlot)item.Slot, item.GameItem.Id.Id, [item.GameStain.Stain1.Id, item.GameStain.Stain2.Id], 0);
            _logger.LogDebug($"Updated Restricted Slot" + item.Slot, LoggerType.ClientPlayerData);
        });
    }

    public async Task UpdateActiveSlot(IEnumerable<GlamourSlot> items)
    {
        await ExecuteWithSemaphore(async () =>
        {
            foreach (var item in items)
            {
                RestrictedSlots[item.Slot] = item;
                await _glamourer.SetClientItemSlot((ApiEquipSlot)item.Slot, item.GameItem.Id.Id, [item.GameStain.Stain1.Id, item.GameStain.Stain2.Id], 0);
            }
            _logger.LogDebug($"Updated Restricted Slots {string.Join(", ", items.Select(x => x.Slot))}", LoggerType.ClientPlayerData);
        });
    }

    public async Task RemoveRestrictionSlot(IEnumerable<EquipSlot> slots)
    {
        await ExecuteWithSemaphore(async () =>
        {
            foreach (var slot in slots)
            {
                RestrictedSlots.TryRemove(slot, out _);
                LastUnboundState.RecoverSlot(slot, out var customItemId, out var stain, out var stain2);
                await _glamourer.SetClientItemSlot((ApiEquipSlot)slot, customItemId, [stain, stain2], 0);
            }
            _logger.LogDebug($"Removed Restricted Slots {string.Join(", ", slots)}", LoggerType.ClientPlayerData);
        });
    }


    public async Task OnStateChanged(StateChangeType type)
    {
        await ExecuteWithSemaphore(async () =>
        {
            // handle it as either metaData, or as a modified gear item.
            if (type is StateChangeType.Equip or StateChangeType.Stains)
                await ApplyActiveSlots();
            else if (type is StateChangeType.Other)
                await ApplyActiveMetaStates();
        });
    }

    // Execute this regardless of the type. For GagSpeak in particular, all that matters is that the state is finalized.
    public async Task OnStateFinalized(StateFinalizationType type)
    {
        await ExecuteWithSemaphore(async () =>
        {
            // Cache the Glamourer State.
            var latestState = _glamourer.GetClientGlamourerState();
            LastUnboundState = new GlamourCache(latestState);

            // Process through our active slots and apply them.
            await ApplyActiveSlots();
        });
    }

    public async Task ApplyActiveSlots() => await ExecuteWithSemaphore(async () => await ApplyActiveSlotsInternal());
    private async Task ApplyActiveSlotsInternal()
    {
        // configure the tasks to run asynchronously.
        var setItemTasks = RestrictedSlots
            .Select(slot =>
            {
                var equipSlot = (ApiEquipSlot)slot.Key;
                var gameItem = slot.Value.GameItem;
                var gameStain1 = slot.Value.GameStain.Stain1;
                var gameStain2 = slot.Value.GameStain.Stain2;
                // The whole 'Overlay Mode' logic was already handled in the listener, so dont worry about it here and just set.
                _logger.LogTrace($"Correcting slot {equipSlot} to ensure helplessness.", LoggerType.ClientPlayerData);
                return _glamourer.SetClientItemSlot(equipSlot, gameItem.Id.Id, [gameStain1.Id, gameStain2.Id], 0);
            })
            .ToList();

        await Task.WhenAll(setItemTasks);
        _logger.LogTrace("Applied Active Slots to Glamour", LoggerType.ClientPlayerData);
    }

    public async Task UpdateMetaState(MetaIndex metaIdx, OptionalBool newState)
    {
        await ExecuteWithSemaphore(async () =>
        {
            RestrictedMeta.SetMeta(metaIdx, newState);
            await ApplyActiveMetaStates();
        });
    }

    public async Task UpdateMetaStates(IEnumerable<(MetaIndex idx, OptionalBool newState)> newStates)
    {
        await ExecuteWithSemaphore(async () =>
        {
            foreach (var (idx, newState) in newStates)
                RestrictedMeta.SetMeta(idx, newState);
            await ApplyActiveMetaStates();
        });
    }

    public async Task ApplyActiveMetaStates() => await ExecuteWithSemaphore(async () => await ApplyActiveMetaInternal());
    private async Task ApplyActiveMetaInternal()
    {
        await _glamourer.SetMetaStates(RestrictedMeta.OnFlags(), true);
        await _glamourer.SetMetaStates(RestrictedMeta.OffFlags(), false);
        _logger.LogDebug("Updated Meta States", LoggerType.ClientPlayerData);
    }

    public async Task ApplyActiveCustomizations() => await ExecuteWithSemaphore(async () => await ApplyActiveCustomizationsInternal());
    private async Task ApplyActiveCustomizationsInternal()
    {
        if (RestrictedCustomize.Customize is not null && RestrictedCustomize.Parameters is not null)
            await _glamourer.SetCustomize(RestrictedCustomize.Customize, RestrictedCustomize.Parameters);
        _logger.LogDebug("Applied Active Customizations", LoggerType.ClientPlayerData);
    }

    /// <summary> Encapsulates the StateChanges to the Glamourer Appearance.</summary>
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        ApplierCts.Cancel();
        StateChangeBlocked = true;
        await ApplierSlim.WaitAsync();
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
            ApplierSlim.Release();
            // Schedule the re-enabling of glamour change events using RunOnFrameworkTickDelayed to offset Glamourer.
            await _frameworkUtils.RunOnFrameworkTickDelayed(() =>
            {
                _logger.LogDebug("Re-Allowing Glamour Change Event", LoggerType.IpcGlamourer);
                StateChangeBlocked = false;
            }, 1);
        }
    }

    private unsafe delegate nint EquipGearsetDelegate(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId);
    private readonly Hook<EquipGearsetDelegate> _equipGearsetHook = null!;
    private unsafe nint GearSetDetour(RaptureGearsetModule* module, uint gearsetId, byte glamourPlateId)
    {
        StateChangeBlocked = true;
        return _equipGearsetHook.Original(module, gearsetId, glamourPlateId);
    }
}
