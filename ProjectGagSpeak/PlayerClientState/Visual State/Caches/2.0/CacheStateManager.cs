using GagSpeak.Localization;
using GagSpeak.PlayerState.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
using Lumina.Excel.Sheets;

namespace GagSpeak.PlayerState.Visual;

/// <summary>
///     Manages the current collective, and final active state for all visual alterations.
///     Helper functions for appending, removing, and managing individual caches are included.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public class CacheStateManager
{
    private readonly ILogger<CacheStateManager> _logger;
    private readonly GlamourCacheManager _glamourCache;
    private readonly ModCacheManager _modsCache;
    private readonly MoodleCacheManager _moodlesCache;
    private readonly VisualApplierCPlus _cplusApplier;
    private readonly TraitsManager _traitsManager;

    // The current collective active state items.
    private SortedList<CombinedCacheKey, CustomizeProfile> _customize;
    private SortedList<CombinedCacheKey, Traits> _traits;
    private SortedList<CombinedCacheKey, Stimulation> _stimulations;

    // The finalized state cache to be referenced by listeners and other components.
    private CustomizeProfile _finalCustomize;
    private Traits _finalTraits; // just outright want this gone. But im not really sure anymore...
    private Stimulation _finalStim; // I hate this, i want it gone.

    public CacheStateManager(ILogger<CacheStateManager> logger,
        GlamourCacheManager glamourCache,
        ModCacheManager modsCache,
        MoodleCacheManager moodlesCache)
    {
        _logger = logger;
        _glamourCache = glamourCache;
        _modsCache = modsCache;
        _moodlesCache = moodlesCache;
    }

    public async Task TryApply(GarblerRestriction gag, int layerIdx, string enactorUID)
    {
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx);

        // Begin the tasks to run in parallel.
        var tasks = new List<Task>
        {
            _glamourCache.AddToCache(combinedKey, gag.Glamour),
            _modsCache.AddToCache(combinedKey, gag.Mod),
            _moodlesCache.AddToCache(combinedKey, gag.Moodle),
            _glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.HatState, gag.HeadgearState),
            _glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.VisorState, gag.VisorState)
        };

        // Syncronous Tasks.
        HandleCPlusProfile(combinedKey, gag.CPlusProfile);
        HandleTraits(combinedKey, gag.Traits);
        HandleStimulation(combinedKey, gag.Stimulation);

        await Task.WhenAll(tasks);
    }

    public async Task TryRemove(GarblerRestriction gag, int layerIdx, string enactor)
    {
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx);
        // Begin the tasks to run in parallel.
/*        var tasks = new List<Task>
        {
            _glamourCache.RemoveFromCache(combinedKey),
            _modsCache.RemoveFromCache(combinedKey),
            _moodlesCache.RemoveFromCache(combinedKey)
        };

        // Handle syncronous removals here.
        await Task.WhenAll(tasks);*/
        _logger.LogDebug("Removing GarblerRestriction {Gag} from layer {LayerIdx}", gag, layerIdx);
    }

    public async Task TryApply(RestrictionItem item, int layerIdx, string enactorUID)
    {
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx);

        // Begin the tasks to run in parallel.
        var tasks = new List<Task>
        {
            _glamourCache.AddToCache(combinedKey, item.Glamour),
            _modsCache.AddToCache(combinedKey, item.Mod),
            _moodlesCache.AddToCache(combinedKey, item.Moodle)
        };

        // Syncronous Tasks.
        HandleTraits(combinedKey, item.Traits);
        HandleStimulation(combinedKey, item.Stimulation);

        // Type-Spesific Tasks.
        switch(item)
        {
            case BlindfoldRestriction collar:
                tasks.Add(_glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.HatState, collar.HeadgearState));
                tasks.Add(_glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.VisorState, collar.VisorState));
                // TODO: Add some blindfold attribute here or something
                break;

            case HypnoticRestriction hypno:
                tasks.Add(_glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.HatState, hypno.HeadgearState));
                tasks.Add(_glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.VisorState, hypno.VisorState));
                // TODO: Add some hypnotic attribute here or something
                break;
        }

        await Task.WhenAll(tasks);
    }

    public async Task TryRemove(RestrictionItem item, int layerIdx, string enactor)
    {
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx);
/*        // Begin the tasks to run in parallel.
        var tasks = new List<Task>
        {
            _glamourCache.RemoveFromCache(combinedKey),
            _modsCache.RemoveFromCache(combinedKey),
            _moodlesCache.RemoveFromCache(combinedKey)
        };
        // Handle syncronous removals here.
        await Task.WhenAll(tasks);*/
        _logger.LogDebug("Removing RestrictionItem {Item} from layer {LayerIdx}", item, layerIdx);
    }

    // This is going to be a whole other ballpark to deal with, so save for later, it will come with additional complexity.
    public async Task TryApply(RestraintSet restraint, string enactorUID)
    {
        // -1 is the base, 0-4 are the layers. (could make it 0 to line things up but this makes it better in code)
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, -1);

        // Begin the tasks to run in parallel.
        var metas = restraint.GetMetaData();
        var tasks = new List<Task>
        {
            _glamourCache.AddToCache(combinedKey, restraint.GetGlamour()),
            _modsCache.AddToCache(combinedKey, restraint.GetMods()),
            _moodlesCache.AddToCache(combinedKey, restraint.GetMoodles()),
            _glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.HatState, metas.Headgear),
            _glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.VisorState, metas.Visor),
            _glamourCache.AddMetaInCache(combinedKey, Components.MetaIndex.WeaponState, metas.Weapon),
        };

        // Syncronous Tasks.
        HandleTraits(combinedKey, restraint.Traits);
        HandleStimulation(combinedKey, restraint.Stimulation);

        await Task.WhenAll(tasks);
    }

    public async Task TryRemove(RestraintSet restraint, string enactor)
    {
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, -1);
        /*        // Begin the tasks to run in parallel.
        var tasks = new List<Task>
        {
            _glamourCache.RemoveFromCache(combinedKey),
            _modsCache.RemoveFromCache(combinedKey),
            _moodlesCache.RemoveFromCache(combinedKey)
        };
        // Handle syncronous removals here.
        await Task.WhenAll(tasks);*/
        _logger.LogDebug($"Removing RestrictionItem {restraint.Label}");
    }

    // Reapplies the cached final state to all active states and stuff.
    public async Task ReapplyFinalState()
    {
        _logger.LogWarning("What is this some kind of expectation it worked?");
    }

    private void HandleCPlusProfile(CombinedCacheKey key, CustomizeProfile profile)
    {
        if (profile.ProfileGuid != Guid.Empty)
        {
            _customize.Add(key, profile);
            var prevVal = _finalCustomize;
            _finalCustomize = _customize.Values.FirstOrDefault();

            if (!prevVal.Equals(_finalCustomize))
                _cplusApplier.SetOrUpdateProfile(_finalCustomize);
            else if (_finalCustomize == CustomizeProfile.Empty)
                _cplusApplier.ClearRestrictedProfile();
        }
    }

    private void HandleTraits(CombinedCacheKey key, Traits traits)
    {
        if (traits is not Traits.None)
        {
            _traits.Add(key, traits);
            // update the final traits, then do nothing else for now, still figuring that out.
            // (mainly due to the nature of multiple hypnotic / blindfold layers at once but yeah)
            _finalTraits = _traits.Values.Aggregate(Traits.None, (current, next) => current | next);
        }
    }

    private void HandleStimulation(CombinedCacheKey key, Stimulation stimulation)
    {
        if (stimulation is not Stimulation.None)
        {
            _stimulations.Add(key, stimulation);
            // update the final stimulation, then do nothing else for now, still figuring that out.
            // (mainly due to the nature of just wanting an entirely different approach to stimulation but whatever.
            _finalStim = _stimulations.Values.Max();
        }
    }
}
