namespace Penumbra.GameData.Enums;

/// <summary> Bonus Equip Slots as flags, currently two. </summary>
[Flags]
public enum BonusItemFlag : byte
{
    Unknown = 0x00,
    Glasses = 0x01,
    UnkSlot = 0x02,
}

public static class BonusExtensions
{
    public const BonusItemFlag All = BonusItemFlag.Glasses;

    public static readonly IReadOnlyList<BonusItemFlag> AllFlags = [BonusItemFlag.Glasses];

    public static uint ToIndex(this BonusItemFlag flag)
        => flag switch
        {
            BonusItemFlag.Glasses => 0,
            BonusItemFlag.UnkSlot => 1,
            _                     => uint.MaxValue,
        };

    public static uint ToSlot(this BonusItemFlag flag)
        => flag switch
        {
            BonusItemFlag.Glasses => 10,
            BonusItemFlag.UnkSlot => 11,
            _                     => uint.MaxValue,
        };

    public static uint ToModelIndex(this BonusItemFlag flag)
        => flag switch
        {
            BonusItemFlag.Glasses => 16,
            BonusItemFlag.UnkSlot => 17,
            _                     => uint.MaxValue,
        };

    public static string ToSuffix(this BonusItemFlag value)
        => value switch
        {
            BonusItemFlag.Glasses => "met",
            BonusItemFlag.UnkSlot => "unk",
            _                     => "unk",
        };

    public static EquipSlot ToEquipSlot(this BonusItemFlag value)
        => value switch
        {
            BonusItemFlag.Glasses => EquipSlot.Head,
            BonusItemFlag.UnkSlot => EquipSlot.Unknown,
            _                     => EquipSlot.Unknown,
        };

    public static string ToName(this BonusItemFlag value)
        => value switch
        {
            BonusItemFlag.Glasses => "Glasses",
            BonusItemFlag.UnkSlot => "Bonus Slot 2",
            _                     => "Unknown",
        };

    public static BonusItemFlag ToBonusSlot(this uint slot)
        => slot switch
        {
            0 => BonusItemFlag.Glasses,
            1 => BonusItemFlag.UnkSlot,
            _ => BonusItemFlag.Unknown,
        };
}
