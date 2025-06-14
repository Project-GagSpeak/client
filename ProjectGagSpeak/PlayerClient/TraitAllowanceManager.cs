using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Hardcore.Movement;
using GagSpeak.Kinksters.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagSpeak.GameInternals;
using Dalamud.Plugin.Services;
using GagSpeak.GameInternals.Structs;

namespace GagSpeak.PlayerClient;

// Needs a rework ffs.
public class TraitAllowanceManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public unsafe GameCameraManager* cameraManager = GameCameraManager.Instance(); // for the camera manager object

    public TraitAllowanceManager(
        ILogger<TraitAllowanceManager> logger,
        GagspeakMediator mediator,
        FavoritesManager favorites,
        ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
        Load();

        Mediator.Subscribe<DalamudLogoutMessage>(this, _ => ClearTraits());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // if we are forced to follow when the plugin disabled, we need to revert the controls.
        if (CachedMovementMode is not MovementMode.NotSet)
        {
            // if we were using standard movement, but it is set to legacy at the time of closing, set it back to standard.
            if (CachedMovementMode is MovementMode.Standard && GameConfig.UiControl.GetBool("MoveMode") is true)
                GameConfig.UiControl.Set("MoveMode", (int)MovementMode.Standard);
        }
    }

    /// <summary> Cache the Movement Mode of our player during ForcedFollow </summary>
    public MovementMode CachedMovementMode = MovementMode.NotSet;
    public EmoteState CachedEmoteState = new EmoteState();


    /// <summary> Lame overhead necessary to avoid mare conflicts with first person fuckery. </summary>
    public bool InitialBlindfoldRedrawMade = false;

    /// <summary> Is the player currently immobile? </summary>
    public bool IsImmobile => ActiveTraits.HasAny(Traits.Immobile) || ActiveHcState.HasAny(HardcoreState.ForceEmote);
    public bool ForceWalking => ActiveTraits.HasAny(Traits.Weighty) || ActiveHcState.HasAny(HardcoreState.ForceFollow);
    public bool ShouldBlockKeys => ActiveHcState.HasAny(HardcoreState.ForceFollow) || IsImmobile;

    public readonly HashSet<string> TraitAllowancesRestraints = [];
    public readonly HashSet<string> TraitAllowancesRestrictions = [];
    public readonly HashSet<string> TraitAllowancesGags = [];
    public readonly HashSet<string> TraitAllowancesPatterns = []; // Unsure how yet.
    public readonly HashSet<string> TraitAllowancesTriggers = []; // Probably Not.

    public Dictionary<GagspeakModule, string[]> GetLightAllowances()
        => new Dictionary<GagspeakModule, string[]>
        {
            { GagspeakModule.Restraint, TraitAllowancesRestraints.ToArray() },
            { GagspeakModule.Restriction, TraitAllowancesRestrictions.ToArray() },
            { GagspeakModule.Gag, TraitAllowancesGags.ToArray() },
            { GagspeakModule.Pattern, TraitAllowancesPatterns.ToArray() },
            { GagspeakModule.Trigger, TraitAllowancesTriggers.ToArray() },
        };

    #region Allowance Sets.
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
        Logger.LogDebug("Adding Allowances: " + string.Join(", ", allowances));
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
    #endregion Allowance Sets.

    #region Saver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.StreamWrite;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool iau) => (iau = false, files.TraitAllowances).Item2;
    public string JsonSerialize() => throw new NotImplementedException();
    public void Load()
    {
        var file = _saver.FileNames.TraitAllowances;
        Logger.LogWarning("Loading in Config for file: " + file);

        if (!File.Exists(file))
        {
            Logger.LogWarning("No Config File found for TraitsManager.");
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
        catch (Exception e)
        {
            GagSpeak.StaticLog.Error(e, "Failed to load TraitsManager.");
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
