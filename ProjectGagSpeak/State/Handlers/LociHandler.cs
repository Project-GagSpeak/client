using GagSpeak.Interop;
using GagSpeak.Interop.Helpers;
using GagSpeak.State.Caches;
using GagspeakAPI.Data;
using LociApi.Enums;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.State.Handlers;

public class LociHandler
{
    private readonly ILogger<LociHandler> _logger;
    private readonly LociCache _cache;
    private readonly IpcCallerMoodles _ipcMoodles;
    private readonly IpcCallerLoci _ipcLoci;
    public LociHandler(ILogger<LociHandler> logger, LociCache cache, IpcCallerMoodles moodles, IpcCallerLoci loci)
    {
        _logger = logger;
        _cache = cache;
        _ipcMoodles = moodles;
        _ipcLoci = loci;
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

        // Handle application accordingly.
        await ApplyStatus(idsToApply.ToList(), true);
        _logger.LogDebug("Applied all Statuses to the client.", LoggerType.IpcLoci);
    }

    /// <summary>
    ///     Removes lociItems no longer meant to be present, then reapplies restricted ones
    /// </summary>
    private async Task RestoreAndReapplyCache(IEnumerable<Guid> itemsToRemove)
    {
        await RemoveStatus([.. itemsToRemove], true);
        _logger.LogDebug($"Removed LociItems: {string.Join(", ", itemsToRemove)}", LoggerType.IpcLoci);
        // Reapply restricted.
        await ApplyLociCache();
    }

    public async Task ApplyStatus(Guid status, bool lockId)
    {
        // Prioritize Loci
        if (IpcCallerLoci.APIAvailable)
            await _ipcLoci.ApplyStatus(status, lockId);
        else if (IpcCallerMoodles.APIAvailable)
            await _ipcMoodles.ApplyStatus([status]);
    }

    public async Task ApplyStatus(List<Guid> statusIds, bool lockIds)
    {
        // Prioritize Loci
        if (IpcCallerLoci.APIAvailable)
            await _ipcLoci.ApplyStatus(statusIds, lockIds);
        else if (IpcCallerMoodles.APIAvailable)
            await _ipcMoodles.ApplyStatus(statusIds);
    }

    public async Task RemoveStatus(Guid statusId, bool lockId)
    {
        // Prioritize Loci
        if (IpcCallerLoci.APIAvailable)
            await _ipcLoci.BombStatus(statusId, lockId);
        else if (IpcCallerMoodles.APIAvailable)
            await _ipcMoodles.RemoveStatuses([statusId]);
    }

    private async Task RemoveStatus(List<Guid> statusIds, bool lockIds)
    {
        // Prioritize Loci
        if (IpcCallerLoci.APIAvailable)
            await _ipcLoci.BombStatus(statusIds, lockIds);
        else if (IpcCallerMoodles.APIAvailable)
            await _ipcMoodles.RemoveStatuses(statusIds);
    }

    public async Task<bool> ApplyLociItem(LociItem item, bool lockItem)
    {
        // Prioritize loci.
        if (IpcCallerLoci.APIAvailable)
        {
            return item switch
            {
                LociTuple tuple => await _ipcLoci.ApplyStatusInfo(tuple.Tuple.ToTuple(), lockItem).ConfigureAwait(false),
                LociPreset preset => await _ipcLoci.ApplyPreset(preset.Id, lockItem).ConfigureAwait(false),
                _ => await _ipcLoci.ApplyStatus(item.Id, lockItem).ConfigureAwait(false)
            };
        }
        else if (IpcCallerMoodles.APIAvailable)
        {
            switch (item)
            {
                case LociTuple:
                    _logger.LogWarning("Moodles cannot support applying by tuple!");
                    return false;
                case LociPreset preset:
                    await _ipcMoodles.ApplyStatus([.. preset.StatusIds]).ConfigureAwait(false);
                    return true; // Can lead to false positives since moodles api isnt as granular
                default:
                    await _ipcMoodles.RemoveStatuses([item.Id]).ConfigureAwait(false);
                    return true; // Can lead to false positives since moodles api isnt as granular
            }
        }
        _logger.LogWarning("No API available to apply LociItem!");
        return false;
    }

    public async Task<bool> RemoveLociItem(LociItem item, bool unlock)
    {
        // Prioritize loci.
        if (IpcCallerLoci.APIAvailable)
        {
            return item switch
            {
                LociTuple tuple => await _ipcLoci.BombStatus(tuple.Tuple.GUID, unlock).ConfigureAwait(false),
                LociPreset preset => await _ipcLoci.BombStatus(preset.StatusIds, unlock).ConfigureAwait(false),
                _ => await _ipcLoci.BombStatus(item.Id, unlock).ConfigureAwait(false)
            };
        }
        else if (IpcCallerMoodles.APIAvailable)
        {
            switch (item)
            {
                case LociTuple:
                    _logger.LogWarning("Moodles cannot support removing by tuple!");
                    return false;
                case LociPreset preset:
                    await _ipcMoodles.RemoveStatuses(preset.StatusIds).ConfigureAwait(false);
                    return true; // Can lead to false positives since moodles api isnt as granular
                default:
                    await _ipcMoodles.RemoveStatuses([item.Id]).ConfigureAwait(false);
                    return true; // Can lead to false positives since moodles api isnt as granular
            }
        }
        _logger.LogWarning("No API available to remove LociItem!");
        return false;
    }

    public async Task<bool> ApplyLociItem(List<LociItem> items, bool lockItems)
    {
        if (IpcCallerLoci.APIAvailable)
        {
            var statusIds = new List<Guid>();
            var presetIds = new List<Guid>();
            var tuples = new List<LociStatusInfo>();
            foreach (var item in items)
            {
                switch (item)
                {
                    case LociTuple t: tuples.Add(t.Tuple.ToTuple()); break;
                    case LociPreset p: presetIds.Add(p.Id); break;
                    default: statusIds.Add(item.Id); break;
                }
            }

            var res = await _ipcLoci.ApplyStatus(statusIds, lockItems).ConfigureAwait(false);
            var presetRes = await _ipcLoci.ApplyPreset(presetIds, lockItems).ConfigureAwait(false);
            var tupleRes = await _ipcLoci.ApplyStatusInfo(tuples, lockItems).ConfigureAwait(false);
            // Ensure all 3 had either success or partial success.
            return res && presetRes && tupleRes;
        }
        else if (IpcCallerMoodles.APIAvailable)
        {
            var statusIds = items.SelectMany(i => i is LociPreset p ? p.StatusIds : [i.Id]).ToList(); 
            await _ipcMoodles.ApplyStatus(statusIds).ConfigureAwait(false);
            return true; // Can lead to false positives since moodles api isnt as granular
        }

        _logger.LogWarning("No API available to apply LociItems!");
        return false;
    }

    public async Task<bool> RemoveLociItem(List<LociItem> items, bool unlock)
    {
        var statusIds = items.SelectMany(i => i is LociPreset p ? p.StatusIds : [i.Id]).ToList();
        if (IpcCallerLoci.APIAvailable)
            return await _ipcLoci.BombStatus(statusIds, unlock).ConfigureAwait(false);
        else if (IpcCallerMoodles.APIAvailable)
        {
            await _ipcMoodles.RemoveStatuses(statusIds).ConfigureAwait(false);
            return true; // Can lead to false positives since moodles api isnt as granular
        }

        _logger.LogWarning("No API available to remove LociItems!");
        return false;
    }
}
