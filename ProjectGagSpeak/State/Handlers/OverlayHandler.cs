using GagSpeak.Services.Controller;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;

namespace GagSpeak.State.Handlers;

// could maybe merge this with the OverlayController down the line, see how things go.
public class OverlayHandler
{
    private readonly ILogger<OverlayHandler> _logger;
    private readonly OverlayCache _cache;
    private readonly OverlayController _controller;

    private SemaphoreSlim _applySlim = new(1, 1);

    public OverlayHandler(ILogger<OverlayHandler> logger, OverlayCache cache, OverlayController controller)
    {
        _logger = logger;
        _cache = cache;
        _controller = controller;
    }

    /// <summary> Add a single BlindfoldOverlay to the Blindfold Cache for the key. </summary>
    public bool TryAddBlindfoldToCache(CombinedCacheKey key, BlindfoldOverlay overlay)
        => _cache.TryAddBlindfold(key, overlay);

    /// <summary> Add a single HypnoEffectOverlay to the HypnoEffect Cache for the key. </summary>
    public bool TryAddEffectToCache(CombinedCacheKey key, HypnoticOverlay overlay)
        => _cache.TryAddHypnoEffect(key, overlay);

    /// <summary> Removes a single key from the Blindfold Cache. </summary>
    public bool TryRemBlindfoldFromCache(CombinedCacheKey key)
        => _cache.TryRemoveBlindfold(key);

    /// <summary> Removes a single key from the HypnoEffect Cache. </summary>
    public bool TryRemEffectFromCache(CombinedCacheKey key)
        => _cache.TryRemoveHypnoEffect(key);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing Blindfold and Hypnosis Caches, then applying updates.");
        _cache.ClearCaches();
        await UpdateCaches();
    }

    /// <summary>
    ///     The Go-To All-In-One update for both overlay caches at once.
    /// </summary>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task UpdateCaches()
    {
        _logger.LogDebug("Updating Blindfold & HypnoEffect Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Run both operations in parallel.
            await Task.WhenAll(
                UpdateBlindfoldInternal(),
                UpdateHypnoEffectInternal()
            );
            _logger.LogInformation($"Processed Cache Updates Successfully!");
        });
    }


    /// <summary> Remove this if nothing else needs to use it under any case. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateBlindfoldCacheSlim()
        => await ExecuteWithSemaphore(UpdateBlindfoldInternal);

    /// <summary> Remove this if nothing else needs to use it under any case. </summary>
    /// <remarks> Handled safely through a SemaphoreSlim. </remarks>
    public async Task UpdateHypnoEffectCacheSlim()
        => await ExecuteWithSemaphore(UpdateHypnoEffectInternal);

    /// <summary>
    ///    Updates the Final Blindfold Cache, and then applies the visual updates.
    /// </summary>
    private async Task UpdateBlindfoldInternal()
    {
        // Update the final cache. If the blindfolds have changed, we need to perform a swap operation on them.
        if (_cache.UpdateFinalBlindfoldCache())
        {
            _logger.LogDebug($"Final Blindfold Cache updated with a change, calling swap function.", LoggerType.VisualCache);
            await ApplyBlindfoldCache();
        }
        else
            _logger.LogTrace("No change in Final Blindfold Cache.", LoggerType.VisualCache);
    }

    /// <summary>
    ///    Updates the Final Hypnosis Caches, and then applies the visual updates.
    /// </summary>
    private async Task UpdateHypnoEffectInternal()
    {
        // Update the final cache. If the hypno effect has changed, we need to perform a swap operation on them.
        if (_cache.UpdateFinalHypnoEffectCache())
        {
            _logger.LogDebug($"Final HypnoEffect Cache updated with a change, reapplying cache!", LoggerType.VisualCache);
            await ApplyHypnoEffectCache();
        }
        else
            _logger.LogTrace("No change in Final HypnoEffect Cache.", LoggerType.VisualCache);
    }


    // Do not await these or you will be delaying your cache optimization by like 3000ms lol.
    public Task ApplyBlindfoldCache()
    {
        var hasActiveBlindfold = _cache.ActiveBlindfold is not null;
        // If we have an active item, but there is no more items to apply, remove it.
        if (!hasActiveBlindfold && _controller.HasValidBlindfold)
        {
            _logger.LogDebug("No active blindfold found in cache, removing current blindfold.");
            _controller.RemoveBlindfold().ConfigureAwait(false);
            return Task.CompletedTask;
        }
        // Otherwise, swap / apply it.
        if (hasActiveBlindfold)
        {
            _logger.LogDebug("Active blindfold found in cache, applying / swapping it.");
            _controller.ApplyBlindfold(_cache.ActiveBlindfold!, _cache.ActiveBlindfoldEnactor!).ConfigureAwait(false);
        }
        return Task.CompletedTask;
    }

    // Do not await these or you will be delaying your cache optimization by like 3000ms lol.
    public Task ApplyHypnoEffectCache()
    {
        var hasActiveEffect = _cache.ActiveEffect is not null;
        // If we have an active item, but there is no more items to apply, remove it.
        if (!hasActiveEffect && _controller.HasValidHypnoEffect)
        {
            _logger.LogDebug("No active Effect found in cache, removing current effect.");
            _controller.RemoveHypnoEffect().ConfigureAwait(false);
            return Task.CompletedTask;
        }
        // Otherwise, swap / apply it.
        if (hasActiveEffect)
        {
            _logger.LogDebug("Active blindfold found in cache, applying / swapping it.");
            _controller.ApplyHypnoEffect(_cache.ActiveEffect!, _cache.ActiveEffectEnactor!).ConfigureAwait(false);
        }
        return Task.CompletedTask;
    }

    /// <summary> Ensures that all calls for the hypno service are performed one after the other. </summary>
    /// <remarks> This is nessisary to avoid deadlocks, self-calling loops, and for halting excessive calls. </remarks>
    private async Task ExecuteWithSemaphore(Func<Task> action)
    {
        // First, acquire the semaphore.
        await _applySlim.WaitAsync();
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            _applySlim.Release();
        }
    }
}
