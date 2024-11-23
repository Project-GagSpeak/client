using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OtterGui.Log;
using Penumbra.GameData.DataContainers.Bases;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace Penumbra.GameData.DataContainers;

/// <summary> A list to efficiently identify equipment pieces. This requires ItemsByType to be finished. </summary>
public sealed class IdentificationListEquipment(IDalamudPluginInterface pi, Logger log, IDataManager gameData, ItemsByType items)
    : KeyList<PseudoEquipItem>(pi, log, "EquipmentIdentification", gameData.Language, 11, () => CreateEquipmentList(items), ToKey, ValidKey,
        ValueKeySelector, items.Awaiter)
{
    /// <summary> Find all items affected by the given set of input data. </summary>
    /// <param name="modelId"> The primary ID of the piece. </param>
    /// <param name="slot"> The slot. If Unknown, check all slots. </param>
    /// <param name="variant"> The variant. If 0, check all variants. </param>
    /// <returns> A list of all affected EquipItems. </returns>
    public IEnumerable<EquipItem> Between(PrimaryId modelId, EquipSlot slot = EquipSlot.Unknown, Variant variant = default)
    {
        if (slot == EquipSlot.Unknown)
            return Between(ToKey(modelId, 0, 0), ToKey(modelId, (EquipSlot)0xFF, 0xFF)).Select(e => (EquipItem)e);
        if (variant == 0)
            return Between(ToKey(modelId, slot, 0), ToKey(modelId, slot, 0xFF)).Select(e => (EquipItem)e);

        return Between(ToKey(modelId, slot, variant), ToKey(modelId, slot, variant)).Select(e => (EquipItem)e);
    }

    /// <summary> Convert a set of data to its key representation. </summary>
    public static ulong ToKey(PrimaryId modelId, EquipSlot slot, Variant variant)
        => ((ulong)modelId.Id << 32) | ((ulong)slot << 16) | variant.Id;

    /// <summary> Turn a specific item to its key representation. </summary>
    public static ulong ToKey(EquipItem i)
        => ToKey(i.PrimaryId, i.Type.ToSlot(), i.Variant);

    /// <summary> Turn a pseudo equip item to its key representation. </summary>
    private static ulong ToKey(PseudoEquipItem i)
        => ToKey((EquipItem)i);

    /// <summary> All non-zero keys are valid. </summary>
    private static bool ValidKey(ulong key)
        => key != 0;

    /// <summary> Order by an items full ItemId after the keys. </summary>
    private static int ValueKeySelector(PseudoEquipItem data)
        => (int)data.Item2;

    /// <summary> Create the key list of all equipment pieces, including the custom defined ones without actual item representation.  </summary>
    private static IEnumerable<PseudoEquipItem> CreateEquipmentList(ItemsByType items)
    {
        return items.Where(kvp => kvp.Key.IsEquipment() || kvp.Key.IsAccessory() || kvp.Key.IsBonus())
            .SelectMany(kvp => kvp.Value)
            .Select(i => (PseudoEquipItem)i)
            .Concat(CustomList);
    }

    /// <summary> Custom items without actual items in the game data. </summary>
    private static IEnumerable<PseudoEquipItem> CustomList
        =>
        [
            // @formatter:off
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)8100, (SecondaryId)0, 01, FullEquipType.Body, name:"Reaper Shroud (8100-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9041, (SecondaryId)0, 01, FullEquipType.Head,  name:"Cid's Bandana (9041-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9041, (SecondaryId)0, 01, FullEquipType.Body,  name:"Cid's Body (9041-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9903, (SecondaryId)0, 01, FullEquipType.Head,  name:"Smallclothes (NPC, 9903-1, Head)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9903, (SecondaryId)0, 01, FullEquipType.Body,  name:"Smallclothes (NPC, 9903-1, Body)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9903, (SecondaryId)0, 01, FullEquipType.Hands, name:"Smallclothes (NPC, 9903-1, Hands)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9903, (SecondaryId)0, 01, FullEquipType.Legs,  name:"Smallclothes (NPC, 9903-1, Legs)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9903, (SecondaryId)0, 01, FullEquipType.Feet,  name:"Smallclothes (NPC, 9903-1, Feet)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9212, (SecondaryId)0, 12, FullEquipType.Body,  name:"Ancient Robes (Lahabrea, 9212-12)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9212, (SecondaryId)0, 01, FullEquipType.Legs,  name:"Ancient Legs (9212-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 0, (PrimaryId)9212, (SecondaryId)0, 01, FullEquipType.Feet,  name:"Ancient Shoes (9212-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 40672, (PrimaryId)0199, (SecondaryId)0, 01, FullEquipType.Head, name:"Veil of Eternal Innocence (Long, 199-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 40673, (PrimaryId)0199, (SecondaryId)0, 01, FullEquipType.Head, name:"Veil of Eternal Passion (Long, 199-1)"),
            (PseudoEquipItem)EquipItem.FromIds(0, 40674, (PrimaryId)0199, (SecondaryId)0, 01, FullEquipType.Head, name:"Veil of Eternal Devotion (Long, 199-1)"),
            // @formatter:on
        ];

    /// <inheritdoc/>
    protected override long ComputeMemory()
        => 24 + Value.Count * 40;
}
