using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using ImGuiNET;
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

    private static CharaIPCData _ipcData = new CharaIPCData();

    private SortedList<(CombinedCacheKey, Guid), Moodle> _moodles = new();
    private HashSet<Moodle> _finalMoodleItems = new();
    private HashSet<Guid> _finalStatusIds = new();

    /// <summary>
    ///     The current stored IPCData for the clients Moodles Data.
    /// </summary>
    public static CharaIPCData IpcData => _ipcData;


    /// <summary>
    ///     The combined Moodle collection from the full Moodle Cache.
    /// </summary>
    public IReadOnlySet<Moodle> FinalMoodleItems => _finalMoodleItems;

    /// <summary>
    ///     The Moodle Status GUID's extracted from <see cref="_finalMoodleItems"/> status/preset ambiguity.
    /// </summary>
    public IReadOnlySet<Guid> FinalStatusIds => _finalStatusIds;


    /// <summary>
    ///     Applies a <paramref name="moodle"/> with <paramref name="combinedKey"/> to <see cref="_moodles"/> Cache.
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMoodles"/></b></remarks>
    public void AddMoodle(CombinedCacheKey combinedKey, Moodle moodle)
        => AddMoodle(combinedKey, [moodle]);

    /// <summary>
    ///     Applies all <paramref name="moodles"/> with <paramref name="combinedKey"/> to <see cref="_moodles"/> Cache.
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMoodles"/></b></remarks>
    public void AddMoodle(CombinedCacheKey combinedKey, IEnumerable<Moodle> moodles)
    {
        if (_moodles.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add [{combinedKey}] to Cache, the Key already exists!");
            return;
        }

        foreach (var item in moodles)
        {
            if (_moodles.TryAdd((combinedKey, item.Id), item))
                _logger.LogWarning($"KeyValuePair ([{combinedKey}] - [{item.Id}]) already exists in the Cache!");
            else
                _logger.LogDebug($"Added KeyValuePair ([{combinedKey}] - [{item.Id}]) to Cache.");
        }
    }

    /// <summary>
    ///     Applies a <paramref name="moodle"/> with <paramref name="combinedKey"/> to the <see cref="_moodles"/> Cache,
    ///     then updates <see cref="_finalMoodleItems"/> and <see cref="_finalMoodleIds"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateMoodle(CombinedCacheKey combinedKey, Moodle moodle)
        => AddAndUpdateMoodle(combinedKey, [moodle]);

    /// <summary>
    ///     Applies <paramref name="moodles"/> with <paramref name="combinedKey"/> to the <see cref="_moodles"/> Cache,
    ///     then updates <see cref="_finalMoodleItems"/> and <see cref="_finalMoodleIds"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateMoodle(CombinedCacheKey combinedKey, IEnumerable<Moodle> moodles)
    {
        if (_moodles.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add GlamourSlot to cache at key [{combinedKey}], it already exists!");
            return false;
        }

        AddMoodle(combinedKey, moodles);
        return UpdateFinalCache();
    }

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using any of the <paramref name="combinedKey"/>
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMoodleItems"/></b></remarks>
    public void RemoveMoodle(CombinedCacheKey combinedKey)
        => RemoveMoodle([combinedKey]);

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKeys"/>
    /// </summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalMoodleItems"/></b></remarks>
    public void RemoveMoodle(List<CombinedCacheKey> combinedKeys)
    {
        var keys = _moodles.Keys.Where(k => combinedKeys.Contains(k.Item1)).ToList();
        if (!keys.Any())
        {
            _logger.LogWarning($"None of the CombinedKeys were found in the MoodleCache!");
            return;
        }

        // Remove all glamours for the combined key.
        foreach (var key in keys)
        {
            _logger.LogDebug($"Removing GlamourCache key ([{key.Item1}] - [{key.Item2}])");
            _moodles.Remove(key);
        }
    }

    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/>, 
    ///     then updates the <see cref="_finalMoodleItems"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed moodles Id's are collected in <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateMoodle(CombinedCacheKey combinedKey, out List<Guid> removed)
        => RemoveAndUpdateMoodle([combinedKey], out removed);


    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKeys"/>, 
    ///     then updates the <see cref="_finalMoodleItems"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed moodles Id's are collected in <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateMoodle(List<CombinedCacheKey> combinedKeys, out List<Guid> removed)
    {
        var prevIds = _finalStatusIds.ToList();
        // Remove all glamours for the combined keys.
        RemoveMoodle(combinedKeys);

        var changes = UpdateFinalCache();
        removed = prevIds.Except(_finalStatusIds).ToList();
        return changes;
    }

    private bool UpdateFinalCache()
    {
        var anyChanges = false;
        _finalMoodleItems.Clear();
        _finalStatusIds.Clear();

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
        return anyChanges;
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

        // Draw the final State.
        ImGuiUtil.DrawFrameColumn("Final State");
        ImGui.TableNextColumn();
        drawer.DrawIconsOrEmpty(_finalStatusIds, ImGui.GetContentRegionAvail().X, rows: 2);
    }
    
    #endregion DebugHelper
}
