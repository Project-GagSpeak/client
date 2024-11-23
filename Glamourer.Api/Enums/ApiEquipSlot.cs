namespace Glamourer.Api.Enums;

/// <summary> Equip slots restricted to API-relevant slots, but compatible with GameData.EquipSlots. </summary>
public enum ApiEquipSlot : byte
{
    /// <summary> No slot. </summary>
    Unknown = 0,

    /// <summary> Mainhand, also used for both-handed weapons. </summary>
    MainHand = 1,

    /// <summary> Offhand, used for shields or if you want to apply the offhand component of certain weapons. </summary>
    OffHand = 2,

    /// <summary> Head. </summary>
    Head = 3,

    /// <summary> Body. </summary>
    Body = 4,

    /// <summary> Hands. </summary>
    Hands = 5,

    /// <summary> Legs. </summary>
    Legs = 7,

    /// <summary> Feet. </summary>
    Feet = 8,

    /// <summary> Ears. </summary>
    Ears = 9,

    /// <summary> Neck. </summary>
    Neck = 10,

    /// <summary> Wrists. </summary>
    Wrists = 11,

    /// <summary> Right Finger. </summary>
    RFinger = 12,

    /// <summary> Left Finger. </summary>
    /// <remarks> Not officially existing, means "weapon could be equipped in either hand" for the game. </remarks>
    LFinger = 14,
}
