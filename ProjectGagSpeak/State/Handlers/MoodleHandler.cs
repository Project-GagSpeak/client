using GagSpeak.Interop;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.State.Handlers;

public class MoodleHandler
{
    private readonly ILogger<MoodleHandler> _logger;
    private readonly MoodleCache _cache;
    private readonly IpcCallerMoodles _ipc;
    public MoodleHandler(
        ILogger<MoodleHandler> logger,
        MoodleCache cache,
        IpcCallerMoodles ipc)
    {
        _logger = logger;
        _cache = cache;
        _ipc = ipc;
    }

    /// <summary> Add a single Moodle to the GlamourCache for the key. </summary>
    public bool TryAddMoodleToCache(CombinedCacheKey key, Moodle? moodle)
    {
        if (moodle is null)
            return false;
        return _cache.AddMoodle(key, moodle);
    }

    /// <summary> Add Multiple Moodles to the GlamourCache for the key. </summary>
    public bool TryAddMoodleToCache(CombinedCacheKey key, IEnumerable<Moodle> moodles)
    {
        if (moodles.IsNullOrEmpty())
            return false;
        return _cache.AddMoodle(key, moodles);
    }

    public bool TryUpdateMoodleInCache(CombinedCacheKey key, Moodle moodle)
    {
        if (moodle is null)
            return false;
        return _cache.UpdateMoodle(key, moodle);
    }

    /// <summary> Remove a single key from the GlamourCache. </summary>
    public bool TryRemMoodleFromCache(CombinedCacheKey key)
        => _cache.RemoveMoodle(key);

    /// <summary> Remove Multiple keys from the GlamourCache. </summary>
    public bool TryRemMoodleFromCache(IEnumerable<CombinedCacheKey> keys)
        => _cache.RemoveMoodle(keys);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing Moodles Cache.", LoggerType.VisualCache);
        _cache.ClearCache();
        await UpdateMoodleCache();
    }

    /// <summary>
    ///     Updates the Final Glamour Cache, and then applies the visual updates.
    /// </summary>
    public async Task UpdateMoodleCache()
    {
        // Update the final cache. `removedSlots` contains slots that are no longer restricted after the change.
        if (_cache.UpdateFinalCache(out var removedMoodles))
        {
            _logger.LogDebug($"FinalMoodles Cache was updated! RemovedMoodles: {removedMoodles.Count()}", LoggerType.VisualCache);
            if (removedMoodles.Any())
                await RestoreAndReapplyCache(removedMoodles);
            else
                await ApplyMoodleCache();
        }
        else
            _logger.LogTrace("No change in FinalMoodles Cache.", LoggerType.VisualCache);
        _logger.LogDebug("Finished Updating Moodle Caches.", LoggerType.VisualCache);
    }

    /// <summary>
    ///     Applies all cached restricted moodles to the client.
    /// </summary>
    /// <remarks>
    ///     Likely makes things fucky with timed moodles but yeah, idk.
    ///     Could add more overhead but not in the mood, just want efficiency rn.
    /// </remarks>
    private async Task ApplyMoodleCache()
    {
        var idsToApply = MoodleCache.IpcData.DataInfo.Any()
            ? _cache.FinalStatusIds.Except(MoodleCache.IpcData.DataInfo.Keys)
            : _cache.FinalStatusIds;
        await _ipc.ApplyOwnStatus(idsToApply, true);
        _logger.LogDebug("Applied all cached moodles to the client.", LoggerType.IpcMoodles);
    }

    /// <summary>
    ///     Removes moodles no longer meant to be present, then reapplies restricted ones
    /// </summary>
    private async Task RestoreAndReapplyCache(IEnumerable<Guid> moodlesToRemove)
    {
        await _ipc.RemoveOwnStatuses(moodlesToRemove, true);
        _logger.LogDebug($"Removed Moodles: {string.Join(", ", moodlesToRemove)}", LoggerType.IpcMoodles);
        // Reapply restricted.
        await ApplyMoodleCache();
    }

    /// <summary>
    ///     Applies a moodle to the client. Can be souced from anything.
    /// </summary>
    /// <remarks> If this moodle is not present in the client's Moodle Status List, it will not work. </remarks>
    public async Task ApplyMoodle(Moodle moodle)
    {
        await _ipc.ApplyOwnStatus(moodle is MoodlePreset p ? p.StatusIds : [moodle.Id], false);
    }

    // Hopefully never use this.
    public async Task ApplyMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await ApplyMoodle(moodle));
    }

    /// <summary>
    ///     Assumes they have already been removed from the finalMoodles cache.
    /// </summary>
    private async Task RemoveMoodle(Moodle moodle)
    {
        await _ipc.RemoveOwnStatuses((moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]).Except(_cache.FinalStatusIds), true);
    }

    // Hopefully never use this.
    public async Task RemoveMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await RemoveMoodle(moodle));
    }
}
