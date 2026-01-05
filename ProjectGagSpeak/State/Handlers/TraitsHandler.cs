using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagspeakAPI.Attributes;

namespace GagSpeak.State.Handlers;

public class TraitsHandler
{
    private readonly ILogger<TraitsHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly TraitsCache _cache;
    private readonly HotbarActionHandler _controller;

    public TraitsHandler(ILogger<TraitsHandler> logger, GagspeakMediator mediator,
        TraitsCache cache, HotbarActionHandler controller)
    {
        _logger = logger;
        _mediator = mediator;
        _cache = cache;
        _controller = controller;
    }

    public Traits FinalTraits => _cache.FinalTraits;

    /// <summary> Add additional traits into the Traits Cache for the key. </summary>
    public bool TryAddTraitsToCache(CombinedCacheKey key, Traits traits)
    {
        if (traits is Traits.None)
            return false;
        return _cache.AddTraits(key, traits);
    }

    /// <summary> Remove a single key from the Traits Cache. </summary>
    public bool TryRemTraitsFromCache(CombinedCacheKey key)
        => _cache.RemoveTraits(key);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing Traits Cache.");
        _cache.ClearCache();
        await UpdateTraitCache();
    }

    /// <summary>
    ///     Updates the Final Traits Cache, then Updates the 
    ///     controller's sources with the latest trait data.
    /// </summary>
    public Task UpdateTraitCache()
    {
        // If true, a change occured and there was a difference in traits from the previous.
        if (_cache.UpdateFinalCache())
        {
            _logger.LogDebug("Final Traits updated.", LoggerType.VisualCache);
            _controller.UpdateHardcoreStatus();
            _mediator.Publish(new HcStateCacheChanged());
        }
        else
            _logger.LogTrace("No change in Final Traits.", LoggerType.VisualCache);

        return Task.CompletedTask;
    }

}
