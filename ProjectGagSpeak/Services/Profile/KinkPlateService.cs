using Dalamud.Interface.Textures;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Profile;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.Dto.User;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services;

public class KinkPlateService : MediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly KinkPlateFactory _profileFactory;

    // concurrent dictionary of cached profile data.
    private readonly ConcurrentDictionary<UserData, KinkPlate> _kinkPlates= new(UserDataComparer.Instance);

    public KinkPlateService(ILogger<KinkPlateService> logger,
        GagspeakMediator mediator, MainHub apiHubMain, 
        KinkPlateFactory profileFactory) : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _profileFactory = profileFactory;

        // Clear profiles when called.
        Mediator.Subscribe<ClearProfileDataMessage>(this, (msg) =>
        {
            // if UserData exists, clear the profile, otherwise, clear whole cache and reload things again.
            if (msg.UserData != null)
            {
                RemoveKinkPlate(msg.UserData);
            }
            else
            {
                ClearAllKinkPlates();
            }
        });

        // Clear all profiles on disconnect
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (_) => ClearAllKinkPlates());
    }

    // Obtain the clients kink plate information if valid.
    public bool TryGetClientKinkPlateContent([NotNullWhen(true)] out KinkPlateContent? clientPlateContent)
    {
        if (_kinkPlates.TryGetValue(MainHub.PlayerUserData, out var plate))
        {
            clientPlateContent = plate.KinkPlateInfo;
            return true;
        }
        clientPlateContent = null;
        return false;
    }

    /// <summary> Get the Gagspeak Profile data for a user. </summary>
    public KinkPlate GetKinkPlate(UserData userData)
    {
        // Locate the profile data for the pair.
        if (!_kinkPlates.TryGetValue(userData, out var kinkPlate))
        {
            Logger.LogTrace("KinkPlate™ for " + userData.UID+ " not found, creating loading KinkPlate™.", LoggerType.KinkPlateMonitor);
            // If not found, create a loading profile template for the user,
            AssignLoadingProfile(userData);
            // then run a call to the GetKinkPlate API call to fetch it.
            _ = Task.Run(() => GetKinkPlateFromService(userData));
            // in the meantime, return the loading profile data
            // (it will have the loadingProfileState at this point)
            return _kinkPlates[userData]; 
        }

        // Profile found, so return it.
        return (kinkPlate);
    }

    private void AssignLoadingProfile(UserData data)
    {
        // add the user & profile data to the concurrent dictionary.
        _kinkPlates[data] = _profileFactory.CreateProfileData(new KinkPlateContent(), string.Empty);
        Logger.LogTrace("Assigned new KinkPlate™ for " + data.UID, LoggerType.KinkPlateMonitor);
    }

    public void RemoveKinkPlate(UserData userData)
    {
        Logger.LogDebug("Removing KinkPlate™ for " + userData.UID+" if it exists.", LoggerType.KinkPlateMonitor);
        // Check if the profile exists before attempting to dispose and remove it
        if (_kinkPlates.TryGetValue(userData, out var profile))
        {
            // Run the cleanup on the object first before removing it
            profile.Dispose();
            // Remove them from the dictionary
            _kinkPlates.TryRemove(userData, out _);
        }
    }

    public void ClearAllKinkPlates()
    {
        Logger.LogInformation("Clearing all KinkPlates™", LoggerType.KinkPlateMonitor);
        // dispose of all the profile data.
        foreach (var kinkPlate in _kinkPlates.Values)
        {
            kinkPlate.Dispose();
        }
        // clear the dictionary.
        _kinkPlates.Clear();
    }

    // This fetches the profile data and assigns it. Only updated profiles are cleared, so this is how we grab initial data.
    private async Task GetKinkPlateFromService(UserData data)
    {
        try
        {
            Logger.LogTrace("Fetching profile for "+data.UID, LoggerType.KinkPlateMonitor);
            // Fetch userData profile info from server
            var profile = await _apiHubMain.UserGetKinkPlate(new UserDto(data)).ConfigureAwait(false);

            // apply the retrieved profile data to the profile object.
            _kinkPlates[data].KinkPlateInfo = profile.Info;
            _kinkPlates[data].Base64ProfilePicture = profile.ProfilePictureBase64 ?? string.Empty;
            Logger.LogDebug("KinkPlate™ for "+data.UID+" loaded.", LoggerType.KinkPlateMonitor);
        }
        catch (Exception ex)
        {
            // log the failure and set default data.
            Logger.LogWarning(ex, "Failed to get KinkPlate™ from service for user " + data.UID);
            _kinkPlates[data].KinkPlateInfo = new KinkPlateContent();
            _kinkPlates[data].Base64ProfilePicture = string.Empty;
        }
    }
}
