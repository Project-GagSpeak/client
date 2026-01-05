using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.Interop;

public sealed class IpcCallerCustomize : DisposableMediatorSubscriberBase, IIpcCaller
{
    // Version Checks.
    private readonly ICallGateSubscriber<(int, int)> ApiVersion;

    // Event Calls.
    public readonly ICallGateSubscriber<ushort, Guid, object> OnProfileUpdate;

    // API Getters
    private readonly ICallGateSubscriber<ushort, (int, Guid?)> GetActiveProfile; // get the active profile of a Kinkster via object index.
    private readonly ICallGateSubscriber<Guid, (int, string?)> GetProfileById; // obtain that active profiles dataString by GUID.
    private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> GetProfileList; // grab the list of all client profile ids.

    // API Enactors
    private readonly ICallGateSubscriber<Guid, int> EnableProfile;
    private readonly ICallGateSubscriber<Guid, int> DisableProfile;

    private readonly ILogger<IpcCallerCustomize> _logger;
    private readonly GagspeakMediator _mediator;

    public IpcCallerCustomize(ILogger<IpcCallerCustomize> logger, GagspeakMediator mediator) : base(logger, mediator)
    {
        _logger = logger;
        _mediator = mediator;
        // API Version Check
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        // API Events
        OnProfileUpdate = Svc.PluginInterface.GetIpcSubscriber<ushort, Guid, object>("CustomizePlus.Profile.OnUpdate");
        // API Getter Functions
        GetActiveProfile = Svc.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        GetProfileById = Svc.PluginInterface.GetIpcSubscriber<Guid, (int, string?)>("CustomizePlus.Profile.GetByUniqueId");
        GetProfileList = Svc.PluginInterface.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
        // API Enactor Functions
        EnableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.EnableByUniqueId");
        DisableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");

        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var version = ApiVersion.InvokeFunc();
            var success = version.Item1 == 6 && version.Item2 >= 0;
            if (!APIAvailable && success)
                _mediator.Publish(new CustomizeReady());
            APIAvailable = success;
        }
        catch
        {
            APIAvailable = false;
        }
    }

    public CustomizeProfile CurrentActiveProfile()
    {
        if (!APIAvailable) return CustomizeProfile.Empty;

        var result = GetActiveProfile.InvokeFunc(0);
        if (result.Item2 is null)
            return CustomizeProfile.Empty;
        _logger.LogDebug($"Retrieved active profile [{result.Item2}] with EC: [{result.Item1}]", LoggerType.IpcCustomize);
        return new(result.Item2.Value, result.Item1);
    }

    public async Task<string> GetClientProfile()
    {
        if (!APIAvailable) return string.Empty;

        var profileStr = await Svc.Framework.RunOnFrameworkThread(() =>
        {
            var res = GetActiveProfile.InvokeFunc(0);
            if (res.Item1 != 0 || res.Item2 is null)
                return string.Empty;
            // get the valid str.
            return GetProfileById.InvokeFunc(res.Item2.Value).Item2 ?? string.Empty;
        }).ConfigureAwait(false);

        if (string.IsNullOrEmpty(profileStr))
            return string.Empty;
        // return the valid profile string.
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(profileStr));
    }

    public List<CustomizeProfile> GetClientProfiles()
    {
        if (!APIAvailable) return new List<CustomizeProfile>();

        _logger.LogTrace("IPC-Customize is fetching profile list.", LoggerType.IpcCustomize);
        var res = GetProfileList.InvokeFunc();
        return res.Select(tuple => new CustomizeProfile(tuple.UniqueId, tuple.Priority, tuple.Name)).ToList();
    }

    /// <summary>
    ///     Enables one of the client's personal profiles by GUID.
    /// </summary>
    public void EnableClientProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;

        _logger.LogTrace("IPC-Customize is enabling profile " + profileIdentifier, LoggerType.IpcCustomize);
        Generic.Safe(() => EnableProfile.InvokeFunc(profileIdentifier));
    }

    /// <summary>
    ///     Disables one of the client's personal profiles by GUID.
    /// </summary>
    public void DisableClientProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;
        _logger.LogTrace("IPC-Customize is disabling profile [" + profileIdentifier + "]", LoggerType.IpcCustomize);
        Generic.Safe(() => DisableProfile!.InvokeFunc(profileIdentifier));
    }
}
