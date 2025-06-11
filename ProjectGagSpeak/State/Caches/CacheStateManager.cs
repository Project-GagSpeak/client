using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Util;
using OtterGui.Classes;

namespace GagSpeak.PlayerState.Visual;

/// <summary>
///     Manages the current collective, and final active state for all visual alterations.
///     Helper functions for appending, removing, and managing individual caches are included.
/// </summary>
/// <remarks> Helps with code readability, and optimal sorting of storage caches. </remarks>
public class CacheStateManager : DisposableMediatorSubscriberBase
{
    private readonly GlamourHandler _glamourCache;
    private readonly ModHandler _modsCache;
    private readonly MoodleHandler _moodlesCache;
    private readonly CPlusHandler _cplusCache;
    private readonly ArousalHandler _arousalHandler;
    private readonly TraitAllowanceManager _traitsManager;

    public CacheStateManager(ILogger<CacheStateManager> logger,
        GagspeakMediator mediator,
        GlamourHandler glamourCache,
        ModHandler modsCache,
        MoodleHandler moodlesCache,
        CPlusHandler cplusCache,
        ArousalHandler arousalHandler,
        TraitAllowanceManager traitsManager)
        : base(logger, mediator)
    {
        _glamourCache = glamourCache;
        _modsCache = modsCache;
        _moodlesCache = moodlesCache;
        _cplusCache = cplusCache;
        _arousalHandler = arousalHandler;
        _traitsManager = traitsManager;
    }

    /// <summary> Adds a <see cref="GarblerRestriction"/>'s visuals to the caches for a <paramref name="layerIdx"/>. </summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task AddGagToCache(GarblerRestriction gag, ActiveGagSlot serverData, int layerIdx)
    {
        Logger.LogInformation($"Adding {gag.GagType.GagName()} to cache at layer {layerIdx} with enabler {serverData.Enabler}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx);

        Logger.LogInformation("Running tasks in parallel for GarblerRestriction");
        var sw = Stopwatch.StartNew();

        await Task.WhenAll(
            SetAndApplyGlamour(combinedKey, gag.Glamour, new(gag.HeadgearState, gag.VisorState)),
            SetAndApplyMod(combinedKey, gag.Mod),
            SetAndApplyMoodle(combinedKey, gag.Moodle),
            SetAndApplyCPlusProfile(combinedKey, gag.CPlusProfile),
            SetAndApplyArousal(combinedKey, gag.Arousal)
        );

        sw.Stop();
        Logger.LogDebug($"[{combinedKey}]'s Visual Attributes added to caches in {sw.ElapsedMilliseconds}ms.");
    }

    /// <summary> Removes the visuals of a <see cref="GarblerRestriction"/> stored in the caches at a <paramref name="layerIdx"/></summary>
    /// <remarks> Changes are immidiately reflected and updated to the player. </remarks>
    public async Task RemoveGagFromCache(GarblerRestriction item, int layerIdx)
    {
        Logger.LogInformation($"Removing {item.GagType.GagName()} from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Gags, layerIdx);

        Logger.LogInformation("Running tasks in parallel for GarblerRestriction");
        var sw = Stopwatch.StartNew();
        
        await Task.WhenAll(
            RemoveGlamourWithKey(combinedKey),
            RemoveModWithKey(combinedKey),
            RemoveMoodleWithKey(combinedKey),
            RemoveCPlusProfileWithKey(combinedKey),
            RemoveArousalWithKey(combinedKey)
        );

        Logger.LogDebug($"[{combinedKey}] removed from cache and base states restored.");
    }

    public async Task AddRestrictionToCache(RestrictionItem item, ActiveRestriction serverData, int layerIdx)
    {
        Logger.LogInformation($"Adding Restriction [{item.Label}] to cache at layer {layerIdx} with enabler {serverData.Enabler}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx);

        Logger.LogInformation("Running tasks in parallel for RestrictionItem");
        var sw = Stopwatch.StartNew();
        
        var metaStruct = item switch
        {
            BlindfoldRestriction c => new MetaDataStruct(c.HeadgearState, c.VisorState, OptionalBool.Null),
            HypnoticRestriction h => new MetaDataStruct(h.HeadgearState, h.VisorState, OptionalBool.Null),
            _ => MetaDataStruct.Empty
        };
        await Task.WhenAll(
            SetAndApplyGlamour(combinedKey, item.Glamour, metaStruct),
            SetAndApplyMod(combinedKey, item.Mod),
            SetAndApplyMoodle(combinedKey, item.Moodle),
            SetAndApplyArousal(combinedKey, item.Arousal)
        );

        sw.Stop();
        Logger.LogDebug($"[{combinedKey}]'s Visual Attributes added to caches in {sw.ElapsedMilliseconds}ms.");
    }

    public async Task RemoveRestrictionFromCache(RestrictionItem item, int layerIdx)
    {
        Logger.LogInformation($"Removing Restriction [{item.Label}] from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restrictions, layerIdx);

        Logger.LogInformation("Running tasks in parallel for RestrictionItem");
        var sw = Stopwatch.StartNew();
        
        await Task.WhenAll(
            RemoveGlamourWithKey(combinedKey),
            RemoveModWithKey(combinedKey),
            RemoveMoodleWithKey(combinedKey),
            RemoveArousalWithKey(combinedKey)
        );

        Logger.LogDebug($"[{combinedKey}] removed from cache and base states restored.");
    }

    // This is going to be a whole other ballpark to deal with, so save for later, it will come with additional complexity.
    public async Task AddRestraintToCache(RestraintSet item, CharaActiveRestraint serverData, int layerIdx)
    {
        Logger.LogInformation($"Adding Restraint [{item.Label}] to cache at layer {layerIdx} with enabler {serverData.Enabler}");
        // -1 is the base, 0-4 are the layers. (could make it 0 to line things up but this makes it better in code)
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, layerIdx);

        Logger.LogInformation("Running tasks in parallel for RestraintSet");
        var sw = Stopwatch.StartNew();
        
        await Task.WhenAll(
            SetAndApplyGlamour(combinedKey, item.GetGlamour().Values, item.GetMetaData()),
            SetAndApplyMod(combinedKey, item.GetMods()),
            SetAndApplyMoodle(combinedKey, item.GetMoodles()),
            SetAndApplyArousal(combinedKey, item.Arousal)
        );

        sw.Stop();
        Logger.LogDebug($"[{combinedKey}]'s Visual Attributes added to caches in {sw.ElapsedMilliseconds}ms.");
    }

    public async Task RemoveRestraintFromCache(RestraintSet item, int layerIdx)
    {
        Logger.LogInformation($"Removing Restraint from cache at layer {layerIdx}");
        var combinedKey = new CombinedCacheKey(ManagerPriority.Restraints, -1);

        Logger.LogInformation("Running tasks in parallel for RestraintSet");
        var sw = Stopwatch.StartNew();

        await Task.WhenAll(
            RemoveGlamourWithKey(combinedKey),
            RemoveModWithKey(combinedKey),
            RemoveMoodleWithKey(combinedKey),
            RemoveArousalWithKey(combinedKey)
        );

        Logger.LogDebug($"[{combinedKey}] removed from cache and base states restored.");
    }


    #region Cache Update Helpers
    private async Task SetAndApplyGlamour(CombinedCacheKey key, GlamourSlot glamSlot, MetaDataStruct meta)
    {
        Logger.LogDebug("Updating Glamour & Meta Cache with new GlamourSlot.");
        var applyFlags = CacheType.None;
        if (_glamourCache.AddAndUpdateGlamour(key, glamSlot)) applyFlags |= CacheType.Glamour;
        if (_glamourCache.AddAndUpdateMeta(key, meta)) applyFlags |= CacheType.Meta;

        Logger.LogDebug("Applying Glamour Cache.");
        await _glamourCache.ApplySemaphore(applyFlags);
        Logger.LogDebug($"Glamour Cache Updated for key [{key}] with flags [{applyFlags}].");
    }

    private async Task SetAndApplyGlamour(CombinedCacheKey key, IEnumerable<GlamourSlot> glamSlots, MetaDataStruct meta)
    {
        Logger.LogDebug("Updating Glamour & Meta Cache with new GlamourSlots.");
        var applyFlags = CacheType.None;
        if (_glamourCache.AddAndUpdateGlamour(key, glamSlots)) applyFlags |= CacheType.Glamour;
        if (_glamourCache.AddAndUpdateMeta(key, meta)) applyFlags |= CacheType.Meta;

        Logger.LogDebug("Applying Glamour Cache.");
        await _glamourCache.ApplySemaphore(applyFlags);
        Logger.LogDebug($"Glamour Cache applied for key [{key}] with flags [{applyFlags}].");
    }

    private async Task RemoveGlamourWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Glamour & Meta Cache with Removal.");
        var applyFlags = CacheType.None;
        if (_glamourCache.RemoveAndUpdateGlamour(key, out var removedSlots)) applyFlags |= CacheType.Glamour;
        if (_glamourCache.RemoveAndUpdateMeta(key)) applyFlags |= CacheType.Meta;

        Logger.LogDebug("Restoring Removed Slots and Reapplying Glamour Cache.");
        await _glamourCache.RemoveSemaphore(applyFlags, removedSlots);
        Logger.LogDebug($"Glamour Cache Updated for key [{key}] with flags [{applyFlags}].");
    }

    private async Task SetAndApplyMod(CombinedCacheKey key, ModSettingsPreset preset)
    {
        Logger.LogDebug("Updating Mod Cache with new Mod.");
        _modsCache.AddAndUpdateMod(key, preset);

        Logger.LogDebug("Applying Mod Cache.");
        await _modsCache.ApplyModCache();
        Logger.LogDebug($"Mod Cache applied for key [{key}].");
    }

    private async Task SetAndApplyMod(CombinedCacheKey key, IEnumerable<ModSettingsPreset> presets)
    {
        Logger.LogDebug("Updating Mod Cache with new Mods.");
        _modsCache.AddAndUpdateMod(key, presets);

        Logger.LogDebug("Applying Mod Cache.");
        await _modsCache.ApplyModCache();
        Logger.LogDebug($"Mod Cache applied for key [{key}].");
    }

    private async Task RemoveModWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Mod Cache with Removal.");
        _modsCache.RemoveAndUpdateMod(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying Mod Cache.");
        await _modsCache.RestoreAndReapplyCache(removed);
        Logger.LogDebug($"Mod Cache removed all entries for [{key}] and reapplied Cache.");
    }

    private async Task SetAndApplyMoodle(CombinedCacheKey key, Moodle moodle)
    {
        Logger.LogDebug("Updating Moodle Cache with new Moodle.");
        _moodlesCache.AddAndUpdateMoodle(key, moodle);

        Logger.LogDebug("Applying Moodles Cache.");
        await _modsCache.ApplyModCache();
        Logger.LogDebug($"Moodles Cache applied for key [{key}].");
    }

    private async Task SetAndApplyMoodle(CombinedCacheKey key, IEnumerable<Moodle> moodles)
    {
        Logger.LogDebug("Updating Moodle Cache with new Moodles.");
        _moodlesCache.AddAndUpdateMoodle(key, moodles);

        Logger.LogDebug("Applying Moodles Cache.");
        await _modsCache.ApplyModCache();
        Logger.LogDebug($"Moodles Cache applied for key [{key}].");
    }

    private async Task RemoveMoodleWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Moodle Cache with a Removal.");
        _moodlesCache.RemoveAndUpdateMoodle(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying Moodle Cache.");
        await _moodlesCache.RestoreAndReapplyCache(removed);
        Logger.LogDebug($"Moodle Cache removed entries for [{key}] and reapplied Cache.");
    }

    private async Task SetAndApplyCPlusProfile(CombinedCacheKey key, CustomizeProfile profile)
    {
        Logger.LogDebug("Calculating Caches in parallel.");
        _cplusCache.AddAndUpdateprofile(key, profile);

        Logger.LogDebug("Applying CPlusProfile Cache.");
        await _modsCache.ApplyModCache();
        Logger.LogDebug($"CPlusProfile Cache applied for key [{key}].");
    }

    private async Task RemoveCPlusProfileWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating CPlusProfile Cache with a Removal.");
        _cplusCache.RemoveAndUpdateprofile(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying CPlusProfile Cache.");
        await _cplusCache.ApplyProfileCache();
        Logger.LogDebug($"CPlusProfile Cache removed entries for [{key}] and reapplied Cache.");
    }

    private Task SetAndApplyArousal(CombinedCacheKey key, Arousal strength)
    {
        Logger.LogDebug("Appending and updating Arousal Cache!");
        _arousalHandler.AddAndUpdateArousal(key, strength);
        Logger.LogDebug($"Arousal strength applied key [{key}] to the Cache!");
        return Task.CompletedTask;
    }

    private Task RemoveArousalWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Removing Arousal State!");
        _arousalHandler.RemoveAndUpdateArousal(key);
        Logger.LogDebug($"Arousal Cache removed entry [{key}], and reapplied Cache.");
        return Task.CompletedTask;
    }

    #endregion Cache Update Helpers
}
