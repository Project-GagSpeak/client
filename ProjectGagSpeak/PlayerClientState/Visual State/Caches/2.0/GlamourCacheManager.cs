using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using OtterGui.Classes;
using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

// Handles all Glamour Caching and applier interactions.
public class GlamourCacheManager
{
    private readonly ILogger<GlamourCacheManager> _logger;
    private readonly VisualApplierGlamour _handler;

    private SortedList<(CombinedCacheKey, EquipSlot), GlamourSlot> _glamours = new();
    // Maybe convert to a metastruct later idk im tired of this constant pain....
    private SortedList<CombinedCacheKey, OptionalBool> _headgear = new();
    private SortedList<CombinedCacheKey, OptionalBool> _visors = new();
    private SortedList<CombinedCacheKey, OptionalBool> _weapons = new();

    private Dictionary<EquipSlot, GlamourSlot> _finalGlamour = new();
    private MetaDataStruct _finalMeta = new MetaDataStruct();

    public GlamourCacheManager(ILogger<GlamourCacheManager> logger, VisualApplierGlamour handler)
    {
        _logger = logger;
        _handler = handler;
    }

    public Dictionary<EquipSlot, GlamourSlot> FinalRef => _finalGlamour;
    public OptionalBool FinalHeadgear => _finalMeta.Headgear;
    public OptionalBool FinalVisors => _finalMeta.Visor;
    public OptionalBool FinalWeapons => _finalMeta.Weapon;

    /// <summary>
    ///     Adds multiple GlamourSlots to the cache for a given combinedKey.
    /// </summary>
    public async Task AddToCache(CombinedCacheKey combinedKey, IReadOnlyDictionary<EquipSlot, GlamourSlot> glamourDict)
    {
        // Check for any conflicting keys first. If any key already exists, skip the entire addition.
        if (_glamours.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning("Cannot add GlamourSlots to cache, key already exists: {Key}", combinedKey);
            return;
        }

        // Add all glamour items for each slot.
        foreach (var (slot, glamour) in glamourDict)
            _glamours.Add((combinedKey, slot), glamour);

        // update the final cache and retrieve the changed slots.
        var updatedSlots = UpdateFinalCache();

        // UPDATE THIS LATER AFTER WE CORRECT THE APPLIER OR MERGE IT HERE.
        // If any slots were updated, update them in the handler.
        if (updatedSlots.Any())
            await _handler.UpdateAllActiveSlots(_finalGlamour);
    }

    /// <summary>
    ///     Adds a GlamourSlot to the cache, returning the affected equipSlot, if changed.
    /// </summary>
    public async Task AddToCache(CombinedCacheKey combinedKey, GlamourSlot glamour)
    {
        // if the combined key is already present in the sorted list, reject the addition.
        if (_glamours.Keys.Any(k => k.Item1.Equals(combinedKey)))
        {
            _logger.LogWarning("Cannot add GlamourSlot to cache, key already exists: {Key}", combinedKey);
            return;
        }

        // Append the glamour item.
        _glamours.Add((combinedKey, glamour.Slot), glamour);

        // Update the finalGlamour for that particular slot.
        if(UpdateFinalCacheForSlot(glamour.Slot))
            await _handler.UpdateActiveSlot(glamour);
    }

    public async Task AddMetaInCache(CombinedCacheKey combinedKey, MetaIndex metaIndex, OptionalBool value)
    {
        bool canAccess = metaIndex switch
        {
            MetaIndex.HatState => _headgear.ContainsKey(combinedKey),
            MetaIndex.VisorState => _visors.ContainsKey(combinedKey),
            MetaIndex.WeaponState => _weapons.ContainsKey(combinedKey),
            _ => false
        };
        // do not add if the value is OptionalBool.Null.
        if (!canAccess || value == OptionalBool.Null)
        {
            _logger.LogWarning("Cannot add MetaData to cache, value is null, or item was already present.");
            return;
        }

        // Update the Metadata based on the metaIndex.
        switch (metaIndex)
        {
            case MetaIndex.HatState:
                _headgear[combinedKey] = value;
                _finalMeta.SetMeta(MetaIndex.HatState, _headgear.Values.FirstOrDefault());
                if(_finalMeta.Headgear != value)
                    await _handler.UpdateMetaState(MetaIndex.HatState, value);
                break;

            case MetaIndex.VisorState:
                _visors[combinedKey] = value;
                _finalMeta.SetMeta(MetaIndex.VisorState, _visors.Values.FirstOrDefault());
                if (_finalMeta.Visor != value)
                    await _handler.UpdateMetaState(MetaIndex.VisorState, value);
                break;

            case MetaIndex.WeaponState:
                _weapons[combinedKey] = value;
                _finalMeta.SetMeta(MetaIndex.WeaponState, _weapons.Values.FirstOrDefault());
                if (_finalMeta.Weapon != value)
                    await _handler.UpdateMetaState(MetaIndex.WeaponState, value);
                break;

            default:
                _logger.LogWarning($"Unknown MetaIndex: {metaIndex}");
                return;
        }
    }


    /// <summary>
    ///     Efficiently iterates through the glamours, returning the updated slots.
    /// </summary>
    /// <returns> All slots that were changed in the final cache update. </returns>
    private IEnumerable<EquipSlot> UpdateFinalCache()
    {
        var updatedSlots = Enumerable.Empty<EquipSlot>();
        var seenSlots = Enumerable.Empty<EquipSlot>();

        // Cycle through the glamours in the order they are sorted in.
        foreach (var glamourItem in _glamours.Values)
        {
            if (seenSlots.Contains(glamourItem.Slot))
                continue;

            seenSlots = seenSlots.Append(glamourItem.Slot);

            // Check if the slot should be updated because it is not present or different.
            if (!_finalGlamour.TryGetValue(glamourItem.Slot, out var currentGlamour) 
             || !currentGlamour.Equals(glamourItem))
            {
                _finalGlamour[glamourItem.Slot] = glamourItem;
                updatedSlots = updatedSlots.Append(glamourItem.Slot);
            }
        }

        // Find the slots that were in _finalGlamour but are no longer in _glamours.
        var toRemove = _finalGlamour.Keys.Except(seenSlots).ToList();
        foreach (var slot in toRemove)
        {
            _finalGlamour.Remove(slot);
            updatedSlots = updatedSlots.Append(slot);
        }

        return updatedSlots;
    }

    /// <summary>
    ///     Attempts to update the final cache for a single EquipSlot.
    /// </summary>
    /// <returns> Returns true if a change was made, false otherwise. </returns>
    private bool UpdateFinalCacheForSlot(EquipSlot slot)
    {
        // Find the top-priority glamour for this slot.
        if (_glamours.FirstOrDefault(kvp => kvp.Key.Item2 == slot).Value is { } validGlamour)
        {
            // If not in dictionary or value has changed, update and return true.
            if (!_finalGlamour.TryGetValue(slot, out var current) || !current.Equals(validGlamour))
            {
                _finalGlamour[slot] = validGlamour;
                return true;
            }
        }
        else
        {
            // If the slot was previously filled, remove and return true.
            if (_finalGlamour.Remove(slot))
                return true;
        }

        // If no changes were made, return false.
        return false;
    }
}
