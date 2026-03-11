using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Interop;

public sealed class IpcCallerLoci : IIpcCaller
{
    internal const string LOCI_IDENTIFIER = "GagSpeak";
    internal const uint LOCI_KEY = 696465; // 69GAGS

    private readonly ICallGateSubscriber<int> ApiVersion;

    public readonly ICallGateSubscriber<object>             OnReady;
    public readonly ICallGateSubscriber<object>             OnDisposing;
    public readonly ICallGateSubscriber<nint, object>       OnManagerModified;
    public readonly ICallGateSubscriber<Guid, bool, object> OnStatusUpdated;
    public readonly ICallGateSubscriber<Guid, bool, object> OnPresetUpdated;

    public readonly ICallGateSubscriber<nint, string, LociStatusInfo, object>       OnTargetApplyStatus;
    public readonly ICallGateSubscriber<nint, string, List<LociStatusInfo>, object> OnTargetApplyStatuses;

    // Actor Registry
    private static ICallGateSubscriber<nint, string, bool>   RegisterActorByPtr;
    private static ICallGateSubscriber<string, string, bool> RegisterActorByName;
    private static ICallGateSubscriber<nint, string, bool>   UnregisterActorByPtr;
    private static ICallGateSubscriber<string, string, bool> UnregisterActorByName;

    // Client Locking
    private static ICallGateSubscriber<Guid, uint, bool>                     LockStatus;
    private static ICallGateSubscriber<List<Guid>, uint, (bool, List<Guid>)> LockStatuses;
    private static ICallGateSubscriber<Guid, uint, bool>                     UnlockStatus;
    private static ICallGateSubscriber<List<Guid>, uint, (bool, List<Guid>)> UnlockStatuses;
    private static ICallGateSubscriber<uint, bool>                           ClearLocks;

    // Status Managers
    private readonly ICallGateSubscriber<string>         GetOwnManager;
    private readonly ICallGateSubscriber<nint, string>   GetManagerByPtr;

    private readonly ICallGateSubscriber<List<LociStatusInfo>>         GetOwnManagerInfo;
    private readonly ICallGateSubscriber<nint, List<LociStatusInfo>>   GetManagerInfoByPtr;

    // Data Aquisition
    private readonly ICallGateSubscriber<Guid, LociStatusInfo> GetStatusInfo;
    private readonly ICallGateSubscriber<List<LociStatusInfo>> GetStatusInfoAll;
    private readonly ICallGateSubscriber<Guid, LociPresetInfo> GetPresetInfo;
    private readonly ICallGateSubscriber<List<LociPresetInfo>> GetPresetsInfoAll;

    // Apply by ID
    private readonly ICallGateSubscriber<Guid, object>         ApplyStatus;
    private readonly ICallGateSubscriber<Guid, nint, object>   ApplyStatusByPtr;
    private readonly ICallGateSubscriber<Guid, uint, bool>     ApplyLockedStatus;
    private readonly ICallGateSubscriber<Guid, nint, object>   ApplyPresetByPtr;

    // Apply bulk by ID
    private readonly ICallGateSubscriber<List<Guid>, object>         ApplyStatuses;
    private readonly ICallGateSubscriber<List<Guid>, nint, object>   ApplyStatusesByPtr;
    private readonly ICallGateSubscriber<List<Guid>, uint, bool>     ApplyLockedStatuses;
    private readonly ICallGateSubscriber<List<Guid>, nint, object>   ApplyPresetsByPtr;

    // Apply by Tuple
    private readonly ICallGateSubscriber<LociStatusInfo, object>           ApplyStatusInfo;
    private readonly ICallGateSubscriber<LociStatusInfo, uint, bool>       ApplyLockedStatusInfo;
    private readonly ICallGateSubscriber<List<LociStatusInfo>, object>     ApplyStatusInfos;
    private readonly ICallGateSubscriber<List<LociStatusInfo>, uint, bool> ApplyLockedStatusInfos;

    // Removal by ID
    private readonly ICallGateSubscriber<Guid, bool>         RemoveStatus;
    private readonly ICallGateSubscriber<List<Guid>, object> RemoveStatuses;


    private readonly GagspeakMediator _mediator;

    public IpcCallerLoci(GagspeakMediator mediator)
    {
        _mediator = mediator;

        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<int>("Loci.GetApiVersion");
        OnReady = Svc.PluginInterface.GetIpcSubscriber<object>("Loci.Ready");
        OnDisposing = Svc.PluginInterface.GetIpcSubscriber<object>("Loci.Disposing");
        // Events
        OnManagerModified = Svc.PluginInterface.GetIpcSubscriber<nint, object>("Loci.OnManagerModified");
        OnStatusUpdated = Svc.PluginInterface.GetIpcSubscriber<Guid, bool, object>("Loci.OnStatusUpdated");
        OnPresetUpdated = Svc.PluginInterface.GetIpcSubscriber<Guid, bool, object>("Loci.OnPresetUpdated");
        OnTargetApplyStatus = Svc.PluginInterface.GetIpcSubscriber<nint, string, LociStatusInfo, object>("Loci.OnTargetApplyStatus");
        OnTargetApplyStatuses = Svc.PluginInterface.GetIpcSubscriber<nint, string, List<LociStatusInfo>, object>("Loci.OnTargetApplyStatus");
        // SM Control
        RegisterActorByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, string, bool>("Loci.RegisterActorByPtr");
        RegisterActorByName = Svc.PluginInterface.GetIpcSubscriber<string, string, bool>("Loci.RegisterActorByName");
        UnregisterActorByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, string, bool>("Loci.UnregisterActorByPtr");
        UnregisterActorByName = Svc.PluginInterface.GetIpcSubscriber<string, string, bool>("Loci.UnregisterActorByName");
        // Locking
        LockStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, uint, bool>("Loci.LockStatus");
        LockStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, uint, (bool, List<Guid>)>("Loci.LockStatuses");
        UnlockStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, uint, bool>("Loci.UnlockStatus");
        UnlockStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, uint, (bool, List<Guid>)>("Loci.UnlockStatuses");
        ClearLocks = Svc.PluginInterface.GetIpcSubscriber<uint, bool>("Loci.ClearLocks");
        // Status Managers
        GetOwnManager = Svc.PluginInterface.GetIpcSubscriber<string>("Loci.GetOwnManager");
        GetManagerByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, string>("Loci.GetManagerByPtr");
        GetOwnManagerInfo = Svc.PluginInterface.GetIpcSubscriber<List<LociStatusInfo>>("Loci.GetOwnManagerInfo");
        GetManagerInfoByPtr = Svc.PluginInterface.GetIpcSubscriber<nint, List<LociStatusInfo>>("Loci.GetManagerInfoByPtr");
        // Data Aquision
        GetStatusInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, LociStatusInfo>("Loci.GetStatusInfo");
        GetStatusInfoAll = Svc.PluginInterface.GetIpcSubscriber<List<LociStatusInfo>>("Loci.GetAllStatusInfo");
        GetPresetInfo = Svc.PluginInterface.GetIpcSubscriber<Guid, LociPresetInfo>("Loci.GetPresetInfo");
        GetPresetsInfoAll = Svc.PluginInterface.GetIpcSubscriber<List<LociPresetInfo>>("Loci.GetAllPresetInfo");
        // Client Application
        // Single ID Application
        ApplyStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, object>("Loci.ApplyStatus");
        ApplyStatusByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Loci.ApplyStatusByPtr");
        ApplyLockedStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, uint, bool>("Loci.ApplyLockedStatus");
        ApplyPresetByPtr = Svc.PluginInterface.GetIpcSubscriber<Guid, nint, object>("Loci.ApplyPresetByPtr");
        // Bulk ID Application
        ApplyStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, object>("Loci.ApplyStatuses");
        ApplyStatusesByPtr = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, nint, object>("Loci.ApplyStatusesByPtr");
        ApplyLockedStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, uint, bool>("Loci.ApplyLockedStatuses");
        ApplyPresetsByPtr = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, nint, object>("Loci.ApplyPresetsByPtr");
        // Tuple Application
        ApplyStatusInfo = Svc.PluginInterface.GetIpcSubscriber<LociStatusInfo, object>("Loci.ApplyStatusInfo");
        ApplyLockedStatusInfo = Svc.PluginInterface.GetIpcSubscriber<LociStatusInfo, uint, bool>("Loci.ApplyLockedStatusInfo");
        ApplyStatusInfos = Svc.PluginInterface.GetIpcSubscriber<List<LociStatusInfo>, object>("Loci.ApplyStatusInfos");
        ApplyLockedStatusInfos = Svc.PluginInterface.GetIpcSubscriber<List<LociStatusInfo>, uint, bool>("Loci.ApplyLockedStatusInfos");
        // Removal
        RemoveStatus = Svc.PluginInterface.GetIpcSubscriber<Guid, bool>("Loci.RemoveStatus");
        RemoveStatuses = Svc.PluginInterface.GetIpcSubscriber<List<Guid>, object>("Loci.RemoveStatuses");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var result = ApiVersion.InvokeFunc() == 2;
            if (!APIAvailable && result)
                _mediator.Publish(new LociReady());
            if (APIAvailable && !result)
                _mediator.Publish(new LociDisposed());
            APIAvailable = result;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public void Dispose()
    {
        // Bawawa
    }

    #region Registry
    public async Task<bool> Register(nint address)
    {
        if (!APIAvailable) return false;
        return await Svc.Framework.RunOnFrameworkThread(() => RegisterActorByPtr.InvokeFunc(address, LOCI_IDENTIFIER)).ConfigureAwait(false);
    }
    public async Task<bool> Register(string nameWorld)
    {
        if (!APIAvailable) return false;
        return await Svc.Framework.RunOnFrameworkThread(() => RegisterActorByName.InvokeFunc(nameWorld, LOCI_IDENTIFIER)).ConfigureAwait(false);
    }

    public async Task<bool> Unregister(nint address)
    {
        if (!APIAvailable) return false;
        return await Svc.Framework.RunOnFrameworkThread(() => UnregisterActorByPtr.InvokeFunc(address, LOCI_IDENTIFIER)).ConfigureAwait(false);
    }
    public async Task<bool> Unregister(string nameWorld)
    {
        if (!APIAvailable) return false;
        return await Svc.Framework.RunOnFrameworkThread(() => UnregisterActorByName.InvokeFunc(nameWorld, LOCI_IDENTIFIER)).ConfigureAwait(false);
    }
    #endregion Registry

    #region Managers
    public async Task<string> GetManagerStr()
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetOwnManager.InvokeFunc() ?? string.Empty).ConfigureAwait(false);
    }
    public async Task<string> GetManagerStr(nint address)
    {
        if (!APIAvailable) return string.Empty;
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerByPtr.InvokeFunc(address) ?? string.Empty).ConfigureAwait(false);
    }

    public async Task<List<LociStatusInfo>> GetManagerInfo()
    {
        if (!APIAvailable) return new List<LociStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(GetOwnManagerInfo.InvokeFunc).ConfigureAwait(false);
    }
    public async Task<List<LociStatusInfo>> GetManagerInfo(nint address)
    {
        if (!APIAvailable) return new List<LociStatusInfo>();
        return await Svc.Framework.RunOnFrameworkThread(() => GetManagerInfoByPtr.InvokeFunc(address)).ConfigureAwait(false);
    }
    #endregion Managers

    #region Data Collection
    public async Task<LociStatusInfo> GetStatusDetails(Guid guid)
    {
        if (!APIAvailable) return new LociStatusInfo();
        return await Svc.Framework.RunOnFrameworkThread(() => GetStatusInfo.InvokeFunc(guid)).ConfigureAwait(false);
    }
    public async Task<List<LociStatusInfo>> GetStatusListDetails()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetStatusInfoAll.InvokeFunc).ConfigureAwait(false);
    }
    public async Task<LociPresetInfo> GetPresetDetails(Guid guid)
    {
        if (!APIAvailable) return new LociPresetInfo();
        return await Svc.Framework.RunOnFrameworkThread(() => GetPresetInfo.InvokeFunc(guid)).ConfigureAwait(false);
    }
    public async Task<List<LociPresetInfo>> GetPresetListDetails()
    {
        if (!APIAvailable) return [];
        return await Svc.Framework.RunOnFrameworkThread(GetPresetsInfoAll.InvokeFunc).ConfigureAwait(false);
    }
    #endregion Data Collection

    #region Moodle Manipulation
    public async Task ApplyLociStatus(Guid id, bool lockData)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (lockData)
                ApplyLockedStatus.InvokeAction(id, LOCI_KEY);
            else
                ApplyStatus.InvokeAction(id);
        }).ConfigureAwait(false);
    }

    public async Task ApplyLociStatus(LociStatusInfo info, bool lockData)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (lockData)
                ApplyLockedStatusInfo.InvokeAction(info, LOCI_KEY);
            else
                ApplyStatusInfo.InvokeAction(info);
        }).ConfigureAwait(false);
    }

    public async Task ApplyLociStatus(IEnumerable<Guid> ids, bool lockData)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (lockData)
                ApplyLockedStatuses.InvokeAction(ids.ToList(), LOCI_KEY);
            else
                ApplyStatuses.InvokeAction(ids.ToList());
        }).ConfigureAwait(false);
    }

    public async Task ApplyLociStatus(IEnumerable<LociStatusInfo> infos, bool lockData)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (lockData)
                ApplyLockedStatusInfos.InvokeAction(infos.ToList(), LOCI_KEY);
            else
                ApplyStatusInfos.InvokeAction(infos.ToList());
        }).ConfigureAwait(false);
    }

    public async Task ApplyPresetID(Guid guid)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ApplyPresetByPtr.InvokeAction(guid, PlayerData.Address)).ConfigureAwait(false);
    }

    public async Task RemoveStatusID(Guid id, bool unlock)
    {
        if (!APIAvailable) return;
        // Unlock first if requested
        if (unlock)
             await Svc.Framework.RunOnFrameworkThread(() => UnlockStatus.InvokeAction(id, LOCI_KEY)).ConfigureAwait(false);
        // Then remove it
        await Svc.Framework.RunOnFrameworkThread(() => RemoveStatus.InvokeAction(id)).ConfigureAwait(false);
    }

    public async Task RemoveStatusID(IEnumerable<Guid> ids, bool unlock)
    {
        if (!APIAvailable) return;
        // Unlock first if requested
        if (unlock)
            await Svc.Framework.RunOnFrameworkThread(() => UnlockStatuses.InvokeAction(ids.ToList(), LOCI_KEY)).ConfigureAwait(false);
        // Then remove them
        await Svc.Framework.RunOnFrameworkThread(() => RemoveStatuses.InvokeAction(ids.ToList())).ConfigureAwait(false);
    }

    public async Task LockStatusID(Guid id)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => LockStatus.InvokeAction(id, LOCI_KEY)).ConfigureAwait(false);
    }

    public async Task LockStatusID(IEnumerable<Guid> ids)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => LockStatuses.InvokeAction(ids.ToList(), LOCI_KEY)).ConfigureAwait(false);
    }

    public async Task UnlockStatusID(Guid id)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => UnlockStatus.InvokeAction(id, LOCI_KEY)).ConfigureAwait(false);
    }

    public async Task UnlockStatusID(IEnumerable<Guid> ids)
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => UnlockStatuses.InvokeAction(ids.ToList(), LOCI_KEY)).ConfigureAwait(false);
    }

    public async Task ClearLocked()
    {
        if (!APIAvailable) return;
        await Svc.Framework.RunOnFrameworkThread(() => ClearLocks.InvokeAction(LOCI_KEY)).ConfigureAwait(false);
    }
    #endregion Moodle Manipulation
}
