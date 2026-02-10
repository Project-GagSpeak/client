
using CkCommons;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;
using System.CodeDom;

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

/* old acctmanager


using CkCommons;
using GagSpeak.PlayerClient;
using GagspeakAPI.Network;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services.Configs;

// Maybe make these helper methods in the config and remove the manager all together or something... idk.
public class AccountManager
{
    private readonly ILogger<AccountManager> _logger;
    private readonly AccountConfig _config;

    public AccountManager(ILogger<AccountManager> logger, AccountConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public int TotalProfiles => AccountData.Profiles.Count;
    public int TotalAlts => AccountData.Profiles.Count(a => !a.IsMainProfile);
    public bool HasAnyProfile => TotalProfiles > 0;
    public bool HasAltProfiles => TotalAlts > 0;
    public bool HasValidMainProfile() => AccountData.Profiles.Any(a => a.IsMainProfile && a.HadValidConnection);
    public bool CharaHasValidProfile() => AccountData.Profiles.Any(a => a.ContentId == PlayerData.CID && a.HadValidConnection);
    public bool ProfileExistsForChara() => AccountData.Profiles.Any(a => a.ContentId == PlayerData.CID);

    // All Internal methods below to ensure secure access not exposed to reflection.
    internal AccountStorage AccountData => _config.Current;

    internal AccountProfile? GetProfileForChara()
        => TryGetPlayerProfile(out var profile) ? profile : null;

    internal List<AccountProfile> GetAltProfiles() 
        => AccountData.Profiles.Where(p => !p.IsMainProfile)
        .OrderBy(p => p.PlayerName)
        .ThenBy(p => p.HadValidConnection)
        .ToList();

    internal bool TryGetMainProfile([NotNullWhen(true)] out AccountProfile profile)
    {
        // fetch the main profile.
        if (AccountData.Profiles.FirstOrDefault(la => la.IsMainProfile) is not { } match)
        {
            _logger.LogDebug("No main profile found.");
            profile = null!;
            return false;
        }
        // a match was found, so mark it.
        profile = match;
        return true;
    }

    internal bool TryGetPlayerProfile([NotNullWhen(true)] out AccountProfile profile)
    {
        // fetch the cid of our current player.
        var cid = Svc.Framework.RunOnFrameworkThread(() => PlayerData.CID).Result;
        // if we cannot find any authentications with this data, it means that none exist.
        if (AccountData.Profiles.FirstOrDefault(la => la.ContentId == cid) is not { } match)
        {
            _logger.LogDebug("No authentication found for the current character.");
            profile = null!;
            return false;
        }

        // a match was found, so mark it, but update name and world before returning.
        profile = match;
        UpdateAuthForNameAndWorldChange(cid);
        return true;
    }

    internal void UpdateAuthForNameAndWorldChange(ulong cid)
    {
        // locate the auth with the matching local content ID, and update the name and world if they do not match.
        if (AccountData.Profiles.FirstOrDefault(la => la.ContentId == cid) is not { } profile)
            return;
        // Id was valid, compare against current.
        var currentName = PlayerData.Name;
        var currentWorld = PlayerData.HomeWorldId;
        // update the name if it has changed.
        if (profile.PlayerName == currentName && profile.WorldId == currentWorld)
            return;

        // Otherwise update and save.
        if (profile.PlayerName != currentName)
            profile.PlayerName = currentName;

        if (profile.WorldId != currentWorld)
            profile.WorldId = currentWorld;

        _config.Save();
    }

    internal void CreateProfileForChara()
    {
        var name = PlayerData.Name;
        var world = PlayerData.HomeWorldId;
        var cid = PlayerData.CID;

        // If we already have an auth for this character, do nothing.
        // (Just an additional safeguard because accounts shouldnt be modified unless required)
        if (AccountData.Profiles.Any(a => a.ContentId == cid))
            return;

        _logger.LogDebug($"Generating new AccountProfile for character {name}@{ItemSvc.WorldData[world]}:{cid}");
        AccountData.Profiles.Add(new AccountProfile
        {
            PlayerName = name,
            WorldId = world,
            ContentId = cid,
            IsMainProfile = AccountData.Profiles.Count is 0
        });
        _config.Save();
    }

    internal void UpdateFromValidConnection(string secretKeyUsed, ConnectionResponse response)
    {
        var accountUids = response.AccountProfileUids;
        // Move over each profile and update it according the UIDS and key used.
        foreach (var profile in AccountData.Profiles.ToList())
        {
            // Mark the UID for the matching secret key entry.
            if (profile.SecretKey.Equals(secretKeyUsed))
            {
                profile.ProfileUID = response.User.UID;
                // Safegaurd ensuring that a connection is bound to the correct CID also maybe?
                continue;
            }

            // Otherwise, if the entry has a ProfileUID, and it is not in our list, clear the UID.
            if (!string.IsNullOrWhiteSpace(profile.ProfileUID) && !accountUids.Contains(profile.ProfileUID))
                profile.ProfileUID = string.Empty; // Marks a profile as 'invalid'
        }
        // Save changes.
        _config.Save();
    }

    internal bool TryUpdateSecretKey(AccountProfile profile, string newKey)
    {
        // If any other profiles use this key, reject it.
        if (AccountData.Profiles.Any(p => p != profile && p.SecretKey.Equals(newKey)))
            return false;

        profile.SecretKey = newKey;
        _config.Save();
        return true;
    }

    /// <summary>
    ///     Assign a freshly created account as the main profile. Will throw exceptions if one is present already.
    /// </summary>
    internal void AssignFreshAccountProfile(string uid, string secretKey)
    {
        if (!TryGetMainProfile(out var existingMainProfile))
            throw new InvalidOperationException("No main profile exists to assign a fresh account to.");

        if (existingMainProfile.ContentId != PlayerData.CID)
            throw new InvalidOperationException("Main profile does not match the current character, cannot assign a fresh account.");

        if (existingMainProfile.HadValidConnection)
            throw new InvalidOperationException("Main profile already has a valid connection, cannot assign a fresh account.");

        existingMainProfile.ProfileUID = uid;
        existingMainProfile.SecretKey = secretKey;
        _config.Save();
    }

    internal void RemoveProfile(AccountProfile profile)
    {
        AccountData.Profiles.Remove(profile);
        _config.Save();
    }

    internal void ClearAllProfiles()
    {
        AccountData.Profiles.Clear();
        _config.Save();
    }
}*/
