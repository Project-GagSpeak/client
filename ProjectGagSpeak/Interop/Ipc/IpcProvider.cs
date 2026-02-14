using Dalamud.Plugin.Ipc;
using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Interop;

/// <summary>
///     <b>NOTE:</b><br />
///     A lot of this was functional, and recently all got erased without warning, as such it won't
///     be usable until until its author returns.
/// </summary>
public class IpcProvider : DisposableMediatorSubscriberBase, IHostedService
{
    private const int GagSpeakApiVersion = 2;

    private readonly KinksterManager _kinksters;

    private readonly Dictionary<nint, ProviderMoodleAccessTuple> _handledKinksters = [];

    // Plugin State events.
    private ICallGateProvider<int>    ApiVersion;
    private ICallGateProvider<object> Ready;
    private ICallGateProvider<object> Disposing;

    // IPC Events
    private ICallGateProvider<nint, object> PairRendered;   // When a kinkster becomes rendered.
    private ICallGateProvider<nint, object> PairUnrendered; // When a kinkster is no longer rendered.
    private ICallGateProvider<nint, object> AccessUpdated;  // A rendered pair's access permissions changed.

    // IPC Getters
    private ICallGateProvider<List<nint>>                                  GetAllRendered;     // Get rendered kinksters pointers.
    private ICallGateProvider<Dictionary<nint, ProviderMoodleAccessTuple>> GetAllRenderedInfo; // Get rendered kinksters & their access info) (could make list)
    private ICallGateProvider<nint, ProviderMoodleAccessTuple>             GetAccessInfo;      // Get a kinkster's access info.

    // IPC Event Actions (for Moodles)
    // private static ICallGateProvider<MoodlesStatusInfo, bool, object?>        ApplyStatusInfo;    // Apply a moodle tuple to the client actor.
    // private static ICallGateProvider<List<MoodlesStatusInfo>, bool, object?>  ApplyStatusInfoList;// Apply moodle tuples to the client actor.
    // private static ICallGateProvider<List<Guid>, object>                      LockIds;            // Locks statuses by their GUID on the Client.
    // private static ICallGateProvider<List<Guid>, object>                      UnlockIds;          // Unlocks statuses by their GUID on the Client.
    // private static ICallGateProvider<object>                                  ClearLocks;         // Removes all locks from the Client StatusManager.
    
    /// <summary>
    ///     --<br/>(From Moodles) a request to apply Moodles to another Kinkster. <para/>
    ///     <b>You send this message from your own Moodles Client.</b><br/>
    ///     This request is processed by GagSpeak and sent to that kinkster if allowed. <br/>
    ///     When the Kinkster receives it, they simply apply it to themselves.
    /// </summary>
    // private ICallGateProvider<nint, List<MoodlesStatusInfo>, bool, object?>? ApplyToPairRequest;

    public IpcProvider(ILogger<IpcProvider> logger, GagspeakMediator mediator, KinksterManager kinksters)
        : base(logger, mediator)
    {
        _kinksters = kinksters;

        // Should subscribe to characterActorCreated or rendered / unrendered events.
        Mediator.Subscribe<KinksterPlayerRendered>(this, _ =>
        {
            // Add to handled kinksters.
            _handledKinksters.TryAdd(_.Handler.Address, _.Kinkster.ToAccessTuple().ToCallGate());
            NotifyPairRendered(_.Handler.Address);
        });

        Mediator.Subscribe<KinksterPlayerUnrendered>(this, _ =>
        {
            // Remove from handled kinksters.
            _handledKinksters.Remove(_.Address, out var removed);
            NotifyPairUnrendered(_.Address);
        });

        Mediator.Subscribe<MoodleAccessPermsChanged>(this, _ =>
        {
            // Update the permission if they are rendered.
            if (!_.Kinkster.IsRendered)
                return;
            // Update the access permissions.
            _handledKinksters[_.Kinkster.PlayerAddress] = _.Kinkster.ToAccessTuple().ToCallGate();
            NotifyAccessUpdated(_.Kinkster.PlayerAddress);
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Starting IpcProvider");
        // ===========================
        // ---- IPC REGISTRATIONS ----
        // ===========================
        // Init API
        ApiVersion = Svc.PluginInterface.GetIpcProvider<int>("GagSpeak.GetApiVersion");
        // Init Events
        Ready = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Ready");
        Disposing = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.Disposing");
        // Init renderedList events.
        PairRendered = Svc.PluginInterface.GetIpcProvider<nint, object>("GagSpeak.PairRendered");
        PairUnrendered = Svc.PluginInterface.GetIpcProvider<nint, object>("GagSpeak.PairUnrendered");
        AccessUpdated = Svc.PluginInterface.GetIpcProvider<nint, object>("GagSpeak.AccessUpdated");
        // Init Getters
        GetAllRendered = Svc.PluginInterface.GetIpcProvider<List<nint>>("GagSpeak.GetAllRendered");
        GetAllRenderedInfo = Svc.PluginInterface.GetIpcProvider<Dictionary<nint, ProviderMoodleAccessTuple>>("GagSpeak.GetAllRenderedInfo");
        GetAccessInfo = Svc.PluginInterface.GetIpcProvider<nint, ProviderMoodleAccessTuple>("GagSpeak.GetAccessInfo");
        // Init appliers
        
        // -- All of the below did work until it all got erased :> --
        //ApplyStatusInfo = Svc.PluginInterface.GetIpcProvider<MoodlesStatusInfo, bool, object?>("GagSpeak.ApplyStatusInfo");
        //ApplyStatusInfoList = Svc.PluginInterface.GetIpcProvider<List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyStatusInfoList");
        //LockIds = Svc.PluginInterface.GetIpcProvider<List<Guid>, object>("GagSpeak.LockMoodleStatusIds");
        //UnlockIds = Svc.PluginInterface.GetIpcProvider<List<Guid>, object>("GagSpeak.UnlockMoodleStatusIds");
        //ClearLocks = Svc.PluginInterface.GetIpcProvider<object>("GagSpeak.ClearMoodleStatusLocks");
        //ApplyToPairRequest = Svc.PluginInterface.GetIpcProvider<nint, List<MoodlesStatusInfo>, bool, object?>("GagSpeak.ApplyToPairRequest");
        
        // =====================================
        // ---- FUNC & ACTION REGISTRATIONS ----
        // =====================================
        // By Registering a func, or action, we declare that this IPC Provider when called returns a value.
        // This distguishes it from being invokable by us, versus invokable by other plugins.
        ApiVersion.RegisterFunc(() => GagSpeakApiVersion);
        GetAllRendered.RegisterFunc(() => _handledKinksters.Keys.ToList());
        GetAllRenderedInfo.RegisterFunc(() => new Dictionary<nint, ProviderMoodleAccessTuple>(_handledKinksters));
        GetAccessInfo.RegisterFunc((address) => _handledKinksters.TryGetValue(address, out var access) ? access : (0, 0, 0, 0));
        // Register the action that moodles can call upon.
        // ApplyToPairRequest.RegisterAction(ProcessApplyToPairRequest);

        Logger.LogInformation("Started IpcProviderService");
        NotifyReady();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Stopping IpcProvider Service");
        NotifyDisposing();

        ApiVersion?.UnregisterFunc();
        Ready?.UnregisterFunc();
        Disposing?.UnregisterFunc();
        // unregister the event actions.
        PairRendered?.UnregisterAction();
        PairUnrendered?.UnregisterAction();
        AccessUpdated?.UnregisterAction();
        // unregister the functions for getters.
        GetAllRendered?.UnregisterFunc();
        GetAllRenderedInfo?.UnregisterFunc();
        GetAccessInfo?.UnregisterFunc();
        
        // ApplyToPairRequest?.UnregisterAction();

        Mediator.UnsubscribeAll(this);
        return Task.CompletedTask;
    }

    private void NotifyReady() => Ready?.SendMessage();
    private void NotifyDisposing() => Disposing?.SendMessage();
    private void NotifyPairRendered(nint pairPtr) => PairRendered?.SendMessage(pairPtr);
    private void NotifyPairUnrendered(nint pairPtr) => PairUnrendered?.SendMessage(pairPtr);
    private void NotifyAccessUpdated(nint pairPtr) => AccessUpdated?.SendMessage(pairPtr);

    ///// <summary>
    /////     Applies a <see cref="MoodlesStatusInfo"/> tuple to the CLIENT ONLY via Moodles. <para />
    /////     This helps account for trying on Moodle Presets, or applying the preset's StatusTuples. <para />
    /////     Method is invoked via GagSpeak's IpcProvider to prevent miss-use of bypassing permissions.
    ///// </summary>
    //internal static void ApplyStatusTuple(MoodlesStatusInfo status, bool lockStatus) => ApplyStatusInfo?.SendMessage(status, lockStatus);

    ///// <summary>
    /////     Applies a group of <see cref="MoodlesStatusInfo"/> tuples to the CLIENT ONLY via Moodles. <para />
    /////     This helps account for trying on Moodle Presets, or applying the preset's StatusTuples. <para />
    /////     Method is invoked via GagSpeak's IpcProvider to prevent miss-use of bypassing permissions.
    ///// </summary>
    //internal static void ApplyStatusTuples(IEnumerable<MoodlesStatusInfo> statuses, bool lockStatuses) => ApplyStatusInfoList?.SendMessage(statuses.ToList(), lockStatuses);

    ///// <summary>
    /////     Locks the select GUID's, if currently present in the Client's StatusManager. <para/>
    /////     Locked ID's cannot be right clicked off, and can only be removed via Unlock or Clear 
    /////     methods, or on plugin shutdown.
    ///// </summary>
    //internal static void LockClientStatuses(List<Guid> ids) => LockIds?.SendMessage(ids);

    ///// <summary>
    /////     Unlocks Statuses from the Client's StatusManager by their GUID's, if they are currently locked.
    ///// </summary>
    //internal static void UnlockClientStatuses(List<Guid> ids) => UnlockIds?.SendMessage(ids);

    ///// <summary>
    /////     Clears all locks from the Client's StatusManager.
    ///// </summary>
    //internal static void ClearClientLocks() => ClearLocks?.SendMessage();


    // Used to ensure integrity before pushing update to the server.
    //private void ProcessApplyToPairRequest(nint recipientAddr, List<MoodlesStatusInfo> toApply, bool isPreset)
    //{
    //    if (_kinksters.DirectPairs.FirstOrDefault(p => p.IsRendered && p.PlayerAddress == recipientAddr) is not { } pair)
    //        return;

    //    // Validate.
    //    foreach (var status in toApply)
    //        if (!IsStatusValid(status, out var errorMsg))
    //        {
    //            Logger.LogWarning(errorMsg);
    //            return;
    //        }

    //    // If valid, publish
    //    Mediator.Publish(new MoodlesApplyStatusToPair(new(pair.UserData, toApply, false)));

    //    bool IsStatusValid(MoodlesStatusInfo status, [NotNullWhen(false)] out string? error)
    //    {
    //        if (!pair.PairPerms.MoodleAccess.HasAny(MoodleAccess.AllowOther))
    //            return (error = "Attempted to apply to a pair without 'AllowOther' active.") is null;
    //        else if (!pair.PairPerms.MoodleAccess.HasAny(MoodleAccess.Positive))
    //            return (error = "Pair does not allow application of Moodles with positive status types.") is null;
    //        else if (!pair.PairPerms.MoodleAccess.HasAny(MoodleAccess.Negative))
    //            return (error = "Pair does not allow application of Moodles with negative status types.") is null;
    //        else if (!pair.PairPerms.MoodleAccess.HasAny(MoodleAccess.Special))
    //            return (error = "Pair does not allow application of Moodles with special status types.") is null;
    //        else if (!pair.PairPerms.MoodleAccess.HasAny(MoodleAccess.Permanent) && status.ExpireTicks == -1)
    //            return (error = "Pair does not allow application of permanent Moodles.") is null;
    //        else if (pair.PairPerms.MaxMoodleTime < TimeSpan.FromMilliseconds(status.ExpireTicks))
    //            return (error = "Moodle duration of requested Moodle was longer than the pair allows!") is null;

    //        return (error = null) is null;
    //    }
    //}
}

