using GagSpeak.PlayerData.Storage;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services.Configs;

/// <summary>
/// This configuration manager helps manage the various interactions with all config files related to server-end activity.
/// <para> It provides a comprehensive interface for configuring servers, managing tags and nicknames, and handling authentication keys. </para>
/// </summary>
public class ServerConfigurationManager
{
    private readonly ILogger<ServerConfigurationManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ClientMonitor _clientMonitor;
    private readonly ServerConfigService _serverConfig;
    private readonly NicknamesConfigService _nicknameConfig;

    public ServerConfigurationManager(ILogger<ServerConfigurationManager> logger, GagspeakMediator mediator,
        ClientMonitor clientMonitor, ServerConfigService serverConfig, NicknamesConfigService nicksConfig)
    {
        _logger = logger;
        _mediator = mediator;
        _clientMonitor = clientMonitor;
        _serverConfig = serverConfig;
        _nicknameConfig = nicksConfig;
        // ensure main exists
        if (!string.Equals(_serverConfig.Storage.ServiceUri, MainHub.MainServiceUri, StringComparison.OrdinalIgnoreCase))
        {
            _serverConfig.Storage = new ServerStorage();
            _serverConfig.Save();
        }
    }

    /// <summary> The current API URL for the server </summary>
    public string CurrentApiUrl => _serverConfig.Storage.ServiceUri;

    /// <summary> The current server we are connected to, taken from the ServerStorage class </summary>
    public ServerStorage ServerStorage => _serverConfig.Storage;
    public ServerNicknamesStorage NickStorage => _nicknameConfig.Storage;

    /// <summary> Returns the authentication for the current player. </summary>
    /// <returns> Returns true if found, false if not. Outputs Authentication if true. </returns>
    public bool TryGetAuthForCharacter([NotNullWhen(true)] out Authentication auth)
    {
        // fetch the players local content ID (matches regardless of name or world change) and the name & worldId.
        var LocalContentID = _clientMonitor.ContentIdAsync().GetAwaiter().GetResult();
        // Once we have obtained the information, check to see if the currently logged in character has a matching authentication with the same local content ID.
        auth = ServerStorage.Authentications.FirstOrDefault(f => f.CharacterPlayerContentId == LocalContentID)!;
        if (auth is null)
        {
            _logger.LogDebug("No authentication found for the current character.");
            return false;
        }

        // update the auth for name and world change
        UpdateAuthForNameAndWorldChange(LocalContentID);

        // Return value authentication.
        return true;
    }


    /// <summary> Retrieves the key for the currently logged in character. Returns null if none found. </summary>
    /// <returns> The Secret Key </returns>
    public string? GetSecretKeyForCharacter()
    {
        if(TryGetAuthForCharacter(out var auth))
        {
            return auth.SecretKey.Key;
        }
        else
        {
            _logger.LogDebug("No authentication found for the current character.");
            return null;
        }
    }

    public void UpdateAuthForNameAndWorldChange(ulong localContentId)
    {
        // locate the auth with the matching local content ID, and update the name and world if they do not match.
        var auth = ServerStorage.Authentications.Find(f => f.CharacterPlayerContentId == localContentId);
        if (auth == null) return;

        // fetch the players name and world ID.
        var charaName = _clientMonitor.NameAsync().GetAwaiter().GetResult();
        var worldId = _clientMonitor.HomeWorldIdAsync().GetAwaiter().GetResult();

        // update the name if it has changed.
        if (auth.CharacterName != charaName)
        {
            auth.CharacterName = charaName;
        }

        // update the world ID if it has changed.
        if (auth.WorldId != worldId)
        {
            auth.WorldId = worldId;
        }
    }

    public int AuthCount() => ServerStorage.Authentications.Count;

    public bool HasAnyAltAuths() => ServerStorage.Authentications.Any(a => !a.IsPrimary);

    public bool CharacterHasSecretKey()
    {
        return ServerStorage.Authentications.Any(a => a.CharacterPlayerContentId == _clientMonitor.ContentId && !string.IsNullOrEmpty(a.SecretKey.Key));
    }

    public bool AuthExistsForCurrentLocalContentId()
    {
        return ServerStorage.Authentications.Any(a => a.CharacterPlayerContentId == _clientMonitor.ContentId);
    }

    public void GenerateAuthForCurrentCharacter()
    {
        _logger.LogDebug("Generating new auth for current character");
        // generates a new auth object for the list of authentications with no secret key.
        var auth = new Authentication
        {
            CharacterPlayerContentId = _clientMonitor.ContentIdAsync().GetAwaiter().GetResult(),
            CharacterName = _clientMonitor.NameAsync().GetAwaiter().GetResult(),
            WorldId = _clientMonitor.HomeWorldIdAsync().GetAwaiter().GetResult(),
            IsPrimary = !ServerStorage.Authentications.Any(),
            SecretKey = new SecretKey()
        };

        // add the new authentication to the list of authentications.
        ServerStorage.Authentications.Add(auth);
        Save();
    }

    public void SetSecretKeyForCharacter(ulong localContentID, SecretKey keyToAdd)
    {
        // Check if the currently logged-in character has a matching authentication with the same local content ID.
        var auth = ServerStorage.Authentications.Find(f => f.CharacterPlayerContentId == localContentID);

        // If the authentication is null, throw an exception.
        if (auth == null) throw new Exception("No authentication found for the current character.");

        // Update the existing authentication with the new secret key.
        auth.SecretKey = keyToAdd;
        // Save the updated configuration.
        Save();
    }

    public void SetSecretKeyAsValid(string secretKey)
    {
        // locate the authentication with the matching key.
        var auth = ServerStorage.Authentications.Find(f => f.SecretKey.Key == secretKey);
        if (auth == null) return;

        // set the authentication as having had a successful connection.
        auth.SecretKey.HasHadSuccessfulConnection = true;
        Save();
    }


    /// <summary> Gets the server API Url </summary>
    public string GetServerApiUrl() => ServerStorage.ServiceUri;

    /// <summary> Gets the server name </summary>
    public string GetServerName() => ServerStorage.ServerName;

    /// <summary> Checks if the configuration is valid </summary>
    /// <returns> True if the current server storage object is not null </returns>
    public bool HasValidConfig() => ServerStorage != null;

    /// <summary> Requests to save the configuration service file to the clients computer. </summary>
    public void Save()
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        _logger.LogDebug("{caller} Calling config save", caller);
        _serverConfig.Save();
    }

    /// <summary> Saves the nicknames config </summary>
    internal void SaveNicknames() => _nicknameConfig.Save();

    /// <summary>Retrieves the nickname associated with a given UID (User Identifier).</summary>
    /// <returns>Returns the nickname as a string if found; otherwise, returns null.</returns>
    internal string? GetNicknameForUid(string uid)
    {
        if (NickStorage.Nicknames.TryGetValue(uid, out var nickname))
        {
            if (string.IsNullOrEmpty(nickname))
                return null;
            // Return the found nickname
            return nickname;
        }
        return null;
    }


    /// <summary> Set a nickname for a user identifier. </summary>
    /// <param name="uid">the user identifier</param>
    /// <param name="nickname">the nickname to add</param>
    internal void SetNicknameForUid(string uid, string nickname)
    {
        if (string.IsNullOrEmpty(uid))
            return;

        NickStorage.Nicknames[uid] = nickname;
        _nicknameConfig.Save();
    }
}
