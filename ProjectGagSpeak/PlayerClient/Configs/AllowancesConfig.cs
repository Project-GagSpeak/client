using CkCommons.HybridSaver;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerClient;

public class AllowancesConfig : IHybridSavable
{
    private readonly ILogger<AllowancesConfig> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly HybridSaveService _saver;

    public AllowancesConfig(ILogger<AllowancesConfig> logger, GagspeakMediator mediator, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _saver = saver;
        Load();
    }

    public readonly HashSet<string> Restraints = [];
    public readonly HashSet<string> Restrictions = [];
    public readonly HashSet<string> Gags = [];
    public readonly HashSet<string> Patterns = []; // Unsure how yet.
    public readonly HashSet<string> Triggers = []; // doubles as puppeteer.

    public Dictionary<GSModule, string[]> GetLightAllowances()
        => new Dictionary<GSModule, string[]>
        {
            { GSModule.Restraint, Restraints.ToArray() },
            { GSModule.Restriction, Restrictions.ToArray() },
            { GSModule.Gag, Gags.ToArray() },
            { GSModule.Pattern, Patterns.ToArray() },
            { GSModule.Trigger, Triggers.ToArray() },
        };

    public void AddAllowance(GSModule type, string kinksterUid)
    {
        var allowances = type switch
        {
            GSModule.Restraint => Restraints,
            GSModule.Restriction => Restrictions,
            GSModule.Gag => Gags,
            GSModule.Pattern => Patterns,
            GSModule.Trigger => Triggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        allowances.Add(kinksterUid);
        _saver.Save(this);
        _mediator.Publish(new AllowancesChanged(type, allowances));
        
    }

    public void AddAllowance(GSModule type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            GSModule.Restraint => Restraints,
            GSModule.Restriction => Restrictions,
            GSModule.Gag => Gags,
            GSModule.Pattern => Patterns,
            GSModule.Trigger => Triggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };
        _logger.LogDebug("Adding Allowances: " + string.Join(", ", allowances));
        set.UnionWith(allowances);
        _saver.Save(this);
        _mediator.Publish(new AllowancesChanged(type, allowances));
    }

    public void RemoveAllowance(GSModule type, string kinksterUid)
    {
        var allowances = type switch
        {
            GSModule.Restraint => Restraints,
            GSModule.Restriction => Restrictions,
            GSModule.Gag => Gags,
            GSModule.Pattern => Patterns,
            GSModule.Trigger => Triggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        allowances.Remove(kinksterUid);
        _saver.Save(this);
        _mediator.Publish(new AllowancesChanged(type, allowances));
    }

    public void RemoveAllowance(GSModule type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            GSModule.Restraint => Restraints,
            GSModule.Restriction => Restrictions,
            GSModule.Gag => Gags,
            GSModule.Pattern => Patterns,
            GSModule.Trigger => Triggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };
        
        set.ExceptWith(allowances);
        _saver.Save(this);
        _mediator.Publish(new AllowancesChanged(type, allowances));
    }

    public void ResetAllowances(GSModule type)
    {
        var set = type switch
        {
            GSModule.Restraint => Restraints,
            GSModule.Restriction => Restrictions,
            GSModule.Gag => Gags,
            GSModule.Pattern => Patterns,
            GSModule.Trigger => Triggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid GagspeakModule!"),
        };

        set.Clear();
        _saver.Save(this);
        _mediator.Publish(new AllowancesChanged(type, set));
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

            Restraints.UnionWith(load.Restraints);
            Restrictions.UnionWith(load.Restrictions);
            Gags.UnionWith(load.Gags);
            Patterns.UnionWith(load.Patterns);
            Triggers.UnionWith(load.Triggers);
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
        foreach (var restraint in Restraints)
            j.WriteValue(restraint);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Restrictions));
        j.WriteStartArray();
        foreach (var restriction in Restrictions)
            j.WriteValue(restriction);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Gags));
        j.WriteStartArray();
        foreach (var gag in Gags)
            j.WriteValue(gag);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Patterns));
        j.WriteStartArray();
        foreach (var pattern in Patterns)
            j.WriteValue(pattern);
        j.WriteEndArray();

        j.WritePropertyName(nameof(LoadIntermediary.Triggers));
        j.WriteStartArray();
        foreach (var trigger in Triggers)
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
