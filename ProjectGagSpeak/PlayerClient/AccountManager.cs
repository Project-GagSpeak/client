
using CkCommons;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     Config Management for all Server related configs in one, including
///     helper methods to make interfacing with config data easier.
/// </summary>
public class AccountManager
{
    private readonly ILogger<AccountManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly AccountConfig _config;
    private readonly ConfigFileProvider _fileProvider;

    public AccountManager(ILogger<AccountManager> logger, GagspeakMediator mediator,
        AccountConfig config, ConfigFileProvider files)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
        _fileProvider = files;
    }

    public List<AccountProfile> Profiles => _config.Current.Profiles.Values.ToList();

    // Avoid calling this wherever possible maybe?
    public void Save()
        => _config.Save();

    public void UpdateFileProviderForConnection(ConnectionResponse response)
    {
        _logger.LogDebug($"Setting FileProvider ProfileUID to {response.User.UID}");
        var isProfileDifferent = _fileProvider.CurrentUserUID != response.User.UID;
        _fileProvider.UpdateConfigs(response.User.UID);
    }

    /// <summary>
    /// Determines whether any profiles are currently available.
    /// </summary>
    /// <returns>true if at least one profile exists; otherwise, false.</returns>
    internal bool HasAnyProfile()
        => Profiles.Count != 0;

    /// <summary>
    /// Determines whether the current instance has at least one profile with a valid connection.
    /// </summary>
    /// <returns>true if there is at least one profile with a valid connection; otherwise, false.</returns>
    public bool HasValidProfile()
        => HasAnyProfile() && Profiles.Any(p => p.HadValidConnection);

    /// <summary>
    /// Determines whether the current player has a valid profile with a successful connection.
    /// </summary>
    /// <returns>true if the player has a profile matching their content ID and the profile indicates a valid connection;
    /// otherwise, false.</returns>
    public bool CharaHasValidProfile()
        => Profiles.Exists(p => p.ContentId == PlayerData.CID && p.HadValidConnection);

    /// <summary>
    /// Retrieves the primary account profile associated with this account, if one exists.
    /// </summary>
    /// <returns>The primary <see cref="AccountProfile"/> if available; otherwise, <see langword="null"/>.</returns>
    public AccountProfile? GetMainProfile()
        => Profiles.FirstOrDefault(p => p.IsPrimary);

    /// <summary>
    /// Retrieves the account profile associated with the current player's content ID.
    /// </summary>
    /// <returns>The account profile for the current player if found; otherwise, <see langword="null"/>.</returns>
    public AccountProfile? GetCharaProfile()
        => Profiles.FirstOrDefault(p => p.ContentId == PlayerData.CID);

    // Might need to run on framework thread? Not sure.
    // Does a Name & World update on connection.
    /// <summary>
    /// Gets the tracked account profile for the current character, or returns null if no profile is found.
    /// </summary>
    /// <remarks>If a profile is found, the method updates the profile's name and world to match the current
    /// character before returning it. The changes are saved automatically. This method does not create a new profile if
    /// one does not exist.</remarks>
    /// <returns>The <see cref="AccountProfile"/> associated with the current character, or <see langword="null"/> if no profile
    /// exists.</returns>
    public AccountProfile? GetTrackedCharaOrDefault()
    {
        _config.Current.Profiles.TryGetValue(PlayerData.CID, out var auth); // don't want a default here?
        if (auth is null)
        {
            _logger.LogDebug("No profile found for current character.");
            return null;
        }

        var curName = PlayerData.Name;
        var curWorld = PlayerData.HomeWorldId;
        // If no name/world change, return.
        if (auth.PlayerName == curName && auth.WorldId == curWorld)
            return auth;

        // Otherwise update and save.
        auth.PlayerName = curName;
        auth.WorldId = curWorld;
        _config.Save();
        return auth;
    }

    /// <summary>
    /// Creates and adds a new account profile for the current player.
    /// </summary>
    /// <remarks>This method generates a new profile using the current player's name, home world, and
    /// content ID. If a profile with the same content ID already exists, the method does not overwrite it and
    /// instead throws an exception.</remarks>
    /// <returns>The newly created <see cref="AccountProfile"/> instance associated with the current player.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a profile for the current player's content ID already exists.</exception>
    public AccountProfile AddNewProfile()
    {
        var name = PlayerData.Name;
        var world = PlayerData.HomeWorldId;
        var cid = PlayerData.CID;

        if (CharaHasValidProfile())
            throw new InvalidOperationException($"Cannot create duplicate profile, key {cid} already exists.");

        // Generate it.
        _config.Current.Profiles[cid] = new AccountProfile
        {
            ContentId = cid,
            PlayerName = name,
            WorldId = world,
        };
        _config.Save();
        return _config.Current.Profiles[cid];
    }

    public bool TryUpdateSecretKey(AccountProfile profile, string newKey)
    {
        if (profile.HadValidConnection)
            return false;

        // Update the key and save.
        profile.Key = newKey;
        _config.Save();
        return true;
    }

    /// <summary> 
    ///     Updates the authentication.
    /// </summary>
    public void UpdateAuthentication(string secretKey, ConnectionResponse response)
    {
        // If no profile exists with this key, this is a big red flag.
        if (Profiles.FirstOrDefault(p => p.Key == secretKey) is not { } profile)
        {
            _logger.LogError("Somehow connected with a SecretKey not stored, you should NEVER see this!");
            return;
        }

        profile.UserUID = response.User.UID;
        profile.HadValidConnection = true;

        // now we should iterate through the rest of our profiles, and check the UID's.
        // Any UID's listed in the profiles that are not in the associated profiles from the response are outdated.
        HashSet<string> accountProfileUids = [..response.AccountProfileUids, response.User.UID];

        foreach (var checkedProfile in Profiles)
        {
            if (string.IsNullOrWhiteSpace(checkedProfile.UserUID) || !checkedProfile.HadValidConnection)
                continue;

            // If the account UID list no longer has the profile UserUID, it was deleted via discord or a cleanup service.
            if (!accountProfileUids.Contains(checkedProfile.UserUID, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning($"Removing outdated profile {checkedProfile.PlayerName} with UID {checkedProfile.UserUID}");
                Profiles.Remove(checkedProfile);
                _config.Save();
            }
        }
    }
}
