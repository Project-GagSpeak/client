using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.PlayerState.Visual;

public sealed partial class CPlusHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerCustomize _ipc;
    public CPlusHandler(ILogger<CPlusHandler> logger, GagspeakMediator mediator,
        IpcCallerCustomize ipc) : base(logger, mediator)
    {
        _ipc = ipc;

        _ipc.OnProfileUpdate.Subscribe(OnProfileUpdate);

        if (IpcCallerCustomize.APIAvailable) 
            OnCustomizeReady();

        Mediator.Subscribe<CustomizeReady>(this, (msg) => OnCustomizeReady());
    }

    public static List<CustomizeProfile> LatestProfiles { get; private set; } = new();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ipc.OnProfileUpdate.Unsubscribe(OnProfileUpdate);
    }

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    private void OnCustomizeReady()
    {
        LatestProfiles = _ipc.GetAllProfiles();
        Logger.LogInformation("All CustomizePlus Profiles Retrieved!", LoggerType.IpcCustomize);
    }

    /// <summary> Called whenever a profile is updated. </summary>
    /// <remarks> Calls upon EnsureRestrictedProfile is the object is for the Client. </remarks>
    private void OnProfileUpdate(ushort characterObjectIndex, Guid g)
    {
        Logger.LogInformation("IPC-Customize received profile update for character " + characterObjectIndex + " with profile " + g, LoggerType.IpcCustomize);
        if (characterObjectIndex != 0)
            return;

        EnsureRestrictedProfile();
    }

    private void EnsureRestrictedProfile()
    {
        if (!IpcCallerCustomize.APIAvailable) 
            return;

        if (_finalProfile.Equals(CustomizeProfile.Empty))
            return;

        // Grab the active profile
        var active = _ipc.CurrentActiveProfile();

        // Ensure the item is staying enabled.
        if (active.ProfileGuid != _finalProfile.ProfileGuid)
        {
            Logger.LogTrace($"C+ Profile [{_finalProfile.ProfileName}] found in Cache! Reapplying to enforce helplessness!", LoggerType.IpcCustomize);
            _ipc.SetProfileEnable(_finalProfile.ProfileGuid);
        }
    }

    public Task ApplyProfileCache()
    {
        if (_finalProfile.Equals(CustomizeProfile.Empty))
        {
            Logger.LogWarning("No C+ Profile found in Cache! Cannot apply profile cache.", LoggerType.IpcCustomize);
            if (_ipc.CurrentActiveProfile() is { } activeProfile && activeProfile.ProfileGuid != Guid.Empty)
                _ipc.SetProfileDisable(activeProfile.ProfileGuid);
            return Task.CompletedTask;
        }
        else
        {
            Logger.LogDebug($"Applying C+ Profile Cache ([{_finalProfile.ProfileName} - Priority {_finalProfile.Priority}]", LoggerType.IpcGlamourer);
            EnsureRestrictedProfile();
            return Task.CompletedTask;
        }
    }
}
