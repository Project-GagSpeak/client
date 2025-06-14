using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Struct;
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
    private readonly CustomizePlusCache _cplusCache;
    private readonly CustomizePlusHandler _cplusHandler;
    private readonly GlamourCache _glamourCache;
    private readonly GlamourHandler _glamourHandler;
    private readonly ModCache _modsCache;
    private readonly ModHandler _modHandler;
    private readonly MoodleCache _moodlesCache;
    private readonly MoodleHandler _moodleHandler;
    private readonly TraitsCache _traitsCache;
    private readonly TraitsHandler _traitsHandler;
    private readonly ArousalService _arousalHandler;

    public CacheStateManager(ILogger<CacheStateManager> logger,
        GagspeakMediator mediator,
        CustomizePlusCache cplusCache,
        CustomizePlusHandler cplusHandler,
        GlamourCache glamourCache,
        GlamourHandler glamourHandler,
        ModCache modsCache,
        ModHandler modHandler,
        MoodleCache moodlesCache,
        MoodleHandler moodleHandler,
        TraitsCache traitsCache,
        TraitsHandler traitsHandler,
        ArousalService arousalHandler) 
        : base(logger, mediator)
    {
        _cplusCache = cplusCache;
        _cplusHandler = cplusHandler;
        _glamourCache = glamourCache;
        _glamourHandler = glamourHandler;
        _modsCache = modsCache;
        _modHandler = modHandler;
        _moodlesCache = moodlesCache;
        _moodleHandler = moodleHandler;
        _traitsCache = traitsCache;
        _traitsHandler = traitsHandler;
        _arousalHandler = arousalHandler;

        // Subscribe to the logout message to clear the caches.
        // Mediator.Subscribe<DalamudLogoutMessage>(this, _ => ClearCaches());
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
        bool applyGlam = _glamourCache.AddAndUpdateGlamour(key, glamSlot);
        bool applyMeta = _glamourCache.AddAndUpdateMeta(key, meta);

        Logger.LogDebug("Applying Glamour Cache.");
        await _glamourHandler.ApplySemaphore(applyGlam, applyMeta, false);
        Logger.LogDebug($"Glamour Cache Updated for key [{key}]!");
    }

    private async Task SetAndApplyGlamour(CombinedCacheKey key, IEnumerable<GlamourSlot> glamSlots, MetaDataStruct meta)
    {
        Logger.LogDebug("Updating Glamour & Meta Cache with new GlamourSlots.");
        bool applyGlam = _glamourCache.AddAndUpdateGlamour(key, glamSlots);
        bool applyMeta = _glamourCache.AddAndUpdateMeta(key, meta);

        Logger.LogDebug("Applying Glamour Cache.");
        await _glamourHandler.ApplySemaphore(applyGlam, applyMeta, false);
        Logger.LogDebug($"Glamour Cache applied for key [{key}]!");
    }

    private async Task RemoveGlamourWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Glamour & Meta Cache with Removal.");
        bool remGlam = _glamourCache.RemoveAndUpdateGlamour(key, out var removedSlots);
        bool remMeta = _glamourCache.RemoveAndUpdateMeta(key);
        Logger.LogDebug("Restoring Removed Slots and Reapplying Glamour Cache.");
        await _glamourHandler.RemoveSemaphore(remGlam, remMeta, false, removedSlots);
        Logger.LogDebug($"Glamour Cache Updated for key [{key}]!");
    }

    private async Task SetAndApplyMod(CombinedCacheKey key, ModSettingsPreset preset)
    {
        Logger.LogDebug("Updating Mod Cache with new Mod.");
        _modsCache.AddAndUpdateMod(key, preset);

        Logger.LogDebug("Applying Mod Cache.");
        await _modHandler.ApplyModCache();
        Logger.LogDebug($"Mod Cache applied for key [{key}].");
    }

    private async Task SetAndApplyMod(CombinedCacheKey key, IEnumerable<ModSettingsPreset> presets)
    {
        Logger.LogDebug("Updating Mod Cache with new Mods.");
        _modsCache.AddAndUpdateMod(key, presets);

        Logger.LogDebug("Applying Mod Cache.");
        await _modHandler.ApplyModCache();
        Logger.LogDebug($"Mod Cache applied for key [{key}].");
    }

    private async Task RemoveModWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Mod Cache with Removal.");
        _modsCache.RemoveAndUpdateMod(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying Mod Cache.");
        await _modHandler.RestoreAndReapplyCache(removed);
        Logger.LogDebug($"Mod Cache removed all entries for [{key}] and reapplied Cache.");
    }

    private async Task SetAndApplyMoodle(CombinedCacheKey key, Moodle moodle)
    {
        Logger.LogDebug("Updating Moodle Cache with new Moodle.");
        _moodlesCache.AddAndUpdateMoodle(key, moodle);

        Logger.LogDebug("Applying Moodles Cache.");
        await _moodleHandler.ApplyMoodleCache();
        Logger.LogDebug($"Moodles Cache applied for key [{key}].");
    }

    private async Task SetAndApplyMoodle(CombinedCacheKey key, IEnumerable<Moodle> moodles)
    {
        Logger.LogDebug("Updating Moodle Cache with new Moodles.");
        _moodlesCache.AddAndUpdateMoodle(key, moodles);

        Logger.LogDebug("Applying Moodles Cache.");
        await _moodleHandler.ApplyMoodleCache();
        Logger.LogDebug($"Moodles Cache applied for key [{key}].");
    }

    private async Task RemoveMoodleWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Moodle Cache with a Removal.");
        _moodlesCache.RemoveAndUpdateMoodle(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying Moodle Cache.");
        await _moodleHandler.RestoreAndReapplyCache(removed);
        Logger.LogDebug($"Moodle Cache removed entries for [{key}] and reapplied Cache.");
    }

    private async Task SetAndApplyCPlusProfile(CombinedCacheKey key, CustomizeProfile profile)
    {
        Logger.LogDebug("Calculating Caches in parallel.");
        _cplusCache.AddAndUpdateprofile(key, profile);

        Logger.LogDebug("Applying CPlusProfile Cache.");
        await _cplusHandler.ApplyProfileCache();
        Logger.LogDebug($"CPlusProfile Cache applied for key [{key}].");
    }

    private async Task RemoveCPlusProfileWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating CPlusProfile Cache with a Removal.");
        _cplusCache.RemoveAndUpdateprofile(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying CPlusProfile Cache.");
        await _cplusHandler.ApplyProfileCache();
        Logger.LogDebug($"CPlusProfile Cache removed entries for [{key}] and reapplied Cache.");
    }

    private async Task SetAndApplyTraits(CombinedCacheKey key, Traits traits)
    {
        Logger.LogDebug("Calculating Caches in parallel.");
        _traitsCache.AddAndUpdatetraits(key, traits);

        Logger.LogDebug("Applying Traits Cache Changes.");
        // For now, dont do anything. We need to set this up later but im too eepy girl rn.
        // await _traitsHandler.ApplyProfileCache();
        Logger.LogDebug($"Traits Cache applied for key [{key}].");
    }

    private async Task RemoveTraitsWithKey(CombinedCacheKey key)
    {
        Logger.LogDebug("Updating Traits Cache with a Removal.");
        _cplusCache.RemoveAndUpdateprofile(key, out var removed);

        Logger.LogDebug("Restoring And Reapplying Traits Cache.");
        // For now, dont do anything. We need to set this up later but im too eepy girl rn.
        // await _traitsHandler.ApplyProfileCache();
        Logger.LogDebug($"Traits Cache removed entries for [{key}] and reapplied Cache.");
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
