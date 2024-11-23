namespace Penumbra.GameData.Structs;

/// <summary> A packed struct representing the model data for a piece of armor or accessory. </summary>
/// <param name="skeleton"> The primary ID. </param>
/// <param name="set"> The secondary ID. </param>
/// <param name="variant"> The variant. </param>
/// <param name="stains"> The Stain IDs. </param>
[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 8)]
public struct CharacterWeapon(PrimaryId skeleton, SecondaryId set, Variant variant, StainIds stains)
    : IEquatable<CharacterWeapon>, IComparable<CharacterWeapon>
{
    public const int Size = 8;

    /// <summary> The data as a single value. </summary>
    [FieldOffset(0)]
    public ulong Value = 0;

    [FieldOffset(0)]
    public PrimaryId Skeleton = skeleton;

    [FieldOffset(2)]
    public SecondaryId Weapon = set;

    [FieldOffset(4)]
    public Variant Variant = variant;

    [FieldOffset(5)]
    private readonly byte _padding = 0;

    [FieldOffset(6)]
    public StainIds Stains = stains;

    /// <inheritdoc/>
    public override readonly string ToString()
        => $"{Skeleton},{Weapon},{Variant},{Stains}";

    /// <summary> Compare in inverse order. </summary>
    private readonly ulong CompareValue
        => ((ulong)Skeleton.Id << 48)
          | ((ulong)Weapon.Id << 32)
          | ((ulong)Variant.Id << 16)
          | ((ulong)Stains.Stain1.Id << 8)
          | Stains.Stain2.Id;

    /// <summary> Create a character weapon from a single value. </summary>
    public CharacterWeapon(ulong value)
        : this((PrimaryId)value, (SecondaryId)(value >> 16), (Variant)(value >> 32),
            new StainIds((StainId)(value >> 48), (StainId)(value >> 56)))
    { }

    /// <summary> Return the same weapon with a different stain. </summary>
    public readonly CharacterWeapon With(StainIds stainIds)
        => new(Skeleton, Weapon, Variant, stainIds);

    /// <summary> Turn to a CharacterArmor by forgetting the secondary ID. </summary>
    public readonly CharacterArmor ToArmor()
        => new(Skeleton, Variant, Stains);

    /// <summary> Turn to a CharacterArmor by forgetting the secondary ID, with a different stain. </summary>
    public readonly CharacterArmor ToArmor(StainIds stainIds)
        => new(Skeleton, Variant, stainIds);

    /// <summary> The empty CharacterWeapon. </summary>
    public static readonly CharacterWeapon Empty = new(0, 0, 0, StainIds.None);

    /// <inheritdoc/>
    public readonly bool Equals(CharacterWeapon other)
        => Value == other.Value;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj)
        => obj is CharacterWeapon other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode()
        => Value.GetHashCode();

    /// <inheritdoc/>
    public readonly int CompareTo(CharacterWeapon rhs)
        => CompareValue.CompareTo(rhs.CompareValue);

    public static bool operator ==(CharacterWeapon left, CharacterWeapon right)
        => left.Value == right.Value;

    public static bool operator !=(CharacterWeapon left, CharacterWeapon right)
        => left.Value != right.Value;

    /// <summary> A helper to create CharacterWeapons without casting from integers. </summary>
    public static CharacterWeapon Int(int skeleton, int weapon, int variant)
        => new((PrimaryId)skeleton, (SecondaryId)weapon, (Variant)variant, StainIds.None);
}

[StructLayout(LayoutKind.Sequential, Size = 7, Pack = 1)]
public struct LegacyCharacterWeapon(CharacterWeapon weapon)
{
    public PrimaryId   Skeleton = weapon.Skeleton;
    public SecondaryId Weapon   = weapon.Weapon;
    public Variant     Variant  = weapon.Variant;
    public byte        Padding;
    public StainId     Stain = weapon.Stains.Stain1;
}
