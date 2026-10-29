using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;

namespace GagSpeak.PlayerClient;

public class AccountConfigData
{
    // Every user can have 1 Account. 1 Account is made up of 1 Main Profile, and multiple AltProfiles.
    // Each Profile is bound to a character. This can be further inforced but I'd rather not unless necessary.
    // (This will change later as we shift puppeteer implementation & achievement logic to follow sundouleia's formatting)
    public Dictionary<ulong, AccountProfile> Profiles { get; set; } = [];
}

public record AccountProfile
{
    /// <summary>
    ///   The unique value of this authentication. <para />
    ///   A ContentID is a static value given to a character of a FFXIV Service Account. <br/>
    ///   Persists through name and world changes.
    /// </summary>
    public ulong ContentId { get; set; } = 0;

    /// <summary>
    ///   The Character Name associated with this ContentID.
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    ///   The HomeWorld associated with this ContentID.
    /// </summary>
    public ushort WorldId { get; set; } = 0;

    /// <summary>
    ///   The UserUID associated with this secret key. <para />
    ///   This is recieved from the server upon the first valid connection with this key.
    /// </summary>
    public string UserUID { get; set; } = string.Empty;

    /// <summary>
    ///   The secret key used to authenticate with the server and connect.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///   If this is the primary key, all other keys are removed when it is removed.
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    ///   If a valid connection was established. This could easily be removed or replaced with if UserUID != string.Empty (?)
    /// </summary>
    public bool HadValidConnection { get; set; } = false;

}

public class AccountConfig : IHybridSavable
{
    private readonly ILogger<AccountConfig> _logger;
    private readonly ConnectionsConfig _servers;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC => DateTime.MinValue;
    public int ConfigVersion => 3;
    public int MaxBackups => 3;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string ToFilePath(GsFiles files) => files.AccountConfig;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        var serversObj = new JObject();
        foreach (var (serverUri, storage) in _config)
            serversObj[serverUri] = JObject.FromObject(storage);

        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Servers"] = serversObj,
        }.ToString(Formatting.Indented);
    }

    // Keyed by the ServerURI
    private Dictionary<string, AccountConfigData> _config = [];

    public AccountConfig(ILogger<AccountConfig> logger, ConnectionsConfig servers, HybridSaveService saver)
    {
        _logger = logger;
        _servers = servers;
        _saver = saver;
        Load();
    }

    public AccountConfigData Current => GetOrCreateConfig();
    private AccountConfigData GetOrCreateConfig()
    {
        if (_config.TryGetValue(ConnectionsConfig.CurrentHubURI, out var res))
            return res;
        // Create and return.
        _config[ConnectionsConfig.CurrentHubURI] = new AccountConfigData();
        Save(); // Maybe remove idk.
        return _config[ConnectionsConfig.CurrentHubURI];
    }

    public void Save()
        => _saver.Save(this);

    public void Load()
    {
        var file = _saver.FileNames.AccountConfig;
        _logger.LogInformation($"Loading AccountConfig file: {file}");
        if (_saver.Load(this) is not { } jObject)
        {
            _logger.LogWarning($"Config file not found for: {file}");
            _config = new();
            _saver.Save(this);
            return;
        }

        var version = jObject["Version"]?.Value<int>() ?? 0;

        // execute based on version.
        // Old config formats are stored at the bottom of this file for reference.
        try
        {
            switch (version)
            {
                case 0:
                    MigrateV0ToV2(jObject);
                    MigrateV2ToV3(jObject);
                    LoadV3(jObject);
                    break;
                case 1:
                    MigrateV1ToV2(jObject);
                    MigrateV2ToV3(jObject);
                    LoadV3(jObject);
                    break;
                case 2:
                    MigrateV2ToV3(jObject);
                    LoadV3(jObject);
                    break;
                case 3:
                    LoadV3(jObject);
                    break;
                default:
                    _logger.LogError("Invalid Version!");
                    return;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load AccountConfig.");
            return;
        }

        _logger.LogInformation("Config loaded.");
        Save();
    }

    // Migrates V0 ("ServerStorage" -> "Authentications") into V2 ("AccountInfo" -> "Profiles")
    private void MigrateV0ToV2(JObject root)
    {
        if (root["ServerStorage"] is not JObject oldStorage)
            throw new Exception("Failed to find old ServerStorage for V0 migration.");

        var newStorage = new AccountConfigData();

        if (oldStorage["Authentications"] is JArray authentications)
        {
            foreach (var auth in authentications.OfType<JObject>())
            {
                var secretKey = auth["SecretKey"] as JObject;
                var profile = new AccountProfile
                {
                    ContentId = auth["CharacterPlayerContentId"]?.Value<ulong>() ?? 0,
                    PlayerName = auth["CharacterName"]?.Value<string>() ?? string.Empty,
                    WorldId = auth["WorldId"]?.Value<ushort>() ?? 0,
                    IsPrimary = auth["IsPrimary"]?.Value<bool>() ?? false,
                    UserUID = secretKey?["LinkedProfileUID"]?.Value<string>() ?? string.Empty,
                    Key = secretKey?["Key"]?.Value<string>() ?? string.Empty,
                    HadValidConnection = secretKey?["HasHadSuccessfulConnection"]?.Value<bool>() ?? false
                };

                newStorage.Profiles[profile.ContentId] = profile;
            }
        }

        // Preserve custom ServiceUri if V0 had one, otherwise fall back on V2->V3 migration
        if (oldStorage["ServiceUri"]?.Value<string>() is { Length: > 0 } legacyUri)
            root["_LegacyServiceUri"] = legacyUri;

        root.Remove("ServerStorage");
        root["AccountInfo"] = JObject.FromObject(newStorage);
        root["Version"] = 2;
    }

    // Migrates V1 ("AccountInfo" -> "Profiles" JArray) into V2 ("AccountInfo" -> "Profiles" Dictionary)
    private void MigrateV1ToV2(JObject root)
    {
        if (root["AccountInfo"] is not JObject oldStorage)
            throw new Bagagwa("Failed to find/parse old storage for v1 migration.");

        var newStorage = new AccountConfigData();

        if (oldStorage["Profiles"] is JArray profiles)
        {
            foreach (var profile in profiles.OfType<JObject>())
            {
                var newProfile = new AccountProfile
                {
                    IsPrimary = profile["IsMainProfile"]?.Value<bool>() ?? false,
                    HadValidConnection = profile["HadValidConnection"]?.Value<bool>() ?? false,
                    UserUID = profile["ProfileUID"]?.Value<string>() ?? string.Empty,
                    Key = profile["SecretKey"]?.Value<string>() ?? string.Empty,
                    PlayerName = profile["PlayerName"]?.Value<string>() ?? string.Empty,
                    WorldId = profile["WorldId"]?.Value<ushort>() ?? 0,
                    ContentId = profile["ContentId"]?.Value<ulong>() ?? 0
                };
                newStorage.Profiles[newProfile.ContentId] = newProfile;
            }
        }

        root["AccountInfo"] = JObject.FromObject(newStorage);
        root["Version"] = 2;
    }

    private void MigrateV2ToV3(JObject root)
    {
        if (root["AccountInfo"] is not JObject accountInfo)
            throw new Bagagwa("Failed to find AccountInfo for V2 -> V3 migration.");

        var targetUri = root["_LegacyServiceUri"]?.Value<string>() ?? ConnectionsConfig.MAIN_SERVER_URI;

        root["Servers"] = new JObject
        {
            [targetUri] = accountInfo.DeepClone()
        };

        root.Remove("AccountInfo");
        root.Remove("_LegacyServiceUri");
        root["Version"] = 3;
    }

    private void LoadV3(JObject root)
    {
        if (root["Servers"] is not JObject serversObj)
            throw new Bagagwa("Failed to load Servers object for V3.");

        var newConfig = new Dictionary<string, AccountConfigData>(StringComparer.OrdinalIgnoreCase);

        foreach (var (serverUri, serverToken) in serversObj)
        {
            if (serverToken is not JObject storageObj)
                continue;

            var storage = storageObj.ToObject<AccountConfigData>() ?? new AccountConfigData();
            newConfig[serverUri] = storage;
        }

        _config = newConfig;
    }
}

/* FOR FUTURE REFERENCE - DO NOT DELETE
 * 
 * == V0 CONFIG FORMAT ==
{
  "Version": 0,
  "ServerStorage": {
    "Authentications": [ 
      {
        "CharacterPlayerContentId": 0,
        "CharacterName": "",
        "WorldId": 0,
        "IsPrimary": true,
        "SecretKey": {
          "Label": "",
          "Key": "",
          "HasHadSuccessfulConnection": true,
          "LinkedProfileUID": ""
        }
      }
    ]
    "FullPause": false,
    "ServerName": "GagSpeak Main",
    "ServiceUri": "wss://gagspeak.kinkporium.studio"
  }
}

== V1 CONFIG FORMAT ==
{
  "Version": 1,
  "AccountInfo": {
    "Profiles": [
      {
        "IsMainProfile": false,
        "HadValidConnection": false,
        "ProfileUID": "",
        "SecretKey": "",
        "PlayerName": "",
        "WorldId": 75,
        "ContentId": 0
      }
    ]
  }
}

== V2 CONFIG FORMAT ==
{
  "Version": 2,
  "AccountInfo": {
    "Profiles": {
      "99999999999": {
        "ContentId": 99999999999,
        "PlayerName": "",
        "WorldId": 0,
        "UserUID": "",
        "Key": "",
        "IsPrimary": true,
        "HadValidConnection": true
      }
    }
  }
}

== V3 CONFIG FORMAT ==
{
  "Version": 3,
  "Servers": {
    "wss://gagspeak.kinkporium.studio": {
      "Profiles": {
        "99999999999": {
          "ContentId": 99999999999,
          "PlayerName": "",
          "WorldId": 0,
          "UserUID": "",
          "Key": "",
          "IsPrimary": true,
          "HadValidConnection": true
        }
      }
    }
  }
}
*/
