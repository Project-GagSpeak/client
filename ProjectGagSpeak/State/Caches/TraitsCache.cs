using Dalamud.Interface.Utility.Raii;
using CkCommons;
using GagspeakAPI.Attributes;
using Dalamud.Bindings.ImGui;
using OtterGui;
using System.Collections.Immutable;
using CkCommons.Gui;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected traits while restricted.
/// </summary>
public class TraitsCache
{
    private readonly ILogger<TraitsCache> _logger;
    public TraitsCache(ILogger<TraitsCache> logger)
    {
        _logger = logger;
    }

    /// <summary> The stored traits for every active gag, restriction, or restraint set. </summary>
    private SortedList<CombinedCacheKey, Traits> _traits = new();
    private Traits _finalTraits = Traits.None;

    public Traits FinalTraits => _finalTraits;

    public string GetSourceName(Traits trait)
    {
        if (_traits.FirstOrDefault(kvp => (kvp.Value & trait) != Traits.None) is { } match)
            return match.Key.Label;
        return "Unknown Source";
    }

    /// <summary>
    ///     Adds the given <paramref name="traits"/> to the cache for the specified <paramref name="key"/>.
    /// </summary>
    /// <returns> If the traits were successfully added. </returns>
    public bool AddTraits(CombinedCacheKey key, Traits traits)
    {
        if (!_traits.TryAdd(key, traits))
        {
            _logger.LogWarning($"Attempted to add traits for {key} but it already exists.");
            return false;
        }

        _logger.LogDebug($"[{key}] added traits [{traits}].");
        return true;
    }

    /// <summary>
    ///     Removes the traits for the given <paramref name="key"/> from the cache.
    /// </summary>
    /// <returns> If the traits were successfully removed. </returns>
    public bool RemoveTraits(CombinedCacheKey key)
    {
        if (!_traits.Remove(key, out var traits))
        {
            _logger.LogWarning($"Failed to remove traits at [{key}]");
            return false;
        }

        _logger.LogDebug($"[{key}] removed traits [{traits}].");
        return true;
    }

    /// <summary>
    ///     Updates the _finalTraits based on the current _traits by aggregating all traits
    /// </summary>
    /// <returns> If the cache changed at all. </returns>
    public bool UpdateFinalCache()
    {
        var aggregatedTraits = Traits.None;
        // Aggregate all traits from the _traits dictionary.
        foreach (var kvp in _traits)
            aggregatedTraits |= kvp.Value;

        // Log and return if there was a change.
        _logger.LogDebug($"Final traits updated from [{_finalTraits}] to [{aggregatedTraits}].");
        bool changed = aggregatedTraits != _finalTraits;
        // set the new finalTraits.
        _finalTraits = aggregatedTraits;
        return changed;
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCache()
        => _traits.Clear();

    #region DebugHelper
    public void DrawCacheTable()
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("IndividualTraitsRows"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("TraitsCache", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("Traits Included");
                    ImGui.TableHeadersRow();

                    foreach (var group in _traits)
                    {
                        ImGuiUtil.DrawFrameColumn($"{group.Key.Manager} / {group.Key.LayerIndex}");
                        ImGui.TableNextColumn();
                        CkGui.ColorText(group.Value.ToString(), CkColor.LushPinkButton.Uint());
                    }
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Final State");
        CkGui.ColorTextInline(_finalTraits.ToString(), CkColor.LushPinkButton.Uint());
    }
    #endregion Debug Helper
}
