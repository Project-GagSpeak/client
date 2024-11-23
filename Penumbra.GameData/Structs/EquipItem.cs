global using PseudoEquipItem = System.ValueTuple<string, ulong, uint, ushort, ushort, byte, uint>;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;


namespace Penumbra.GameData.Structs;

/// <summary> Flags with useful information about an item. </summary>
[Flags]
public enum ItemFlags : byte
{
    IsDyable1     = 0x01,
    IsTradable    = 0x02,
    IsCrestWorthy = 0x04,
    IsDyable2     = 0x08,
}

/// <summary> All useful information for a single model of a single item. </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct EquipItem : IEquatable<EquipItem>
{
    /// <summary> The name of the item. </summary>
    public readonly string Name;

    /// <summary> A full item ID that may not represent an item that the game knows. </summary>
    public readonly CustomItemId Id;

    /// <summary> An associated icon if it exists. </summary>
    public readonly IconId IconId;

    /// <summary> The primary ID of the represented model. </summary>
    public readonly PrimaryId PrimaryId;

    /// <summary> The secondary ID of the represented model, or 0. </summary>
    public readonly SecondaryId SecondaryId;

    /// <summary> The variant of the represented model. </summary>
    public readonly Variant Variant;

    /// <summary> The full type of the item, which also tells us if this is a primary, secondary or tertiary model. </summary>
    public readonly FullEquipType Type;

    /// <summary> Miscellaneous additional information. </summary>
    public readonly ItemFlags Flags;

    /// <summary> The level required to wear the item, if any. </summary>
    public readonly CharacterLevel Level;

    /// <summary> The job group required to wear the item, if any. </summary>
    public readonly JobGroupId JobRestrictions;

    /// <summary> The actual item ID. Will return 0 for fake items. </summary>
    public ItemId ItemId
        => Id.Item;

    /// <summary> Whether the item is valid. </summary>
    public bool Valid
        => Type != FullEquipType.Unknown;

    /// <summary> Get the represented model as armor without stain. </summary>
    public CharacterArmor Armor()
        => new(PrimaryId, Variant, StainIds.None);

    /// <summary> Get the represented model as armor with a specific stain. </summary>
    public CharacterArmor Armor(StainIds stain)
        => new(PrimaryId, Variant, stain);

    /// <summary> Get the represented model as weapon without stain. </summary>
    public CharacterWeapon Weapon()
        => new(PrimaryId, SecondaryId, Variant, StainIds.None);

    /// <summary> Get the represented model as weapon with a specific stain. </summary>
    public CharacterWeapon Weapon(StainIds stain)
        => new(PrimaryId, SecondaryId, Variant, stain);

    /// <summary> An empty item. </summary>
    public EquipItem()
        => Name = string.Empty;

    /// <summary> Create an EquipItem from all data used. </summary>
    public EquipItem(string name, CustomItemId id, IconId iconId, PrimaryId primaryId, SecondaryId secondaryId, Variant variant,
        FullEquipType type, ItemFlags flags, CharacterLevel level, JobGroupId restrictions)
    {
        Name            = string.Intern(name);
        Id              = id;
        IconId          = iconId;
        PrimaryId       = primaryId;
        SecondaryId     = secondaryId;
        Variant         = variant;
        Type            = type;
        Flags           = flags;
        Level           = level;
        JobRestrictions = restrictions;
    }

    /// <summary> Write the model as a string. </summary>
    public string ModelString
        => SecondaryId == 0 ? $"{PrimaryId}-{Variant}" : $"{PrimaryId}-{SecondaryId}-{Variant}";

    /// <summary> Convert a PseudoEquipItem to EquipItem. </summary>
    public static implicit operator EquipItem(PseudoEquipItem it)
    {
        var (type, flags, level, restrictions) = SplitInt(it.Item7);
        return new EquipItem(it.Item1, it.Item2, it.Item3, it.Item4, it.Item5, it.Item6, type, flags, level, restrictions);
    }

    /// <summary> Convert an EquipItem to PseudoEquipItem. </summary>
    public static explicit operator PseudoEquipItem(EquipItem it)
        => (it.Name, it.Id.Id, it.IconId.Id, it.PrimaryId.Id, it.SecondaryId.Id, it.Variant.Id,
            PackBytes(it.Type, it.Flags, it.Level, it.JobRestrictions));

    /// <summary> Create an EquipItem from a lumina item representing armor. </summary>
    public static EquipItem FromArmor(in Item item)
    {
        var type            = item.ToEquipType();
        var name            = item.Name.ExtractText();
        var id              = item.RowId;
        var icon            = item.Icon;
        var model           = (PrimaryId)item.ModelMain;
        var weapon          = (SecondaryId)0;
        var variant         = (Variant)(item.ModelMain >> 16);
        var flags           = GetFlags(item);
        var level           = item.LevelEquip;
        var jobRestrictions = (byte)item.ClassJobCategory.RowId;
        return new EquipItem(name, id, icon, model, weapon, variant, type, flags, level, jobRestrictions);
    }

    /// <summary> Convert lumina data to misc. information. </summary>
    private static ItemFlags GetFlags(in Item item)
        => item.DyeCount switch
            {
                1 => ItemFlags.IsDyable1,
                2 => ItemFlags.IsDyable1 | ItemFlags.IsDyable2,
                _ => 0,
            }
          | (item.IsCrestWorthy ? ItemFlags.IsCrestWorthy : 0)
          | (item.IsUntradable ? 0 : ItemFlags.IsTradable);

    /// <summary> Create an EquipItem from a lumina item representing a weapon using the primary model. </summary>
    public static EquipItem FromMainhand(in Item item)
    {
        var type            = item.ToEquipType();
        var name            = item.Name.ExtractText();
        var id              = item.RowId;
        var icon            = item.Icon;
        var model           = (PrimaryId)item.ModelMain;
        var weapon          = (SecondaryId)(item.ModelMain >> 16);
        var variant         = (Variant)(item.ModelMain >> 32);
        var flags           = GetFlags(item);
        var level           = item.LevelEquip;
        var jobRestrictions = (byte)item.ClassJobCategory.RowId;
        return new EquipItem(name, id, icon, model, weapon, variant, type, flags, level, jobRestrictions);
    }

    /// <summary> Create an EquipItem from a lumina item representing a weapon using the secondary model. </summary>
    public static EquipItem FromOffhand(Item item)
    {
        var type            = item.ToEquipType().ValidOffhand();
        var name            = item.Name.ExtractText() + type.OffhandTypeSuffix();
        var id              = item.RowId;
        var icon            = item.Icon;
        var model           = (PrimaryId)item.ModelSub;
        var weapon          = (SecondaryId)(item.ModelSub >> 16);
        var variant         = (Variant)(item.ModelSub >> 32);
        var flags           = GetFlags(item);
        var level           = item.LevelEquip;
        var jobRestrictions = (byte)item.ClassJobCategory.RowId;
        return new EquipItem(name, id, icon, model, weapon, variant, type, flags, level, jobRestrictions);
    }

    /// <summary> Create an EquipItem from a set of IDs. </summary>
    public static EquipItem FromIds(ItemId itemId, IconId iconId, PrimaryId modelId, SecondaryId type, Variant variant,
        FullEquipType equipType = FullEquipType.Unknown, ItemFlags flags = 0, string? name = null)
        => FromIds(itemId, iconId, modelId, type, variant, 0, 0, equipType, flags, name);

    /// <summary> Create an EquipItem from a set of IDs. </summary>
    public static EquipItem FromIds(ItemId itemId, IconId iconId, PrimaryId modelId, SecondaryId type, Variant variant,
        CharacterLevel level, JobGroupId restrictions, FullEquipType equipType = FullEquipType.Unknown, ItemFlags flags = 0,
        string? name = null)
    {
        name ??= $"Unknown ({modelId}-{(type.Id != 0 ? $"{type}-" : string.Empty)}{variant})";
        if (equipType.IsUnknown() && type.Id != 0)
            equipType = ItemData.ConvertWeaponId(modelId);
        var fullId = itemId.Id is 0
            ? new CustomItemId(modelId, type, variant, equipType)
            : (CustomItemId)itemId;
        return new EquipItem(name, fullId, iconId, modelId, type, variant, equipType, flags, level, restrictions);
    }

    public static EquipItem FromBonusIds(BonusItemId itemId, IconId iconId, PrimaryId modelId, Variant variant, BonusItemFlag type, string? name = null)
    {
        name ??= $"Unknown ({modelId}-{variant})";
        var fullId = itemId.Id is 0
            ? new CustomItemId(modelId, variant, type)
            : itemId;
        return new EquipItem(name, fullId, iconId, modelId, 0, variant, type.ToEquipType(), 0, 0, 0);
    }


    /// <summary> Create an EquipItem from a singular custom ID, if possible. </summary>
    public static EquipItem FromId(CustomItemId id)
    {
        var (setId, weaponType, variant, equipType) = id.Split;
        if (equipType.IsUnknown() && weaponType.Id != 0)
            equipType = ItemData.ConvertWeaponId(setId);

        return new EquipItem($"Unknown ({setId}-{(weaponType.Id != 0 ? $"{weaponType}-" : string.Empty)}{variant})", id, 0, setId, weaponType,
            variant, equipType, 0, 0, 0);
    }

    /// <inheritdoc/>
    public override string ToString()
        => Name;

    /// <summary> Pack the miscellaneous data to a single value. </summary>
    private static uint PackBytes(FullEquipType type, ItemFlags flags, CharacterLevel level, JobGroupId restrictions)
        => (uint)type | ((uint)flags << 8) | ((uint)level.Value << 16) | ((uint)restrictions.Id << 24);

    /// <summary> Split a pack of miscellaneous data into its parts. </summary>
    private static (FullEquipType, ItemFlags, CharacterLevel, JobGroupId) SplitInt(uint data)
        => ((FullEquipType)(data & 0xFF), (ItemFlags)((data >> 8) & 0xFF), (CharacterLevel)((data >> 16) & 0xFF), (JobGroupId)(data >> 24));

    public bool Equals(EquipItem other)
        => Id == other.Id;

    public override bool Equals(object? obj)
        => obj is EquipItem other && Equals(other);

    public override int GetHashCode()
        => Id.Id.GetHashCode();

    public const string Nothing = "Nothing";

    /// <summary> An empty bonus item for a specific slot. </summary>
    public static EquipItem BonusItemNothing(BonusItemFlag slot)
        => new(Nothing, new CustomItemId(0, 0, slot), 0, 0, 0, 0, slot.ToEquipType(), 0, 0, 0);
}

/// <summary> A list wrapping a PseudoEquipItem list to an EquipItemList. </summary>
internal readonly struct EquipItemList(IReadOnlyList<PseudoEquipItem> items) : IReadOnlyList<EquipItem>
{
    /// <inheritdoc/>
    public IEnumerator<EquipItem> GetEnumerator()
        => items.Select(i => (EquipItem)i).GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    public int Count
        => items.Count;

    /// <inheritdoc/>
    public EquipItem this[int index]
        => items[index];
}
