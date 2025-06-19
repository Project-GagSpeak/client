using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using OtterGui.Classes;

namespace GagSpeak.State.Managers;

/// <summary>
///     Manages the current collective, and final active state for all visual alterations.
///     Helper functions for appending, removing, and managing individual caches are included.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public class CacheStateManager : DisposableMediatorSubscriberBase
{
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CustomizePlusHandler _cplusHandler;
    private readonly GlamourHandler _glamourHandler;
    private readonly ModHandler _modHandler;
    private readonly MoodleHandler _moodleHandler;
    private readonly TraitsHandler _traitsHandler;
    private readonly ArousalService _arousalHandler;

    public CacheStateManager(ILogger<CacheStateManager> logger,
        GagspeakMediator mediator,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CustomizePlusHandler profiles,
        GlamourHandler glamours,
        ModHandler mods,
        MoodleHandler moodles,
        TraitsHandler traits,
        ArousalService arousals) 
        : base(logger, mediator)
    {
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cplusHandler = profiles;
        _glamourHandler = glamours;
        _modHandler = mods;
        _moodleHandler = moodles;
        _traitsHandler = traits;
        _arousalHandler = arousals;

        // Only clear on logout, not disconnect, we want to keep people helpless~
        Mediator.Subscribe<DalamudLogoutMessage>(this, _ => ClearCaches());
    }

    private async void ClearCaches()
    {
        Logger.LogInformation("------- Clearing all caches on logout -------");
        await Task.WhenAll(
            _glamourHandler.ClearCache(),
            _modHandler.ClearCache(),
            _moodleHandler.ClearCache(),
            _cplusHandler.ClearCache(),
            _traitsHandler.ClearCache(),
            _arousalHandler.ClearArousals()
        );
        Logger.LogInformation("------- All caches cleared -------");
    }

    // Keep in mind that while this looks heavy, everything uses .TryAdd, meaning duplicates will not be reapplied.
    public async Task SyncWithServerData(ConnectionResponse connectionDto)
    {
        Logger.LogWarning("Syncing Server Data to Active Items & Visuals");
        var sw = Stopwatch.StartNew();
        // Sync all server gag data with the GagRestrictionManager.
        _gags.LoadServerData(connectionDto.SyncedGagData);
        Logger.LogInformation("------ Syncing Gag Data to Cache ------");
        foreach (var (layer, gagItem) in _gags.ActiveItems)
        {
            Logger.LogDebug($"Adding ({gagItem.GagType.GagName()}) at layer {layer}, which " +
                $"was enabled by {_gags.ServerGagData!.GagSlots[layer].Enabler}.");
            var key = new CombinedCacheKey(ManagerPriority.Gags, layer, gagItem.GagType.GagName());
            _glamourHandler.TryAddGlamourToCache(key, gagItem.Glamour);
            _glamourHandler.TryAddMetaToCache(key, new(gagItem.HeadgearState, gagItem.VisorState));
            _modHandler.TryAddModToCache(key, gagItem.Mod);
            _moodleHandler.TryAddMoodleToCache(key, gagItem.Moodle);
            _traitsHandler.TryAddTraitsToCache(key, gagItem.Traits);
            _cplusHandler.TryAddToCache(key, gagItem.CPlusProfile);
            _arousalHandler.TryAddArousalToCache(key, gagItem.Arousal);
        }
        Logger.LogInformation("------ Gag Data synced to Cache ------ ");

        // Sync all server restriction data with the RestrictionManager.
        _restrictions.LoadServerData(connectionDto.SyncedRestrictionsData);
        Logger.LogInformation("------ Syncing Restriction Data to Cache ------");
        foreach (var (layer, item) in _restrictions.ActiveItems)
        {
            Logger.LogDebug($"Adding Restriction [{item.Label}] at layer {layer}, which " +
                $"was enabled by {_restrictions.ServerRestrictionData!.Restrictions[layer].Enabler}.");
            var key = new CombinedCacheKey(ManagerPriority.Restrictions, layer, item.Label);
            var metaStruct = item switch
            {
                BlindfoldRestriction c => new MetaDataStruct(c.HeadgearState, c.VisorState),
                HypnoticRestriction h => new MetaDataStruct(h.HeadgearState, h.VisorState),
                _ => MetaDataStruct.Empty
            };
            _glamourHandler.TryAddGlamourToCache(key, item.Glamour);
            _glamourHandler.TryAddMetaToCache(key, metaStruct);
            _modHandler.TryAddModToCache(key, item.Mod);
            _moodleHandler.TryAddMoodleToCache(key, item.Moodle);
            _arousalHandler.TryAddArousalToCache(key, item.Arousal);
        }
        Logger.LogInformation("------ Restriction Data synced to Cache ------ ");

        // Sync all server restraint data with the RestraintManager.
        _restraints.LoadServerData(connectionDto.SyncedRestraintSetData);
        Logger.LogInformation("------ Syncing Restraint Data to Cache ------");
        if(_restraints.AppliedRestraint is { } restraintSet)
        {
            Logger.LogDebug($"Adding RestraintSet [{restraintSet.Label}), which was enabled by {_restraints.ServerRestraintData?.Enabler}.");
            var key = new CombinedCacheKey(ManagerPriority.Gags, -1, restraintSet.Label);
            _glamourHandler.TryAddGlamourToCache(key, restraintSet.GetGlamour().Values);
            _glamourHandler.TryAddMetaToCache(key, restraintSet.GetMetaData());
            _modHandler.TryAddModToCache(key, restraintSet.GetMods());
            _moodleHandler.TryAddMoodleToCache(key, restraintSet.GetMoodles());
            _traitsHandler.TryAddTraitsToCache(key, restraintSet.GetTraits());
            _arousalHandler.TryAddArousalToCache(key, restraintSet.Arousal);
        }
        Logger.LogInformation("------ Restraint Data synced to Cache ------ ");

        // Now perform all updates in parallel.
        Logger.LogInformation("------ Applying all Cache Updates In Parallel ------");
        await Task.WhenAll(
            _glamourHandler.UpdateCaches(true), // this should maybe probably be false incase we trigger this in a bound state? Idk.
            _modHandler.UpdateModCache(),
            _moodleHandler.UpdateMoodleCache(),
            _cplusHandler.UpdateProfileCache(),
            _traitsHandler.UpdateTraitCache(),
            _arousalHandler.UpdateFinalCache()
        );
        sw.Stop();
        Logger.LogWarning($"------ All Updates & Visuals Applied in {sw.ElapsedMilliseconds}ms ------");
    }

    /// <summary> Adds a GagItem's visual properties to the cache at the defined layer. </summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task AddGagItem(GarblerRestriction item, int layerIdx, string enabler)
    {
        Logger.LogDebug($"Adding GagItem ({item.GagType.GagName()}) at layer {layerIdx}, which was enabled by {enabler}.");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx, item.GagType.GagName());
        // perform the updates in parallel.
        await TimedWhenAll($"[{combinedKey}]'s Visual Attributes added to caches",
            AddGlamourMeta(combinedKey, item.Glamour, new(item.HeadgearState, item.VisorState)),
            AddModPreset(combinedKey, item.Mod),
            AddMoodle(combinedKey, item.Moodle),
            AddProfile(combinedKey, item.CPlusProfile),
            AddTraits(combinedKey, item.Traits),
            AddArousalStrength(combinedKey, item.Arousal)
        );
    }

    /// <summary> Removes the visuals of a <see cref="GarblerRestriction"/> stored in the caches at a <paramref name="layerIdx"/></summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task RemoveGagItem(GarblerRestriction item, int layerIdx)
    {
        Logger.LogDebug($"Removing {item.GagType.GagName()} from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx, item.GagType.GagName());
        // Remove and update in parallel.
        await TimedWhenAll($"[{combinedKey}] removed from cache and base states restored",
            RemoveGlamourMeta(combinedKey),
            RemoveModPreset(combinedKey),
            RemoveMoodle(combinedKey),
            RemoveProfile(combinedKey),
            RemoveTraits(combinedKey),
            RemoveArousalStrength(combinedKey)
        );
    }

    public async Task AddRestrictionItem(RestrictionItem item, int layerIdx, string enabler)
    {
        Logger.LogInformation($"Adding Restriction ({item.Label}) at layer {layerIdx}, which was enabled by {enabler}.");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx, item.Label);        
        var metaStruct = item switch
        {
            BlindfoldRestriction c => new MetaDataStruct(c.HeadgearState, c.VisorState),
            HypnoticRestriction h => new MetaDataStruct(h.HeadgearState, h.VisorState),
            _ => MetaDataStruct.Empty
        };
        await TimedWhenAll("[{combinedKey}]'s Visual Attributes added to caches",
            AddGlamourMeta(combinedKey, item.Glamour, metaStruct),
            AddModPreset(combinedKey, item.Mod),
            AddMoodle(combinedKey, item.Moodle),
            AddTraits(combinedKey, item.Traits),
            AddArousalStrength(combinedKey, item.Arousal)
        );
    }

    public async Task RemoveRestrictionItem(RestrictionItem item, int layerIdx)
    {
        Logger.LogInformation($"Removing Restriction [{item.Label}] from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx, item.Label);
        // Remove and update in parallel.
        await TimedWhenAll($"[{combinedKey}] removed from cache and base states restored",
            RemoveGlamourMeta(combinedKey),
            RemoveModPreset(combinedKey),
            RemoveMoodle(combinedKey),
            RemoveTraits(combinedKey),
            RemoveArousalStrength(combinedKey)
        );
    }

    // This is going to be a whole other ballpark to deal with, so save for later, it will come with additional complexity.
    public async Task AddRestraintSet(RestraintSet item, int layerIdx, string enabler)
    {
        Logger.LogInformation($"Adding RestraintSet ({item.Label}), which was enabled by {enabler}.");
        // -1 is the base, 0-4 are the layers. (could make it 0 to line things up but this makes it better in code)
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, layerIdx, item.Label);
        // Add and update in parallel.
        await TimedWhenAll($"[{combinedKey}]'s Visual Attributes added to caches",
            AddGlamourMeta(combinedKey, item.GetGlamour().Values, item.GetMetaData()),
            AddModPreset(combinedKey, item.GetMods()),
            AddMoodle(combinedKey, item.GetMoodles()),
            AddTraits(combinedKey, item.GetTraits()),
            AddArousalStrength(combinedKey, item.Arousal)
        );
    }

    // the layerIdx shouldnt even be here really if we are calling the layers seperately? idk.
    public async Task RemoveRestraintSet(RestraintSet item, int layerIdx)
    {
        Logger.LogInformation($"Removing RestraintSet [{item.Label}] from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, -1, item.Label);
        // Remove and update in parallel.
        await TimedWhenAll($"[{combinedKey}] removed from cache and base states restored",
            RemoveGlamourMeta(combinedKey),
            RemoveModPreset(combinedKey),
            RemoveMoodle(combinedKey),
            RemoveTraits(combinedKey),
            RemoveArousalStrength(combinedKey)
        );
    }

    private async Task TimedWhenAll(string label, params Task[] tasks)
    {
        var sw = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        sw.Stop();
        Logger.LogDebug($"{label} in {sw.ElapsedMilliseconds}ms.");
    }


    #region Cache Update Helpers
    private async Task AddGlamourMeta(CombinedCacheKey key, GlamourSlot glamSlot, MetaDataStruct meta)
    {
        _glamourHandler.TryAddGlamourToCache(key, glamSlot);
        _glamourHandler.TryAddMetaToCache(key, meta);
        await _glamourHandler.UpdateCaches(false);
    }

    private async Task AddGlamourMeta(CombinedCacheKey key, IEnumerable<GlamourSlot> glamSlots, MetaDataStruct meta)
    {
        _glamourHandler.TryAddGlamourToCache(key, glamSlots);
        _glamourHandler.TryAddMetaToCache(key, meta);
        await _glamourHandler.UpdateCaches(false);
    }

    private async Task RemoveGlamourMeta(CombinedCacheKey key)
    {
        _glamourHandler.TryRemGlamourFromCache(key);
        _glamourHandler.TryRemMetaFromCache(key);
        await _glamourHandler.UpdateCaches(false);
    }

    private async Task AddModPreset(CombinedCacheKey key, ModSettingsPreset preset)
    {
        _modHandler.TryAddModToCache(key, preset);
        await _modHandler.UpdateModCache();
    }

    private async Task AddModPreset(CombinedCacheKey key, IEnumerable<ModSettingsPreset> presets)
    {
        _modHandler.TryAddModToCache(key, presets);
        await _modHandler.UpdateModCache();
    }

    private async Task RemoveModPreset(CombinedCacheKey key)
    {
        _modHandler.TryRemModFromCache(key);
        await _modHandler.UpdateModCache();
    }

    private async Task AddMoodle(CombinedCacheKey key, Moodle moodle)
    {
        _moodleHandler.TryAddMoodleToCache(key, moodle);
        await _moodleHandler.UpdateMoodleCache();
    }

    private async Task AddMoodle(CombinedCacheKey key, IEnumerable<Moodle> moodles)
    {
        _moodleHandler.TryAddMoodleToCache(key, moodles);
        await _moodleHandler.UpdateMoodleCache();
    }

    private async Task RemoveMoodle(CombinedCacheKey key)
    {
        _moodleHandler.TryRemMoodleFromCache(key);
        await _moodleHandler.UpdateMoodleCache();
    }

    private async Task AddProfile(CombinedCacheKey key, CustomizeProfile profile)
    {
        _cplusHandler.TryAddToCache(key, profile);
        await _cplusHandler.UpdateProfileCache();
    }

    private async Task RemoveProfile(CombinedCacheKey key)
    {
        _cplusHandler.TryRemoveFromCache(key);
        await _cplusHandler.UpdateProfileCache();
    }

    private async Task AddTraits(CombinedCacheKey key, Traits traits)
    {
        _traitsHandler.TryAddTraitsToCache(key, traits);
        await _traitsHandler.UpdateTraitCache();
    }

    private async Task RemoveTraits(CombinedCacheKey key)
    {
        _traitsHandler.TryRemTraitsFromCache(key);
        await _traitsHandler.UpdateTraitCache();
    }

    private async Task AddArousalStrength(CombinedCacheKey key, Arousal strength)
    {
        _arousalHandler.TryAddArousalToCache(key, strength);
        await _arousalHandler.UpdateFinalCache();
    }

    private Task RemoveArousalStrength(CombinedCacheKey key)
    {
        _arousalHandler.TryRemArousalFromCache(key);
        return _arousalHandler.UpdateFinalCache();
    }
    #endregion Cache Update Helpers
}
