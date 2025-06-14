using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Widgets;
using GagSpeak.State.Components;
using GagSpeak.State;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace GagSpeak.State.Caches;

/// <summary>
///     Stores the expected glamour and metadata restrictions that should be applied.
///     Also Stores the latest unbound state for restoring slots no longer restrained after removal.
/// </summary>
public class GlamourCache
{
    private readonly ILogger<GlamourCache> _logger;
    public GlamourCache(ILogger<GlamourCache> logger)
    {
        _logger = logger;
    }

    private readonly SortedList<(CombinedCacheKey, EquipSlot), GlamourSlot> _glamours = new();
    private readonly Dictionary<MetaIndex, SortedList<CombinedCacheKey, OptionalBool>> _metaStates = new()
    {
        { MetaIndex.HatState, new SortedList<CombinedCacheKey, OptionalBool>() },
        { MetaIndex.VisorState, new SortedList<CombinedCacheKey, OptionalBool>() },
        { MetaIndex.WeaponState, new SortedList<CombinedCacheKey, OptionalBool>() },
    };

    private Dictionary<EquipSlot, GlamourSlot> _finalGlamour       = new();
    private MetaDataStruct                     _finalMeta          = MetaDataStruct.Empty;
    private GlamourActorState                  _latestUnboundState = GlamourActorState.Empty;

    public IReadOnlyDictionary<EquipSlot, GlamourSlot> FinalGlamour => _finalGlamour;
    public MetaDataStruct FinalMeta => _finalMeta;
    public GlamourActorState LastUnboundState => _latestUnboundState;


    public void CacheUnboundState(GlamourActorState state)
    {
        _latestUnboundState = state;
        _logger.LogDebug($"Unbound Glamour Actor State Cached Successfully!");
    }


    #region GlamourCache Functions
    /// <summary>Applies a <paramref name="glamour"/> with <paramref name="combinedKey"/> to <see cref="_glamours"/> Cache.</summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalGlamour"/></b></remarks>
    public void AddGlamour(CombinedCacheKey combinedKey, GlamourSlot glamour)
        => AddGlamour(combinedKey, [glamour]);

    /// <summary>Applies multiple <paramref name="glamours"/> with <paramref name="combinedKey"/> to <see cref="_glamours"/> Cache.</summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalGlamour"/></b></remarks>
    private void AddGlamour(CombinedCacheKey combinedKey, IEnumerable<GlamourSlot> glamours)
    {
        if (_glamours.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add GlamourSlot to cache at key [{combinedKey}], it already exists!");
            return;
        }

        foreach (var item in glamours)
        {
            _logger.LogDebug($"Adding GlamourCache key ([{combinedKey}] - [{item.Slot}]) with vaue [{item.GameItem.Name}]");
            _glamours.Add((combinedKey, item.Slot), item);
        }
    }

    /// <summary>
    ///     Applies a <paramref name="glamour"/> with the <paramref name="combinedKey"/> to 
    ///     <see cref="_glamours"/> Cache, then updates the <see cref="_finalGlamour"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateGlamour(CombinedCacheKey combinedKey, GlamourSlot glamour)
        => AddAndUpdateGlamour(combinedKey, [glamour]);

    /// <summary>
    ///     Applies list of <paramref name="glamours"/> with <paramref name="combinedKey"/> to
    ///     <see cref="_glamours"/> Cache, then updates the <see cref="_finalGlamour"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    public bool AddAndUpdateGlamour(CombinedCacheKey combinedKey, IEnumerable<GlamourSlot> glamours)
    {
        if (_glamours.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add GlamourSlot to cache at key [{combinedKey}], it already exists!");
            return false;
        }

        AddGlamour(combinedKey, glamours);
        return UpdateFinalGlamourCache();
    }

    /// <summary> Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/></summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalGlamour"/></b></remarks>
    public void RemoveGlamour(CombinedCacheKey combinedKey)
        => RemoveGlamour([combinedKey]);

    /// <summary> Removes all <see cref="CombinedCacheKey"/>'s using any of the <paramref name="combinedKeys"/></summary>
    /// <remarks><b>THIS DOES NOT UPDATE <see cref="_finalGlamour"/></b></remarks>
    public void RemoveGlamour(List<CombinedCacheKey> combinedKeys)
    {
        var keys = _glamours.Keys.Where(k => combinedKeys.Contains(k.Item1)).ToList();
        if(!keys.Any())
        {
            _logger.LogWarning($"None of the CombinedKeys were found in the GlamourCache!");
            return;
        }

        // Remove all glamours for the combined key.
        foreach (var key in keys)
        {
            _logger.LogDebug($"Removing GlamourCache key ([{key.Item1}] - [{key.Item2}])");
            _glamours.Remove(key);
        }
    }

    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/>, 
    ///     then updates the <see cref="_finalGlamour"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed slots from the operation are pass to <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateGlamour(CombinedCacheKey combinedKey, out List<EquipSlot> removed)
        => RemoveAndUpdateGlamour([combinedKey], out removed);


    /// <summary> 
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/>, 
    ///     then updates the <see cref="_finalGlamour"/> Cache.
    /// </summary>
    /// <returns> True if any change occured, false otherwise. </returns>
    /// <remarks> The removed slots from the operation are pass to <paramref name="removed"/></remarks>
    public bool RemoveAndUpdateGlamour(List<CombinedCacheKey> combinedKeys, out List<EquipSlot> removed)
    {
        var prevFinalKeys = _finalGlamour.Keys.ToList();
        // Remove all glamours for the combined keys.
        RemoveGlamour(combinedKeys);

        var changes = UpdateFinalGlamourCache();
        removed = prevFinalKeys.Except(_finalGlamour.Keys).ToList();
        return changes;
    }

    #endregion GlamourCache Functions

    #region MetaCache Functions
    /// <summary> 
    ///     Adds an entry to <see cref="_metaStates"/>'s <paramref name="metaIdx"/> Cache, with the key <paramref name="combinedKey"/>
    ///     and <see cref="OptionalBool"/> value <paramref name="value"/>, then updates the Final Cache.
    /// </summary>
    public void AddMeta(CombinedCacheKey key, MetaIndex metaIdx, OptionalBool value)
    {
        if(metaIdx is MetaIndex.Wetness || value.Equals(OptionalBool.Null))
            return;

        if (!_metaStates[metaIdx].TryAdd(key, value))
        {
            _logger.LogWarning($"Can't set MetaState to cache at key [{key}], it already exists!");
            return;
        }
    }

    /// <summary> 
    ///     Adds <paramref name="meta"/>'s <see cref="OptionalBool"/>'s to the <see cref="_metaStates"/> Cache,
    ///     with key <paramref name="combinedKey"/>, then updates the final MetaCache.
    /// </summary>
    public void AddMeta(CombinedCacheKey combinedKey, MetaDataStruct meta)
    {
        if (!meta.Headgear.Equals(OptionalBool.Null))
            _metaStates[MetaIndex.HatState].TryAdd(combinedKey, meta.Headgear);

        if (!meta.Visor.Equals(OptionalBool.Null))
            _metaStates[MetaIndex.VisorState].TryAdd(combinedKey, meta.Visor);

        if (!meta.Weapon.Equals(OptionalBool.Null))
            _metaStates[MetaIndex.WeaponState].TryAdd(combinedKey, meta.Weapon);
    }

    /// <summary> 
    ///     Adds an entry to <see cref="_metaStates"/>'s <paramref name="metaIdx"/> Cache, with the key <paramref name="combinedKey"/>
    ///     and <see cref="OptionalBool"/> value <paramref name="value"/>, then updates the Final Cache.
    /// </summary>
    /// <returns> True if any changes occured, false otherwise. </returns>
    public bool AddAndUpdateMeta(CombinedCacheKey combinedKey, MetaIndex metaIdx, OptionalBool value)
    {
        if (metaIdx is MetaIndex.Wetness || value.Equals(OptionalBool.Null))
            return false;

        AddMeta(combinedKey, metaIdx, value);
        return _finalMeta.SetMeta(metaIdx, _metaStates[metaIdx].Values.FirstOrDefault());
    }

    /// <summary> 
    ///     Adds <paramref name="meta"/>'s <see cref="OptionalBool"/>'s to the <see cref="_metaStates"/> Cache,
    ///     with key <paramref name="combinedKey"/>, then updates the final MetaCache.
    /// </summary>
    /// <returns> True if any changes occured, false otherwise. </returns>
    public bool AddAndUpdateMeta(CombinedCacheKey combinedKey, MetaDataStruct meta)
    {
        AddMeta(combinedKey, meta);

        var changed = false;
        changed |= _finalMeta.SetMeta(MetaIndex.HatState, _metaStates[MetaIndex.HatState].Values.FirstOrDefault());
        changed |= _finalMeta.SetMeta(MetaIndex.VisorState, _metaStates[MetaIndex.VisorState].Values.FirstOrDefault());
        changed |= _finalMeta.SetMeta(MetaIndex.WeaponState, _metaStates[MetaIndex.WeaponState].Values.FirstOrDefault());
        return changed;
    }

    /// <summary> Removes all entries with <see cref="CombinedCacheKey"/>.</summary>
    public void RemoveMeta(CombinedCacheKey key, MetaIndex metaIdx)
    {
        if (metaIdx is MetaIndex.Wetness)
            return;

        if (!_metaStates[metaIdx].Remove(key))
            _logger.LogWarning($"Can't remove [{key}] from the [{metaIdx}] MetaStateCache, it does not exist!");
    }

    /// <summary> Removes all entries with <see cref="CombinedCacheKey"/>.</summary>
    public void RemoveMeta(List<CombinedCacheKey> combinedKeys)
        => combinedKeys.ForEach(RemoveMeta);

    /// <summary> Removes all entries with <see cref="CombinedCacheKey"/>.</summary>
    public void RemoveMeta(CombinedCacheKey combinedKey)
    {
        foreach (var metaIdx in _metaStates.Keys)
            if (!_metaStates[metaIdx].Remove(combinedKey))
                _logger.LogWarning($"Can't remove key [{combinedKey}] from the [{metaIdx}] MetaStateCache, it does not exist!");
    }

    /// <summary> Removes all entries with <see cref="CombinedCacheKey"/>, then updates the final MetaCache. </summary>
    /// <returns> True if any changes occured, false otherwise. </returns>
    public bool RemoveAndUpdateMeta(CombinedCacheKey combinedKey)
        => RemoveAndUpdateMeta([combinedKey]);

    /// <summary> Removes all entries with <see cref="CombinedCacheKey"/>, then updates the final MetaCache. </summary>
    /// <returns> True if any changes occured, false otherwise. </returns>
    public bool RemoveAndUpdateMeta(List<CombinedCacheKey> combinedKeys)
    {
        // Remove all glamours for the combined keys.
        combinedKeys.ForEach(RemoveMeta);

        var anyChanges = false;
        // True if any update occured (which it always will in this case)
        anyChanges |= _finalMeta.SetMeta(MetaIndex.HatState, _metaStates[MetaIndex.HatState].Values.FirstOrDefault());
        anyChanges |= _finalMeta.SetMeta(MetaIndex.VisorState, _metaStates[MetaIndex.VisorState].Values.FirstOrDefault());
        anyChanges |= _finalMeta.SetMeta(MetaIndex.WeaponState, _metaStates[MetaIndex.WeaponState].Values.FirstOrDefault());
        return anyChanges;
    }

    #endregion MetaCache Functions

    private bool UpdateFinalGlamourCache()
    {
        var anyChanges = false;
        var seenSlots = new HashSet<EquipSlot>();

        // Cycle through the glamours in the order they are sorted in.
        foreach (var glamItem in _glamours.Values)
        {
            var slot = glamItem.Slot;

            if (!seenSlots.Add(slot))
                continue;

            if(TryUpdateFinalWithItem(glamItem))
                anyChanges |= true;
        }

        var removedSlots = _finalGlamour.Keys.Except(seenSlots).ToList();
        foreach (var slot in removedSlots)
        {
            _finalGlamour.Remove(slot);
            anyChanges |= true;
        }

        return anyChanges;
    }

    private bool TryUpdateFinalWithItem(GlamourSlot newGlamItem)
    {
        if (!_finalGlamour.TryGetValue(newGlamItem.Slot, out var curr) || !curr.Equals(newGlamItem))
        {
            _finalGlamour[newGlamItem.Slot] = newGlamItem;
            return true;
        }
        return false;
    }

    #region DebugHelper
    public void DrawCacheTable(TextureService textures)
    {
        using var display = ImRaii.Group();


        var iconSize = new Vector2(ImGui.GetFrameHeight());
        using (var node = ImRaii.TreeNode("IndividualGlamRows"))
        {
            if (node)
            {
                using (var table = ImRaii.Table("GlamourCache", 11, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
                {
                    if (!table)
                        return;

                    ImGui.TableSetupColumn("Combined Key");
                    ImGui.TableSetupColumn("HeadSlot");
                    ImGui.TableSetupColumn("Chestpiece");
                    ImGui.TableSetupColumn("Gloves");
                    ImGui.TableSetupColumn("Legs");
                    ImGui.TableSetupColumn("Feet");
                    ImGui.TableSetupColumn("Earring");
                    ImGui.TableSetupColumn("Necklace");
                    ImGui.TableSetupColumn("Bracelet");
                    ImGui.TableSetupColumn("LeftRing");
                    ImGui.TableSetupColumn("RightRing");
                    ImGui.TableHeadersRow();

                    var grouped = _glamours.GroupBy(kvp => kvp.Key.Item1);

                    foreach (var group in grouped)
                    {
                        ImGuiUtil.DrawFrameColumn($"{group.Key.Manager} / {group.Key.LayerIndex}");

                        var slotMap = group.ToDictionary(kvp => kvp.Key.Item2, kvp => kvp.Value);
                        foreach (var slot in EquipSlotExtensions.EqdpSlots)
                        {
                            ImGui.TableNextColumn();
                            if (slotMap.TryGetValue(slot, out var glam))
                                glam.GameItem.DrawIcon(textures, iconSize, slot);
                            else
                                ItemService.NothingItem(slot).DrawIcon(textures, iconSize, slot);
                        }
                    }
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Final Glamour State:");
        using (var table = ImRaii.Table("GlamourCacheFinal", 11, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;

            ImGui.TableSetupColumn("Combined Key");
            ImGui.TableSetupColumn("HeadSlot");
            ImGui.TableSetupColumn("Chestpiece");
            ImGui.TableSetupColumn("Gloves");
            ImGui.TableSetupColumn("Legs");
            ImGui.TableSetupColumn("Feet");
            ImGui.TableSetupColumn("Earring");
            ImGui.TableSetupColumn("Necklace");
            ImGui.TableSetupColumn("Bracelet");
            ImGui.TableSetupColumn("LeftRing");
            ImGui.TableSetupColumn("RightRing");
            ImGui.TableHeadersRow();
            // Draw the final State.
            ImGuiUtil.DrawFrameColumn("Final State");
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                ImGui.TableNextColumn();
                if (_finalGlamour.TryGetValue(slot, out var glamour))
                    glamour.GameItem.DrawIcon(textures, iconSize, slot);
                else
                    ItemService.NothingItem(slot).DrawIcon(textures, iconSize, slot);
            }
        }

        // split into 3 columns.
        ImGui.Separator();
        using (var node = ImRaii.TreeNode($"Cached MetaData Rows"))
            if(node)
            {
                var columnWidth = ImGui.GetContentRegionAvail().X / 3f;
                ImGui.Columns(3, "MetaDataColumns", true);
                ImGui.SetColumnWidth(0, columnWidth);
                DrawMetaTableRows(MetaIndex.HatState);
                ImGui.NextColumn();
                DrawMetaTableRows(MetaIndex.VisorState);
                ImGui.NextColumn();
                DrawMetaTableRows(MetaIndex.WeaponState);
                // Reset the columns to 1.
                ImGui.Columns(1);
            }

        ImGui.Separator();
        CkGui.ColorText("Final State:", ImGuiColors.ParsedGold);
        CkGui.TextInline("Headgear", false);
        CkGui.ColorTextInline(_finalMeta.Headgear.ToString(), CkColor.LushPinkLine.Uint());
        CkGui.TextInline("Visor", false);
        CkGui.ColorTextInline(_finalMeta.Visor.ToString(), CkColor.LushPinkLine.Uint());
        CkGui.TextInline("Weapon", false);
        CkGui.ColorTextInline(_finalMeta.Weapon.ToString(), CkColor.LushPinkLine.Uint());
    }

    private void DrawMetaTableRows(MetaIndex idx)
    {
        using var table = ImRaii.Table($"{idx.ToString()} RowsTable", 2, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg);
        
        if (!table)
            return;

        ImGui.TableSetupColumn("##Blended-Key");
        ImGui.TableSetupColumn($"{idx.ToString()} State");
        ImGui.TableHeadersRow();

        foreach (var (key, metaState) in _metaStates[idx])
        {
            ImGuiUtil.DrawFrameColumn($"{key.Manager} (Layer {key.LayerIndex})");

            ImGui.TableNextColumn();
            var (text, col) = metaState.Value switch
            {
                true => ("On", ImGuiColors.HealerGreen),
                false => ("Off", ImGuiColors.DalamudRed),
                _ => ("Untouched", ImGuiColors.DalamudGrey),
            };
            CkGui.ColorText(text, col);
        }
    }

    #endregion DebugHelper
}
