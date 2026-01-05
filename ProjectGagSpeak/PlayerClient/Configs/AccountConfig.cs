using CkCommons;
using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;

namespace GagSpeak.PlayerClient;

public class AccountConfig : IHybridSavable
{
    private readonly ILogger<AccountConfig> _logger;
    private readonly HybridSaveService _saver;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public int ConfigVersion => 1;
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
        switch (version)
        {
            case 0:
                MigrateAndLoadV0AsV1(jObject);
                break;
            case 1:
                LoadV1(jObject["AccountInfo"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _logger.LogInformation("Config loaded.");
        Save();
    }

    private void MigrateAndLoadV0AsV1(JObject root)
    {
        // Get the old ServerStorage node.
        var oldStorage = root["ServerStorage"] as JObject;
        if (oldStorage == null)
            throw new Exception("Failed to find old ServerStorage for V0 migration.");

        var newStorage = new AccountStorage()
        {
            Profiles = new List<AccountProfile>()
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
                    IsMainProfile = auth["IsPrimary"]?.Value<bool>() ?? false,
                    ProfileUID = secretKey?["LinkedProfileUID"]?.Value<string>() ?? string.Empty,
                    SecretKey = secretKey?["Key"]?.Value<string>() ?? string.Empty
                };

                newStorage.Profiles.Add(profile);
            }
        }

        // Replace the old storage in the root object
        root["ServerStorage"] = JObject.FromObject(newStorage);

        // Update version to 1
        root["Version"] = 1;

        // Also update Current to reflect the new AccountStorage
        Current = newStorage;
    }

    private void LoadV1(JToken? data)
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
    public List<AccountProfile> Profiles { get; set; } = [];
}

public record AccountProfile
{
    /// <summary>
    ///     True if this is the main account. Alts are removed if main is removed.
    /// </summary>
    public bool IsMainProfile { get; set; } = false;

    /// <summary>
    ///     True if the UID and SecretKey are set (a valid connection was made.).
    /// </summary>
    public bool HadValidConnection => !string.IsNullOrEmpty(SecretKey) && !string.IsNullOrEmpty(ProfileUID);

    /// <summary>
    ///     Unique identifier for this profile within the database. <para/>
    ///     Assigned on the first connection or when linking to an existing profile if no matching UID is found.
    /// </summary>
    public string ProfileUID { get; set; } = string.Empty;

    /// <summary>
    ///     Secret key used to identify and validate this profile with the server. <para />
    ///     Generated upon MainAccount One-Time fetch, or after manual insertion.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    ///     Name of the character associated with this profile. <para />
    ///     Stored once a valid connection has been made and the character is recognized.
    /// </summary>
    public string PlayerName { get; set; } = string.Empty;

    /// <summary>
    ///     World ID of the character associated with this profile. <para />
    ///     Used together with PlayerName to track the character across sessions.
    /// </summary>
    public ushort WorldId { get; set; } = 0;

    /// <summary>
    ///     Persistent Content ID of the character. <para />
    ///     Unique across all registered XIV characters and used to detect name/world changes.
    /// </summary>
    public ulong ContentId { get; set; } = 0;
}
