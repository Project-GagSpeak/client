using CkCommons;
using CkCommons.Classes;
using CkCommons.Gui;
using CkCommons.Widgets;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using GagSpeak.Services;
using GagSpeak.Services.Textures;
using GagSpeak.State.Models;
using OtterGui;
using OtterGui.Extensions;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

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
    private readonly Dictionary<MetaIndex, SortedList<CombinedCacheKey, TriStateBool>> _metaStates = new()
    {
        { MetaIndex.HatState, new SortedList<CombinedCacheKey, TriStateBool>() },
        { MetaIndex.VisorState, new SortedList<CombinedCacheKey, TriStateBool>() },
        { MetaIndex.WeaponState, new SortedList<CombinedCacheKey, TriStateBool>() },
    };
    private Dictionary<EquipSlot, GlamourSlot> _finalGlamour       = new();
    private MetaDataStruct                     _finalMeta          = MetaDataStruct.Empty;
    private GlamourActorState                  _latestUnboundState = GlamourActorState.Empty;

    public IReadOnlyDictionary<EquipSlot, GlamourSlot> FinalGlamour => _finalGlamour;
    public MetaDataStruct FinalMeta => _finalMeta;
    public bool AnyHatMeta => _metaStates.TryGetValue(MetaIndex.HatState, out var sl) && sl.Count > 0;
    public bool AnyVisorMeta => _metaStates.TryGetValue(MetaIndex.VisorState, out var sl) && sl.Count > 0;
    public bool AnyWeaponMeta => _metaStates.TryGetValue(MetaIndex.WeaponState, out var sl) && sl.Count > 0;
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

    public bool UpdateGlamourDyes(CombinedCacheKey combinedKey, EquipSlot slot, StainIds newDyes)
    {
        // if the key is not present, fail it.
        if (!_glamours.TryGetValue((combinedKey, slot), out var glamItem))
        {
            _logger.LogWarning($"Cannot update GlamourSlot dyes at key [{combinedKey}] for slot [{slot}], it does not exist!");
            return false;
        }
        // Update the dyes.
        glamItem.GameStain = newDyes;
        _logger.LogDebug($"GlamourCache ([{combinedKey}] - [{slot}]) updated dyes to [{newDyes.Stain1}, {newDyes.Stain2}]");
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
    ///     The TriStateBool <paramref name="value"/> for the provided <paramref name="metaIdx"/>.
    /// </summary>
    public bool AddMeta(CombinedCacheKey key, MetaIndex metaIdx, TriStateBool value)
    {
        if (metaIdx is MetaIndex.Wetness || value.Equals(TriStateBool.Null))
            return false;

        if (!_metaStates[metaIdx].TryAdd(key, value))
        {
            _logger.LogWarning($"Can't set MetaState to cache at key [{key}], it already exists!");
            return false;
        }

        return true;
    }

    /// <summary> 
    ///     Adds <paramref name="meta"/>'s <see cref="TriStateBool"/>'s to the <see cref="_metaStates"/> Cache,
    ///     with key <paramref name="combinedKey"/>, then updates the final MetaCache.
    /// </summary>
    public bool AddMeta(CombinedCacheKey combinedKey, MetaDataStruct meta)
    {
        var anyAdded = false;

        if (!meta.Headgear.Equals(TriStateBool.Null))
            anyAdded |= _metaStates[MetaIndex.HatState].TryAdd(combinedKey, meta.Headgear);

        if (!meta.Visor.Equals(TriStateBool.Null))
            anyAdded |= _metaStates[MetaIndex.VisorState].TryAdd(combinedKey, meta.Visor);

        if (!meta.Weapon.Equals(TriStateBool.Null))
            anyAdded |= _metaStates[MetaIndex.WeaponState].TryAdd(combinedKey, meta.Weapon);

        return anyAdded;
    }

    /// <summary>
    ///     Removes all entries for each CombinedCacheKey across all MetaIndex states.
    /// </summary>
    public bool RemoveMeta(IEnumerable<CombinedCacheKey> combinedKeys)
    {
        var anyRemoved = false;
        foreach (var key in combinedKeys)
            anyRemoved |= RemoveMeta(key);
        return anyRemoved;
    }

    /// <summary>
    ///     Removes all entries with the given CombinedCacheKey across all MetaIndex caches.
    /// </summary>
    public bool RemoveMeta(CombinedCacheKey key)
    {
        var anyRemoved = false;
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
                _logger.LogTrace($"Final Slot was: {curr?.GameItem.Name} and will now be {glamItem.GameItem.Name} for slot {glamItem.Slot}");
                _finalGlamour[glamItem.Slot] = glamItem;
                anyChanges |= true;
            }
        }

        removedSlots = _finalGlamour.Keys.Except(seenSlots).ToList();
        foreach (var slot in removedSlots)
        {
            _logger.LogTrace($"Removing Final Glamour Slot {slot} with Item {_finalGlamour[slot].GameItem.Name}");
            _finalGlamour.Remove(slot);
            anyChanges |= true;
        }

        return anyChanges || removedSlots.Any();
    }

    public bool UpdateFinalMetaCache(out bool noHat, out bool noVisor, out bool noWeapon)
    {
        var firstHat = GetFirstHatState();
        var firstVisor = GetFirstVisorState();
        var firstWeapon = GetFirstWeaponState();
        noHat = !_metaStates.TryGetValue(MetaIndex.HatState, out var hatList) || hatList.Count <= 0;
        noVisor = !_metaStates.TryGetValue(MetaIndex.VisorState, out var visorList) || visorList.Count <= 0;
        noWeapon = !_metaStates.TryGetValue(MetaIndex.WeaponState, out var weaponList) || weaponList.Count <= 0;
        // True if any update occured (which it always will in this case)
        var anyChanges = false;
        if (_finalMeta.IsDifferent(MetaIndex.HatState, firstHat))
        {
            anyChanges |= true;
            _logger.LogDebug($"Updating Final Meta Cache: Hat({firstHat})");
            _finalMeta = _finalMeta.WithMeta(MetaIndex.HatState, firstHat);
        }
        if (_finalMeta.IsDifferent(MetaIndex.VisorState, firstVisor))
        {
            anyChanges |= true;
            _logger.LogDebug($"Updating Final Meta Cache: Visor({firstVisor})");
            _finalMeta = _finalMeta.WithMeta(MetaIndex.VisorState, firstVisor);
        }
        if (_finalMeta.IsDifferent(MetaIndex.WeaponState, firstWeapon))
        {
            anyChanges |= true;
            _logger.LogDebug($"Updating Final Meta Cache: Weapon({firstWeapon})");
            _finalMeta = _finalMeta.WithMeta(MetaIndex.WeaponState, firstWeapon);
        }
        return anyChanges;
    }

    public TriStateBool GetFirstHatState()
    {
        if (_metaStates.TryGetValue(MetaIndex.HatState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return TriStateBool.Null;
    }

    public TriStateBool GetFirstVisorState()
    {
        if (_metaStates.TryGetValue(MetaIndex.VisorState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return TriStateBool.Null;
    }

    public TriStateBool GetFirstWeaponState()
    {
        if (_metaStates.TryGetValue(MetaIndex.WeaponState, out var stateList) && stateList.Any())
            return stateList.Values.First();
        return TriStateBool.Null;
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

                    ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("Restrictionss(0)").X);
                    ImGui.TableSetupColumn("Head");
                    ImGui.TableSetupColumn("Chest");
                    ImGui.TableSetupColumn("Gloves");
                    ImGui.TableSetupColumn("Legs");
                    ImGui.TableSetupColumn("Feet");
                    ImGui.TableSetupColumn("Earring");
                    ImGui.TableSetupColumn("Necklace");
                    ImGui.TableSetupColumn("Bracelet");
                    ImGui.TableSetupColumn("L-Ring");
                    ImGui.TableSetupColumn("R-Ring");
                    ImGui.TableHeadersRow();

                    var grouped = _glamours.GroupBy(kvp => kvp.Key.Item1);

                    foreach (var group in grouped)
                    {
                        ImGuiUtil.DrawFrameColumn($"{group.Key.Manager}({group.Key.LayerIndex})");

                        var slotMap = group.ToDictionary(kvp => kvp.Key.Item2, kvp => kvp.Value);
                        foreach (var slot in EquipSlotExtensions.EqdpSlots)
                        {
                            ImGui.TableNextColumn();
                            if (slotMap.TryGetValue(slot, out var glam))
                                glam.GameItem.DrawIcon(textures, iconSize, slot);
                            else
                                ItemSvc.NothingItem(slot).DrawIcon(textures, iconSize, slot);
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
            ImGui.TableSetupColumn("Head");
            ImGui.TableSetupColumn("Chest");
            ImGui.TableSetupColumn("Gloves");
            ImGui.TableSetupColumn("Legs");
            ImGui.TableSetupColumn("Feet");
            ImGui.TableSetupColumn("Earring");
            ImGui.TableSetupColumn("Necklace");
            ImGui.TableSetupColumn("Bracelet");
            ImGui.TableSetupColumn("L-Ring");
            ImGui.TableSetupColumn("R-Ring");
            ImGui.TableHeadersRow();
            // Draw the final State.
            ImGuiUtil.DrawFrameColumn("Final State");
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                ImGui.TableNextColumn();
                if (_finalGlamour.TryGetValue(slot, out var glamour))
                    glamour.GameItem.DrawIcon(textures, iconSize, slot);
                else
                    ItemSvc.NothingItem(slot).DrawIcon(textures, iconSize, slot);
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

    public void DrawUnboundCacheStates(TextureService textures)
    {
        using var display = ImRaii.Group();


        var iconSize = new Vector2(ImGui.GetFrameHeight());
        ImGui.Text("Unbound State Cache History:");
        using (var table = ImRaii.Table("UnboundStateCacheHistory", 10, ImGuiTableFlags.BordersInner | ImGuiTableFlags.RowBg))
        {
            if (!table)
                return;

            ImGui.TableSetupColumn("Head");
            ImGui.TableSetupColumn("Chest");
            ImGui.TableSetupColumn("Gloves");
            ImGui.TableSetupColumn("Legs");
            ImGui.TableSetupColumn("Feet");
            ImGui.TableSetupColumn("Earring");
            ImGui.TableSetupColumn("Necklace");
            ImGui.TableSetupColumn("Bracelet");
            ImGui.TableSetupColumn("L-Ring");
            ImGui.TableSetupColumn("R-Ring");
            ImGui.TableHeadersRow();
            // Draw the final State.
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                ImGui.TableNextColumn();
                if (_latestUnboundState.ParsedEquipment.TryGetValue(slot, out var glamour))
                    glamour.DrawIcon(textures, iconSize, slot);
                else
                    ItemSvc.NothingItem(slot).DrawIcon(textures, iconSize, slot);
            }
        }
        ImGui.Text("Headgear");
        CkGui.ColorTextInline(_latestUnboundState.MetaStates.Headgear.ToString(), CkColor.LushPinkLine.Uint());
        CkGui.TextInline("Visor", false);
        CkGui.ColorTextInline(_latestUnboundState.MetaStates.Visor.ToString(), CkColor.LushPinkLine.Uint());
        CkGui.TextInline("Weapon", false);
        CkGui.ColorTextInline(_latestUnboundState.MetaStates.Weapon.ToString(), CkColor.LushPinkLine.Uint());
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
