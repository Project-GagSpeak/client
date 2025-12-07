using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using OtterGui;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected moodles applied while restricted.
/// </summary>
public class MoodleCache
{
    private readonly ILogger<MoodleCache> _logger;
    public MoodleCache(ILogger<MoodleCache> logger)
    {
        _logger = logger;
    }

    private static CharaMoodleData _ipcData = new CharaMoodleData();

    private SortedList<(CombinedCacheKey, Guid), Moodle> _moodles = new();
    private HashSet<Moodle> _finalMoodleItems = new();
    private HashSet<Guid> _finalStatusIds = new();

    /// <summary>
    ///     The current stored IPCData for the clients Moodles Data.
    /// </summary>
    public static CharaMoodleData IpcData => _ipcData;


    /// <summary>
    ///     The combined Moodle collection from the full Moodle Cache.
    /// </summary>
    public IReadOnlySet<Moodle> FinalMoodleItems => _finalMoodleItems;

    /// <summary>
    ///     The Moodle Status GUID's extracted from <see cref="_finalMoodleItems"/> status/preset ambiguity.
    /// </summary>
    public IReadOnlySet<Guid> FinalStatusIds => _finalStatusIds;


    /// <summary> Applies a <paramref name="moodle"/> with <paramref name="key"/> to the Moodles Cache. </summary>

    public bool AddMoodle(CombinedCacheKey key, Moodle moodle)
        => AddMoodle(key, [moodle]);

    /// <summary> Applies many <paramref name="moodles"/> with <paramref name="key"/> to the Moodles Cache. </summary>
    public bool AddMoodle(CombinedCacheKey key, IEnumerable<Moodle> moodles)
    {
        if (_moodles.Keys.Any(keys => keys.Item1.Equals(key)))
        {
            _logger.LogWarning($"Cannot add [{key}] to Cache, the Key already exists!", LoggerType.VisualCache);
            return false;
        }

        bool added = false;
        foreach (var item in moodles)
        {
            added |= _moodles.TryAdd((key, item.Id), item);
            if (added) _logger.LogDebug($"Added KeyValuePair ([{key}] - [{item.Id}]) to Cache.", LoggerType.VisualCache);
        }
        return added;
    }

    public bool UpdateMoodle(CombinedCacheKey key, Moodle newMoodle)
    {
        var originalKey = _moodles.Keys.FirstOrDefault(k => k.Item1.Equals(key));
        if (!_moodles.ContainsKey(originalKey))
        {
            _logger.LogWarning($"Cannot update Moodle with Key [{key}], it does not exist in the Cache!", LoggerType.VisualCache);
            return false;
        }
        // Remove the old entry
        _moodles.Remove(originalKey);
        // Add the new entry with the same CombinedCacheKey and new moodle's Id
        _moodles.Add((originalKey.Item1, newMoodle.Id), newMoodle);

        _logger.LogDebug($"Updated Moodle with Key [{key}] to new Moodle Id [{newMoodle.Id}]", LoggerType.VisualCache);
        return true;
    }

    /// <summary> Removes the <paramref name="key"/> from the Moodles Cache. </summary>
    public bool RemoveMoodle(CombinedCacheKey key)
        => RemoveMoodle([key]);

    /// <summary> Removes all <paramref name="keys"/> from the Moodles Cache. </summary>
    public bool RemoveMoodle(IEnumerable<CombinedCacheKey> keys)
    {
        var allKeys = _moodles.Keys.Where(k => keys.Contains(k.Item1)).ToList();
        if (!allKeys.Any())
        {
            _logger.LogWarning($"None of the CombinedKeys were found in the MoodleCache!", LoggerType.VisualCache);
            return false;
        }

        // Remove all glamours for the combined key.
        bool anyRemoved = false;
        foreach (var key in allKeys)
        {
            _logger.LogDebug($"Removing Cache key ([{key.Item1}] - [{key.Item2}])", LoggerType.VisualCache);
            anyRemoved |= _moodles.Remove(key);
        }
        return anyRemoved;
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCache()
        => _moodles.Clear();

    public bool UpdateFinalCache(out IEnumerable<Guid> removedStatusIds)
    {
        var prevIds = _finalStatusIds.ToList();
        var anyChanges = false;
        _finalMoodleItems.Clear();
        _finalStatusIds.Clear();

        // Automatically recalculates the statusIds list based on the moodle type for the moodles added.
        foreach (var moodle in _moodles.Values)
        {
            if (moodle is MoodlePreset p)
            {
                // Continue if all of the ID's are in the seenStatuses.
                if (p.StatusIds.All(_finalStatusIds.Contains))
                    continue;

                _finalMoodleItems.Add(p);
                _finalStatusIds.UnionWith(p.StatusIds);
                anyChanges = true;
            }
            else
            {
                // If we can add the status it means it doesn't exist yet.
                if (_finalStatusIds.Add(moodle.Id))
                {
                    _finalMoodleItems.Add(moodle);
                    anyChanges = true;
                }
            }
        }

        removedStatusIds = prevIds.Except(_finalStatusIds);
        return anyChanges || removedStatusIds.Any();
    }


    #region DebugHelper
    public void DrawCacheTable(TextureService textures, MoodleDrawer drawer)
    {
        using var display = ImRaii.Group();

        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("IndividualMoodleRows"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("MoodlesCache", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("MoodlesDisplay");
                    ImGui.TableHeadersRow();

                    var grouped = _moodles.GroupBy(kvp => kvp.Key.Item1); // Group by CombinedCacheKey

                    foreach (var group in grouped)
                    {
                        ImGuiUtil.DrawFrameColumn($"{group.Key.Manager} / {group.Key.LayerIndex}");

                        // get values.
                        var values = group.Select(kvp => kvp.Value);

                        ImGui.TableNextColumn();
                        drawer.ShowStatusIcons(values, ImGui.GetContentRegionAvail().X);
                    }
                }
            }
        }
        ImGui.Separator();
        ImGui.Text("Final State");
        ImGui.SameLine();
        drawer.DrawIconsOrEmpty(_finalStatusIds, ImGui.GetContentRegionAvail().X, rows: 2);
    }

    #endregion DebugHelper
}
