using GagSpeak.State.Models;
using GagSpeak.Utils;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Runtime.CompilerServices;

//namespace GagSpeak.Services;

//// Taken from Glamourer to identify items, since it is not provided in any submodules.
//public class ItemService
//{
//    public const string Nothing = EquipItem.Nothing;
//    public const string SmallClothesNpc = "Smallclothes (NPC)";
//    public const ushort SmallClothesNpcModel = 9903;

//    public readonly DictStain            Stains;
//    public readonly ItemData             ItemData;
//    public readonly DictBonusItems       BonusData;

//    public ItemService(DictStain stains, ItemData itemData, DictBonusItems bonusData)
//    {
//        Stains    = stains;
//        ItemData  = itemData;
//        BonusData = bonusData;
//    }

//    public static ItemId NothingId(EquipSlot slot)
//        => uint.MaxValue - 128 - (uint)slot.ToSlot();

//    public static ItemId SmallclothesId(EquipSlot slot)
//        => uint.MaxValue - 256 - (uint)slot.ToSlot();

//    public static ItemId NothingId(FullEquipType type)
//        => uint.MaxValue - 384 - (uint)type;

//    public static EquipItem NothingItem(EquipSlot slot)
//        => new(Nothing, NothingId(slot), 0, 0, 0, 0, slot.ToEquipType(), 0, 0, 0);

//    public static EquipItem NothingItem(FullEquipType type)
//        => new(Nothing, NothingId(type), 0, 0, 0, 0, type, 0, 0, 0);

//    public static EquipItem SmallClothesItem(EquipSlot slot)
//        => new(SmallClothesNpc, SmallclothesId(slot), 0, SmallClothesNpcModel, 0, 1, slot.ToEquipType(), 0, 0, 0);

//    public EquipItem Resolve(EquipSlot slot, CustomItemId itemId)
//    {
//        slot = slot.ToSlot();
//        if (itemId == NothingId(slot))
//            return NothingItem(slot);
//        if (itemId == SmallclothesId(slot))
//            return SmallClothesItem(slot);

//        if (!itemId.IsItem)
//        {
//            var item = EquipItem.FromId(itemId);
//            return item;
//        }
//        else if (!ItemData.TryGetValue(itemId.Item, slot, out var item))
//        {
//            return EquipItem.FromId(itemId);
//        }
//        else
//        {
//            if (item.Type.ToSlot() != slot)
//                return new EquipItem(string.Intern($"Invalid #{itemId}"), itemId, item.IconId, item.PrimaryId, 
//                    item.SecondaryId, item.Variant, 0, 0, 0, 0);

//            return item;
//        }
//    }

//    public IReadOnlyList<EquipItem> GetBonusItems(BonusItemFlag slot)
//    {
//        var nothing = EquipItem.BonusItemNothing(slot);
//        return ItemData.ByType[slot.ToEquipType()].OrderBy(i => i.Name).Prepend(nothing).ToList();
//    }

//    /// <summary> Returns whether an item id represents a valid item for a slot and gives the item. </summary>
//    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
//    public bool IsItemValid(EquipSlot slot, CustomItemId itemId, out EquipItem item)
//    {
//        item = Resolve(slot, itemId);
//        return item.Valid;
//    }

//    /// <summary> Returns whether a bonus item id represents a valid item for a slot and gives the item. </summary>
//    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
//    public bool IsBonusItemValid(BonusItemFlag slot, BonusItemId itemId, out EquipItem item)
//    {
//        if (itemId.Id != 0)
//            return BonusData.TryGetValue(itemId, out item) && slot == item.Type.ToBonus();

//        item = EquipItem.BonusItemNothing(slot);
//        return true;
//    }

//    public EquipItem Resolve(BonusItemFlag slot, BonusItemId id)
//        => IsBonusItemValid(slot, id, out var item) ? item : new EquipItem($"Invalid ({id.Id})", id, 0, 0, 0, 0, slot.ToEquipType(), 0, 0, 0);

//    public EquipItem Resolve(BonusItemFlag slot, CustomItemId id)
//    {
//        // Only from early designs as migration.
//        if (!id.IsBonusItem || id.Id == 0)
//        {
//            IsBonusItemValid(slot, (BonusItemId)id.Id, out var item);
//            return item;
//        }

//        if (!id.IsCustom)
//        {
//            if (IsBonusItemValid(slot, id.BonusItem, out var item))
//                return item;

//            return EquipItem.BonusItemNothing(slot);
//        }

//        var (model, variant, slot2) = id.SplitBonus;
//        if (slot != slot2)
//            return EquipItem.BonusItemNothing(slot);

//        return Resolve(slot, id.BonusItem);
//    }


//    /// <summary> Serializes the GlamourSlot into a JToken. </summary>
//    /// <param name="slot"> The GlamourSlot to serialize. </param>
//    /// <returns> The JToken representing the GlamourSlot. </returns>
//    public JObject SerializeGlamourSlot(GlamourSlot slot)
//        => new JObject
//        {
//            ["Slot"] = slot.Slot.ToString(),
//            ["CustomItemId"] = slot.GameItem.Id.ToString(),
//            ["Stains"] = slot.GameStain.ToString(),
//        };


//    /// <summary> Parses out the GlamourSlot from the JToken. </summary>
//    /// <param name="item"> The JToken encapsulating a GlamourSlot Item. </param>
//    /// <returns> The GlamourSlot item parsed from the token. </returns>
//    /// <exception cref="InvalidDataException"> Thrown if the JToken is not a valid GlamourSlot object. </exception>"
//    public GlamourSlot ParseGlamourSlot(JToken? item)
//    {
//        if(item is not JObject json)
//            throw new InvalidDataException("Invalid GlamourSlot JToken object.");

//        var slot = (EquipSlot)Enum.Parse(typeof(EquipSlot), json["Slot"]?.Value<string>() ?? string.Empty);
//        return new GlamourSlot()
//        {
//            Slot = slot,
//            GameItem = Resolve(slot, new CustomItemId(json["CustomItemId"]?.Value<ulong>() ?? 4294967164)),
//            GameStain = GsExtensions.ParseCompactStainIds(json)
//        };
//    }

//    /// <summary> Serializes the GlamourBonusSlot into a JToken. </summary>
//    /// <param name="slot"> The GlamourBonusSlot to serialize. </param>
//    /// <returns> The JToken representing the GlamourBonusSlot. </returns>
//    public JObject SerializeBonusSlot(GlamourBonusSlot slot)
//        => new JObject
//        {
//            ["Slot"] = slot.Slot.ToString(),
//            ["CustomItemId"] = slot.GameItem.Id.BonusItem.ToString(),
//        };

//    public GlamourBonusSlot ParseBonusSlot(JToken? item)
//    {
//        if(item is not JObject json)
//            throw new InvalidDataException("Invalid GlamourBonusSlot JToken object.");

//        var slot = (BonusItemFlag)Enum.Parse(typeof(BonusItemFlag), json["Slot"]?.Value<string>() ?? string.Empty);
//        return new GlamourBonusSlot()
//        {
//            Slot = slot,
//            GameItem = Resolve(slot, json["CustomItemId"]?.Value<ushort>() ?? ushort.MaxValue),
//        };
//    }



//}
