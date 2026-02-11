using CkCommons;
using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using TerraFX.Interop.Windows;

namespace GagSpeak.PlayerClient;

public class AccountConfig : IHybridSavable
{
    private readonly ILogger<AccountConfig> _logger;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 2;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.ServerConfig).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["AccountInfo"] = JObject.FromObject(Current),
        }.ToString(Formatting.Indented);
    }
    public AccountConfig(ILogger<AccountConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.ServerConfig;
        _logger.LogInformation("Loading in Config for file: " + file);
        if (!File.Exists(file))
        {
            _logger.LogWarning("Config file not found for: " + file);
            return;
        }

        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // execute based on version.
        // Old config formats are stored at the bottom of this file for reference.
        switch (version)
        {
            case 0:
                MigrateAndLoadV0AsV2(jObject);
                break;
            case 1:
                MigrateAndLoadV1AsV2(jObject);
                break;
            case 2:
                LoadV2(jObject["AccountInfo"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _logger.LogInformation("Config loaded.");
        Save();
    }

    // V1->V2 migration is mostly just a property rename but also indexes accounts by their contentID
    private void MigrateAndLoadV1AsV2(JObject root)
    {
        var oldStorage = root["AccountInfo"] as JObject;

        if (oldStorage == null)
            throw new Bagagwa("Failed to find/parse old storage for v1 migration.");

        var newStorage = new AccountStorage() { Profiles = new Dictionary<ulong, AccountProfile>() };

        // Parse old profiles, and update key names...
        var profiles = oldStorage["Profiles"] as JArray;
        if (profiles != null)
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
                newStorage.Profiles.Add(newProfile.ContentId, newProfile);
            }
        }

        // write the new data back out.
        root["AccountInfo"] = JObject.FromObject(newStorage);

        // update config version
        root["Version"] = 2;

        Current = newStorage;
    }

    // TODO: UPDATE THIS FOR V2 AS WELL
    private void MigrateAndLoadV0AsV2(JObject root)
    {
        // Get the old ServerStorage node.
        var oldStorage = root["ServerStorage"] as JObject;
        if (oldStorage == null)
            throw new Exception("Failed to find old ServerStorage for V0 migration.");

        var newStorage = new AccountStorage()
        {
            Profiles = new Dictionary<ulong, AccountProfile>()
        };

        // Migrate each old authentication into a new AccountProfile.
        var authentications = oldStorage["Authentications"] as JArray;
        if (authentications != null)
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

                newStorage.Profiles.Add(profile.ContentId, profile);
            }
        }

        // Replace the old storage in the root object
        root["ServerStorage"] = JObject.FromObject(newStorage);

        // Update version to 2
        root["Version"] = 2;

        // Also update Current to reflect the new AccountStorage
        Current = newStorage;
    }

    private void LoadV2(JToken? data)
    {
        if (data is not JObject storage)
            return;
        Current = storage.ToObject<AccountStorage>() ?? throw new Exception("Failed to load AccountStorage.");   
    }

    public AccountStorage Current { get; set; } = new AccountStorage();
}

public class AccountStorage
{
    // Every user can have 1 Account. 1 Account is made up of 1 Main Profile, and multiple AltProfiles.
    // Each Profile is bound to a character. This can be further inforced but I'd rather not unless necessary.
    public Dictionary<ulong, AccountProfile> Profiles { get; set; } = [];
}

public record AccountProfile
{
    /// <summary>
    ///     The unique value of this authentication. <para />
    ///     A ContentID is a static value given to a character of a FFXIV Service Account. <br/>
    ///     Persists through name and world changes.
    /// </summary>
    public ulong ContentId { get; set; } = 0;

    /// <summary>
    ///     The Character Name associated with this ContentID.
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    ///     The HomeWorld associated with this ContentID.
    /// </summary>
    public ushort WorldId { get; set; } = 0;

    /// <summary>
    ///     The UserUID associated with this secret key. <para />
    ///     This is recieved from the server upon the first valid connection with this key.
    /// </summary>
    public string UserUID { get; set; } = string.Empty;

    /// <summary>
    ///     The secret key used to authenticate with the server and connect.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     If this is the primary key, all other keys are removed when it is removed.
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    ///     If a valid connection was established. This could easily be removed or replaced with if UserUID != string.Empty (?)
    /// </summary>
    public bool HadValidConnection { get; set; } = false;

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
 */
