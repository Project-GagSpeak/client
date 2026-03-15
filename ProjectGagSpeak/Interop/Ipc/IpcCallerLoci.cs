using GagSpeak.Services.Mediator;
using LociApi.Enums;
using LociApi.Helpers;
using LociApi.Ipc;

namespace GagSpeak.Interop;

public sealed class IpcCallerLoci : IIpcCaller
{
    internal const string GAGSPEAK_TAG = "GagSpeak";
    internal const uint GAGSPEAK_KEY = 696465; // 69GAGS

    private readonly ApiVersion ApiVersion;
    private readonly IsEnabled IsEnabled;
    // Events that can handled within this class to dicate enabled state.
    private readonly EventSubscriber Ready;
    private readonly EventSubscriber Disposed;
    private readonly EventSubscriber<bool> EnabledChanged;

    // API Registry
    private readonly RegisterByPtr Register;
    private readonly RegisterByName RegisterName;
    private readonly UnregisterByPtr Unregister;
    private readonly UnregisterByName UnregisterName;
    private readonly UnregisterAll UnregisterAll;

    // API StatusManagers (Mostly for data aquisition)
    private readonly GetManager GetManager;
    private readonly GetManagerByPtr GetManagerByPtr;
    private readonly GetManagerInfo GetManagerInfo;
    private readonly GetManagerInfoByPtr GetManagerInfoByPtr;

    // API Statuses
    private readonly GetStatusInfo GetStatusTuple;
    private readonly GetStatusInfoList GetAllStatuseTuples;
    private readonly ApplyStatus ApplyStatusById;
    private readonly ApplyStatuses ApplyStatusByIds;
    private readonly ApplyStatusInfo ApplyStatusTuple;
    private readonly ApplyStatusInfos ApplyStatusTuples;
    private readonly RemoveStatus RemoveStatus;
    private readonly RemoveStatuses RemoveStatuses;

    // Presets
    private readonly GetPresetInfo GetPresetTuple;
    private readonly GetPresetInfoList GetAllPresetTuples;
    private readonly ApplyPreset ApplyPresetById;
    private readonly ApplyPresetInfo ApplyPresetTuple;

    // Additional lock stuff
    private readonly CanLock IsLockable;
    private readonly LockStatus LockStatusById;
    private readonly LockStatuses LockStatusesById;
    private readonly UnlockStatus UnlockStatusById;
    private readonly UnlockStatuses UnlockStatusesById;
    private readonly UnlockAll ClearLocks;

    private readonly ILogger<IpcCallerLoci> _logger;
    private readonly GagspeakMediator _mediator;
    public IpcCallerLoci(ILogger<IpcCallerLoci> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;

        // Base
        ApiVersion = new ApiVersion(Svc.PluginInterface);
        IsEnabled = new IsEnabled(Svc.PluginInterface);
        Ready = LociApi.Ipc.Ready.Subscriber(Svc.PluginInterface, () =>
        {
            APIAvailable = true;
            FeaturesEnabled = IsEnabled.Invoke();
            _logger.LogDebug("Loci Enabled!", LoggerType.IpcLoci);
            mediator.Publish(new LociReady());
        });
        Disposed = LociApi.Ipc.Disposed.Subscriber(Svc.PluginInterface, () =>
        {
            APIAvailable = false;
            FeaturesEnabled = false;
            _logger.LogDebug("Loci Disabled!", LoggerType.IpcLoci);
            mediator.Publish(new LociDisposed());
        });
        EnabledChanged = EnabledStateChanged.Subscriber(Svc.PluginInterface, state => FeaturesEnabled = state);

        // Registry
        Register = new RegisterByPtr(Svc.PluginInterface);
        RegisterName = new RegisterByName(Svc.PluginInterface);
        Unregister = new UnregisterByPtr(Svc.PluginInterface);
        UnregisterName = new UnregisterByName(Svc.PluginInterface);
        UnregisterAll = new UnregisterAll(Svc.PluginInterface);
        // Status Managers
        GetManager = new GetManager(Svc.PluginInterface);
        GetManagerByPtr = new GetManagerByPtr(Svc.PluginInterface);
        GetManagerInfo = new GetManagerInfo(Svc.PluginInterface);
        GetManagerInfoByPtr = new GetManagerInfoByPtr(Svc.PluginInterface);
        // Statuses
        GetStatusTuple = new GetStatusInfo(Svc.PluginInterface);
        GetAllStatuseTuples = new GetStatusInfoList(Svc.PluginInterface);
        ApplyStatusById = new ApplyStatus(Svc.PluginInterface);
        ApplyStatusByIds = new ApplyStatuses(Svc.PluginInterface);
        ApplyStatusTuple = new ApplyStatusInfo(Svc.PluginInterface);
        ApplyStatusTuples = new ApplyStatusInfos(Svc.PluginInterface);
        RemoveStatus = new RemoveStatus(Svc.PluginInterface);
        RemoveStatuses = new RemoveStatuses(Svc.PluginInterface);
        // Presets
        GetPresetTuple = new GetPresetInfo(Svc.PluginInterface);
        GetAllPresetTuples = new GetPresetInfoList(Svc.PluginInterface);
        ApplyPresetById = new ApplyPreset(Svc.PluginInterface);
        ApplyPresetTuple = new ApplyPresetInfo(Svc.PluginInterface);
        // Lockables
        IsLockable = new CanLock(Svc.PluginInterface);
        LockStatusById = new LockStatus(Svc.PluginInterface);
        LockStatusesById = new LockStatuses(Svc.PluginInterface);
        UnlockStatusById = new UnlockStatus(Svc.PluginInterface);
        UnlockStatusesById = new UnlockStatuses(Svc.PluginInterface);
        ClearLocks = new UnlockAll(Svc.PluginInterface);

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;
    public static bool FeaturesEnabled { get; private set; } = false;
    public void Dispose()
    {
        Ready.Dispose();
        Disposed.Dispose();
        EnabledChanged.Dispose();
    }

    public void CheckAPI()
    {
        try
        {
            var version = ApiVersion.Invoke();
            APIAvailable = (version.Item1 == 1 && version.Item2 >= 0);
        }
        catch
        {
            APIAvailable = false;
        }
    }

    // Assuming we know an actor is valid, these calls could all run syncronously.

    /// <inheritdoc cref="LociApi.Ipc.RegisterByPtr"/>
    public async Task<bool> RegisterActor(nint address)
    {
        if (!APIAvailable) return false;
        var res = await Svc.Framework.RunOnFrameworkThread(() => Register.Invoke(address, GAGSPEAK_TAG)).ConfigureAwait(false);
        if (res is not (LociApiEc.Success or LociApiEc.NoChange))
            _logger.LogWarning($"Loci Failed to register Actor {address} with Loci! Error: {res}");
        return res is (LociApiEc.Success or LociApiEc.NoChange);
    }

    /// <inheritdoc cref="LociApi.Ipc.RegisterByName"/>
    public async Task<bool> RegisterPlayer(string playerNameWorld)
    {
        if (!APIAvailable) return false;
        var res = await Svc.Framework.RunOnFrameworkThread(() => RegisterName.Invoke(playerNameWorld, GAGSPEAK_TAG)).ConfigureAwait(false);
        if (res is not (LociApiEc.Success or LociApiEc.NoChange))
            _logger.LogWarning($"Loci Failed to register Player {playerNameWorld} with Loci! Error: {res}");
        return res is (LociApiEc.Success or LociApiEc.NoChange);
    }

    /// <inheritdoc cref="LociApi.Ipc.UnregisterByPtr"/>
    public async Task UnregisterActor(nint address)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => Unregister.Invoke(address, GAGSPEAK_TAG)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.UnregisterByName"/>
    public async Task UnregisterPlayer(string playerNameWorld)
    {
        if (!APIAvailable) return;
        var res = await Svc.Framework.RunOnFrameworkThread(() => UnregisterName.Invoke(playerNameWorld, GAGSPEAK_TAG)).ConfigureAwait(false);
        if (res is not (LociApiEc.Success or LociApiEc.NoChange))
            _logger.LogWarning($"Loci Failed to unregister Player {playerNameWorld} with Loci! Error: {res}");
    }

    /// <inheritdoc cref="LociApi.Ipc.UnregisterAll"/>
    public async Task HailMerryUnregister()
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => UnregisterAll.Invoke(GAGSPEAK_TAG)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetManager"/>
    public async Task<string> GetOwnManagerStr()
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetManager.Invoke().Item2 ?? string.Empty).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetManagerByPtr"/>
    public async Task<string> GetActorSMStr(nint actorAddr)
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerByPtr.Invoke(actorAddr).Item2 ?? string.Empty).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetManagerInfo"/>
    public async Task<List<LociStatusInfo>> GetOwnManagerInfo()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerInfo.Invoke()).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetManagerInfoByPtr"/>
    public async Task<List<LociStatusInfo>> GetActorSMInfo(nint actorAddr)
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerInfoByPtr.Invoke(actorAddr)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetStatusInfo"/>
    public async Task<LociStatusInfo> GetStatusInfo(Guid guid)
    {
        if (!APIAvailable) return default;
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusTuple.Invoke(guid).Item2).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetStatusInfoList"/>
    public async Task<List<LociStatusInfo>> GetStatusInfos()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetAllStatuseTuples.Invoke).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyStatus"/>
    public async Task ApplyStatus(Guid id, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyStatusById.Invoke(id, asLocked ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyStatuses"/>
    public async Task ApplyStatus(List<Guid> ids, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyStatusByIds.Invoke(ids, asLocked ? GAGSPEAK_KEY : 0, out _)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyStatusInfo"/>
    public async Task ApplyStatusInfo(LociStatusInfo tuple, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyStatusTuple.Invoke(tuple, asLocked ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyStatusInfos"/>
    public async Task ApplyStatusInfo(List<LociStatusInfo> tuples, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyStatusTuples.Invoke(tuples, asLocked ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.RemoveStatus"/>
    public async Task BombStatus(Guid id, bool useKey)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => RemoveStatus.Invoke(id, useKey ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.RemoveStatuses"/>
    public async Task BombStatus(List<Guid> ids, bool useKey)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => RemoveStatuses.Invoke(ids, useKey ? GAGSPEAK_KEY : 0, out _)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetPresetInfo"/>
    public async Task<LociPresetInfo> GetPresetInfo(Guid guid)
    {
        if (!APIAvailable) return default;
        return await Svc.Framework.RunOnFrameworkThread(() => GetPresetTuple.Invoke(guid).Item2).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.GetPresetInfoList"/>
    public async Task<List<LociPresetInfo>> GetPresetInfos()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetAllPresetTuples.Invoke).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyPreset"/>
    public async Task ApplyPreset(Guid id, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyPresetById.Invoke(id, asLocked ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.ApplyPresetInfo"/>
    public async Task ApplyPresetInfo(LociPresetInfo tuple, bool asLocked)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyPresetTuple.Invoke(tuple, asLocked ? GAGSPEAK_KEY : 0)).ConfigureAwait(false);
    }

    // All of the below should be able to all be called syncronously?..

    /// <inheritdoc cref="LociApi.Ipc.CanLock"/>
    public bool IsStatusLocked(Guid id)
    {
        if (!APIAvailable) return false;
        return IsLockable.Invoke(id);
    }

    /// <inheritdoc cref="LociApi.Ipc.LockStatus"/>
    public async Task LockStatus(Guid id)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => LockStatusById.Invoke(id, GAGSPEAK_KEY)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.LockStatuses"/>
    public async Task LockStatuses(List<Guid> ids)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => LockStatusesById.Invoke(ids, GAGSPEAK_KEY, out _)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.UnlockStatus"/>
    public async Task UnlockStatus(Guid id)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => UnlockStatusById.Invoke(id, GAGSPEAK_KEY)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.UnlockStatuses"/>
    public async Task UnlockStatuses(List<Guid> ids)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => UnlockStatusesById.Invoke(ids, GAGSPEAK_KEY, out _)).ConfigureAwait(false);
    }

    /// <inheritdoc cref="LociApi.Ipc.UnlockAll"/>
    public async Task ClearAllLocks()
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ClearLocks.Invoke(GAGSPEAK_KEY)).ConfigureAwait(false);
    }




}
