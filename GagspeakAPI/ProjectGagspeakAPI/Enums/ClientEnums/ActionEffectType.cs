namespace GagspeakAPI.Enums;


// Only ones we care about for GagSpeak.
public enum LimitedActionEffectType : byte
{
    Miss = 1,
    Damage = 3,
    Heal = 4,
    BlockedDamage = 5,
    ParriedDamage = 6,
    Knockback = 32,
    Attract1 = 33, //Here is an issue bout knockback. some is 32 some is 33.
}


// ActionEffectTypes from ReceiveActionEffect calls.
// https://github.com/NightmareXIV/ECommons/blob/master/ECommons/Hooks/ActionEffectTypes/ActionEffectType.cs
public enum ActionEffectType : byte
{
    Nothing = 0,
    Miss = 1,
    FullResist = 2,
    Damage = 3,
    Heal = 4,
    BlockedDamage = 5,
    ParriedDamage = 6,
    Invulnerable = 7,
    NoEffectText = 8,
    Unknown_0 = 9,
    MpLoss = 10,
    MpGain = 11,
    TpLoss = 12,
    TpGain = 13,

    ApplyStatusEffectTarget = 14,
    [Obsolete("Please use ApplyStatusEffectTarget instead.")]
    GpGain = ApplyStatusEffectTarget,

    ApplyStatusEffectSource = 15,
    RecoveredFromStatusEffect = 16,
    LoseStatusEffectTarget = 17,
    LoseStatusEffectSource = 18,
    StatusNoEffect = 20,
    ThreatPosition = 24,
    EnmityAmountUp = 25,
    EnmityAmountDown = 26,

    StartActionCombo = 27,
    [Obsolete("Please use StartActionCombo instead.")]
    Unknown0 = StartActionCombo,

    ComboSucceed = 28,
    [Obsolete("Please use ComboSucceed instead.")]
    Unknown1 = ComboSucceed,

    Retaliation = 29,
    Knockback = 32,
    Attract1 = 33, //Here is an issue bout knockback. some is 32 some is 33.
    Attract2 = 34,
    Mount = 40,
    FullResistStatus = 52,
    FullResistStatus2 = 55,
    VFX = 59,
    Gauge = 60,
    JobGauge = 61,
    Shirk = 62, // temp filler from what i understand.
    SetModelState = 72,
    SetHP = 73,
    PartialInvulnerable = 74,
    Interrupt = 75,
};
