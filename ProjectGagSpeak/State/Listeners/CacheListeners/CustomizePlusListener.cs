using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;

namespace GagSpeak.State.Listeners;

public class CustomizePlusListener : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerCustomize _ipc; 
    private readonly CustomizePlusCache _cache;
    private readonly CustomizePlusHandler _handler;
    public CustomizePlusListener(
        ILogger<CustomizePlusListener> logger,
        GagspeakMediator mediator,
        IpcCallerCustomize ipc,
        CustomizePlusCache cache,
        CustomizePlusHandler handler)
        : base(logger, mediator)
    {
        _ipc = ipc;
        _cache = cache;
        _handler = handler;

        _ipc.OnProfileUpdate.Subscribe(OnProfileUpdate);

        if (IpcCallerCustomize.APIAvailable)
            FetchProfileList();

        Mediator.Subscribe<CustomizeReady>(this, _ => FetchProfileList());
        Mediator.Subscribe<CustomizeProfileListRequest>(this, _ => FetchProfileList());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnProfileUpdate.Unsubscribe(OnProfileUpdate);
    }

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    private void FetchProfileList()
    {
        _cache.UpdateIpcProfileList(_ipc.GetAllProfiles());
        Logger.LogInformation("All CustomizePlus Profiles Retrieved!", LoggerType.IpcCustomize);
    }

    /// <summary> Called whenever a profile is updated. </summary>
    /// <remarks> Calls upon EnsureRestrictedProfile is the object is for the Client. </remarks>
    private void OnProfileUpdate(ushort characterObjectIndex, Guid g)
    {
        Logger.LogInformation("IPC-Customize received profile update for character " + characterObjectIndex + " with profile " + g, LoggerType.IpcCustomize);
        if (characterObjectIndex != 0)
            return;

        _handler.EnsureRestrictedProfile();
    }
}
