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

    private static List<CustomizeProfile> _cPlusProfileList = new();

    private SortedList<CombinedCacheKey, CustomizeProfile> _profiles = new();
    private CustomizeProfile _finalProfile = CustomizeProfile.Empty;

    public static IReadOnlyList<CustomizeProfile> CPlusProfileList => _cPlusProfileList;
    public CustomizeProfile FinalProfile => _finalProfile;

    /// <summary>
    ///     Updates the stored C+ Profile list from C+ locally in our cache.
    /// </summary>
    public void UpdateIpcProfileList(IEnumerable<CustomizeProfile> profiles)
    {
        _cPlusProfileList = profiles.ToList();
        _logger.LogDebug($"Updated C+ Profile List to C+ Cache with [{_cPlusProfileList.Count}] profiles.");
    }

    /// <summary>
    ///     Adds a <paramref name="profile"/> to the cache with <paramref name="combinedKey"/>
    /// </summary>
    public bool Addprofile(CombinedCacheKey combinedKey, CustomizeProfile profile)
    {
        if (profile.Equals(CustomizeProfile.Empty))
            return false;

        if (!_profiles.TryAdd(combinedKey, profile))
        {
            _logger.LogWarning($"KeyValuePair ([{combinedKey}]) already exists in the Cache!");
            return false;
        }
        else
        {
            _logger.LogDebug($"Added ([{combinedKey}] <-> [{profile.ProfileName} Priority {profile.Priority}]) to Cache.");
            return true;
        }
    }

    /// <summary>
    ///     Removes the <paramref name="combinedKey"/> from the cache.
    /// </summary>
    public bool Removeprofile(CombinedCacheKey combinedKey)
    {
        if (_profiles.Remove(combinedKey, out var profile))
        {
            _logger.LogDebug($"Removed C+Profile [{profile.ProfileName}] from cache at key [{combinedKey}].");
            return true;
        }
        else
        {
            _logger.LogWarning($"ProfileCache key ([{combinedKey}]) not found in the profileCache!");
            return false;
        }
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCache()
        => _profiles.Clear();

    /// <summary>
    ///     Updates the final profile cache to the first profile in the cache.
    /// </summary>
    /// <returns> If the profile Changed. </returns>
    public bool UpdateFinalCache()
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
