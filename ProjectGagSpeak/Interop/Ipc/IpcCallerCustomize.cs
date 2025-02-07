using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Enums;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.Interop;

public sealed class IpcCallerCustomize : DisposableMediatorSubscriberBase, IIpcCaller
{
    /* ------------- Class Attributes ------------- */
    private readonly OnFrameworkService _frameworkUtils;
    private readonly IpcFastUpdates _fastUpdates;

    /* --------- Glamourer API Event Subscribers -------- */
    // called when our client updates state of any profile in their customizePlus
    // use this to prevent unwanted sets or disables while restrained.
    private readonly ICallGateSubscriber<ushort, Guid, object> _onProfileUpdate;

    /* ---------- Glamourer API IPC Subscribers --------- */
    // MAINTAINERS NOTE: Majority of the IPC calls here are made with the intent for YOU to call upon CUSTOMIZE+ to execute actions for YOURSELF.
    // This means that most of the actions we will call here, are triggered by client callbacks coming from the server forcing us to change something.

    private readonly ICallGateSubscriber<(int, int)> _apiVersion;
    private readonly ICallGateSubscriber<IList<IPCProfileDataTuple>> _getProfileList; // fetches our clients profileList
    private readonly ICallGateSubscriber<ushort, (int, Guid?)> _getActiveProfile; // fetches currently active profile.
    private readonly ICallGateSubscriber<Guid, int> _enableProfileByUniqueId; // enabled a particular profile via its GUID
    private readonly ICallGateSubscriber<Guid, int> _disableProfileByUniqueId; // disables a particular profile via its GUID

    public IpcCallerCustomize(ILogger<IpcCallerCustomize> logger,
        GagspeakMediator mediator, OnFrameworkService frameworkUtils,
        IpcFastUpdates fastUpdates, IDalamudPluginInterface pluginInterface, 
        IClientState clientState) : base(logger, mediator)
    {
        // remember that we made a disposable mediator subscriber base. if we no longer need it when all is said and done, remove it.
        _frameworkUtils = frameworkUtils;
        _fastUpdates = fastUpdates;

        // setup IPC subscribers
        _apiVersion = pluginInterface.GetIpcSubscriber<(int, int)>("CustomizePlus.General.GetApiVersion");
        _getProfileList = pluginInterface.GetIpcSubscriber<IList<IPCProfileDataTuple>>("CustomizePlus.Profile.GetList");
        _getActiveProfile = pluginInterface.GetIpcSubscriber<ushort, (int, Guid?)>("CustomizePlus.Profile.GetActiveProfileIdOnCharacter");
        _enableProfileByUniqueId = pluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.EnableByUniqueId");
        _disableProfileByUniqueId = pluginInterface.GetIpcSubscriber<Guid, int>("CustomizePlus.Profile.DisableByUniqueId");

        // set up event subscribers
        _onProfileUpdate = pluginInterface.GetIpcSubscriber<ushort, Guid, object>("CustomizePlus.Profile.OnUpdate");

        // subscribe to events.
        _onProfileUpdate.Subscribe(OnProfileUpdate);

        // check API status.
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;

    public void CheckAPI()
    {
        bool previousState = APIAvailable;

        try
        {
            var version = _apiVersion.InvokeFunc();
            APIAvailable = (version.Item1 == 6 && version.Item2 >= 0);
        }
        catch
        {
            APIAvailable = false;
        }

        if (APIAvailable != previousState)
        {
            if (APIAvailable)
            {
                Logger.LogInformation("Customize+ API is now available.", LoggerType.IpcCustomize);
                Mediator.Publish(new CustomizeReady());
            }
            else
            {
                Logger.LogInformation("Customize+ API is now disconnected.", LoggerType.IpcCustomize);
                Mediator.Publish(new CustomizeDispose());
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _onProfileUpdate.Unsubscribe(OnProfileUpdate);
        base.Dispose(disposing);
    }

    public List<CustomizeProfile> GetProfileList()
    {
        if (!APIAvailable)
        {
            Logger.LogWarning("Customize+ API is not available, returning empty list.", LoggerType.IpcCustomize);
            return new List<CustomizeProfile>();
        }

        Logger.LogTrace("IPC-Customize is fetching profile list.", LoggerType.IpcCustomize);
        var res = _getProfileList.InvokeFunc();
        return res.Select(tuple => new CustomizeProfile(tuple.UniqueId, tuple.Name)).ToList();
    }

    public Guid? GetActiveProfile()
    {
        if (!APIAvailable) return Guid.Empty;

        var result = _getActiveProfile.InvokeFunc(0);
        // log result and return it
        Logger.LogTrace($"IPC-Customize obtained active profile [{result.Item2}] with error code [{result.Item1}]", LoggerType.IpcCustomize);
        return result.Item2;
    }

    public void EnableProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;

        Logger.LogTrace("IPC-Customize is enabling profile "+ profileIdentifier, LoggerType.IpcCustomize);
        _enableProfileByUniqueId.InvokeFunc(profileIdentifier);
    }

    public void DisableProfile(Guid profileIdentifier)
    {
        if (!APIAvailable) return;
        try
        {
            Logger.LogTrace("IPC-Customize is disabling profile ["+profileIdentifier+"]", LoggerType.IpcCustomize);
            _disableProfileByUniqueId!.InvokeFunc(profileIdentifier);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "IPC-Customize failed to disable profile ["+profileIdentifier+"]", LoggerType.IpcCustomize);
        }
    }

    private void OnProfileUpdate(ushort c, Guid g)
    {
        Logger.LogInformation("IPC-Customize received profile update for character "+c+" with profile "+g, LoggerType.IpcCustomize);
        if(c == 0) // if the character is our own character
        {
            // publish a message to our mediator to let our other services know that our profile has changed.
            IpcFastUpdates.InvokeCustomize(g);
        }
    }
}
