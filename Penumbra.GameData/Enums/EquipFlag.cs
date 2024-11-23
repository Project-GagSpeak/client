namespace Penumbra.GameData.Enums;

/// <summary> Flags for specific parts of equipment pieces. </summary>
[Flags]
public enum EquipFlag : uint
{
    Head          = 0x00000001,
    Body          = 0x00000002,
    Hands         = 0x00000004,
    Legs          = 0x00000008,
    Feet          = 0x00000010,
    Ears          = 0x00000020,
    Neck          = 0x00000040,
    Wrist         = 0x00000080,
    RFinger       = 0x00000100,
    LFinger       = 0x00000200,
    Mainhand      = 0x00000400,
    Offhand       = 0x00000800,
    HeadStain     = 0x00001000,
    BodyStain     = 0x00002000,
    HandsStain    = 0x00004000,
    LegsStain     = 0x00008000,
    FeetStain     = 0x00010000,
    EarsStain     = 0x00020000,
    NeckStain     = 0x00040000,
    WristStain    = 0x00080000,
    RFingerStain  = 0x00100000,
    LFingerStain  = 0x00200000,
    MainhandStain = 0x00400000,
    OffhandStain  = 0x00800000,
}

public static class EquipFlagExtensions
{
    /// <summary> All equipment flags. </summary>
    public const EquipFlag All = (EquipFlag)(((uint)EquipFlag.OffhandStain << 1) - 1);

    /// <summary> The number of available equipment flags. </summary>
    public const int NumEquipFlags = 24;

    /// <summary> Convert an equip slot to its flag, if possible. </summary>
    public static EquipFlag ToFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.Mainhand,
            EquipSlot.OffHand  => EquipFlag.Offhand,
            EquipSlot.Head     => EquipFlag.Head,
            EquipSlot.Body     => EquipFlag.Body,
            EquipSlot.Hands    => EquipFlag.Hands,
            EquipSlot.Legs     => EquipFlag.Legs,
            EquipSlot.Feet     => EquipFlag.Feet,
            EquipSlot.Ears     => EquipFlag.Ears,
            EquipSlot.Neck     => EquipFlag.Neck,
            EquipSlot.Wrists   => EquipFlag.Wrist,
            EquipSlot.RFinger  => EquipFlag.RFinger,
            EquipSlot.LFinger  => EquipFlag.LFinger,
            _                  => 0,
        };

    /// <summary> Convert an equip slot to the flag for its stain, if possible. </summary>
    public static EquipFlag ToStainFlag(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.MainhandStain,
            EquipSlot.OffHand  => EquipFlag.OffhandStain,
            EquipSlot.Head     => EquipFlag.HeadStain,
            EquipSlot.Body     => EquipFlag.BodyStain,
            EquipSlot.Hands    => EquipFlag.HandsStain,
            EquipSlot.Legs     => EquipFlag.LegsStain,
            EquipSlot.Feet     => EquipFlag.FeetStain,
            EquipSlot.Ears     => EquipFlag.EarsStain,
            EquipSlot.Neck     => EquipFlag.NeckStain,
            EquipSlot.Wrists   => EquipFlag.WristStain,
            EquipSlot.RFinger  => EquipFlag.RFingerStain,
            EquipSlot.LFinger  => EquipFlag.LFingerStain,
            _                  => 0,
        };

    /// <summary> Convert an equip slot to the combination of both its associated flags. </summary>
    public static EquipFlag ToBothFlags(this EquipSlot slot)
        => slot switch
        {
            EquipSlot.MainHand => EquipFlag.Mainhand | EquipFlag.MainhandStain,
            EquipSlot.OffHand  => EquipFlag.Offhand | EquipFlag.OffhandStain,
            EquipSlot.Head     => EquipFlag.Head | EquipFlag.HeadStain,
            EquipSlot.Body     => EquipFlag.Body | EquipFlag.BodyStain,
            EquipSlot.Hands    => EquipFlag.Hands | EquipFlag.HandsStain,
            EquipSlot.Legs     => EquipFlag.Legs | EquipFlag.LegsStain,
            EquipSlot.Feet     => EquipFlag.Feet | EquipFlag.FeetStain,
            EquipSlot.Ears     => EquipFlag.Ears | EquipFlag.EarsStain,
            EquipSlot.Neck     => EquipFlag.Neck | EquipFlag.NeckStain,
            EquipSlot.Wrists   => EquipFlag.Wrist | EquipFlag.WristStain,
            EquipSlot.RFinger  => EquipFlag.RFinger | EquipFlag.RFingerStain,
            EquipSlot.LFinger  => EquipFlag.LFinger | EquipFlag.LFingerStain,
            _                  => 0,
        };

    /// <summary> Convert an equip flag back to its slot. </summary>
    public static EquipSlot ToSlot(this EquipFlag flag)
        => ToSlot(flag, out _);

    /// <summary> Convert an equip flag back to its slot. </summary>
    public static EquipSlot ToSlot(this EquipFlag flag, out bool stain)
    {
        (var ret, stain) = flag switch
        {
            EquipFlag.Mainhand      => (EquipSlot.MainHand, false),
            EquipFlag.Offhand       => (EquipSlot.OffHand, false),
            EquipFlag.Head          => (EquipSlot.Head, false),
            EquipFlag.Body          => (EquipSlot.Body, false),
            EquipFlag.Hands         => (EquipSlot.Hands, false),
            EquipFlag.Legs          => (EquipSlot.Legs, false),
            EquipFlag.Feet          => (EquipSlot.Feet, false),
            EquipFlag.Ears          => (EquipSlot.Ears, false),
            EquipFlag.Neck          => (EquipSlot.Neck, false),
            EquipFlag.Wrist         => (EquipSlot.Wrists, false),
            EquipFlag.RFinger       => (EquipSlot.RFinger, false),
            EquipFlag.LFinger       => (EquipSlot.LFinger, false),
            EquipFlag.MainhandStain => (EquipSlot.MainHand, true),
            EquipFlag.OffhandStain  => (EquipSlot.OffHand, true),
            EquipFlag.HeadStain     => (EquipSlot.Head, true),
            EquipFlag.BodyStain     => (EquipSlot.Body, true),
            EquipFlag.HandsStain    => (EquipSlot.Hands, true),
            EquipFlag.LegsStain     => (EquipSlot.Legs, true),
            EquipFlag.FeetStain     => (EquipSlot.Feet, true),
            EquipFlag.EarsStain     => (EquipSlot.Ears, true),
            EquipFlag.NeckStain     => (EquipSlot.Neck, true),
            EquipFlag.WristStain    => (EquipSlot.Wrists, true),
            EquipFlag.RFingerStain  => (EquipSlot.RFinger, true),
            EquipFlag.LFingerStain  => (EquipSlot.LFinger, true),
            _                       => (EquipSlot.Unknown, false),
        };
        return ret;
    }
}
