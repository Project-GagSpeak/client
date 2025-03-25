using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Services.Configs;

namespace GagSpeak.Services;

public enum FavoriteIdContainer
{
    Restraint,
    Restriction,
    CursedLoot,
    Pattern,
    Alarm,
    Trigger,
}

public class FavoritesManager : IHybridSavable
{
    private readonly HybridSaveService _saver;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider ser, out bool upa) => (upa = false, ser.Favorites).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public FavoritesManager(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    // Favorites Sections.
    public readonly HashSet<Guid>    _favoriteRestraints = [];
    public readonly HashSet<Guid>    _favoriteRestrictions = [];
    public readonly HashSet<GagType> _favoriteGags         = [];
    public readonly HashSet<Guid>    _favoriteCursedLoot   = [];

    public readonly HashSet<Guid>    _favoritePatterns     = [];
    public readonly HashSet<Guid>    _favoriteAlarms       = [];
    public readonly HashSet<Guid>    _favoriteTriggers     = [];

    public readonly HashSet<string>  _favoriteKinksters    = []; // Stores the UID

    public void Load()
    {
        var file = _saver.FileNames.Favorites;
        GagSpeak.StaticLog.Warning("Loading in Favorites Config for file: " + file);
        if (!File.Exists(file))
        {
            GagSpeak.StaticLog.Warning("No Favorites Config file found at {0}", file);
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
            _favoriteRestraints.UnionWith(load.Restraints);
            _favoriteRestrictions.UnionWith(load.Restrictions);
            _favoriteGags.UnionWith(load.Gags);
            _favoriteCursedLoot.UnionWith(load.CursedLoot);
            _favoritePatterns.UnionWith(load.Patterns);
            _favoriteAlarms.UnionWith(load.Alarms);
            _favoriteTriggers.UnionWith(load.Triggers);
            _favoriteKinksters.UnionWith(load.Kinksters);
        }
        catch (Exception e)
        {
            GagSpeak.StaticLog.Error(e, "Failed to load favorites.");
        }
    }

    #region Additions
    public bool TryAddRestriction(FavoriteIdContainer type, Guid restriction)
    {
        var res = type switch
        {
            FavoriteIdContainer.Restriction => _favoriteRestrictions.Add(restriction),
            FavoriteIdContainer.Restraint => _favoriteRestraints.Add(restriction),
            FavoriteIdContainer.CursedLoot => _favoriteCursedLoot.Add(restriction),
            FavoriteIdContainer.Pattern => _favoritePatterns.Add(restriction),
            FavoriteIdContainer.Alarm => _favoriteAlarms.Add(restriction),
            FavoriteIdContainer.Trigger => _favoriteTriggers.Add(restriction),
            _ => false
        };

        if (res)
        {
            GagSpeak.StaticLog.Information("Added {0} to favorites.", type);
            _saver.Save(this);
        }

        return res;
    }

    public bool TryAddGag(GagType gag)
    {
        if (_favoriteGags.Add(gag))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public bool TryAddKinkster(string kinkster)
    {
        if (_favoriteKinksters.Add(kinkster))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public void AddKinksters(IEnumerable<string> kinksters)
    {
        _favoriteKinksters.UnionWith(kinksters);
        _saver.Save(this);
    }

    #endregion Additions

    #region Removals
    public bool RemoveRestriction(FavoriteIdContainer type, Guid restriction)
    {
        var res = type switch
        {
            FavoriteIdContainer.Restraint => _favoriteRestraints.Remove(restriction),
            FavoriteIdContainer.Restriction => _favoriteRestrictions.Remove(restriction),
            FavoriteIdContainer.CursedLoot => _favoriteCursedLoot.Remove(restriction),
            FavoriteIdContainer.Pattern => _favoritePatterns.Remove(restriction),
            FavoriteIdContainer.Alarm => _favoriteAlarms.Remove(restriction),
            FavoriteIdContainer.Trigger => _favoriteTriggers.Remove(restriction),
            _ => false
        };
        if (res) _saver.Save(this);
        return res;
    }

    public bool RemoveGag(GagType gag)
    {
        if (_favoriteGags.Remove(gag))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public bool RemoveKinkster(string kinkster)
    {
        if (_favoriteKinksters.Remove(kinkster))
        {
            _saver.Save(this);
            return true;
        }
        return false;
    }

    public void RemoveKinksters(IEnumerable<string> kinksters)
    {
        _favoriteKinksters.ExceptWith(kinksters);
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
        foreach (var restriction in _favoriteRestrictions)
            j.WriteValue(restriction);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Restraints));
        j.WriteStartArray();
        foreach (var restraint in _favoriteRestraints)
            j.WriteValue(restraint);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Gags));
        j.WriteStartArray();
        foreach (var gag in _favoriteGags)
            j.WriteValue(gag);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.CursedLoot));
        j.WriteStartArray();
        foreach (var loot in _favoriteCursedLoot)
            j.WriteValue(loot);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Patterns));
        j.WriteStartArray();
        foreach (var pattern in _favoritePatterns)
            j.WriteValue(pattern);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Alarms));
        j.WriteStartArray();
        foreach (var alarm in _favoriteAlarms)
            j.WriteValue(alarm);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Triggers));
        j.WriteStartArray();
        foreach (var trigger in _favoriteTriggers)
            j.WriteValue(trigger);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Kinksters));
        j.WriteStartArray();
        foreach (var kinkster in _favoriteKinksters)
            j.WriteValue(kinkster);
        j.WriteEndArray();

        j.WriteEndObject();
    }
    #endregion Saver

    // Used to help with object based deserialization from the json loader.
    private class LoadIntermediary
    {
        public int Version = 0;
        public IEnumerable<Guid>    Restraints = [];
        public IEnumerable<Guid>    Restrictions = [];
        public IEnumerable<GagType> Gags         = [];
        public IEnumerable<Guid>    CursedLoot   = [];
        public IEnumerable<Guid>    Patterns     = [];
        public IEnumerable<Guid>    Alarms       = [];
        public IEnumerable<Guid>    Triggers     = [];
        public IEnumerable<string>  Kinksters    = [];
    }
}
