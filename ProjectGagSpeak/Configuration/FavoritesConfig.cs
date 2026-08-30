using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerClient;

public enum FavoriteType
{
    Restraint,
    Restriction,
    Collar,
    CursedLoot,
    Alias,
    Pattern,
    Alarm,
    Trigger
}

public class FavoritesAccountData
{
    public HashSet<string> Kinksters { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> Emotes { get; set; } = new HashSet<string>(StringComparer.Ordinal);
    public HashSet<string> GsTells { get; set; } = new(StringComparer.Ordinal);
}

public class FavoritesConfig : IHybridSavable
{
    private readonly ILogger<FavoritesConfig> _logger;
    private readonly HybridSaveService _saver;
    public int ConfigVersion => 2;
    public int MaxBackups => 3;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC => DateTime.MinValue;
    public string ToFilePath(GsFiles files) => files.Favorites;
    public string JsonSerialize() => throw new NotImplementedException();

    private static readonly Dictionary<string, FavoritesAccountData> _dataByServer = new(StringComparer.Ordinal);
    private static readonly HashSet<Guid> _restraints = [];
    private static readonly HashSet<Guid> _restrictions = [];
    private static readonly HashSet<GagType> _gags = [];
    private static readonly HashSet<Guid> _collars = [];
    private static readonly HashSet<Guid> _cursedLoot = [];
    private static readonly HashSet<Guid> _aliases = [];
    private static readonly HashSet<Guid> _patterns = [];
    private static readonly HashSet<Guid> _alarms = [];
    private static readonly HashSet<Guid> _triggers = [];
    private static FavoritesAccountData _current => GetOrCreateConfigData();

    public FavoritesConfig(ILogger<FavoritesConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    public static IReadOnlySet<string> Kinksters    => _current.Kinksters;
    public static IReadOnlySet<string> Emotes       => _current.Emotes;
    public static IReadOnlySet<string> ChatLogs     => _current.GsTells;
    public static IReadOnlySet<Guid> Restraints     => _restraints;
    public static IReadOnlySet<Guid> Restrictions   => _restrictions;
    public static IReadOnlySet<GagType> Gags        => _gags;
    public static IReadOnlySet<Guid> Collars        => _collars;
    public static IReadOnlySet<Guid> CursedLoot     => _cursedLoot;
    public static IReadOnlySet<Guid> Aliases        => _aliases;
    public static IReadOnlySet<Guid> Patterns       => _patterns;
    public static IReadOnlySet<Guid> Alarms         => _alarms;
    public static IReadOnlySet<Guid> Triggers       => _triggers;

    private static FavoritesAccountData GetOrCreateConfigData()
    {
        if (_dataByServer.TryGetValue(ConnectionsConfig.CurrentHubURI, out var set))
            return set;
        // Create
        return (_dataByServer[ConnectionsConfig.CurrentHubURI] = new FavoritesAccountData());
    }

    private static HashSet<Guid>? GetTypeSet(FavoriteType type) => type switch
    {
        FavoriteType.Restraint => _restraints,
        FavoriteType.Restriction => _restrictions,
        FavoriteType.Collar => _collars,
        FavoriteType.CursedLoot => _cursedLoot,
        FavoriteType.Alias => _aliases,
        FavoriteType.Pattern => _patterns,
        FavoriteType.Alarm => _alarms,
        FavoriteType.Trigger => _triggers,
        _ => null
    };

    #region Additions
    public void Favorite(FavoriteType type, Guid id)
    {
        var set = GetTypeSet(type);
        if (set?.Add(id) == true)
            _saver.Save(this);
    }

    public void FavoriteGag(GagType gag)
    {
        if (_gags.Add(gag))
            _saver.Save(this);
    }

    public void FavoriteKinkster(string kinksterId)
    {
        if (_current.Kinksters.Add(kinksterId))
            _saver.Save(this);
    }

    public void FavoriteEmote(string emoteId)
    {
        if (_current.Emotes.Add(emoteId))
            _saver.Save(this);
    }

    public void FavoriteTell(string chatId)
    {
        if (_current.GsTells.Add(chatId))
            _saver.Save(this);
    }

    public void BulkFavorite(FavoriteType type, IEnumerable<Guid> ids)
    {
        var set = GetTypeSet(type);
        if (set != null)
        {
            set.UnionWith(ids);
            _saver.Save(this);
        }
    }

    public void BulkFavoriteKinksters(IEnumerable<string> kinksterIds)
    {
        _current.Kinksters.UnionWith(kinksterIds);
        _saver.Save(this);
    }
    #endregion

    #region Removals & Toggles
    public void Unfavorite(FavoriteType type, Guid id)
    {
        var set = GetTypeSet(type);
        if (set?.Remove(id) == true)
            _saver.Save(this);
    }

    public void UnfavoriteGag(GagType gag)
    {
        if (_gags.Remove(gag))
            _saver.Save(this);
    }

    public void UnfavoriteKinkster(string kinksterId)
    {
        if (_current.Kinksters.Remove(kinksterId))
            _saver.Save(this);
    }

    public void UnfavoriteEmote(string emoteId)
    {
        if (_current.Emotes.Remove(emoteId))
            _saver.Save(this);
    }

    public void UnfavoriteChat(string chatId)
    {
        if (_current.GsTells.Remove(chatId))
            _saver.Save(this);
    }

    public void ToggleFavorite(FavoriteType type, Guid id)
    {
        var set = GetTypeSet(type);
        if (set is null) return;

        if (!set.Remove(id))
            set.Add(id);

        _saver.Save(this);
    }
    #endregion

    #region Saver
    public void Load()
    {
        var file = _saver.FileNames.Favorites;
        _logger.LogInformation($"Loading FavoritesConfig file: {file}");
        if (!File.Exists(file))
        {
            _logger.LogWarning($"FavoritesConfig file not found: {file}");
            _dataByServer.Clear();
            _gags.Clear();
            _restraints.Clear();
            _restrictions.Clear();
            _collars.Clear();
            _cursedLoot.Clear();
            _aliases.Clear();
            _patterns.Clear();
            _alarms.Clear();
            _triggers.Clear();
            _saver.Save(this);
            return;
        }

        try
        {
            var load = JsonConvert.DeserializeObject<LoadIntermediary>(File.ReadAllText(file))
                ?? throw new Exception("Failed to deserialize FavoritesConfig");

            // Builds up to 2.2.0.4 wrote the v2 format but output as Version 1
            // so check for the presence of the Accounts object and force LoadV2.
            if (load.Accounts is not null)
            {
                LoadV2(load);
            }
            else
            {
                switch (load.Version)
                {
                    case 0:
                    case 1:
                        LoadV1(load);
                        break;
                    case 2:
                        LoadV2(load);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported FavoritesConfig version {load.Version}");
                }
            }
        }
        catch (Exception e)
        {
            // Leave the file on disk untouched so it stays recoverable.
            _logger.LogError(e, "Failed to load FavoritesConfig.");
            return;
        }
        _saver.Save(this);
    }

    private void LoadV1(LoadIntermediary load)
    {
        _logger.LogInformation("Migrating FavoritesConfig from v1 to v2");
        var acc = GetOrCreateConfigData();

        if (load.Kinksters is not null)
            acc.Kinksters.UnionWith(load.Kinksters);

        LoadGlobals(load);
    }

    private void LoadV2(LoadIntermediary load)
    {
        if (load.Accounts is not null)
        {
            foreach (var (server, acc) in load.Accounts)
            {
                _dataByServer[server] = new FavoritesAccountData
                {
                    Kinksters = new HashSet<string>(acc.Kinksters, StringComparer.Ordinal),
                    Emotes = new HashSet<string>(acc.Emotes, StringComparer.Ordinal)
                };
            }
        }

        LoadGlobals(load);
    }

    private void LoadGlobals(LoadIntermediary load)
    {
        _gags.UnionWith(load.Gags ?? []);
        _restraints.UnionWith(load.Restraints ?? []);
        _restrictions.UnionWith(load.Restrictions ?? []);
        _collars.UnionWith(load.Collars ?? []);
        _cursedLoot.UnionWith(load.CursedLoot ?? []);
        _aliases.UnionWith(load.Aliases ?? []);
        _patterns.UnionWith(load.Patterns ?? []);
        _alarms.UnionWith(load.Alarms ?? []);
        _triggers.UnionWith(load.Triggers ?? []);
    }

    public void WriteToStream(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        j.WriteStartObject();

        j.WritePropertyName(nameof(LoadIntermediary.Version));
        j.WriteValue(ConfigVersion);

        j.WritePropertyName(nameof(LoadIntermediary.Accounts));
        j.WriteStartObject();
        foreach (var (server, acc) in _dataByServer)
        {
            j.WritePropertyName(server);
            j.WriteStartObject();

            j.WritePropertyName(nameof(FavoritesAccountData.Kinksters));
            j.WriteStartArray();
            foreach (var uid in acc.Kinksters) j.WriteValue(uid);
            j.WriteEndArray();

            j.WritePropertyName(nameof(FavoritesAccountData.Emotes));
            j.WriteStartArray();
            foreach (var uid in acc.Emotes) j.WriteValue(uid);
            j.WriteEndArray();

            j.WriteEndObject();
        }
        j.WriteEndObject();

        // Write Globals
        WriteGuidArray(j, nameof(LoadIntermediary.Restraints), Restraints);
        WriteGuidArray(j, nameof(LoadIntermediary.Restrictions), Restrictions);
        WriteGuidArray(j, nameof(LoadIntermediary.Collars), Collars);
        WriteGuidArray(j, nameof(LoadIntermediary.CursedLoot), CursedLoot);
        WriteGuidArray(j, nameof(LoadIntermediary.Aliases), Aliases);
        WriteGuidArray(j, nameof(LoadIntermediary.Patterns), Patterns);
        WriteGuidArray(j, nameof(LoadIntermediary.Alarms), Alarms);
        WriteGuidArray(j, nameof(LoadIntermediary.Triggers), Triggers);

        j.WritePropertyName(nameof(LoadIntermediary.Gags));
        j.WriteStartArray();
        foreach (var gag in Gags) j.WriteValue(gag);
        j.WriteEndArray();

        j.WriteEndObject();
    }

    // Helpers to clean up JSON writing
    private static void WriteGuidArray(JsonTextWriter j, string name, IEnumerable<Guid> items)
    {
        j.WritePropertyName(name);
        j.WriteStartArray();
        foreach (var item in items) j.WriteValue(item);
        j.WriteEndArray();
    }
    #endregion

    // Used to help with object based deserialization from the json loader.
    private sealed class LoadIntermediary
    {
        public int Version = 2;

        // v2 Properties
        public Dictionary<string, FavoritesAccountData>? Accounts = null;

        // Globals
        public IEnumerable<Guid>? Restraints = [];
        public IEnumerable<Guid>? Restrictions = [];
        public IEnumerable<GagType>? Gags = [];
        public IEnumerable<Guid>? Collars = [];
        public IEnumerable<Guid>? CursedLoot = [];
        public IEnumerable<Guid>? Aliases = [];
        public IEnumerable<Guid>? Patterns = [];
        public IEnumerable<Guid>? Alarms = [];
        public IEnumerable<Guid>? Triggers = [];

        // v1 Legacy
        public IEnumerable<string>? Kinksters = [];
    }
}
