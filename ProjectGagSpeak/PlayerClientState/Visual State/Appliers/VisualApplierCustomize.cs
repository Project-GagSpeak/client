using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Attempts, heavily attempts, to manage the Customize+ Fuckery IPC. </summary>
/// <remarks> Hopefully this doesn't explode my brain. </remarks>
public class VisualApplierCPlus : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerCustomize _customize;
    public VisualApplierCPlus(ILogger<VisualApplierCPlus> logger, GagspeakMediator mediator,
        IpcCallerCustomize customize) : base(logger, mediator)
    {
        _customize = customize;
        // If Customize+ Loads before GagSpeak, it would already be ready before
        // we could recognize it, so fetch the profiles.
        if(IpcCallerCustomize.APIAvailable) 
            OnCustomizeReady();

        Mediator.Subscribe<CustomizeReady>(this, (msg) => OnCustomizeReady());
    }

    public List<CustomizeProfile> LatestCustomizeProfiles { get; private set; } = new List<CustomizeProfile>();
    // This is the profile that we must ensure stays restricted at all times.
    private Tuple<Guid, int> RestrictedProfile = new Tuple<Guid, int>(Guid.Empty, 0);

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    public void OnCustomizeReady()
    {
        LatestCustomizeProfiles = _customize.GetAllProfiles();
        Logger.LogInformation("All CustomizePlus Profiles Retrieved!", LoggerType.IpcCustomize);
    }

    /// <summary> Called whenever a profile is updated. </summary>
    /// <remarks> Calls upon EnsureRestrictedProfile is the object is for the Client. </remarks>
    public void OnProfileUpdate(ushort characterObjectIndex, Guid g)
    {
        Logger.LogInformation("IPC-Customize received profile update for character " + characterObjectIndex + " with profile " + g, LoggerType.IpcCustomize);
        if (characterObjectIndex != 0)
            return;

        EnsureRestrictedProfile();
    }

    /// <summary> Ensures that the restricted profile is always enabled. </summary>
    /// <remarks> This is called whenever a profile is updated, or after a profile set/update call. </remarks>
    private void EnsureRestrictedProfile()
    {
        if (!IpcCallerCustomize.APIAvailable) 
            return;

        // if the sorted list does not contain any items, return.
        if (RestrictedProfile.Item1.IsEmptyGuid())
            return;

        // Grab the current active profile.
        var activeProfile = _customize.CurrentActiveProfile();

        if(activeProfile.ProfileId.IsEmptyGuid())
            return;

        // Ensure the item is staying enabled.
        if (activeProfile.ProfileId != RestrictedProfile.Item1)
        {
            Logger.LogTrace("Enforcing Customize+ Profile " + RestrictedProfile.Item1 + " for your equipped Gag", LoggerType.IpcCustomize);
            _customize.SetProfileEnable(RestrictedProfile.Item1);
        }
    }

    public void SetOrUpdateProfile(Guid profileId, int priority)
    {
        RestrictedProfile = new Tuple<Guid, int>(profileId, priority);
        Logger.LogInformation("Setting or Updating Profile " + profileId + " with new priority " + priority, LoggerType.IpcCustomize);
        EnsureRestrictedProfile();
    }

    public void ClearRestrictedProfile()
    {
        RestrictedProfile = new Tuple<Guid, int>(Guid.Empty, 0);
        Logger.LogInformation("Cleared Restricted Profile", LoggerType.IpcCustomize);
        // if any profile is active at the time of the clear, disable it.
        var activeProfile = _customize.CurrentActiveProfile();
        if (!activeProfile.ProfileId.IsEmptyGuid())
            _customize.SetProfileDisable(activeProfile.ProfileId);
    }
}
