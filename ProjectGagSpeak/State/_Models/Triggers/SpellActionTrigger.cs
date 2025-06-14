using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils.Enums;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

[Serializable]
public class SpellActionTrigger : Trigger, IThresholdContainer
{
    public override TriggerKind Type => TriggerKind.SpellAction;

    // Should the Action/Spell originate from you, or another target?
    public TriggerDirection Direction { get; set; } = TriggerDirection.Self;

    // If the target should be spesified, who should it be? (maybe make contentID idk)
    public string PlayerNameWorld { get; set; } = string.Empty;

    // If we want to detect generic filter types or not. True == LimitedActionEffectType, false == actionID's.
    public bool IsGenericDetection { get; set; } = false;

    // the type of action we are scanning for.
    public LimitedActionEffectType ActionKind { get; set; } = LimitedActionEffectType.Damage;

    // The actions that we want to detect, sorted by jobId.
    // This is done by having a SpellActionService that can fetch the correct cached lists at runtime for display.
    // Additionally, by only storing small dataTypes, values can be efficiently fetched.
    public Dictionary<JobType, List<uint>> StoredActions { get; set; } = new Dictionary<JobType, List<uint>>();

    // the threshold value that must be healed/dealt to trigger the action (-1 = full, 0 = onAction)
    public int ThresholdMinValue { get; set; } = -1;
    public int ThresholdMaxValue { get; set; } = 10000000;

    public SpellActionTrigger()
    { }

    public SpellActionTrigger(Trigger baseTrigger, bool keepId) 
        : base(baseTrigger, keepId)
    { }

    public SpellActionTrigger(SpellActionTrigger other, bool keepId) 
        : base(other, keepId)
    {
        Direction = other.Direction;
        PlayerNameWorld = other.PlayerNameWorld;
        IsGenericDetection = other.IsGenericDetection;
        ActionKind = other.ActionKind;
        StoredActions = new Dictionary<JobType, List<uint>>(other.StoredActions);
        ThresholdMinValue = other.ThresholdMinValue;
        ThresholdMaxValue = other.ThresholdMaxValue;
    }

    public override SpellActionTrigger Clone(bool keepId) => new SpellActionTrigger(this, keepId);

    // This can either get very optimial or very cancerous with no inbetween, try to find a better approach to this.
    public IEnumerable<uint> GetStoredIds() => StoredActions.Values.SelectMany(_ => _);

    public void ApplyChanges(SpellActionTrigger other)
    {
        Direction = other.Direction;
        PlayerNameWorld = other.PlayerNameWorld;
        IsGenericDetection = other.IsGenericDetection;
        ActionKind = other.ActionKind;
        StoredActions = new Dictionary<JobType, List<uint>>(other.StoredActions);
        ThresholdMinValue = other.ThresholdMinValue;
        ThresholdMaxValue = other.ThresholdMaxValue;
        base.ApplyChanges(other);
    }
}

public static class SpellActionEx
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
