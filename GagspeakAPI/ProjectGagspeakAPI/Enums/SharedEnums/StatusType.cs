namespace GagspeakAPI.Data.IPC;
public enum StatusType
{
    Positive,
    Negative,
    Special
}

/// <summary>
/// Equipment Slot Enum pulled from Penumbra.GameData.Enums namespace.
/// We use this to determine the slot we apply things to.
/// </summary>
public enum EquipSlotGS : byte
{
    Unknown = 0,
    MainHand = 1,
    OffHand = 2,
    Head = 3,
    Body = 4,
    Hands = 5,
    Belt = 6,
    Legs = 7,
    Feet = 8,
    Ears = 9,
    Neck = 10,
    Wrists = 11,
    RFinger = 12,
    BothHand = 13,
    LFinger = 14,
    HeadBody = 15,
    BodyHandsLegsFeet = 16,
    SoulCrystal = 17,
    LegsFeet = 18,
    FullBody = 19,
    BodyHands = 20,
    BodyLegsFeet = 21,
    ChestHands = 22,
    Nothing = 23,
    All = 24,
}

