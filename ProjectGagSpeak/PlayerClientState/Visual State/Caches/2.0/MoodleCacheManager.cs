using GagspeakAPI.Data;

namespace GagSpeak.PlayerState.Visual;

// Handles all Glamour Caching and applier interactions.
public class MoodleCacheManager
{
    private readonly ILogger<MoodleCacheManager> _logger;
    private readonly VisualApplierMoodles _handler;

    private SortedList<(CombinedCacheKey, Guid), Moodle> _moodles = new();
    private HashSet<Moodle> _finalMoodles = new();

    public MoodleCacheManager(ILogger<MoodleCacheManager> logger, VisualApplierMoodles handler)
    {
        _logger = logger;
        _handler = handler;
    }

    public HashSet<Moodle> FinalRef => _finalMoodles;

    /// <summary>
    ///     Adds multiple Moodles's to the cache for a given combinedKey.
    /// </summary>
    public Task AddToCache(CombinedCacheKey combinedKey, IEnumerable<Moodle> moodles)
    {
        if (_moodles.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add moodleSettingsPreset list to cache, {combinedKey} key already exists.");
            return Task.CompletedTask;
        }

        // Add all new moodles to cache
        foreach (var moodle in moodles)
            _moodles.Add((combinedKey, moodle.Id), moodle);

        // Process the cache recalculation.
        UpdateFinalCache();
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Adds a Moodle to the cache.
    /// </summary>
    public Task AddToCache(CombinedCacheKey combinedKey, Moodle moodle)
    {
        // if the combined key is present in the sorted list at all, reject the addition.
        if (_moodles.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add moodleSettingsPreset to cache, {combinedKey} key already exists.");
            return Task.CompletedTask;
        }

        // Append the moodle item.
        _moodles.Add((combinedKey, moodle.Id), moodle);

        // Update the finalGlamour for that particular slot.
        UpdateFinalCacheFormoodle(moodle.Id);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Efficiently iterates through the moodles.
    /// </summary>
    /// <returns> the collection of moodles to set or update. </returns>
    private void UpdateFinalCache()
    {
        var moodlesToAdd = new List<Moodle>();
        var moodlesToRemove = new List<Moodle>();

        var seenmoodles = new HashSet<Moodle>();

        // Cycle through the glamours in the order they are sorted in.
        foreach (var moodleItem in _moodles.Values)
        {
            if (!seenmoodles.Add(moodleItem))
                continue;

            // Check if the slot should be updated because it is not present or different.
            if (_finalMoodles.Add(moodleItem))
                moodlesToAdd.Add(moodleItem);
        }

        // Find the slots that were in _finalmoodles but are no longer in _glamours.
        var toRemove = _finalMoodles.Except(seenmoodles).ToList();
        foreach (var moodle in toRemove)
        {
            _finalMoodles.Remove(moodle);
            moodlesToRemove.Add(moodle);
        }

        // Update Changes.
        _handler.AddRestrictedMoodle(moodlesToAdd);
        _handler.RemoveRestrictedMoodle(moodlesToRemove);
    }

    /// <summary>
    ///     Attempts to update the final cache for a single moodle.
    /// </summary>
    /// <returns> OptionalBool.True is AddOrUpdate, OptionalBool.False is Remove, OptionalBool.Null is no change. </returns>
    private void UpdateFinalCacheFormoodle(Guid moodleId)
    {
        // Find the top-priority glamour for this slot.
        if (_moodles.FirstOrDefault(kvp => kvp.Key.Item2 == moodleId).Value is { } validmoodle)
        {
            // grab the index location of the moodle in the list with the same name.
            if(_finalMoodles.Add(validmoodle))
                _handler.AddRestrictedMoodle(validmoodle);

            // if this fails it just means it is already present.
            // Therefore, don't update it.
            return;
        }
        else
        {
            // If the moodle was previously filled, remove and return true if removed.
            if (_finalMoodles.FirstOrDefault(msp => msp.Id == moodleId) is { } existingMoodle)
            {
                // Remove the moodle from the final cache.
                _finalMoodles.Remove(existingMoodle);
                _handler.RemoveRestrictedMoodle(existingMoodle);
            }
        }

        // If no changes were made, return false.
        return;
    }
}
