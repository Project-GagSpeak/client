using CkCommons;
using GagSpeak.State.Listeners;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Services;

public enum SelfBondageType
{
    Gag,
    Restriction,
    Restraint,
    Collar
}

/// <summary>
///     Services funneled distribution for server calls that relate to self bondage updates. <para />
///     Allows the flow of multiple updates, without allowing more than one of the same type to occur.
/// </summary>
public class SelfBondageService : IDisposable
{
    private readonly ILogger<SelfBondageService> _logger;
    private readonly DistributorService _dds;
    private readonly CallbackHandler _callbacks;

    private readonly Dictionary<SelfBondageType, Task?> _updateTasks = new();
    private CancellationTokenSource _runtimeCTS = new();
    private readonly object _lock = new();

    public SelfBondageService(ILogger<SelfBondageService> logger, DistributorService dds, CallbackHandler callbacks)
    {
        _logger = logger;
        _dds = dds;
        _callbacks = callbacks;
    }

    public void Dispose()
    {
        // Cancel the CTS attached to all dictionary tasks.
        _runtimeCTS.SafeCancelDispose();
        // Clear the dictionary of tasks.
        _updateTasks.Clear();
    }

    /// <summary>
    ///     Checks if we are currently allowed to execute a spesific task.
    /// </summary>
    public bool CanRunTask(SelfBondageType type)
        => !_updateTasks.TryGetValue(type, out var existingTask) || existingTask == null || existingTask.IsCompleted;

    /// <summary>
    ///     Check if we are able to perform a trigger reaction based on its type.
    /// </summary>
    public bool CanExecute(InvokableActionType actionType) => actionType switch
    {
        InvokableActionType.Gag => CanRunTask(SelfBondageType.Gag),
        InvokableActionType.Restriction => CanRunTask(SelfBondageType.Restriction),
        InvokableActionType.Restraint => CanRunTask(SelfBondageType.Restraint),
        _ => true
    };

    /// <summary>
    ///     Check if we're able to perform an alias reaction based on its types. If any fail, all fail.
    /// </summary>
    public bool CanExecute(IEnumerable<InvokableActionType> actionTypes)
        => actionTypes.Any(t => !CanExecute(t));

    /// <summary>
    ///     Attempt to perform a Gag related SelfBondage act. If one is in progress, it will be rejected.
    /// </summary>
    public void DoSelfGag(int layer, ActiveGagSlot newData, DataUpdateType type)
    {
        if (!CanRunTask(SelfBondageType.Gag))
        {
            _logger.LogWarning("Attempted to start a GagUpdateTask while another was still running. Action was rejected.");
            return;
        }

        // Create the dictionary entry for it and assign the task to perform.
        _updateTasks[SelfBondageType.Gag] = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveGagSlot(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockGag(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveGag(layer, MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        }, _runtimeCTS.Token);
    }

    /// <summary>
    ///     Attempt to perform a Gag related SelfBondage act and await its result. If one is in progress, it will be rejected.
    /// </summary>
    public async Task<bool> DoSelfGagResult(int layer, ActiveGagSlot newData, DataUpdateType type)
    {
        // Reject if not available.
        if (!CanRunTask(SelfBondageType.Gag))
        {
            _logger.LogWarning("Attempted to start a GagUpdate while another was still running. Action was rejected.");
            return false;
        }

        var taskToRun = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveGagSlot(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockGag(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockGag(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveGag(layer, MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        }, _runtimeCTS.Token);
        // Assign it 
        _updateTasks[SelfBondageType.Gag] = taskToRun;
        // Await and return the result.
        return await taskToRun.ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempt to perform a Bind / Restriction related SelfBondage act. If one is in progress, it will be rejected.
    /// </summary>
    public void DoSelfBind(int layer, ActiveRestriction newData, DataUpdateType type)
    {
        if (!CanRunTask(SelfBondageType.Restriction))
        {
            _logger.LogWarning("Attempted to start a RestrictionUpdate while another was still running. Action was rejected.");
            return;
        }

        // Create the dictionary entry for it and assign the task to perform.
        _updateTasks[SelfBondageType.Restriction] = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveRestriction(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockRestriction(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveRestriction(layer, MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        }, _runtimeCTS.Token);
    }

    /// <summary>
    ///     Attempt to perform a Restriction related SelfBondage act and await its result. If one is in progress, it will be rejected.
    /// </summary>
    public async Task<bool> DoSelfBindResult(int layer, ActiveRestriction newData, DataUpdateType type)
    {
        // Reject if not available.
        if (!CanRunTask(SelfBondageType.Restriction))
        {
            _logger.LogWarning("Attempted to start a RestrictionUpdate while another was still running. Action was rejected.");
            return false;
        }

        var taskToRun = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveRestriction(layer, newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockRestriction(layer, retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockRestriction(layer, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveRestriction(layer, MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        }, _runtimeCTS.Token);
        // Assign it 
        _updateTasks[SelfBondageType.Restriction] = taskToRun;
        // Await and return the result.
        return await taskToRun.ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempt to perform a RestraintSet related SelfBondage act. If one is in progress, it will be rejected.
    /// </summary>
    public void DoSelfRestraint(CharaActiveRestraint newData, DataUpdateType type)
    {
        if (!CanRunTask(SelfBondageType.Restraint))
        {
            _logger.LogWarning("Attempted to start a RestraintUpdate while another was still running. Action was rejected.");
            return;
        }

        // Create the dictionary entry for it and assign the task to perform.
        _updateTasks[SelfBondageType.Restraint] = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveRestraint(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersChanged => _callbacks.SwapRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersApplied => _callbacks.ApplyRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockRestraint(MainHub.OwnUserData),
                    DataUpdateType.LayersRemoved => _callbacks.RemoveRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveRestraint(MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
            }
        }, _runtimeCTS.Token);
    }

    /// <summary>
    ///     Attempt to perform a RestraintSet related SelfBondage act and await its result. If one is in progress, it will be rejected.
    /// </summary>
    public async Task<bool> DoSelfRestraintResult(CharaActiveRestraint newData, DataUpdateType type)
    {
        // Reject if not available.
        if (!CanRunTask(SelfBondageType.Restraint))
        {
            _logger.LogWarning("Attempted to start a RestraintUpdate while another was still running. Action was rejected.");
            return false;
        }

        var taskToRun = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveRestraint(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.Swapped => _callbacks.SwapRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Applied => _callbacks.ApplyRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersChanged => _callbacks.SwapRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.LayersApplied => _callbacks.ApplyRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Locked => _callbacks.LockRestraint(retData, MainHub.OwnUserData),
                    DataUpdateType.Unlocked => _callbacks.UnlockRestraint(MainHub.OwnUserData),
                    DataUpdateType.LayersRemoved => _callbacks.RemoveRestraintLayers(retData, MainHub.OwnUserData),
                    DataUpdateType.Removed => _callbacks.RemoveRestraint(MainHub.OwnUserData),
                    _ => Task.CompletedTask
                };
                await applierTask.ConfigureAwait(false);
                return true;
            }
            return false;
        }, _runtimeCTS.Token);
        // Assign it 
        _updateTasks[SelfBondageType.Restraint] = taskToRun;
        // Await and return the result.
        return await taskToRun.ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempt to perform a self-invoked CollarUpdate act. If one is in progress, it will be rejected.
    /// </summary>
    public void DoSelfCollarUpdate(CharaActiveCollar newData, DataUpdateType type)
    {
        if (!CanRunTask(SelfBondageType.Collar))
        {
            _logger.LogWarning("Attempted to start a CollarUpdate while another was still running. Action was rejected.");
            return;
        }

        // Create the dictionary entry for it and assign the task to perform.
        _updateTasks[SelfBondageType.Collar] = Task.Run(async () =>
        {
            if (await _dds.PushNewActiveCollar(newData, type).ConfigureAwait(false) is { } retData)
            {
                var applierTask = type switch
                {
                    DataUpdateType.CollarRemoved => _callbacks.RemoveCollar(MainHub.OwnUserData),
                    // everything else is an update.
                    _ => _callbacks.UpdateActiveCollar(retData, MainHub.OwnUserData, type)
                };
                await applierTask.ConfigureAwait(false);
            }
        }, _runtimeCTS.Token);
    }
}
