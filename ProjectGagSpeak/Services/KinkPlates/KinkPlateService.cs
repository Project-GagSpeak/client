using GagSpeak.Kinksters;
using GagSpeak.Pairs;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.User;

namespace GagSpeak.Services;

public class KinkPlateService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly MainHub _hub;
    private readonly ProfileFactory _factory;
    private readonly KinksterManager _kinksters;
    private readonly OnlineKinksterManager _onlineUsers;

    // concurrent dictionary of cached profile data.
    private ConcurrentDictionary<UserData, UserKinkPlate> _kinkplates = new(UserDataComparer.Instance);

    public KinkPlateService(ILogger<KinkPlateService> logger, GagspeakMediator mediator,
        MainConfig config, MainHub hub, ProfileFactory factory, KinksterManager kinksters,
        OnlineKinksterManager onlineUsers)
        : base(logger, mediator)
    {
        _config = config;
        _hub = hub;
        _factory = factory;
        _kinksters = kinksters;
        _onlineUsers = onlineUsers;

        // Profiles can be refreshed by clearing their data, as the UI will try displaying it again.
        Mediator.Subscribe<FetchLatestUserProfile>(this, _ => GetLatestUserProfile(_.UserData));
        Mediator.Subscribe<ClearUserProfileMessage>(this, _ => RemovePlayerProfile(_.UserData));

        // Clear all profiles on disconnect
        Mediator.Subscribe<ConnectedMessage>(this, _ => ClearStaleProfiles());
        Mediator.Subscribe<DisconnectedMessage>(this, _ => FreeAllTextureWraps());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Logger.LogInformation("Clearing User Profiles", LoggerType.KinkPlates);
            foreach (var kvp in _kinkplates)
                if (_kinkplates.TryRemove(kvp.Key, out var profile))
                    profile.Dispose();
        }
        base.Dispose(disposing);
    }

    private void ClearStaleProfiles()
    {
        // Grab all online users, which combines all users online from direct pairs, sanction pairs, and active radar group.
        // If they are not found in here we can reasonably remove them.
        var validUsers = new HashSet<UserData>(UserDataComparer.Instance);
        validUsers.Concat(_kinksters.Keys).Concat(_onlineUsers.Users).Append(MainHub.OwnUserData);

        // Iterate through the users, if any are invalid, we should dispose of them.
        foreach (var kvp in _kinkplates.ToList())
            if (!validUsers.Contains(kvp.Key) && _kinkplates.TryRemove(kvp.Key, out var removedUser))
                removedUser.Dispose();
        Logger.LogInformation($"Cleared all Stale Kinkplates™ on connection.", LoggerType.KinkPlates);
    }

    // Frees up memory for all profiles which we no longer need the texture wraps for.
    private void FreeAllTextureWraps()
    {
        foreach (var userProfile in _kinkplates.Values.ToList())
            userProfile.Dispose();
        Logger.LogInformation($"Freed all TextureWraps for cached Kinkplates.", LoggerType.KinkPlates);
    }

    public bool Contains(UserData user)
        => _kinkplates.ContainsKey(user);

    /// <summary> 
    ///   Attain the UserKinkPlate, or a stand-in while retrieving the profile data from the hub.
    /// </summary>
    public UserKinkPlate GetUserProfile(UserData userData)
    {
        if (_kinkplates.TryGetValue(userData, out var profile))
            return profile;
        // Return a default profile while internally loading the requested profile.
        Logger.LogTrace($"Assigning LoadingProfile stand-in for {userData.AnonName}", LoggerType.KinkPlates);
        _kinkplates[userData] = _factory.CreateKinkplate(userData);
        _ = Task.Run(() => GetUserProfileInternal(userData));
        return _kinkplates[userData];
    }

    /// <summary> Bulk operation of <see cref="GetUserProfile(UserData)"/> </summary>
    /// <returns> All UserProfiles, regardless of valid state. </returns>
    public List<UserKinkPlate> GetUserProfiles(List<UserData> users)
    {
        var missing = new List<UserData>();
        var results = users.Select(u =>
        {
            if (_kinkplates.TryGetValue(u, out var p))
                return p;

            var placeholder = _factory.CreateKinkplate(u);
            _kinkplates[u] = placeholder;
            missing.Add(u);
            return placeholder;
        }).ToList();

        Logger.LogDebug($"GetUserProfiles: {results.Count} profiles returned, {missing.Count} missing.", LoggerType.KinkPlates);
        // Enqueue the bulk fetch operation.
        if (missing.Count > 0)
            _ = Task.Run(() => GetUserProfilesInternal(missing));

        return results;
    }

    public async void GetLatestUserProfile(UserData userData)
    {
        if (!_kinkplates.ContainsKey(userData))
            return;
        // Grab it
        await GetUserProfileInternal(userData).ConfigureAwait(false);
    }

    // Might want to make a difference between "refresh data" and "Clear Data" 
    private void RemovePlayerProfile(UserData userData)
    {
        if (!_kinkplates.TryGetValue(userData, out var profile))
            return;

        Logger.LogDebug($"Removing ProfileCache for {userData.AnonName}.", LoggerType.KinkPlates);
        // Free up the rented image data, then remove from the cache.
        profile.Dispose();
        _kinkplates.TryRemove(userData, out _);
    }

    /// <summary>
    ///   Gets the <paramref name="user"/>'s profile from the Hub,
    ///   and stores it in their <see cref="UserKinkPlate"/>
    /// </summary>
    private async Task GetUserProfileInternal(UserData user)
    {
        try
        {
            Logger.LogTrace($"Fetching profile for {user.AnonName}", LoggerType.KinkPlates);
            var data = await _hub.GetKinkPlate(new(user)).ConfigureAwait(false);
            // apply the retrieved profile data to the profile object.
            _kinkplates[user].ApplyDataFromHub(data.Info, data.ImageBase64);
            Logger.LogDebug($"Profile data fetched for {user.AnonName}", LoggerType.KinkPlates);
        }
        catch (Bagagwa ex)
        {
            Logger.LogWarning($"Couldn't fetch {user.AnonName}'s UserKinkPlate data; Reason: {ex}");
            _kinkplates[user].ApplyDataFromHub(new(), null);
        }
    }

    /// <summary>
    ///   Bulk operation for <see cref="GetUserProfileInternal"/>
    /// </summary>
    private async Task GetUserProfilesInternal(List<UserData> users)
    {
        try
        {
            Logger.LogTrace($"Fetching profiles for {users.Count} users..", LoggerType.KinkPlates);
            var retrieved = await _hub.GetKinkPlates(new(users)).ConfigureAwait(false);
            foreach (var profile in retrieved)
                _kinkplates[profile.User].ApplyDataFromHub(profile.Info, profile.ImageBase64);
            Logger.LogDebug($"Profile data fetched for {users.Count} users.", LoggerType.KinkPlates);
        }
        catch (Bagagwa ex)
        {
            Logger.LogWarning($"Failed to perform UserGetProfiles. Reason: {ex}");
            foreach (var user in users)
                _kinkplates[user].ApplyDataFromHub(new(), null);
        }
    }
}
