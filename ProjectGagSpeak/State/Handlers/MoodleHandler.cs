using GagSpeak.Interop.Ipc;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;

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

    // Likely makes things fucky with timed moodles but yeah, idk.
    // Could add more overhead but not in the mood, just want efficiency rn.
    /// <summary>
    ///     Applies all cached restricted moodles to the client.
    /// </summary>
    public async Task ApplyMoodleCache()
    {
        await _ipc.ApplyOwnStatusByGUID(_cache.FinalStatusIds.Except(MoodleCache.IpcData.DataInfo.Keys));
        _logger.LogDebug("Applied all cached moodles to the client.", LoggerType.IpcMoodles);
    }

    /// <summary>
    ///     Removes moodles no longer meant to be present, then reapplies restricted ones
    /// </summary>
    public async Task RestoreAndReapplyCache(IEnumerable<Guid> moodlesToRemove)
    {
        await _ipc.RemoveOwnStatusByGuid(moodlesToRemove);
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
        await _ipc.ApplyOwnStatusByGUID(moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]);
    }

    public async Task ApplyMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await ApplyMoodle(moodle));
    }

    /// <summary>
    ///     Assumes they have already been removed from the finalMoodles cache.
    /// </summary>
    private async Task RemoveMoodle(Moodle moodle)
    {
        await _ipc.RemoveOwnStatusByGuid((moodle is MoodlePreset p ? p.StatusIds : [moodle.Id]).Except(_cache.FinalStatusIds));
    }

    /// <summary>
    ///     Hopefully we never have to fun this...
    /// </summary>
    /// <remarks> Assumes they have already been removed from the finalMoodles cache. </remarks>
    public async Task RemoveMoodle(IEnumerable<Moodle> moodles)
    {
        await Parallel.ForEachAsync(moodles, async (moodle, _) => await RemoveMoodle(moodle));
    }
}
