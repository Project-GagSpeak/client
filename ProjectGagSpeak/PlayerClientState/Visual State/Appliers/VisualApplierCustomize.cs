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

    public static List<CustomizeProfile> LatestProfiles { get; private set; } = new List<CustomizeProfile>();
    // This is the profile that we must ensure stays restricted at all times.
    private CustomizeProfile RestrictedProfile = CustomizeProfile.Empty;

    protected override void Dispose(bool disposing) => base.Dispose(disposing);

    /// <summary> Fetches all information from moodles we can cache and store. (Presets, Statuses, StatusManager). </summary>
    /// <remarks> This will fire every time that Moodles Plugin initializes. </remarks>
    public void OnCustomizeReady()
    {
        LatestProfiles = _customize.GetAllProfiles();
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
        if (RestrictedProfile.ProfileGuid.IsEmptyGuid())
            return;

        // Grab the current active profile.
        var activeProfile = _customize.CurrentActiveProfile();

        if(activeProfile.ProfileGuid.IsEmptyGuid())
            return;

        // Ensure the item is staying enabled.
        if (activeProfile.ProfileGuid != RestrictedProfile.ProfileGuid)
        {
            Logger.LogTrace($"Enforcing C+ Profile {RestrictedProfile.ProfileGuid} for your Gag", LoggerType.IpcCustomize);
            _customize.SetProfileEnable(RestrictedProfile.ProfileGuid);
        }
    }

    public void SetOrUpdateProfile(CustomizeProfile profile)
    {
        RestrictedProfile = profile;
        Logger.LogInformation($"Setting or Updating Profile {profile.ProfileGuid} with new priority {profile.Priority}", LoggerType.IpcCustomize);
        EnsureRestrictedProfile();
    }

    public void ClearRestrictedProfile()
    {
        RestrictedProfile = CustomizeProfile.Empty;
        Logger.LogInformation("Cleared Restricted Profile", LoggerType.IpcCustomize);
        // if any profile is active at the time of the clear, disable it.
        var activeProfile = _customize.CurrentActiveProfile();
        if (!activeProfile.ProfileGuid.IsEmptyGuid())
            _customize.SetProfileDisable(activeProfile.ProfileGuid);
    }
}
