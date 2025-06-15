using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagspeakAPI.Attributes;
using ImGuiNET;
using OtterGui;
using System.Collections.Immutable;

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

    /// <summary> The currently banned actions determined by <see cref="_finalTraits"/></summary>
    private ImmutableDictionary<uint, Traits> _bannedActions = ImmutableDictionary<uint, Traits>.Empty;

    /// <summary> The stored traits for every active gag, restriction, or restraint set. </summary>
    private SortedList<CombinedCacheKey, Traits> _traits = new();
    private Traits _finalTraits = Traits.None;

    // Public readonly Accessors
    public IReadOnlyDictionary<uint, Traits> BannedActions => _bannedActions;
    public Traits FinalTraits => _finalTraits;


    // Helper methods.
    public void UpdateBannedActions(IImmutableDictionary<uint, Traits> bannedActions)
    {
        _bannedActions = bannedActions.ToImmutableDictionary();
        _logger.LogDebug($"Banned actions updated. Count: {_bannedActions.Count}");
    }

    public void Addtraits(CombinedCacheKey combinedKey, Traits traits)
    {
        if (!_traits.TryAdd(combinedKey, traits))
        {
            _logger.LogWarning($"Attempted to add traits for {combinedKey} but it already exists.");
            return;
        }
        _logger.LogDebug($"[{combinedKey}] added traits [{traits}].");
    }

    public bool AddAndUpdatetraits(CombinedCacheKey combinedKey, Traits traits)
    {
        // Add them and stuff.
        Addtraits(combinedKey, traits);

        // return false if things were not added.
        if (_traits.ContainsKey(combinedKey))
            return false;

        return UpdateFinalCache();
    }

    public void Removetraits(CombinedCacheKey combinedKey)
    {
        if (!_traits.Remove(combinedKey, out var traits))
        {
            _logger.LogWarning($"Failed to remove traits at [{combinedKey}]");
            return;
        }

        _logger.LogDebug($"[{combinedKey}] removed traits [{traits}].");
    }

    public bool RemoveAndUpdatetraits(CombinedCacheKey combinedKey, out Traits removed)
    {
        var prevTraits = _finalTraits;

        // Remove the traits for the combined keys..
        Removetraits(combinedKey);

        var changed = UpdateFinalCache();
        // perform flag enum expression logic to get the resulting traits that is the previous traits not in _finalTraits.
        removed = prevTraits & ~_finalTraits;
        return changed;
    }

    /// <summary>
    ///     Updates the _finalTraits based on the current _traits by aggregating all traits
    /// </summary>
    /// <returns> If the cache changed at all. </returns>
    private bool UpdateFinalCache()
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
