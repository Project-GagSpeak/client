using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Interop.Helpers;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using OtterGui;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected lociData applied while restricted.
/// </summary>
public class LociCache
{
    private readonly ILogger<LociCache> _logger;
    public LociCache(ILogger<LociCache> logger)
    {
        _logger = logger;
    }

    private static LociContainer _cache = new LociContainer();

    private SortedList<(CombinedCacheKey, Guid), LociItem> _lociData = new();
    private HashSet<LociItem> _finalItems = new();
    private HashSet<Guid> _finalStatusIds = new();

    /// <summary>
    ///     The cached IPC Data reflecting their Loci Information.
    /// </summary>
    public static LociContainer Data => _cache;

    /// <summary>
    ///     The combined LociData collection from the full Cache.
    /// </summary>
    public IReadOnlySet<LociItem> FinalLociItems => _finalItems;

    /// <summary>
    ///     The Loci Status GUID's extracted from <see cref="_finalLociItems"/> status/preset ambiguity.
    /// </summary>
    public IReadOnlySet<Guid> FinalStatusIds => _finalStatusIds;


    /// <summary> Applies a <paramref name="lociItem"/> with <paramref name="key"/> to the Locis Cache. </summary>

    public bool AddLoci(CombinedCacheKey key, LociItem lociItem)
        => AddLoci(key, [lociItem]);

    /// <summary> Applies many <paramref name="lociItems"/> with <paramref name="key"/> to the Locis Cache. </summary>
    public bool AddLoci(CombinedCacheKey key, IEnumerable<LociItem> lociItems)
    {
        if (_lociData.Keys.Any(keys => keys.Item1.Equals(key)))
        {
            _logger.LogWarning($"Cannot add [{key}] to Cache, the Key already exists!", LoggerType.VisualCache);
            return false;
        }

        bool added = false;
        foreach (var item in lociItems)
        {
            added |= _lociData.TryAdd((key, item.Id), item);
            if (added) _logger.LogDebug($"Added KeyValuePair ([{key}] - [{item.Id}]) to Cache.", LoggerType.VisualCache);
        }
        return added;
    }

    public bool UpdateLoci(CombinedCacheKey key, LociItem newItem)
    {
        var originalKey = _lociData.Keys.FirstOrDefault(k => k.Item1.Equals(key));
        if (!_lociData.ContainsKey(originalKey))
        {
            _logger.LogWarning($"Cannot update LociData with Key [{key}], it does not exist in the Cache!", LoggerType.VisualCache);
            return false;
        }
        // Remove the old entry
        _lociData.Remove(originalKey);
        // Add the new entry with the same CombinedCacheKey and new lociItem's Id
        _lociData.Add((originalKey.Item1, newItem.Id), newItem);

        _logger.LogDebug($"Updated LociData with Key [{key}] to new Id [{newItem.Id}]", LoggerType.VisualCache);
        return true;
    }

    /// <summary>
    ///     Removes the <paramref name="key"/> from the Cache.
    /// </summary>
    public bool RemoveLoci(CombinedCacheKey key)
        => RemoveLociData([key]);

    /// <summary>
    ///     Removes all <paramref name="keys"/> from the Cache.
    /// </summary>
    public bool RemoveLociData(IEnumerable<CombinedCacheKey> keys)
    {
        var allKeys = _lociData.Keys.Where(k => keys.Contains(k.Item1)).ToList();
        if (!allKeys.Any())
        {
            _logger.LogWarning($"None of the CombinedKeys were found in the LociCache!", LoggerType.VisualCache);
            return false;
        }

        bool anyRemoved = false;
        foreach (var key in allKeys)
        {
            _logger.LogDebug($"Removing Cache key ([{key.Item1}] - [{key.Item2}])", LoggerType.VisualCache);
            anyRemoved |= _lociData.Remove(key);
        }
        return anyRemoved;
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCache()
        => _lociData.Clear();

    public bool UpdateFinalCache(out IEnumerable<Guid> removedIds)
    {
        var prevIds = _finalStatusIds.ToList();
        var anyChanges = false;
        _finalItems.Clear();
        _finalStatusIds.Clear();

        // Automatically recalculates the statusIds list based on the lociItem type for the lociItems added.
        foreach (var lociItem in _lociData.Values)
        {
            if (lociItem is LociPreset p)
            {
                // Continue if all of the ID's are in the seenStatuses.
                if (p.StatusIds.All(_finalStatusIds.Contains))
                    continue;

                _finalItems.Add(p);
                _finalStatusIds.UnionWith(p.StatusIds);
                anyChanges = true;
            }
            else
            {
                // If we can add the status it means it doesn't exist yet.
                if (_finalStatusIds.Add(lociItem.Id))
                {
                    _finalItems.Add(lociItem);
                    anyChanges = true;
                }
            }
        }

        removedIds = prevIds.Except(_finalStatusIds);
        return anyChanges || removedIds.Any();
    }


    #region DebugHelper
    public void DrawCacheTable(TextureService textures)
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("IndividualLociRows"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("LocisCache", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("LocisDisplay");
                    ImGui.TableHeadersRow();

                    var grouped = _lociData.GroupBy(kvp => kvp.Key.Item1); // Group by CombinedCacheKey

                    foreach (var group in grouped)
                    {
                        ImGuiUtil.DrawFrameColumn($"{group.Key.Manager} / {group.Key.LayerIndex}");

                        // get values.
                        var values = group.Select(kvp => kvp.Value);

                        ImGui.TableNextColumn();
                        LociDrawer.DrawIcons(values, ImGui.GetContentRegionAvail().X);
                    }
                }
            }
        }
        ImGui.Separator();
        ImGui.Text("Final State");
        ImGui.SameLine();
        LociDrawer.DrawIconsOrEmpty(_finalStatusIds, ImGui.GetContentRegionAvail().X, rows: 2);
    }

    #endregion DebugHelper
}
