using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;

namespace GagSpeak.PlayerClient;

public class TraitAllowanceManager : IHybridSavable
{
    private readonly ILogger<TraitAllowanceManager> _logger;
    private readonly HybridSaveService _saver;

    public TraitAllowanceManager(ILogger<TraitAllowanceManager> logger, HybridSaveService saver)
    {
        _logger = logger;
        _saver = saver;
        Load();
    }

    public readonly HashSet<string> TraitAllowancesRestraints = [];
    public readonly HashSet<string> TraitAllowancesRestrictions = [];
    public readonly HashSet<string> TraitAllowancesGags = [];
    public readonly HashSet<string> TraitAllowancesPatterns = []; // Unsure how yet.
    public readonly HashSet<string> TraitAllowancesTriggers = []; // doubles as puppeteer.

    public Dictionary<GagspeakModule, string[]> GetLightAllowances()
        => new Dictionary<GagspeakModule, string[]>
        {
            { GagspeakModule.Restraint, TraitAllowancesRestraints.ToArray() },
            { GagspeakModule.Restriction, TraitAllowancesRestrictions.ToArray() },
            { GagspeakModule.Gag, TraitAllowancesGags.ToArray() },
            { GagspeakModule.Pattern, TraitAllowancesPatterns.ToArray() },
            { GagspeakModule.Trigger, TraitAllowancesTriggers.ToArray() },
        };

    public void AddAllowance(GagspeakModule type, string kinksterUid)
    {
        var allowances = type switch
        {
            GagspeakModule.Restraint => TraitAllowancesRestraints,
            GagspeakModule.Restriction => TraitAllowancesRestrictions,
            GagspeakModule.Gag => TraitAllowancesGags,
            GagspeakModule.Pattern => TraitAllowancesPatterns,
            GagspeakModule.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        allowances.Add(kinksterUid);
        _saver.Save(this);
    }

    public void AddAllowance(GagspeakModule type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            GagspeakModule.Restraint => TraitAllowancesRestraints,
            GagspeakModule.Restriction => TraitAllowancesRestrictions,
            GagspeakModule.Gag => TraitAllowancesGags,
            GagspeakModule.Pattern => TraitAllowancesPatterns,
            GagspeakModule.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };
        _logger.LogDebug("Adding Allowances: " + string.Join(", ", allowances));
        set.UnionWith(allowances);
        _saver.Save(this);
    }

    public void RemoveAllowance(GagspeakModule type, string kinksterUid)
    {
        var allowances = type switch
        {
            GagspeakModule.Restraint => TraitAllowancesRestraints,
            GagspeakModule.Restriction => TraitAllowancesRestrictions,
            GagspeakModule.Gag => TraitAllowancesGags,
            GagspeakModule.Pattern => TraitAllowancesPatterns,
            GagspeakModule.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        allowances.Remove(kinksterUid);
        _saver.Save(this);
    }

    public void RemoveAllowance(GagspeakModule type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            GagspeakModule.Restraint => TraitAllowancesRestraints,
            GagspeakModule.Restriction => TraitAllowancesRestrictions,
            GagspeakModule.Gag => TraitAllowancesGags,
            GagspeakModule.Pattern => TraitAllowancesPatterns,
            GagspeakModule.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };
        
        set.ExceptWith(allowances);
        _saver.Save(this);
    }

    public void ResetAllowances(GagspeakModule type)
    {
        var set = type switch
        {
            GagspeakModule.Restraint => TraitAllowancesRestraints,
            GagspeakModule.Restriction => TraitAllowancesRestrictions,
            GagspeakModule.Gag => TraitAllowancesGags,
            GagspeakModule.Pattern => TraitAllowancesPatterns,
            GagspeakModule.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        set.Clear();
        _saver.Save(this);
    }

    public void ResetAllowances()
    {
        TraitAllowancesRestraints.Clear();
        TraitAllowancesRestrictions.Clear();
        TraitAllowancesGags.Clear();
        TraitAllowancesPatterns.Clear();
        TraitAllowancesTriggers.Clear();
        _saver.Save(this);
    }

    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool iau) => (iau = false, files.TraitAllowances).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public void Load()
    {
        var file = _saver.FileNames.TraitAllowances;
        _logger.LogInformation("Loading in Config for file: " + file);

        if (!File.Exists(file))
        {
            _logger.LogWarning("No Config File found for TraitsManager.");
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        try
        {
            var load = JsonConvert.DeserializeObject<LoadIntermediary>(File.ReadAllText(file));
            if (load is null)
                throw new Exception("Failed to load TraitsManager.");
            // Load favorites.
            // (No Migration Needed yet).

            TraitAllowancesRestraints.UnionWith(load.Restraints);
            TraitAllowancesRestrictions.UnionWith(load.Restrictions);
            TraitAllowancesGags.UnionWith(load.Gags);
            TraitAllowancesPatterns.UnionWith(load.Patterns);
            TraitAllowancesTriggers.UnionWith(load.Triggers);
        }
        catch (Bagagwa e)
        {
            Svc.Logger.Error(e, "Failed to load TraitsManager.");
        }
    }

    public void WriteToStream(StreamWriter writer)
    {
        using var j = new JsonTextWriter(writer);
        j.Formatting = Formatting.Indented;
        j.WriteStartObject();

        j.WritePropertyName(nameof(LoadIntermediary.Version));
        j.WriteValue(ConfigVersion);

        j.WritePropertyName(nameof(LoadIntermediary.Restraints));
        j.WriteStartArray();
        foreach (var restraint in TraitAllowancesRestraints)
            j.WriteValue(restraint);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Restrictions));
        j.WriteStartArray();
        foreach (var restriction in TraitAllowancesRestrictions)
            j.WriteValue(restriction);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Gags));
        j.WriteStartArray();
        foreach (var gag in TraitAllowancesGags)
            j.WriteValue(gag);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Patterns));
        j.WriteStartArray();
        foreach (var pattern in TraitAllowancesPatterns)
            j.WriteValue(pattern);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Triggers));
        j.WriteStartArray();
        foreach (var trigger in TraitAllowancesTriggers)
            j.WriteValue(trigger);
        j.WriteEndArray();

        j.WriteEndObject();
    }
    // Used to help with object based deserialization from the json loader.
    private class LoadIntermediary
    {
        public int Version = 0;
        public IEnumerable<string> Restrictions = [];
        public IEnumerable<string> Restraints = [];
        public IEnumerable<string> Gags = [];
        public IEnumerable<string> Patterns = [];
        public IEnumerable<string> Triggers = [];
    }
}
