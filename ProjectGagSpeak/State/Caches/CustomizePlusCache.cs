using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagspeakAPI.Data.Struct;
using OtterGui;

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

        // try and grab the profile from the profile list first, and if not present, add the stored one.
        var toAdd = _cPlusProfileList.FirstOrDefault(x => x.ProfileGuid == profile.ProfileGuid);
        if (!toAdd.Equals(CustomizeProfile.Empty))
            profile = toAdd;


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
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("Individual C+ Listings"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("CPlusCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Name");
                    ImGui.TableSetupColumn("Priority");
                    ImGui.TableSetupColumn("ID");
                    ImGui.TableHeadersRow();

                    foreach (var (key, value) in _profiles)
                    {
                        ImGuiUtil.DrawFrameColumn($"{key.Manager} / {key.LayerIndex}");
                        ImGuiUtil.DrawFrameColumn(string.IsNullOrEmpty(value.ProfileName) ? "<No Label Set>" : value.ProfileName);
                        ImGuiUtil.DrawFrameColumn(value.Priority.ToString());
                        ImGuiUtil.DrawFrameColumn(value.ProfileGuid.ToString());
                    }
                }
            }
        }

        CkGui.ColorText("Final Profile Cache", ImGuiColors.DalamudRed);
        ImGui.Separator();
        using (var table = ImRaii.Table("FinalProfileCache", 4, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;

            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Priority");
            ImGui.TableSetupColumn("ID");
            ImGui.TableHeadersRow();

            var final = _finalProfile;

            if (final.ProfileGuid != Guid.Empty)
            {
                ImGuiUtil.DrawFrameColumn(string.IsNullOrEmpty(final.ProfileName) ? "<No Label Set>" : final.ProfileName);
                ImGuiUtil.DrawFrameColumn(final.Priority.ToString());
                ImGuiUtil.DrawFrameColumn(final.ProfileGuid.ToString());
            }
        }
    }
    #endregion Debug Helper
}
