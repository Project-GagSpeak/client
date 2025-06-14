using GagspeakAPI.Data.Struct;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected restrainted customize profile in a cache.
/// </summary>
public sealed class CustomizePlusCache
{
    private readonly ILogger<CustomizePlusCache> _logger;
    public CustomizePlusCache(ILogger<CustomizePlusCache> logger)
    {
        _logger = logger;
    }

    private List<CustomizeProfile> _cPlusProfileList = new();

    private SortedList<CombinedCacheKey, CustomizeProfile> _profiles = new();
    private CustomizeProfile _finalProfile = CustomizeProfile.Empty;

    public IReadOnlyList<CustomizeProfile> CPlusProfileList => _cPlusProfileList;
    public CustomizeProfile FinalProfile => _finalProfile;

    /// <summary>
    ///     Updates the stored C+ Profile list from C+ locally in our cache.
    /// </summary>
    public void UpdateIpcProfileList(IEnumerable<CustomizeProfile> profiles)
    {
        _cPlusProfileList = profiles.ToList();
        _logger.LogDebug($"Updated C+ Profile List to C+ Cache with [{_cPlusProfileList.Count}] profiles.");
    }


    /// <summary> Adds a <paramref name="profile"/> to the cache with <paramref name="combinedKey"/></summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalprofiles"/></b></remarks>
    public void Addprofile(CombinedCacheKey combinedKey, CustomizeProfile profile)
    {
        if (profile.Equals(CustomizeProfile.Empty))
            return;

        if (!_profiles.TryAdd(combinedKey, profile))
            _logger.LogWarning($"KeyValuePair ([{combinedKey}]) already exists in the Cache!");
        else
            _logger.LogDebug($"Added ([{combinedKey}] <-> [{profile.ProfileName} Priority {profile.Priority}]) to Cache.");
    }

    /// <summary> Adds a <paramref name="profile"/> to the cache with <paramref name="combinedKey"/>, updating <see cref="_finalProfile"/></summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateprofile(CombinedCacheKey combinedKey, CustomizeProfile profile)
    {
        if (profile.Equals(CustomizeProfile.Empty))
            return false;

        // Add the profile at the key.
        Addprofile(combinedKey, profile);
        return UpdateFinalCache();
    }

    /// <summary> Removes the <paramref name="combinedKey"/> from the cache. </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalprofiles"/></b></remarks>
    public void Removeprofile(CombinedCacheKey combinedKey)
    {
        if (_profiles.Remove(combinedKey, out var profile))
            _logger.LogDebug($"Removed C+Profile [{profile.ProfileName}] from cache at key [{combinedKey}].");
        else
            _logger.LogWarning($"GlamourCache key ([{combinedKey}]) not found in the profileCache!");
    }

    /// <summary> Removes the <paramref name="combinedKey"/> from the main cache, then updates the <see cref="_finalProfile"/></summary>
    /// <returns> True if any change occured in <see cref="_finalProfile"/>, false otherwise. </returns>
    /// <remarks> The removed profiles Id's are collected in <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateprofile(CombinedCacheKey combinedKey, out CustomizeProfile removed)
    {
        var prevprofile = _finalProfile;
        Removeprofile(combinedKey);

        var changes = UpdateFinalCache();
        removed = _finalProfile.Equals(prevprofile) ? CustomizeProfile.Empty : prevprofile;
        return changes;
    }

    private bool UpdateFinalCache()
    {
        var newFinal = _profiles.Values.FirstOrDefault();
        var anyChange = !newFinal.Equals(_finalProfile);
        _finalProfile = newFinal;
        return anyChange;
    }

    #region DebugHelper
    public void DrawCacheTable()
    { }
    #endregion Debug Helper
}
