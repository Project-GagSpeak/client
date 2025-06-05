using GagSpeak.PlayerData.Storage;

namespace GagSpeak.PlayerState.Visual;

// Handles all Glamour Caching and applier interactions.
public class ModCacheManager
{
    private readonly ILogger<ModCacheManager> _logger;
    private readonly VisualApplierPenumbra _handler;

    // This is really difficult for me, because the key is for the most part out of my control.
    // If at any point the mod name is changed in penumbra, it would damage the storage in gagspeak...
    // So unless I could resync the cache on every mod name change, this would be difficult to pull off.
    private SortedList<(CombinedCacheKey, string), ModSettingsPreset> _mods = new();
    private HashSet<ModSettingsPreset> _finalMods = new();

    public ModCacheManager(ILogger<ModCacheManager> logger, VisualApplierPenumbra handler)
    {
        _logger = logger;
        _handler = handler;
    }

    public HashSet<ModSettingsPreset> FinalRef => _finalMods;

    /// <summary>
    ///     Adds multiple ModSettingsPreset's to the cache for a given combinedKey.
    /// </summary>
    public Task AddToCache(CombinedCacheKey combinedKey, IEnumerable<ModSettingsPreset> mods)
    {
        if (_mods.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add ModSettingsPreset list to cache, {combinedKey} key already exists.");
            return Task.CompletedTask;
        }

        // Add all new mods to cache
        foreach (var mod in mods)
            _mods.Add((combinedKey, mod.Container.ModName), mod);

        // Process the cache recalculation.
        UpdateFinalCache();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds a ModSettingsPreset to the cache.
    /// </summary>
    public Task AddToCache(CombinedCacheKey combinedKey, ModSettingsPreset mod)
    {
        // if the combined key is present in the sorted list at all, reject the addition.
        if (_mods.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add ModSettingsPreset to cache, {combinedKey} key already exists.");
            return Task.CompletedTask;
        }

        // Append the mod item.
        _mods.Add((combinedKey, mod.Container.ModName), mod);

        // Update the finalGlamour for that particular slot.
        UpdateFinalCacheForMod(mod.Container.ModName);

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Efficiently iterates through the Mods.
    /// </summary>
    /// <returns> the collection of mods to set or update. </returns>
    private void UpdateFinalCache()
    {
        var addOrUpdate = new List<ModSettingsPreset>();
        var remove = new List<ModSettingsPreset>();

        var seenMods = new HashSet<ModSettingsPreset>();

        // Cycle through the glamours in the order they are sorted in.
        foreach (var modItem in _mods.Values)
        {
            if (!seenMods.Add(modItem))
                continue;

            // Check if the slot should be updated because it is not present or different.
            if (_finalMods.Add(modItem))
                addOrUpdate.Add(modItem);
        }

        // Find the slots that were in _finalMods but are no longer in _glamours.
        var toRemove = _finalMods.Except(seenMods).ToList();
        foreach (var mod in toRemove)
        {
            _finalMods.Remove(mod);
            remove.Add(mod);
        }

        // Update Changes.
        _handler.SetOrUpdateTempMod(addOrUpdate);
        _handler.RemoveTempMod(remove);
    }

    /// <summary>
    ///     Attempts to update the final cache for a single mod.
    /// </summary>
    /// <returns> OptionalBool.True is AddOrUpdate, OptionalBool.False is Remove, OptionalBool.Null is no change. </returns>
    private void UpdateFinalCacheForMod(string modName)
    {
        // Find the top-priority glamour for this slot.
        if (_mods.FirstOrDefault(kvp => kvp.Key.Item2 == modName).Value is { } validMod)
        {
            // grab the index location of the mod in the list with the same name.
            if (_finalMods.TryGetValue(validMod, out var existing))
            {
                // if the label is different, update it.
                if (existing.Label != validMod.Label)
                {
                    _finalMods.Remove(existing);
                    _finalMods.Add(validMod);
                    _handler.SetOrUpdateTempMod(validMod);
                }

                // if the label is the same, ignore and return false.
                return;
            }

            // Otherwise, it didnt yet exist, so just add it.
            _finalMods.Add(validMod);
            _handler.SetOrUpdateTempMod(validMod);
        }
        else
        {
            // If the mod was previously filled, remove and return true if removed.
            if (_finalMods.FirstOrDefault(msp => msp.Container.ModName == modName) is { } modToRemove)
            {
                _finalMods.Remove(modToRemove);
                _handler.RemoveTempMod(modToRemove);
            }
        }
    }
}
