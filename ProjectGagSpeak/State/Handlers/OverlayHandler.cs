using GagSpeak.PlayerClient;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Handlers;

// could maybe merge this with the OverlayController down the line, see how things go.
public class OverlayHandler : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly OverlayCache _cache;
    private readonly BlindfoldService _bfService;
    private readonly HypnoService _hypnoService;

    private SemaphoreSlim _applySlim = new(1, 1);
    private System.Timers.Timer _sentHypnosisTimer = new(int.MaxValue) { AutoReset = false };

    public OverlayHandler(ILogger<OverlayHandler> logger, GagspeakMediator mediator,
        MainConfig config, OverlayCache cache, BlindfoldService bfService,
        HypnoService hypnoService)
        : base(logger, mediator)
    {
        _config = config;
        _cache = cache;
        _bfService = bfService;
        _hypnoService = hypnoService;

        _sentHypnosisTimer.Elapsed += (_, _) => OnSentHypnoEffectExpire(true);
        Svc.PluginInterface.UiBuilder.Draw += DrawOverlays;
    }

    public bool IsHypnotized => _hypnoService.HasValidEffect;
    public bool ActiveHypnoIsSentEffect => _hypnoService.IsSentEffect;

    /// <summary>
    ///     Only call this from the Connection Sync Service! <para />
    ///     This is able to reapply the active effect without messing up achievements. <para />
    ///     If possible by merging this with the visualCache sync we could only perform one global 
    ///     perm callback, but only do if optimization is nessisary)
    /// </summary>
    public async Task SyncOverlayWithMetaData()
    {
        Logger.LogWarning("This method still needs to be updated to work properly with the new hardcore state!");
        //Logger.LogDebug("Syncing Active Overlays with playerMetaData on connection!");
        //// if the metadatas hypno effect is null we have nothing to worry about here and can return.
        //if (_metadata.MetaData.HypnoEffectInfo is not { } info)
        //    return;

        //if (ClientData.Globals is not { } gp)
        //    return;

        //// grab the current data from the metadata.
        //var expectedExpireTime = _metadata.MetaData.AppliedTimeUTC + _metadata.MetaData.AppliedDuration;
        //var remainingTime = expectedExpireTime - DateTimeOffset.UtcNow;
        //if (remainingTime <= TimeSpan.Zero)
        //{
        //    // perform a removal. (and remove from service if possible.
        //    if (_hypnoService.IsSentEffect)
        //    {
        //        // Fire Achievements for item removal if true.
        //        // do that here in
        //        // this comment area!
        //        await _hypnoService.RemoveEffect();
        //    }
        //    // remove metadata.
        //    _metadata.ClearHypnoEffect();

        //    // toggle off our applied effect.
        //    Logger.LogError("Effect Expired while offline, setting effect to none.");
        //    Mediator.Publish(new PushGlobalPermChange(nameof(GlobalPerms.HypnosisCustomEffect), string.Empty));
        //    return;
        //}

        //// reapply it.
        //await _hypnoService.ApplyEffect(info, gp.HypnoEnactor(), (int)remainingTime.TotalSeconds, _metadata.MetaData.Base64CustomImageData);

        //// update the timer for the new interval.
        //Logger.LogInformation($"Timed Hypnosis Effect reapplied after connection!", LoggerType.VisualCache);
        //_sentHypnosisTimer.Interval = remainingTime.TotalMilliseconds;
        //_sentHypnosisTimer.Start();
    }

    private void DrawOverlays()
    {
        if (!Svc.ClientState.IsLoggedIn)
            return;
        // Control Blindfold Draw
        _bfService.DrawBlindfoldOverlay();
        // Control Hypnosis Draw (Since it is 'under' the blindfold, but you 'see' it infront of the blindfold)
        _hypnoService.DrawHypnoEffect();
    }

    public bool CanApplyTimedEffect(HypnoticEffect effect, string? base64ImgString = null)
        => _hypnoService.CanApplyTimedEffect(effect, base64ImgString);

    // Effect should be called by a listener that has received an instruction from another Kinkster to hypnotize the client.
    public async Task SetTimedHypnoEffect(UserData enactor, HypnoticEffect effect, TimeSpan length, string? customImage)
    {
        var applyTime = DateTimeOffset.UtcNow;
        // apply the effect, bagagwa should never be thrown here.
        if (!await _hypnoService.ApplyEffect(effect, enactor.UID, (int)length.TotalSeconds, customImage))
            throw new Bagagwa("Summoned Bagagwa while setting a timed hypnotic effect! This should never happen!");
        
        Logger.LogInformation($"Timed Hypnosis Effect successfully applied!", LoggerType.VisualCache);
        // Achievements here maybe.

        // Set the effect.
        _config.Current.HypnoEffectInfo = effect;
        _config.Current.Base64CustomImageData = customImage;
        _config.Save();

        // update the hypnosis timer with our interval timeout.
        _sentHypnosisTimer.Interval = length.TotalMilliseconds;
        _sentHypnosisTimer.Start();
    }

    public async void RemoveHypnoEffect(string enactor, bool giveAchievements, bool fromDispose = false)
    {
        Logger.LogDebug($"HardcoreState Hypnotic Effect cleared by ({enactor})!");
        // remove the effect from the hypno service.
        await _hypnoService.RemoveSentEffectOnExpire().ConfigureAwait(false);
        // Once removed, try to reapply any from our equipped cache. (helps for achievements)
        await OnApplyHypnoEffect(_cache.ActiveEffect, _cache.PriorityEffectKey).ConfigureAwait(false);
        // Remove the stored HardcoreState effect & image from the config if not ran by plugin disposal.
        if (!fromDispose)
        {
            _config.Current.HypnoEffectInfo = null;
            _config.Current.Base64CustomImageData = null;
            _config.Save();
        }
    }

    private async void OnSentHypnoEffectExpire(bool giveAchievements)
    {
        // The sent hypnosis effect has met its expiration time, and should be removed.
        Logger.LogDebug("Sent Hypnosis Effect has expired, removing it from the applied effect.");
        // Fire any achievements related to sent effect removal here.

        // Remove the effect from the hypno service.
        await _hypnoService.RemoveSentEffectOnExpire().ConfigureAwait(false);
        // ABOVE LINE MIGHT CAUSE CALCULATION DELAY, BE CAUTIOUS.

        // Once removed, check if there is anything in the cache to apply in its place.
        // Realistically, this should never happen, but it is important to handle for achievements.
        await OnApplyHypnoEffect(_cache.ActiveEffect, _cache.PriorityEffectKey).ConfigureAwait(false);
    }


    /// <summary> Add a single BlindfoldOverlay to the Blindfold Cache for the key. </summary>
    public bool TryAddBlindfoldToCache(CombinedCacheKey key, BlindfoldOverlay? overlay)
    {
        if (overlay is null)
            return false;
        return _cache.TryAddBlindfold(key, overlay);
    }

    /// <summary> Add a single HypnoEffectOverlay to the HypnoEffect Cache for the key. </summary>
    public bool TryAddEffectToCache(CombinedCacheKey key, HypnoticOverlay? overlay)
    {
        if (overlay is null)
            return false;
        return _cache.TryAddHypnoEffect(key, overlay);
    }

    /// <summary> Removes a single key from the Blindfold Cache. </summary>
    public bool TryRemBlindfoldFromCache(CombinedCacheKey key)
        => _cache.TryRemoveBlindfold(key);

    /// <summary> Removes a single key from the HypnoEffect Cache. </summary>
    public bool TryRemEffectFromCache(CombinedCacheKey key)
        => _cache.TryRemoveHypnoEffect(key);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        Logger.LogDebug("Clearing Blindfold and Hypnosis Caches, then applying updates.");
        _cache.ClearCaches();
        await UpdateCaches();
    }

    /// <summary>
    ///     The Go-To All-In-One update for both overlay caches at once.
    /// </summary>
    /// <remarks> This runs through a SemaphoreSlim execution and is handled safely. </remarks>
    public async Task UpdateCaches()
    {
        Logger.LogDebug("Updating Blindfold & HypnoEffect Caches.");
        await ExecuteWithSemaphore(async () =>
        {
            // Run both operations in parallel.
            await Task.WhenAll(
                UpdateBlindfoldInternal(),
                UpdateHypnoEffectInternal()
            );
            Logger.LogInformation($"Processed Cache Updates Successfully!");
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
        if (_cache.UpdateFinalBlindfoldCache(out var prevActiveKey))
        {
            // The blindfold cache changed, and we should apply the new cached lace overlay.
            Logger.LogDebug($"Blindfold Cache changed! Priority was [{prevActiveKey.ToString()}] and is now [{_cache.PriorityEffectKey.ToString()}] Reapplying cache!", LoggerType.VisualCache);
            // if the previous type was not CombinedCacheKey.Empty, try and remove the blindfold.
            if (!prevActiveKey.Equals(CombinedCacheKey.Empty))
            {
                // use the previous active combined key to properly handle `removing` the current blindfold.
                await OnRemoveBlindfold(prevActiveKey);
            }

            // If the active cache currently has a blindfold overlay, and none are applied, apply it.
            if (!_bfService.HasValidBlindfold && _cache.ActiveBlindfold is { } activeBlindfold)
            {
                Logger.LogDebug("Currently no Blindfold applied, but we have one in our cache, so applying!", LoggerType.VisualCache);
                await OnApplyBlindfold(activeBlindfold, _cache.PriorityEffectKey);
            }
        }
        else
            Logger.LogTrace("No change in Final Blindfold Cache.", LoggerType.VisualCache);
    }

    // Called whenever a spesific visual cached Blindfold should be removed.
    private async Task OnRemoveBlindfold(CombinedCacheKey removedSource)
    {
        // If you run into this error, you've seriously messed up.
        if (!_bfService.HasValidBlindfold)
        {
            Logger.LogError("Somehow you went to remove an blindfold, but none were found!");
            return;
        }
        // Fire achievements related to the keys removal 
        // HERE, DO IT HERE

        // we should remove this effect.
        Logger.LogDebug("Current applied blindfold is a personal overlay. Removing as it is no longer in cache.");
        await _bfService.RemoveBlindfold().ConfigureAwait(false);
        // ABOVE LINE MIGHT CAUSE CALCULATION DELAY, BE CAUTIOUS.
    }

    // Called whenever a spesific visual cached Blindfold should be applied.
    private async Task OnApplyBlindfold(BlindfoldOverlay? blindfold, CombinedCacheKey enactor)
    {
        // Do not validate if effect is null (and maybe even throw an exception lol)
        if (blindfold is null || _bfService.HasValidBlindfold)
        {
            Logger.LogDebug("Not in valid state to apply Blindfold Overlays!");
            return;
        }
        // Fire Achievements related to blindfold application here.

        // If we are here, we are applying.
        Logger.LogDebug($"[{enactor}] has applied a Blindfold Effect: {blindfold.OverlayPath}");
        await _bfService.ApplyBlindfold(blindfold, enactor).ConfigureAwait(false);
    }

    /// <summary>
    ///    Updates the Final Hypnosis Caches, and then applies the visual updates.
    /// </summary>
    private async Task UpdateHypnoEffectInternal()
    {
        // We need to update the hypnotic cache here.
        // It is important to note that if we are currently processing a manually applied effect,
        // it should remain taking precedence over the cached display and their data should not be removed.
        // Instead, we should handle firing achievements for the enactor of the removed effect.
        if (_cache.UpdateFinalHypnoEffectCache(out var prevActiveKey))
        {
            // The hypnotic cache changed, and we should apply the new cached effect.
            Logger.LogDebug($"Hypnosis Cache changed! Priority was [{prevActiveKey.ToString()}] and is now [{_cache.PriorityEffectKey.ToString()}] Reapplying cache!", LoggerType.VisualCache);
            // if the previous type was not CombinedCacheKey.Empty, try and remove the effect.
            if (!prevActiveKey.Equals(CombinedCacheKey.Empty))
            {
                // use the previous active combined key to properly handle `removing` the current effect.
                await OnRemoveHypnoEffect(prevActiveKey);
            }

            // If the active cache currently has an effect, and we are not processing a sent effect, start it.
            if (!_hypnoService.HasValidEffect && _cache.ActiveEffect is { } effect)
            {
                Logger.LogDebug("There is no effect running, and we have an effect in our cache to apply, so applying!", LoggerType.VisualCache);
                await OnApplyHypnoEffect(effect, _cache.PriorityEffectKey);
            }
        }
        else
            Logger.LogTrace("No change in Final HypnoEffect Cache.", LoggerType.VisualCache);
    }

    // Passes in the enactorUID that had applied the effect which just got removed from the cache.
    // This should be the only primary source calling an effect removal, outside of manual applications.
    private async Task OnRemoveHypnoEffect(CombinedCacheKey removedEffectSource)
    {
        // If you run into this error, you've seriously messed up.
        if (!_hypnoService.HasValidEffect)
        {
            Logger.LogError("Somehow you went to remove an effect, but no effect was found!");
            return;
        }
        // Fire achievements related to the keys removal 
        // HERE, DO IT HERE

        // If the effect was a sent effect, only worry about processing achievements for removal, and then return.
        if (_hypnoService.IsSentEffect)
        {
            Logger.LogDebug("Current applied effect is a sent effect on a timer. Keeping effect active and returning.");
            return;
        }
        else
        {
            // we should remove this effect.
            Logger.LogDebug("Current applied effect is a personal effect, removing effect as it is no longer in cache.");
            await _hypnoService.RemoveEffect().ConfigureAwait(false);
            // ABOVE LINE MIGHT CAUSE CALCULATION DELAY, BE CAUTIOUS.
        }
    }

    // Called whenever a hypnotic effect should be applied to the player.
    // There is a chance that this is called while the player is already under a hypnotic effect sent by a Kinkster.
    // If this occurs, we should invoke the achievement for it being applied, but not actually apply the effect.
    // (This makes sense as it is still "active" underneath)
    private async Task OnApplyHypnoEffect(HypnoticOverlay? effect, CombinedCacheKey enactor)
    {
        // Do not validate if effect is null (and maybe even throw an exception lol)
        if (effect is null || _hypnoService.HasValidEffect || _hypnoService.IsSentEffect)
        {
            Logger.LogDebug("Not in valid state to apply hypnotic effects!");
            return;
        }
        // Fire Achievements related to hypnotic application here.

        // If we are here, we are applying a personal effect.
        Logger.LogDebug($"Applying Hypnotic Effect: {effect.OverlayPath} by {enactor}");
        // Apply the effect to the player.
        await _hypnoService.ApplyEffect(effect, enactor).ConfigureAwait(false);
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
        catch (Bagagwa ex)
        {
            Logger.LogError($"Error during semaphore execution: {ex}");
        }
        finally
        {
            _applySlim.Release();
        }
    }
}
