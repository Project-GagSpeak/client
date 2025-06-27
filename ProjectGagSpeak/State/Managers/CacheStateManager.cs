using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.State.Managers;

/// <summary>
///     Manages the current collective, and final active state for all visual alterations.
///     Helper functions for appending, removing, and managing individual caches are included.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public class CacheStateManager : IHostedService
{
    private readonly ILogger<CacheStateManager> _logger;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CustomizePlusHandler _cplusHandler;
    private readonly GlamourHandler _glamourHandler;
    private readonly ModHandler _modHandler;
    private readonly MoodleHandler _moodleHandler;
    private readonly TraitsHandler _traitsHandler;
    private readonly OverlayHandler _overlayHandler;
    private readonly ArousalService _arousalHandler;

    public CacheStateManager(ILogger<CacheStateManager> logger, GagRestrictionManager gags,
        RestrictionManager restrictions, RestraintManager restraints, CustomizePlusHandler profiles, 
        GlamourHandler glamours, ModHandler mods, MoodleHandler moodles, TraitsHandler traits,
        OverlayHandler overlays, ArousalService arousals) 
    {
        _logger = logger;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cplusHandler = profiles;
        _glamourHandler = glamours;
        _modHandler = mods;
        _moodleHandler = moodles;
        _traitsHandler = traits;
        _overlayHandler = overlays;
        _arousalHandler = arousals;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheStateManager started, listening for logout events.");
        Svc.ClientState.Logout += (_, _) => ClearCaches();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("CacheStateManager stopping, clearing caches.");
        Svc.ClientState.Logout -= (_, _) => ClearCaches();
        ClearCaches();
        return Task.CompletedTask;
    }

    private async void ClearCaches()
    {
        _logger.LogInformation("------- Clearing all caches on logout -------");
        await Task.WhenAll(
            _glamourHandler.ClearCache(),
            _modHandler.ClearCache(),
            _moodleHandler.ClearCache(),
            _cplusHandler.ClearCache(),
            _traitsHandler.ClearCache(),
            _overlayHandler.ClearCache(),
            _arousalHandler.ClearArousals()
        );
        _logger.LogInformation("------- All caches cleared -------");
    }

    // Keep in mind that while this looks heavy, everything uses .TryAdd, meaning duplicates will not be reapplied.
    public async Task SyncWithServerData(ConnectionResponse connectionDto)
    {
        _logger.LogWarning("Syncing Server Data to Active Items & Visuals");
        var sw = Stopwatch.StartNew();
        // Sync all server gag data with the GagRestrictionManager.
        _gags.LoadServerData(connectionDto.SyncedGagData);
        _logger.LogInformation("------ Syncing Gag Data to Cache ------");
        foreach (var (layer, gagItem) in _gags.ActiveItems)
        {
            var serverItem = _gags.ServerGagData!.GagSlots[layer];
            _logger.LogDebug($"Adding ({gagItem.GagType.GagName()}) at layer {layer}, which was enabled by {serverItem.Enabler}.");
            
            var key = new CombinedCacheKey(ManagerPriority.Gags, layer, serverItem.Enabler, gagItem.GagType.GagName());
            _glamourHandler.TryAddGlamourToCache(key, gagItem.Glamour);
            _glamourHandler.TryAddMetaToCache(key, new(gagItem.HeadgearState, gagItem.VisorState));
            _modHandler.TryAddModToCache(key, gagItem.Mod);
            _moodleHandler.TryAddMoodleToCache(key, gagItem.Moodle);
            _traitsHandler.TryAddTraitsToCache(key, gagItem.Traits);
            _cplusHandler.TryAddToCache(key, gagItem.CPlusProfile);
            _arousalHandler.TryAddArousalToCache(key, gagItem.Arousal);
        }
        _logger.LogInformation("------ Gag Data synced to Cache ------ ");

        // Sync all server restriction data with the RestrictionManager.
        _restrictions.LoadServerData(connectionDto.SyncedRestrictionsData);
        _logger.LogInformation("------ Syncing Restriction Data to Cache ------");
        foreach (var (layer, item) in _restrictions.ActiveItems)
        {
            var serverItem = _restrictions.ServerRestrictionData!.Restrictions[layer];
            _logger.LogDebug($"Adding Restriction [{item.Label}] at layer {layer}, which was enabled by {serverItem.Enabler}.");
            
            var key = new CombinedCacheKey(ManagerPriority.Restrictions, layer, serverItem.Enabler, item.Label);
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
            _traitsHandler.TryAddTraitsToCache(key, item.Traits);
            _arousalHandler.TryAddArousalToCache(key, item.Arousal);
            // Conditional Additions.
            if (item is BlindfoldRestriction bfr) _overlayHandler.TryAddBlindfoldToCache(key, bfr.Properties);
            if (item is HypnoticRestriction hr) _overlayHandler.TryAddEffectToCache(key, hr.Properties);
        }
        _logger.LogInformation("------ Restriction Data synced to Cache ------ ");


        // Sync all server restraint data with the RestraintManager.
        _restraints.LoadServerData(connectionDto.SyncedRestraintSetData);
        _logger.LogInformation("------ Syncing Restraint Data to Cache ------");
        if(_restraints.AppliedRestraint is { } restraintSet)
        {
            var serverItem = _restraints.ServerRestraintData!;
            _logger.LogDebug($"Adding RestraintSet [{restraintSet.Label}), which was enabled by {serverItem.Enabler}.");
            
            var key = new CombinedCacheKey(ManagerPriority.Restraints, -1, serverItem.Enabler, restraintSet.Label);
            _glamourHandler.TryAddGlamourToCache(key, restraintSet.GetGlamour().Values);
            _glamourHandler.TryAddMetaToCache(key, restraintSet.GetMetaData());
            _modHandler.TryAddModToCache(key, restraintSet.GetMods());
            _moodleHandler.TryAddMoodleToCache(key, restraintSet.GetMoodles());
            _traitsHandler.TryAddTraitsToCache(key, restraintSet.GetTraits());
            _arousalHandler.TryAddArousalToCache(key, restraintSet.Arousal);
            // Conditional Additions.
            if (restraintSet.GetBlindfoldOverlay() is { } bf) _overlayHandler.TryAddBlindfoldToCache(key, bf);
            if (restraintSet.GetHypnoOverlay() is { } h) _overlayHandler.TryAddEffectToCache(key, h);

        }
        _logger.LogInformation("------ Restraint Data synced to Cache ------ ");


        // Now perform all updates in parallel.
        _logger.LogInformation("------ Applying all Cache Updates In Parallel ------");
        await Task.WhenAll(
            _glamourHandler.UpdateCaches(),
            _modHandler.UpdateModCache(),
            _moodleHandler.UpdateMoodleCache(),
            _cplusHandler.UpdateProfileCache(),
            _traitsHandler.UpdateTraitCache(),
            _arousalHandler.UpdateFinalCache(),
            _overlayHandler.UpdateCaches()
        );
        sw.Stop();
        _logger.LogInformation($"------ All Updates & Visuals Applied in {sw.ElapsedMilliseconds}ms ------");
    }

    /// <summary> Adds a GagItem's visual properties to the cache at the defined layer. </summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task AddGagItem(GarblerRestriction item, int layerIdx, string enabler)
    {
        _logger.LogDebug($"Adding ({item.GagType.GagName()}) at layer {layerIdx}, enabled by ({enabler}).");
        var key = new CombinedCacheKey(ManagerPriority.Gags, layerIdx, enabler, item.GagType.GagName());
        // perform the updates in parallel.
        await TimedWhenAll($"[{key}]'s Visual Attributes added to caches",
            AddGlamourMeta(key, item.Glamour, new(item.HeadgearState, item.VisorState)),
            AddModPreset(key, item.Mod),
            AddMoodle(key, item.Moodle),
            AddProfile(key, item.CPlusProfile),
            AddTraits(key, item.Traits),
            AddArousalStrength(key, item.Arousal)
        );
    }

    /// <summary> Removes the visuals of a <see cref="GarblerRestriction"/> stored in the caches at a <paramref name="layerIdx"/></summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task RemoveGagItem(GarblerRestriction item, int layerIdx)
    {
        _logger.LogDebug($"Removing ({item.GagType.GagName()}) from cache at layer {layerIdx}");
        var key = new CombinedCacheKey(ManagerPriority.Gags, layerIdx, string.Empty, item.GagType.GagName());
        // Remove and update in parallel.
        await TimedWhenAll($"[{key}] removed from cache and base states restored",
            RemoveGlamourMeta(key),
            RemoveModPreset(key),
            RemoveMoodle(key),
            RemoveProfile(key),
            RemoveTraits(key),
            RemoveArousalStrength(key)
        );
    }

    public async Task AddRestrictionItem(RestrictionItem item, int layerIdx, string enabler)
    {
        _logger.LogDebug($"Adding Restriction ({item.Label}) at layer {layerIdx}, enabled by ({enabler}).");
        var key = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx, enabler, item.Label);        
        var metaStruct = item switch
        {
            BlindfoldRestriction c => new MetaDataStruct(c.HeadgearState, c.VisorState),
            HypnoticRestriction h => new MetaDataStruct(h.HeadgearState, h.VisorState),
            _ => MetaDataStruct.Empty
        };
        var tasks = new List<Task>
        {
            AddGlamourMeta(key, item.Glamour, metaStruct),
            AddModPreset(key, item.Mod),
            AddMoodle(key, item.Moodle),
            AddTraits(key, item.Traits),
            AddArousalStrength(key, item.Arousal),
        };
        // Conditional additions
        if (item is BlindfoldRestriction bfr) tasks.Add(AddBlindfold(key, bfr.Properties));
        if (item is HypnoticRestriction hr) tasks.Add(AddHypnoEffect(key, hr.Properties));

        // Run in parallel.
        await TimedWhenAll($"[{key}]'s Visual Attributes added to caches", tasks);
    }

    public async Task RemoveRestrictionItem(RestrictionItem item, int layerIdx)
    {
        _logger.LogInformation($"Removing Restriction [{item.Label}] from cache at layer {layerIdx}");
        var key = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx, string.Empty, item.Label);
        // Remove and update in parallel.
        await TimedWhenAll($"[{key}] removed from cache and base states restored",
            RemoveGlamourMeta(key),
            RemoveModPreset(key),
            RemoveMoodle(key),
            RemoveTraits(key),
            RemoveArousalStrength(key),
            // Worth tryiung even if they dont exist, just to be safe.
            RemoveBlindfold(key),
            RemoveHypnoEffect(key)
        );
    }

    // This is going to be a whole other ballpark to deal with, so save for later, it will come with additional complexity.
    public async Task AddRestraintSet(RestraintSet item, int layerIdx, string enabler)
    {
        _logger.LogInformation($"Adding RestraintSet ({item.Label}), which was enabled by {enabler}.");
        
        // -1 is the base, 0-4 are the layers. (could make it 0 to line things up but this makes it better in code)
        var key = new CombinedCacheKey(ManagerPriority.Restraints, layerIdx, enabler, item.Label);
        var tasks = new List<Task>
        {
            AddGlamourMeta(key, item.GetGlamour().Values, item.GetMetaData()),
            AddModPreset(key, item.GetMods()),
            AddMoodle(key, item.GetMoodles()),
            AddTraits(key, item.GetTraits()),
            AddArousalStrength(key, item.Arousal),
        };
        // Conditional additions
        if (item.GetBlindfoldOverlay() is { } bf) tasks.Add(AddBlindfold(key, bf));
        if (item.GetHypnoOverlay() is { } h) tasks.Add(AddHypnoEffect(key, h));

        // Add and update in parallel.
        await TimedWhenAll($"[{key}]'s Visual Attributes added to caches", tasks);
    }

    // the layerIdx shouldnt even be here really if we are calling the layers seperately? idk.
    public async Task RemoveRestraintSet(RestraintSet item, int layerIdx)
    {
        _logger.LogInformation($"Removing RestraintSet [{item.Label}] from cache at layer {layerIdx}");

        // -1 is the base, 0-4 are the layers. (could make it 0 to line things up but this makes it better in code)
        var key = new CombinedCacheKey(ManagerPriority.Restraints, -1, string.Empty, item.Label);
        // Remove and update in parallel.
        await TimedWhenAll($"[{key}] removed from cache and base states restored",
            RemoveGlamourMeta(key),
            RemoveModPreset(key),
            RemoveMoodle(key),
            RemoveTraits(key),
            RemoveArousalStrength(key),
            // Worth trying even if they dont exist, just to be safe.
            RemoveBlindfold(key),
            RemoveHypnoEffect(key)
        );
    }

    private async Task TimedWhenAll(string label, IEnumerable<Task> tasks)
    {
        var sw = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        sw.Stop();
        _logger.LogDebug($"{label} in {sw.ElapsedMilliseconds}ms.");
    }

    private async Task TimedWhenAll(string label, params Task[] tasks)
    {
        var sw = Stopwatch.StartNew();
        await Task.WhenAll(tasks);
        sw.Stop();
        _logger.LogDebug($"{label} in {sw.ElapsedMilliseconds}ms.");
    }


    #region Cache Update Helpers
    private async Task AddGlamourMeta(CombinedCacheKey key, GlamourSlot glamSlot, MetaDataStruct meta)
    {
        _glamourHandler.TryAddGlamourToCache(key, glamSlot);
        _glamourHandler.TryAddMetaToCache(key, meta);
        await _glamourHandler.UpdateCaches();
    }

    private async Task AddGlamourMeta(CombinedCacheKey key, IEnumerable<GlamourSlot> glamSlots, MetaDataStruct meta)
    {
        _glamourHandler.TryAddGlamourToCache(key, glamSlots);
        _glamourHandler.TryAddMetaToCache(key, meta);
        await _glamourHandler.UpdateCaches();
    }

    private async Task RemoveGlamourMeta(CombinedCacheKey key)
    {
        _glamourHandler.TryRemGlamourFromCache(key);
        _glamourHandler.TryRemMetaFromCache(key);
        await _glamourHandler.UpdateCaches();
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

    private async Task AddBlindfold(CombinedCacheKey key, BlindfoldOverlay? overlay)
    {
        if (overlay is null)
            return;

        _overlayHandler.TryAddBlindfoldToCache(key, overlay);
        await _overlayHandler.UpdateBlindfoldCacheSlim();
    }

    private async Task RemoveBlindfold(CombinedCacheKey key)
    {
        _overlayHandler.TryRemBlindfoldFromCache(key);
        await _overlayHandler.UpdateBlindfoldCacheSlim();
    }

    private async Task AddHypnoEffect(CombinedCacheKey key, HypnoticOverlay? overlay)
    {
        if (overlay is null)
            return;

        _overlayHandler.TryAddEffectToCache(key, overlay);
        await _overlayHandler.UpdateHypnoEffectCacheSlim();
    }

    private async Task RemoveHypnoEffect(CombinedCacheKey key)
    {
        _overlayHandler.TryRemEffectFromCache(key);
        await _overlayHandler.UpdateHypnoEffectCacheSlim();
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
