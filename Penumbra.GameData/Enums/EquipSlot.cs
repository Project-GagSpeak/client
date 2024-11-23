using System.Collections.Frozen;

namespace Penumbra.GameData.Enums;

/// <summary> Equip Slot, mostly as defined by the games EquipSlotCategory. </summary>
public enum EquipSlot : byte
{
    Unknown           = 0,
    MainHand          = 1,
    OffHand           = 2,
    Head              = 3,
    Body              = 4,
    Hands             = 5,
    Belt              = 6,
    Legs              = 7,
    Feet              = 8,
    Ears              = 9,
    Neck              = 10,
    Wrists            = 11,
    RFinger           = 12,
    BothHand          = 13,
    LFinger           = 14, // Not officially existing, means "weapon could be equipped in either hand" for the game.
    HeadBody          = 15,
    BodyHandsLegsFeet = 16,
    SoulCrystal       = 17,
    LegsFeet          = 18,
    FullBody          = 19,
    BodyHands         = 20,
    BodyLegsFeet      = 21,
    ChestHands        = 22,
    Nothing           = 23,
    All               = 24, // Not officially existing
}

public enum HumanSlot : uint
{
    Head    = 0,
    Body    = 1,
    Hands   = 2,
    Legs    = 3,
    Feet    = 4,
    Ears    = 5,
    Neck    = 6,
    Wrists  = 7,
    RFinger = 8,
    LFinger = 9,
    Hair    = 10,
    Face    = 11,
    Ear     = 12,
    Glasses = 16,

    Unknown = uint.MaxValue,
}

public static class EquipSlotExtensions
{
    /// <summary> Convert the integer to the EquipSlot it is used to represent in most model code. </summary>
    public static EquipSlot ToEquipSlot(this uint value)
        => value switch
        {
            0  => EquipSlot.Head,
            1  => EquipSlot.Body,
            2  => EquipSlot.Hands,
            3  => EquipSlot.Legs,
            4  => EquipSlot.Feet,
            5  => EquipSlot.Ears,
            6  => EquipSlot.Neck,
            7  => EquipSlot.Wrists,
            8  => EquipSlot.RFinger,
            9  => EquipSlot.LFinger,
            10 => EquipSlot.MainHand,
            11 => EquipSlot.OffHand,
            16 => EquipSlot.Head,
            _  => EquipSlot.Unknown,
        };

    public static EquipSlot ToEquipSlot(this HumanSlot slot)
        => slot switch
        {
            HumanSlot.Head    => EquipSlot.Head,
            HumanSlot.Body    => EquipSlot.Body,
            HumanSlot.Hands   => EquipSlot.Hands,
            HumanSlot.Legs    => EquipSlot.Legs,
            HumanSlot.Feet    => EquipSlot.Feet,
            HumanSlot.Ears    => EquipSlot.Ears,
            HumanSlot.Neck    => EquipSlot.Neck,
            HumanSlot.Wrists  => EquipSlot.Wrists,
            HumanSlot.RFinger => EquipSlot.RFinger,
            HumanSlot.LFinger => EquipSlot.LFinger,
            HumanSlot.Glasses => EquipSlot.Head,
            _                 => EquipSlot.Unknown,
        };

    public static uint ToIndex(this HumanSlot slot)
        => (uint)slot;

    public static HumanSlot ToHumanSlot(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.Head              => HumanSlot.Head,
            EquipSlot.Body              => HumanSlot.Body,
            EquipSlot.Hands             => HumanSlot.Hands,
            EquipSlot.Legs              => HumanSlot.Legs,
            EquipSlot.Feet              => HumanSlot.Feet,
            EquipSlot.Ears              => HumanSlot.Ears,
            EquipSlot.Neck              => HumanSlot.Neck,
            EquipSlot.Wrists            => HumanSlot.Wrists,
            EquipSlot.RFinger           => HumanSlot.RFinger,
            EquipSlot.LFinger           => HumanSlot.LFinger,
            EquipSlot.HeadBody          => HumanSlot.Body,
            EquipSlot.BodyHandsLegsFeet => HumanSlot.Body,
            EquipSlot.LegsFeet          => HumanSlot.Legs,
            EquipSlot.FullBody          => HumanSlot.Body,
            EquipSlot.BodyHands         => HumanSlot.Body,
            EquipSlot.BodyLegsFeet      => HumanSlot.Body,
            EquipSlot.ChestHands        => HumanSlot.Body,
            _                           => HumanSlot.Unknown,
        };

    public static uint ToIndex(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.Head     => 0,
            EquipSlot.Body     => 1,
            EquipSlot.Hands    => 2,
            EquipSlot.Legs     => 3,
            EquipSlot.Feet     => 4,
            EquipSlot.Ears     => 5,
            EquipSlot.Neck     => 6,
            EquipSlot.Wrists   => 7,
            EquipSlot.RFinger  => 8,
            EquipSlot.LFinger  => 9,
            EquipSlot.MainHand => 10,
            EquipSlot.OffHand  => 11,
            _                  => uint.MaxValue,
        };

    /// <summary> Get the suffix used for a specific EquipSlot in file names. </summary>
    public static string ToSuffix(this EquipSlot value)
        => value switch
        {
            EquipSlot.Head    => "met",
            EquipSlot.Hands   => "glv",
            EquipSlot.Legs    => "dwn",
            EquipSlot.Feet    => "sho",
            EquipSlot.Body    => "top",
            EquipSlot.Ears    => "ear",
            EquipSlot.Neck    => "nek",
            EquipSlot.RFinger => "rir",
            EquipSlot.LFinger => "ril",
            EquipSlot.Wrists  => "wrs",
            _                 => "unk",
        };

    /// <summary> Convert the EquipSlotCategory value to the actual inventory slot it is put in. </summary>
    public static EquipSlot ToSlot(this EquipSlot value)
        => value switch
        {
            EquipSlot.MainHand          => EquipSlot.MainHand,
            EquipSlot.OffHand           => EquipSlot.OffHand,
            EquipSlot.Head              => EquipSlot.Head,
            EquipSlot.Body              => EquipSlot.Body,
            EquipSlot.Hands             => EquipSlot.Hands,
            EquipSlot.Belt              => EquipSlot.Belt,
            EquipSlot.Legs              => EquipSlot.Legs,
            EquipSlot.Feet              => EquipSlot.Feet,
            EquipSlot.Ears              => EquipSlot.Ears,
            EquipSlot.Neck              => EquipSlot.Neck,
            EquipSlot.Wrists            => EquipSlot.Wrists,
            EquipSlot.RFinger           => EquipSlot.RFinger,
            EquipSlot.BothHand          => EquipSlot.MainHand,
            EquipSlot.LFinger           => EquipSlot.RFinger,
            EquipSlot.HeadBody          => EquipSlot.Body,
            EquipSlot.BodyHandsLegsFeet => EquipSlot.Body,
            EquipSlot.SoulCrystal       => EquipSlot.SoulCrystal,
            EquipSlot.LegsFeet          => EquipSlot.Legs,
            EquipSlot.FullBody          => EquipSlot.Body,
            EquipSlot.BodyHands         => EquipSlot.Body,
            EquipSlot.BodyLegsFeet      => EquipSlot.Body,
            EquipSlot.ChestHands        => EquipSlot.Body,
            _                           => EquipSlot.Unknown,
        };

    /// <summary> Translate an EquipSlotCategory into a human readable name.  </summary>
    public static string ToName(this EquipSlot value)
        => value switch
        {
            EquipSlot.Head              => "Head",
            EquipSlot.Hands             => "Hands",
            EquipSlot.Legs              => "Legs",
            EquipSlot.Feet              => "Feet",
            EquipSlot.Body              => "Body",
            EquipSlot.Ears              => "Earrings",
            EquipSlot.Neck              => "Necklace",
            EquipSlot.RFinger           => "Right Ring",
            EquipSlot.LFinger           => "Left Ring",
            EquipSlot.Wrists            => "Bracelets",
            EquipSlot.MainHand          => "Primary Weapon",
            EquipSlot.OffHand           => "Secondary Weapon",
            EquipSlot.Belt              => "Belt",
            EquipSlot.BothHand          => "Primary Weapon",
            EquipSlot.HeadBody          => "Head and Body",
            EquipSlot.BodyHandsLegsFeet => "Costume",
            EquipSlot.SoulCrystal       => "Soul Crystal",
            EquipSlot.LegsFeet          => "Bottom",
            EquipSlot.FullBody          => "Costume",
            EquipSlot.BodyHands         => "Top",
            EquipSlot.BodyLegsFeet      => "Costume",
            EquipSlot.All               => "Costume",
            _                           => "Unknown",
        };

    /// <summary> Returns true for the 5 primary equipment slots. </summary>
    public static bool IsEquipment(this EquipSlot value)
    {
        return value switch
        {
            EquipSlot.Head  => true,
            EquipSlot.Hands => true,
            EquipSlot.Legs  => true,
            EquipSlot.Feet  => true,
            EquipSlot.Body  => true,
            _               => false,
        };
    }

    /// <summary> Returns true for the 5 secondary equipment slots, of which LFinger does not really exist. </summary>
    public static bool IsAccessory(this EquipSlot value)
    {
        return value switch
        {
            EquipSlot.Ears    => true,
            EquipSlot.Neck    => true,
            EquipSlot.RFinger => true,
            EquipSlot.LFinger => true,
            EquipSlot.Wrists  => true,
            _                 => false,
        };
    }

    /// <summary> Returns true for anything worn in an actual equipment slot. </summary>
    public static bool IsEquipmentPiece(this EquipSlot value)
    {
        return value switch
        {
            // Accessories
            EquipSlot.RFinger => true,
            EquipSlot.Wrists  => true,
            EquipSlot.Ears    => true,
            EquipSlot.Neck    => true,
            // Equipment
            EquipSlot.Head              => true,
            EquipSlot.Body              => true,
            EquipSlot.Hands             => true,
            EquipSlot.Legs              => true,
            EquipSlot.Feet              => true,
            EquipSlot.BodyHands         => true,
            EquipSlot.BodyHandsLegsFeet => true,
            EquipSlot.BodyLegsFeet      => true,
            EquipSlot.FullBody          => true,
            EquipSlot.HeadBody          => true,
            EquipSlot.LegsFeet          => true,
            EquipSlot.ChestHands        => true,
            _                           => false,
        };
    }

    /// <summary> A list of all primary equipment pieces. </summary>
    public static readonly IReadOnlyList<EquipSlot> EquipmentSlots = Enum.GetValues<EquipSlot>().Where(e => e.IsEquipment()).ToArray();

    /// <summary> A list of all secondary equipment pieces. </summary>
    public static readonly IReadOnlyList<EquipSlot> AccessorySlots = Enum.GetValues<EquipSlot>().Where(e => e.IsAccessory()).ToArray();

    /// <summary> A list of all primary and secondary equipment pieces. </summary>
    public static readonly IReadOnlyList<EquipSlot> EqdpSlots = EquipmentSlots.Concat(AccessorySlots).ToArray();

    /// <summary> A list of both weapon slots. </summary>
    public static readonly IReadOnlyList<EquipSlot> WeaponSlots =
    [
        EquipSlot.MainHand,
        EquipSlot.OffHand,
    ];

    /// <summary> A list of all equipment slots. </summary>
    public static readonly EquipSlot[] FullSlots = [.. WeaponSlots, .. EqdpSlots];
}

public static partial class Names
{
    /// <summary> A dictionary converting path suffices into EquipSlot. </summary>
    public static readonly IReadOnlyDictionary<string, EquipSlot> SuffixToEquipSlot = FrozenDictionary.ToFrozenDictionary(
    [
        new KeyValuePair<string, EquipSlot>(EquipSlot.Head.ToSuffix(),    EquipSlot.Head),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Hands.ToSuffix(),   EquipSlot.Hands),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Legs.ToSuffix(),    EquipSlot.Legs),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Feet.ToSuffix(),    EquipSlot.Feet),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Body.ToSuffix(),    EquipSlot.Body),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Ears.ToSuffix(),    EquipSlot.Ears),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Neck.ToSuffix(),    EquipSlot.Neck),
        new KeyValuePair<string, EquipSlot>(EquipSlot.RFinger.ToSuffix(), EquipSlot.RFinger),
        new KeyValuePair<string, EquipSlot>(EquipSlot.LFinger.ToSuffix(), EquipSlot.LFinger),
        new KeyValuePair<string, EquipSlot>(EquipSlot.Wrists.ToSuffix(),  EquipSlot.Wrists),
    ]);
}
