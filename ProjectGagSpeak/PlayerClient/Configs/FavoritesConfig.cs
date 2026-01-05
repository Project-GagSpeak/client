using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;

namespace GagSpeak.PlayerClient;

public enum FavoriteIdContainer
{
    Restraint,
    Restriction,
    Collar,
    CursedLoot,
    Pattern,
    Alarm,
    Trigger,
}

public class FavoritesConfig : IHybridSavable
{
    private readonly ILogger<FavoritesConfig> _logger;
    private readonly HybridSaveService _saver;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider ser, out bool upa) => (upa = false, ser.Favorites).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public FavoritesConfig(ILogger<FavoritesConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    // Favorites Sections.
    public readonly HashSet<Guid>    Restraints = [];
    public readonly HashSet<Guid>    Restrictions = [];
    public readonly HashSet<GagType> Gags         = [];
    public readonly HashSet<Guid>    Collars      = [];
    public readonly HashSet<Guid>    CursedLoot   = [];

    public readonly HashSet<Guid>    Patterns     = [];
    public readonly HashSet<Guid>    Alarms       = [];
    public readonly HashSet<Guid>    Triggers     = [];

    public readonly HashSet<string>  Kinksters    = []; // Stores the UID

    public void Load()
    {
        var file = _saver.FileNames.Favorites;
        Svc.Logger.Information("Loading in Favorites Config for file: " + file);
        if (!File.Exists(file))
        {
            Svc.Logger.Warning("No Favorites Config file found at {0}", file);
            _saver.Save(this);
            return;
        }

        try
        {
            var load = JsonConvert.DeserializeObject<LoadIntermediary>(File.ReadAllText(file));
            if (load is null)
                throw new Exception("Failed to load favorites.");
            // Load favorites.
            // (No Migration Needed yet).
            Restraints.UnionWith(load.Restraints);
            Restrictions.UnionWith(load.Restrictions);
            Gags.UnionWith(load.Gags);
            Collars.UnionWith(load.Collars);
            CursedLoot.UnionWith(load.CursedLoot);
            Patterns.UnionWith(load.Patterns);
            Alarms.UnionWith(load.Alarms);
            Triggers.UnionWith(load.Triggers);
            Kinksters.UnionWith(load.Kinksters);
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error(e, "Failed to load favorites.");
        }
    }

    #region Additions
    public bool TryAddRestriction(FavoriteIdContainer type, Guid restriction)
    {
        var res = type switch
        {
            FavoriteIdContainer.Restriction => Restrictions.Add(restriction),
            FavoriteIdContainer.Restraint => Restraints.Add(restriction),
            FavoriteIdContainer.Collar => Collars.Add(restriction),
            FavoriteIdContainer.CursedLoot => CursedLoot.Add(restriction),
            FavoriteIdContainer.Pattern => Patterns.Add(restriction),
            FavoriteIdContainer.Alarm => Alarms.Add(restriction),
            FavoriteIdContainer.Trigger => Triggers.Add(restriction),
            _ => false
        };

        if (res)
        {
            Svc.Logger.Information("Added {0} to favorites.", type);
            _saver.Save(this);
        }

        return res;
    }

    public bool TryAddGag(GagType gag)
    {
        if (Gags.Add(gag))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public bool TryAddKinkster(string kinkster)
    {
        if (Kinksters.Add(kinkster))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public void AddKinksters(IEnumerable<string> kinksters)
    {
        Kinksters.UnionWith(kinksters);
        _saver.Save(this);
    }

    #endregion Additions

    #region Removals
    public bool RemoveRestriction(FavoriteIdContainer type, Guid restriction)
    {
        var res = type switch
        {
            FavoriteIdContainer.Restraint => Restraints.Remove(restriction),
            FavoriteIdContainer.Restriction => Restrictions.Remove(restriction),
            FavoriteIdContainer.Collar => Collars.Remove(restriction),
            FavoriteIdContainer.CursedLoot => CursedLoot.Remove(restriction),
            FavoriteIdContainer.Pattern => Patterns.Remove(restriction),
            FavoriteIdContainer.Alarm => Alarms.Remove(restriction),
            FavoriteIdContainer.Trigger => Triggers.Remove(restriction),
            _ => false
        };
        if (res) _saver.Save(this);
        return res;
    }

    public bool RemoveGag(GagType gag)
    {
        if (Gags.Remove(gag))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public bool RemoveKinkster(string kinkster)
    {
        if (Kinksters.Remove(kinkster))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public void RemoveKinksters(IEnumerable<string> kinksters)
    {
        Kinksters.ExceptWith(kinksters);
        _saver.Save(this);
    }

    #endregion Removals

    #region Saver
    public void WriteToStream(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        j.WriteStartObject();

        j.WritePropertyName(nameof(LoadIntermediary.Version));
        j.WriteValue(ConfigVersion);

        j.WritePropertyName(nameof(LoadIntermediary.Restrictions));
        j.WriteStartArray();
        foreach (var restriction in Restrictions)
            j.WriteValue(restriction);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Restraints));
        j.WriteStartArray();
        foreach (var restraint in Restraints)
            j.WriteValue(restraint);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Gags));
        j.WriteStartArray();
        foreach (var gag in Gags)
            j.WriteValue(gag);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Collars));
        j.WriteStartArray();
        foreach (var collar in Collars)
            j.WriteValue(collar);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.CursedLoot));
        j.WriteStartArray();
        foreach (var loot in CursedLoot)
            j.WriteValue(loot);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Patterns));
        j.WriteStartArray();
        foreach (var pattern in Patterns)
            j.WriteValue(pattern);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Alarms));
        j.WriteStartArray();
        foreach (var alarm in Alarms)
            j.WriteValue(alarm);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Triggers));
        j.WriteStartArray();
        foreach (var trigger in Triggers)
            j.WriteValue(trigger);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Kinksters));
        j.WriteStartArray();
        foreach (var kinkster in Kinksters)
            j.WriteValue(kinkster);
        j.WriteEndArray();

        j.WriteEndObject();
    }
    #endregion Saver

    // Used to help with object based deserialization from the json loader.
    private class LoadIntermediary
    {
        public int Version = 1;
        public IEnumerable<Guid>    Restraints   = [];
        public IEnumerable<Guid>    Restrictions = [];
        public IEnumerable<GagType> Gags         = [];
        public IEnumerable<Guid>    Collars      = [];
        public IEnumerable<Guid>    CursedLoot   = [];
        public IEnumerable<Guid>    Patterns     = [];
        public IEnumerable<Guid>    Alarms       = [];
        public IEnumerable<Guid>    Triggers     = [];
        public IEnumerable<string>  Kinksters    = [];
    }
}
