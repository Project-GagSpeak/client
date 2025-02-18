using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerState.Visual;

public enum TraitApplyType
{
    Restraint,
    Restriction,
    Gag,
    Pattern,
    Trigger,
}

/// <summary> Responsible for tracking the custom settings we have configured for a mod. </summary>
public class TraitsManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly PairManager _pairs;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public TraitsManager(
        ILogger<TraitsManager> logger,
        GagspeakMediator mediator,
        GagRestrictionManager gagManager,
        RestrictionManager restrictionManager,
        RestraintManager restraintManager,
        PairManager pairs,
        FavoritesManager favorites,
        ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _gagManager = gagManager;
        _restrictionManager = restrictionManager;
        _restraintManager = restraintManager;
        _pairs = pairs;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
        Load();
    }

    // Store the current state of each trait.
    public Traits ActiveTraits { get; private set; } = new();

    public bool TrySetTraits(TraitApplyType type, string enactor, Traits traitsToSet)
    {
        if (traitsToSet is 0)
            return false;

        var canApply = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints.Contains(enactor),
            TraitApplyType.Restriction => TraitAllowancesRestrictions.Contains(enactor),
            TraitApplyType.Gag => TraitAllowancesGags.Contains(enactor),
            TraitApplyType.Pattern => TraitAllowancesPatterns.Contains(enactor),
            TraitApplyType.Trigger => TraitAllowancesTriggers.Contains(enactor),
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
        };

        // if we cant apply, return.
        if (!canApply)
            return false;

        // Merge the traits into the active traits.
        ActiveTraits |= traitsToSet;
        return true;
    }

    public void RemoveTraits(Traits traits)
        => ActiveTraits &= ~traits;


    // Allowance Sets.
    public readonly HashSet<string> TraitAllowancesRestraints = [];
    public readonly HashSet<string> TraitAllowancesRestrictions = [];
    public readonly HashSet<string> TraitAllowancesGags = [];
    public readonly HashSet<string> TraitAllowancesPatterns = []; // Unsure how yet.
    public readonly HashSet<string> TraitAllowancesTriggers = []; // Unsure how yet.

    public void AddAllowance(TraitApplyType type, string kinksterUid)
    {
        var allowances = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints,
            TraitApplyType.Restriction => TraitAllowancesRestrictions,
            TraitApplyType.Gag => TraitAllowancesGags,
            TraitApplyType.Pattern => TraitAllowancesPatterns,
            TraitApplyType.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
        };

        allowances.Add(kinksterUid);
        _saver.Save(this);
    }

    public void AddAllowance(TraitApplyType type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints,
            TraitApplyType.Restriction => TraitAllowancesRestrictions,
            TraitApplyType.Gag => TraitAllowancesGags,
            TraitApplyType.Pattern => TraitAllowancesPatterns,
            TraitApplyType.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
        };

        set.UnionWith(allowances);
        _saver.Save(this);
    }

    public void RemoveAllowance(TraitApplyType type, string kinksterUid)
    {
        var allowances = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints,
            TraitApplyType.Restriction => TraitAllowancesRestrictions,
            TraitApplyType.Gag => TraitAllowancesGags,
            TraitApplyType.Pattern => TraitAllowancesPatterns,
            TraitApplyType.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
        };

        allowances.Remove(kinksterUid);
        _saver.Save(this);
    }

    public void RemoveAllowance(TraitApplyType type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints,
            TraitApplyType.Restriction => TraitAllowancesRestrictions,
            TraitApplyType.Gag => TraitAllowancesGags,
            TraitApplyType.Pattern => TraitAllowancesPatterns,
            TraitApplyType.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
        };
        
        set.ExceptWith(allowances);
        _saver.Save(this);
    }

    public void ResetAllowances(TraitApplyType type)
    {
        var set = type switch
        {
            TraitApplyType.Restraint => TraitAllowancesRestraints,
            TraitApplyType.Restriction => TraitAllowancesRestrictions,
            TraitApplyType.Gag => TraitAllowancesGags,
            TraitApplyType.Pattern => TraitAllowancesPatterns,
            TraitApplyType.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid TraitApplyType!"),
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

    #region Saver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool iau) => (iau = false, files.TraitAllowances).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public void Load()
    {
        var file = _saver.FileNames.TraitAllowances;
        if (!File.Exists(file))
            return;
        try
        {
            var load = JsonConvert.DeserializeObject<LoadIntermediary>(File.ReadAllText(file));
            if (load is null)
                throw new Exception("Failed to load favorites.");
            // Load favorites.
            // (No Migration Needed yet).

            TraitAllowancesRestraints.UnionWith(load.Restraints);
            TraitAllowancesRestrictions.UnionWith(load.Restrictions);
            TraitAllowancesGags.UnionWith(load.Gags);
            TraitAllowancesPatterns.UnionWith(load.Patterns);
            TraitAllowancesTriggers.UnionWith(load.Triggers);
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogCritical(e, "Failed to load favorites.");
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
    #endregion Saver
}
