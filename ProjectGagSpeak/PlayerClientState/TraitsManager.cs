using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Hardcore.Movement;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.CkCommons.Gui;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Responsible for tracking the custom settings we have configured for a mod. </summary>
public class TraitsManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public unsafe GameCameraManager* cameraManager = GameCameraManager.Instance(); // for the camera manager object

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

    public Action<HardcoreState, HardcoreState>? OnHcStateChanged;
    public Action<Traits, Traits>? OnTraitStateChanged;
    public Action<Stimulation, Stimulation>? OnStimulationStateChanged;
    private HardcoreState _prevHcState = HardcoreState.None;
    private Traits _prevTraits = Traits.None;
    private Stimulation _prevStim = Stimulation.None;
    public HardcoreState ActiveHcState
    {
        get => _prevHcState;
        private set { if (_prevHcState != value) { OnHcStateChanged?.Invoke(_prevHcState, value); _prevHcState = value; } }
    }

    public Traits ActiveTraits
    {
        get => _prevTraits;
        private set { if (_prevTraits != value) { OnTraitStateChanged?.Invoke(_prevTraits, value); _prevTraits = value; } }
    }

    public Stimulation ActiveStim
    {
        get => _prevStim;
        private set { if (_prevStim != value) { OnStimulationStateChanged?.Invoke(_prevStim, value); _prevStim = value; } }
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

    // Helper functions for our Action & Movement monitors & Controllers.
    public float GetVibeMultiplier()
    {
        return ActiveStim switch
        {
            Stimulation.None    => 1f,
            Stimulation.Light   => 1.125f,
            Stimulation.Mild    => 1.375f,
            Stimulation.Heavy   => 1.875f,
            _ => 1f,
        };
    }


    // Get all of this out of here!
    //
    public async Task HandleBlindfoldLogic(NewState newState)
    {
/*        // toggle our window based on conditions
        if (newState is NewState.Enabled)
        {
            // if the window isnt open, open it.
            if (!BlindfoldService.IsWindowOpen) Mediator.Publish(new UiToggleMessage(typeof(BlindfoldService), ToggleType.Show));
            // go in for camera voodoo.
            DoCameraVoodoo(newState);
        }
        else
        {
            if (BlindfoldService.IsWindowOpen) Mediator.Publish(new HardcoreRemoveBlindfoldMessage());
            // wait a bit before doing the camera voodoo
            await Task.Delay(2000);
            DoCameraVoodoo(newState);
        }*/
    }

    private unsafe void DoCameraVoodoo(NewState newValue)
    {
        // force the camera to first person, but dont loop the force
        if (newValue is NewState.Enabled)
        {
            if (cameraManager is not null && cameraManager->Camera is not null && cameraManager->Camera->Mode is not (int)CameraControlMode.FirstPerson)
                cameraManager->Camera->Mode = (int)CameraControlMode.FirstPerson;
        }
        else
        {
            if (cameraManager is not null && cameraManager->Camera is not null && cameraManager->Camera->Mode is (int)CameraControlMode.FirstPerson)
                cameraManager->Camera->Mode = (int)CameraControlMode.ThirdPerson;
        }
    }

    public bool TrySetTraits(ModuleSection type, string enactor, Traits traitsToSet)
    {
        if (traitsToSet is 0)
            return false;

        var canApply = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints.Contains(enactor),
            ModuleSection.Restriction => TraitAllowancesRestrictions.Contains(enactor),
            ModuleSection.Gag => TraitAllowancesGags.Contains(enactor),
            ModuleSection.Pattern => TraitAllowancesPatterns.Contains(enactor), // TBD
            ModuleSection.Trigger => TraitAllowancesTriggers.Contains(enactor), // TBD
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
        };

        if (!canApply)
            return false;

        // Merge the traits into the active traits.
        ActiveTraits |= traitsToSet;
        return true;
    }

    public void RemoveTraits(Traits traits)
        => ActiveTraits &= ~traits;

    private void ClearTraits()
        => ActiveTraits = Traits.None;

    public readonly HashSet<string> TraitAllowancesRestraints = [];
    public readonly HashSet<string> TraitAllowancesRestrictions = [];
    public readonly HashSet<string> TraitAllowancesGags = [];
    public readonly HashSet<string> TraitAllowancesPatterns = []; // Unsure how yet.
    public readonly HashSet<string> TraitAllowancesTriggers = []; // Probably Not.

    public Dictionary<ModuleSection, string[]> GetLightAllowances()
        => new Dictionary<ModuleSection, string[]>
        {
            { ModuleSection.Restraint, TraitAllowancesRestraints.ToArray() },
            { ModuleSection.Restriction, TraitAllowancesRestrictions.ToArray() },
            { ModuleSection.Gag, TraitAllowancesGags.ToArray() },
            { ModuleSection.Pattern, TraitAllowancesPatterns.ToArray() },
            { ModuleSection.Trigger, TraitAllowancesTriggers.ToArray() },
        };

    #region Allowance Sets.
    public void AddAllowance(ModuleSection type, string kinksterUid)
    {
        var allowances = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints,
            ModuleSection.Restriction => TraitAllowancesRestrictions,
            ModuleSection.Gag => TraitAllowancesGags,
            ModuleSection.Pattern => TraitAllowancesPatterns,
            ModuleSection.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
        };

        allowances.Add(kinksterUid);
        _saver.Save(this);
    }

    public void AddAllowance(ModuleSection type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints,
            ModuleSection.Restriction => TraitAllowancesRestrictions,
            ModuleSection.Gag => TraitAllowancesGags,
            ModuleSection.Pattern => TraitAllowancesPatterns,
            ModuleSection.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
        };
        Logger.LogDebug("Adding Allowances: " + string.Join(", ", allowances));
        set.UnionWith(allowances);
        _saver.Save(this);
    }

    public void RemoveAllowance(ModuleSection type, string kinksterUid)
    {
        var allowances = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints,
            ModuleSection.Restriction => TraitAllowancesRestrictions,
            ModuleSection.Gag => TraitAllowancesGags,
            ModuleSection.Pattern => TraitAllowancesPatterns,
            ModuleSection.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
        };

        allowances.Remove(kinksterUid);
        _saver.Save(this);
    }

    public void RemoveAllowance(ModuleSection type, IEnumerable<string> allowances)
    {
        var set = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints,
            ModuleSection.Restriction => TraitAllowancesRestrictions,
            ModuleSection.Gag => TraitAllowancesGags,
            ModuleSection.Pattern => TraitAllowancesPatterns,
            ModuleSection.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
        };
        
        set.ExceptWith(allowances);
        _saver.Save(this);
    }

    public void ResetAllowances(ModuleSection type)
    {
        var set = type switch
        {
            ModuleSection.Restraint => TraitAllowancesRestraints,
            ModuleSection.Restriction => TraitAllowancesRestrictions,
            ModuleSection.Gag => TraitAllowancesGags,
            ModuleSection.Pattern => TraitAllowancesPatterns,
            ModuleSection.Trigger => TraitAllowancesTriggers,
            _ => throw new ArgumentException(nameof(type) + " Is not a valid ModuleSection!"),
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
