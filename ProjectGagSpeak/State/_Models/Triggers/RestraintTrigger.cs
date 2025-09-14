namespace GagSpeak.State.Models;

[Serializable]
public class RestraintTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.RestraintSet;

    // the kind of restraint set that will invoke this trigger's execution
    public Guid RestraintSetId { get; set; } = Guid.Empty;

    // the new state of it that will trigger the execution
    public NewState RestraintState { get; set; } = NewState.Enabled;

    public RestraintTrigger()
    { }

    public RestraintTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public RestraintTrigger(RestraintTrigger other, bool keepId) : base(other, keepId)
    {
        RestraintSetId = other.RestraintSetId;
        RestraintState = other.RestraintState;
    }

    public override RestraintTrigger Clone(bool keepId) => new RestraintTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not RestraintTrigger rst)
            return;

        RestraintSetId = rst.RestraintSetId;
        RestraintState = rst.RestraintState;
    }
}
