using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using CkCommons;
using GagSpeak.Gui;
using CkCommons.Widgets;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using ImGuiNET;
using Lumina.Extensions;
using OtterGui;
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using CkCommons.Gui;

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

    /// <summary>
    ///     Applies a <paramref name="glamour"/> with <paramref name="combinedKey"/> to the Cache.
    /// </summary>
    public bool AddGlamour(CombinedCacheKey combinedKey, GlamourSlot glamour)
        => AddGlamour(combinedKey, [glamour]);

    /// <summary>
    ///     Applies multiple <paramref name="glamours"/> with <paramref name="combinedKey"/> to the Cache.
    /// </summary>
    public bool AddGlamour(CombinedCacheKey combinedKey, IEnumerable<GlamourSlot> glamours)
    {
        if (_glamours.Keys.Any(keys => keys.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning($"Cannot add GlamourSlot to cache at key [{combinedKey}], it already exists!");
            return false;
        }

        foreach (var item in glamours)
        {
            _logger.LogDebug($"Adding GlamourCache key ([{combinedKey}] - [{item.Slot}]) with value [{item.GameItem.Name}]");
            _glamours.TryAdd((combinedKey, item.Slot), item);
        }

        return true;
    }

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using <paramref name="combinedKey"/>
    /// </summary>
    public bool RemoveGlamour(CombinedCacheKey combinedKey)
        => RemoveGlamour([combinedKey]);

    /// <summary>
    ///     Removes all <see cref="CombinedCacheKey"/>'s using any of the <paramref name="combinedKeys"/>
    /// </summary>
    public bool RemoveGlamour(IEnumerable<CombinedCacheKey> combinedKeys)
    {
        var keys = _glamours.Keys.Where(k => combinedKeys.Contains(k.Item1)).ToList();

        if (!keys.Any())
        {
            _logger.LogWarning($"None of the CombinedKeys were found in the GlamourCache!");
            return false;
        }

        foreach (var key in keys)
        {
            _logger.LogDebug($"Removing GlamourCache key ([{key.Item1}] - [{key.Item2}])");
            _glamours.Remove(key);
        }

        return true;
    }

    /// <summary> 
    ///     Adds in the MetaState Cache at the CombinedKey <paramref name="key"/>, 
    ///     The OptionalBool <paramref name="value"/> for the provided <paramref name="metaIdx"/>.
    /// </summary>
    public bool AddMeta(CombinedCacheKey key, MetaIndex metaIdx, OptionalBool value)
    {
        if (metaIdx is MetaIndex.Wetness || value.Equals(OptionalBool.Null))
            return false;

        if (!_metaStates[metaIdx].TryAdd(key, value))
        {
            _logger.LogWarning($"Can't set MetaState to cache at key [{key}], it already exists!");
            return false;
        }

        return true;
    }

    /// <summary> 
    ///     Adds <paramref name="meta"/>'s <see cref="OptionalBool"/>'s to the <see cref="_metaStates"/> Cache,
    ///     with key <paramref name="combinedKey"/>, then updates the final MetaCache.
    /// </summary>
    public bool AddMeta(CombinedCacheKey combinedKey, MetaDataStruct meta)
    {
        bool anyAdded = false;

        if (!meta.Headgear.Equals(OptionalBool.Null))
            anyAdded |= _metaStates[MetaIndex.HatState].TryAdd(combinedKey, meta.Headgear);

        if (!meta.Visor.Equals(OptionalBool.Null))
            anyAdded |= _metaStates[MetaIndex.VisorState].TryAdd(combinedKey, meta.Visor);

        if (!meta.Weapon.Equals(OptionalBool.Null))
            anyAdded |= _metaStates[MetaIndex.WeaponState].TryAdd(combinedKey, meta.Weapon);

        return anyAdded;
    }

    /// <summary>
    ///     Removes all entries for each CombinedCacheKey across all MetaIndex states.
    /// </summary>
    public bool RemoveMeta(IEnumerable<CombinedCacheKey> combinedKeys)
    {
        bool anyRemoved = false;
        foreach (var key in combinedKeys)
            anyRemoved |= RemoveMeta(key);
        return anyRemoved;
    }

    /// <summary>
    ///     Removes all entries with the given CombinedCacheKey across all MetaIndex caches.
    /// </summary>
    public bool RemoveMeta(CombinedCacheKey key)
    {
        bool anyRemoved = false;
        foreach (var (metaIdx, stateList) in _metaStates)
            anyRemoved |= stateList.Remove(key);
        return anyRemoved;
    }

    /// <summary>
    ///     Careful where and how you call this, use responsibly.
    ///     If done poorly, things will go out of sync.
    /// </summary>
    public void ClearCaches()
    {
        _glamours.Clear();
        _metaStates.Clear();
    }

    public bool UpdateFinalGlamourCache(out List<EquipSlot> removedSlots)
    {
        var anyChanges = false;
        var seenSlots = new HashSet<EquipSlot>();

        // Cycle through the glamours in the order they are sorted in.
        foreach (var glamItem in _glamours.Values)
        {
            var slot = glamItem.Slot;

            if (!seenSlots.Add(slot))
                continue;

            if (!_finalGlamour.TryGetValue(glamItem.Slot, out var curr) || !curr.Equals(glamItem))
            {
                _finalGlamour[glamItem.Slot] = glamItem;
                anyChanges |= true;
            }
        }

        removedSlots = _finalGlamour.Keys.Except(seenSlots).ToList();
        foreach (var slot in removedSlots)
        {
            _finalGlamour.Remove(slot);
            anyChanges |= true;
        }

        return anyChanges || removedSlots.Any();
    }

    public bool UpdateFinalMetaCache()
    {
        var anyChanges = false;
        // True if any update occured (which it always will in this case)
        anyChanges |= _finalMeta.SetMeta(MetaIndex.HatState, GetFirstHatState());
        anyChanges |= _finalMeta.SetMeta(MetaIndex.VisorState, GetFirstVisorState());
        anyChanges |= _finalMeta.SetMeta(MetaIndex.WeaponState, GetFirstWeaponState());
        return anyChanges;
    }

    public OptionalBool GetFirstHatState()
    {
        if (_metaStates.TryGetValue(MetaIndex.HatState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return OptionalBool.Null;
    }

    public OptionalBool GetFirstVisorState()
    {
        if (_metaStates.TryGetValue(MetaIndex.VisorState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return OptionalBool.Null;
    }

    public OptionalBool GetFirstWeaponState()
    {
        if (_metaStates.TryGetValue(MetaIndex.WeaponState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return OptionalBool.Null;
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
