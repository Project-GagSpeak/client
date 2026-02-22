using CkCommons;
using GagSpeak.State.Models;

namespace GagSpeak.Utils;

public static class TriggersEx
{
    /// <summary> Gets the only options for direction that should be available. </summary>
    public static IEnumerable<TriggerDirection> GetOptions(this SpellActionTrigger trigger) =>
        trigger.ActionKind switch
        {
            LimitedActionEffectType.Miss or
            LimitedActionEffectType.BlockedDamage or
            LimitedActionEffectType.ParriedDamage or
            LimitedActionEffectType.Knockback or
            LimitedActionEffectType.Attract1
                => [TriggerDirection.Self, TriggerDirection.Other, TriggerDirection.Any],
            LimitedActionEffectType.Damage
                => [TriggerDirection.SelfToOther, TriggerDirection.Other, TriggerDirection.OtherToSelf, TriggerDirection.Any],
            LimitedActionEffectType.Heal or
            LimitedActionEffectType.Nothing or
            _ => Enum.GetValues<TriggerDirection>()
        };

    public static string GetDirectionText(this TriggerDirection dir, LimitedActionEffectType curLAET)
    {
        return (curLAET, dir) switch
        {
            // === Miss ===
            (LimitedActionEffectType.Miss, TriggerDirection.Self) => "you miss an attack.",
            (LimitedActionEffectType.Miss, TriggerDirection.Other) => "someone else misses an attack.",
            (LimitedActionEffectType.Miss, TriggerDirection.Any) => "anyone misses an attack.",

            // === Blocked ===
            (LimitedActionEffectType.BlockedDamage, TriggerDirection.Self) => "you block damage.",
            (LimitedActionEffectType.BlockedDamage, TriggerDirection.Other) => "someone else blocks damage.",
            (LimitedActionEffectType.BlockedDamage, TriggerDirection.Any) => "anyone blocks damage.",

            // === Parried ===
            (LimitedActionEffectType.ParriedDamage, TriggerDirection.Self) => "you parry damage.",
            (LimitedActionEffectType.ParriedDamage, TriggerDirection.Other) => "someone else parries damage.",
            (LimitedActionEffectType.ParriedDamage, TriggerDirection.Any) => "anyone parries damage.",

            // === Knockback ===
            (LimitedActionEffectType.Knockback, TriggerDirection.Self) => "you knock back a target.",
            (LimitedActionEffectType.Knockback, TriggerDirection.Other) => "someone else knocks back a target.",
            (LimitedActionEffectType.Knockback, TriggerDirection.Any) => "anyone knocks back a target.",

            // === Attract1 ===
            (LimitedActionEffectType.Attract1, TriggerDirection.Self) => "you attract a target.",
            (LimitedActionEffectType.Attract1, TriggerDirection.Other) => "someone else attracts a target.",
            (LimitedActionEffectType.Attract1, TriggerDirection.Any) => "anyone attracts a target.",

            // === Damage ===
            (LimitedActionEffectType.Damage, TriggerDirection.SelfToOther) => "you deal damage to someone.",
            (LimitedActionEffectType.Damage, TriggerDirection.Other) => "someone deals damage.",
            (LimitedActionEffectType.Damage, TriggerDirection.OtherToSelf) => "someone deals damage to you.",
            (LimitedActionEffectType.Damage, TriggerDirection.Any) => "any damage is dealt.",

            // === Heal ===
            (LimitedActionEffectType.Heal, TriggerDirection.Self) => "you heal yourself or others.",
            (LimitedActionEffectType.Heal, TriggerDirection.SelfToOther) => "you heal someone.",
            (LimitedActionEffectType.Heal, TriggerDirection.Other) => "someone else heals.",
            (LimitedActionEffectType.Heal, TriggerDirection.OtherToSelf) => "someone heals you.",
            (LimitedActionEffectType.Heal, TriggerDirection.Any) => "any healing is done.",

            // === Nothing ===
            (LimitedActionEffectType.Nothing, TriggerDirection.Self) => "you perform an Action.",
            (LimitedActionEffectType.Nothing, TriggerDirection.SelfToOther) => "you perform an Action to another.",
            (LimitedActionEffectType.Nothing, TriggerDirection.Other) => "another performs a Spell/Action.",
            (LimitedActionEffectType.Nothing, TriggerDirection.OtherToSelf) => "another performs a Spell/Action on you.",
            (LimitedActionEffectType.Nothing, TriggerDirection.Any) => "anyone performs a Spell/Action.",

            // === Fallbacks / Default ===
            (_, _) => $"[{dir}] action ({curLAET})"
        };
    }
}
