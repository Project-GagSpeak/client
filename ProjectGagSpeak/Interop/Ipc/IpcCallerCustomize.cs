using Dalamud.Plugin.Ipc;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.Interop;

public sealed class IpcCallerCustomize : DisposableMediatorSubscriberBase, IIpcCaller
{
    /* --------- Glamourer API Event Subscribers -------- */
    // called when our client updates state of any profile in their customizePlus. Use to prevent unwanted profiles being on.
    public readonly ICallGateSubscriber<ushort, Guid, object> OnProfileUpdate;

    /* ---------- Glamourer API IPC Subscribers --------- */
    // MAINTAINERS NOTE: Majority of the IPC calls here are made with the intent for YOU to call upon CUSTOMIZE+ to execute actions for YOURSELF.
    // This means that most of the actions we will call here, are triggered by client callbacks coming from the server forcing us to change something.
    private readonly ICallGateSubscriber<(int, int)>                 ApiVersion;
    private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> GetProfileList;
    private readonly ICallGateSubscriber<ushort, (int, Guid?)>       GetActiveProfile;
    private readonly ICallGateSubscriber<Guid, int>                  EnableProfile;
    private readonly ICallGateSubscriber<Guid, int>                  DisableProfile;

    public IpcCallerCustomize(ILogger<IpcCallerCustomize> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        // setup IPC subscribers
        ApiVersion = Svc.PluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        GetProfileList = Svc.PluginInterface.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
        GetActiveProfile = Svc.PluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        EnableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.EnableByUniqueId");
        DisableProfile = Svc.PluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");
        // set up event subscribers
        OnProfileUpdate = Svc.PluginInterface.GetIpcSubscriber<ushort, Guid, object>("CustomizePlus.Profile.OnUpdate");
        
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        try
        {
            var version = ApiVersion.InvokeFunc();
            var success = version.Item1 == 6 && version.Item2 >= 0;
            if(!APIAvailable && success)
                Mediator.Publish(new CustomizeReady());
            APIAvailable = success;
        }
        catch // We only catch when the Plugin is not active.
        {
            APIAvailable = false;
        }
    }

    public List<CustomizeProfile> GetAllProfiles()
    {
        if (!APIAvailable) return new List<CustomizeProfile>();
        try
        {
            Logger.LogTrace("IPC-Customize is fetching profile list.", LoggerType.IpcCustomize);
            var res = GetProfileList.InvokeFunc();
            return res.Select(tuple => new CustomizeProfile(tuple.UniqueId, tuple.Priority, tuple.Name)).ToList();

        }
        catch (Exception ex)
        {
            Logger.LogError("Error on fetching profile list" + ex, LoggerType.IpcCustomize);
            return new List<CustomizeProfile>();
        }
    }

    public CustomizeProfile CurrentActiveProfile()
    {
        if (!APIAvailable) return CustomizeProfile.Empty;
        try
        {
            var result = GetActiveProfile.InvokeFunc(0);
            if (result.Item2 is null) 
                return CustomizeProfile.Empty;
            Logger.LogTrace($"IPC-Customize obtained active profile [{result.Item2}] with error code [{result.Item1}]", LoggerType.IpcCustomize);
            return new(result.Item2.Value, result.Item1);
        }
        catch (Exception ex)
        {
            Logger.LogError("Error on fetching active profile" + ex, LoggerType.IpcCustomize);
            return CustomizeProfile.Empty;
        }
    }

    public void SetProfileEnable(Guid profileIdentifier)
    {
        if (!APIAvailable) return;

        Logger.LogTrace("IPC-Customize is enabling profile "+ profileIdentifier, LoggerType.IpcCustomize);
        ExecuteSafely(() => EnableProfile.InvokeFunc(profileIdentifier));
    }

    public void SetProfileDisable(Guid profileIdentifier)
    {
        if (!APIAvailable) return;
        Logger.LogTrace("IPC-Customize is disabling profile ["+profileIdentifier+"]", LoggerType.IpcCustomize);
        ExecuteSafely(() => DisableProfile!.InvokeFunc(profileIdentifier));
    }

    private void ExecuteSafely(Action act)
    {
        try { act(); } catch (Exception ex) { Logger.LogCritical(ex, "Error on executing safely"); }
    }
}
