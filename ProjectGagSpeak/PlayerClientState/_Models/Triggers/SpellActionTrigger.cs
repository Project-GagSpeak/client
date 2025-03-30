namespace GagSpeak.PlayerState.Models;

[Serializable]
public record SpellActionTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.SpellAction;

    // the type of action we are scanning for.
    public LimitedActionEffectType ActionKind { get; set; } = LimitedActionEffectType.Damage;

    // (self = done to you, target = done by you) Conditions vary based on actionKind
    public TriggerDirection Direction { get; set; } = TriggerDirection.Self;

    // the ID of the action to listen to.
    public uint ActionID { get; set; } = uint.MaxValue;

    // the threshold value that must be healed/dealt to trigger the action (-1 = full, 0 = onAction)
    public int ThresholdMinValue { get; set; } = -1;
    public int ThresholdMaxValue { get; set; } = 10000000;

    public SpellActionTrigger() { }

    public SpellActionTrigger(Trigger baseTrigger, bool keepId) : base(baseTrigger, keepId) { }

    public SpellActionTrigger(SpellActionTrigger other, bool keepId) : base(other, keepId)
    {
        ActionKind = other.ActionKind;
        Direction = other.Direction;
        ActionID = other.ActionID;
        ThresholdMinValue = other.ThresholdMinValue;
        ThresholdMaxValue = other.ThresholdMaxValue;
    }
}
