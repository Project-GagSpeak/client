using GagSpeak.Interop;
using GagSpeak.State.Caches;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.State.Handlers;

public class CustomizePlusHandler
{
    private readonly ILogger<CustomizePlusHandler> _logger;
    private readonly CustomizePlusCache _cache;
    private readonly IpcCallerCustomize _ipc;

    public CustomizePlusHandler(ILogger<CustomizePlusHandler> logger,
        CustomizePlusCache cache, IpcCallerCustomize ipc)
    {
        _logger = logger;
        _cache = cache;
        _ipc = ipc;
    }

    private CustomizeProfile FinalProfile => _cache.FinalProfile;

    /// <summary> Adds KVP <paramref name="key"/> - <paramref name="profile"/> to the Cache. </summary>
    public bool TryAddToCache(CombinedCacheKey key, CustomizeProfile profile)
        => _cache.Addprofile(key, profile);

    /// <summary> Removes the <paramref name="key"/> from the Cache. </summary>
    public bool TryRemoveFromCache(CombinedCacheKey key)
        => _cache.Removeprofile(key);

    /// <summary> Clears the Caches contents and updates the visuals after. </summary>
    public async Task ClearCache()
    {
        _logger.LogDebug("Clearing C+ Cache.");
        _cache.ClearCache();
        await UpdateProfileCache();
    }

    public async Task UpdateProfileCache()
    {
        var prevprofile = FinalProfile;
        if (_cache.UpdateFinalCache())
            _logger.LogDebug($"Final C+ Profile updated to [{FinalProfile.ProfileName}] with Priority {FinalProfile.Priority}.", LoggerType.VisualCache);
        else
            _logger.LogTrace("No change in Final C+ Profile.", LoggerType.VisualCache);

        // If the profile changed, apply the profile cache.
        if (!FinalProfile.Equals(prevprofile))
        {
            _logger.LogDebug("Ensuring C+ is locked in the correct state.", LoggerType.VisualCache);
            await ApplyProfileCache();
        }
    }

    public void EnsureRestrictedProfile()
    {
        if (!IpcCallerCustomize.APIAvailable)
            return;

        if (FinalProfile.Equals(CustomizeProfile.Empty))
            return;

        // Grab the active profile
        var active = _ipc.CurrentActiveProfile();

        // Ensure the item is staying enabled.
        if (active.ProfileGuid != FinalProfile.ProfileGuid)
        {
            _logger.LogTrace($"C+ Profile [{FinalProfile.ProfileName}] found in Cache! Reapplying to enforce helplessness!", LoggerType.IpcCustomize);
            _ipc.SetProfileEnable(FinalProfile.ProfileGuid);
        }
    }

    public Task ApplyProfileCache()
    {
        if (FinalProfile.Equals(CustomizeProfile.Empty))
        {
            if (_ipc.CurrentActiveProfile() is { } activeProfile && activeProfile.ProfileGuid != Guid.Empty)
                _ipc.SetProfileDisable(activeProfile.ProfileGuid);
            return Task.CompletedTask;
        }
        else
        {
            _logger.LogDebug($"Applying C+ Cache ([{FinalProfile.ProfileName} - Priority {FinalProfile.Priority}]", LoggerType.IpcGlamourer);
            EnsureRestrictedProfile();
            return Task.CompletedTask;
        }
    }
}
