namespace Penumbra.GameData.Enums;

/// <summary> Flags for all theoretically available crest positions. </summary>
[Flags]
public enum CrestFlag : ushort
{
    OffHand  = 0x0001,
    Head     = 0x0002,
    Body     = 0x0004,
    Hands    = 0x0008,
    Legs     = 0x0010,
    Feet     = 0x0020,
    Ears     = 0x0040,
    Neck     = 0x0080,
    Wrists   = 0x0100,
    RFinger  = 0x0200,
    LFinger  = 0x0400,
    MainHand = 0x0800,
}

/// <summary> Crests can be applied to different draw objects for the character themselves or their weapons. </summary>
public enum CrestType : byte
{
    None,
    Human,
    Mainhand,
    Offhand,
};

public static class CrestExtensions
{
    /// <summary> All theoretically possible crest flags. </summary>
    public const CrestFlag All = (CrestFlag)(((ulong)EquipFlag.Mainhand << 1) - 1);

    /// <summary> All crest flags actually in use by the game. </summary>
    public const CrestFlag AllRelevant = CrestFlag.Head | CrestFlag.Body | CrestFlag.OffHand;

    /// <summary> A set of the crest flags in use by the game. </summary>
    public static readonly IReadOnlyList<CrestFlag> AllRelevantSet = Enum.GetValues<CrestFlag>().Where(f => AllRelevant.HasFlag(f)).ToArray();

    /// <summary> An internally used index that assigns consecutive numbers to the crest flags in use. </summary>
    public static int ToInternalIndex(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head    => 0,
            CrestFlag.Body    => 1,
            CrestFlag.OffHand => 2,
            _                 => -1,
        };

    /// <summary> Get the index and type used in game data for specific crest flags. </summary>
    public static (CrestType Type, byte Index) ToIndex(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head     => (CrestType.Human, 0),
            CrestFlag.Body     => (CrestType.Human, 1),
            CrestFlag.Hands    => (CrestType.Human, 2),
            CrestFlag.Legs     => (CrestType.Human, 3),
            CrestFlag.Feet     => (CrestType.Human, 4),
            CrestFlag.Ears     => (CrestType.None, 0),
            CrestFlag.Neck     => (CrestType.None, 0),
            CrestFlag.Wrists   => (CrestType.None, 0),
            CrestFlag.RFinger  => (CrestType.None, 0),
            CrestFlag.LFinger  => (CrestType.None, 0),
            CrestFlag.MainHand => (CrestType.None, 0),
            CrestFlag.OffHand  => (CrestType.Offhand, 0),
            _                  => (CrestType.None, 0),
        };

    /// <summary> Get the crest flag corresponding to a specific equip slot. </summary>
    public static CrestFlag ToCrestFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => CrestFlag.MainHand,
            EquipSlot.OffHand  => CrestFlag.OffHand,
            EquipSlot.Head     => CrestFlag.Head,
            EquipSlot.Body     => CrestFlag.Body,
            EquipSlot.Hands    => CrestFlag.Hands,
            EquipSlot.Legs     => CrestFlag.Legs,
            EquipSlot.Feet     => CrestFlag.Feet,
            EquipSlot.Ears     => CrestFlag.Ears,
            EquipSlot.Neck     => CrestFlag.Neck,
            EquipSlot.Wrists   => CrestFlag.Wrists,
            EquipSlot.RFinger  => CrestFlag.RFinger,
            EquipSlot.LFinger  => CrestFlag.LFinger,
            _                  => 0,
        };

    /// <summary> Get a human-readable  name for a crest flag.</summary>
    public static string ToLabel(this CrestFlag flag)
        => flag switch
        {
            CrestFlag.Head     => "Head",
            CrestFlag.Body     => "Chest",
            CrestFlag.Hands    => "Gauntlets",
            CrestFlag.Legs     => "Pants",
            CrestFlag.Feet     => "Boot",
            CrestFlag.Ears     => "Earrings",
            CrestFlag.Neck     => "Necklace",
            CrestFlag.Wrists   => "Bracelet",
            CrestFlag.RFinger  => "Right Ring",
            CrestFlag.LFinger  => "Left Ring",
            CrestFlag.MainHand => "Weapon",
            CrestFlag.OffHand  => "Shield",
            _                  => string.Empty,
        };
}
