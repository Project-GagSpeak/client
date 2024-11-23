// AttackType from ReceiveActionEffect calls.
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffectTypes/AttackType.cs

namespace GagspeakAPI.Enums;

public enum AttackType
{
    Unknown = 0,
    Slashing = 1,
    Piercing = 2,
    Blunt = 3,
    Shot = 4,
    Magical = 5,
    Unique = 6,
    Physical = 7,
    LimitBreak = 8,
}
