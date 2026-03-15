using GagSpeak.Interop;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.State.Handlers;

public class LociHandler
{
    private readonly ILogger<LociHandler> _logger;
    private readonly LociCache _cache;
    private readonly IpcCallerLoci _ipc;
    public LociHandler(ILogger<LociHandler> logger, LociCache cache, IpcCallerLoci ipc)
    {
        _logger = logger;
        _cache = cache;
        _ipc = ipc;
    }

    /// <summary> Add a single LociItem to the GlamourCache for the key. </summary>
    public bool TryAddLociItemToCache(CombinedCacheKey key, LociItem? lociItem)
    {
        if (lociItem is null)
            return false;
        return _cache.AddLoci(key, lociItem);
    }

    public bool TryAddLociItemToCache(CombinedCacheKey key, IEnumerable<LociItem> items)
    {
        if (items.IsNullOrEmpty())
            return false;
        return _cache.AddLoci(key, items);
    }

    public bool TryUpdateItemInCache(CombinedCacheKey key, LociItem item)
    {
        if (item is null)
            return false;
        return _cache.UpdateLoci(key, item);
    }

    /// <summary> Remove a single key from the GlamourCache. </summary>
    public bool TryRemLociDataFromCache(CombinedCacheKey key)
        => _cache.RemoveLociData([key]);

    /// <summary> Remove Multiple keys from the GlamourCache. </summary>
    public bool TryRemLociDataFromCache(IEnumerable<CombinedCacheKey> keys)
        => _cache.RemoveLociData(keys);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing LociCache.", LoggerType.VisualCache);
        _cache.ClearCache();
        await UpdateLociCache();
    }

    public async Task UpdateLociCache()
    {
        if (_cache.UpdateFinalCache(out var removedItems))
        {
            _logger.LogDebug($"FinalLociCache was updated! Removed: {removedItems.Count()}", LoggerType.VisualCache);
            if (removedItems.Any())
                await RestoreAndReapplyCache(removedItems);
            else
                await ApplyLociCache();
        }
        else
            _logger.LogTrace("No change in FinalLociCache.", LoggerType.VisualCache);
        _logger.LogDebug("Finished Updating LociCaches.", LoggerType.VisualCache);
    }

    /// <summary>
    ///     Applies all cached restricted loci statuses to the client.
    /// </summary>
    /// <remarks>
    ///     Likely makes things fucky with timed statuses but yeah, idk.
    ///     Could add more overhead but not in the mood, just want efficiency rn.
    /// </remarks>
    private async Task ApplyLociCache()
    {
        var idsToApply = LociCache.Data.DataInfo.Any()
            ? _cache.FinalStatusIds.Except(LociCache.Data.DataInfo.Keys)
            : _cache.FinalStatusIds;
        await _ipc.ApplyStatus([ ..idsToApply ], true);
        _logger.LogDebug("Applied all Statuses to the client.", LoggerType.IpcLoci);
    }

    /// <summary>
    ///     Removes lociItems no longer meant to be present, then reapplies restricted ones
    /// </summary>
    private async Task RestoreAndReapplyCache(IEnumerable<Guid> itemsToRemove)
    {
        await _ipc.BombStatus([..itemsToRemove], true);
        _logger.LogDebug($"Removed LociItems: {string.Join(", ", itemsToRemove)}", LoggerType.IpcLoci);
        // Reapply restricted.
        await ApplyLociCache();
    }

    /// <summary>
    ///     Applies a LociItem to the client. Can be souced from anything.
    /// </summary>
    /// <remarks> If this item is not present in the client's Status List, it will not work. </remarks>
    public async Task ApplyLociItem(LociItem item)
        => await _ipc.ApplyStatus(item is LociPreset p ? p.StatusIds : [item.Id], true);

    // Hopefully never use this.
    public async Task ApplyLociItems(IEnumerable<LociItem> items)
        => await Parallel.ForEachAsync(items, async (item, _) => await ApplyLociItem(item));

    /// <summary>
    ///     Assumes they have already been removed from the finalLociCache.
    /// </summary>
    private async Task RemoveLociItem(LociItem item)
        => await _ipc.BombStatus([.. (item is LociPreset p ? p.StatusIds : [item.Id]).Except(_cache.FinalStatusIds)], true);

    // Hopefully never use this.
    public async Task RemoveLociItems(IEnumerable<LociItem> items)
        => await Parallel.ForEachAsync(items, async (item, _) => await RemoveLociItem(item));
}
