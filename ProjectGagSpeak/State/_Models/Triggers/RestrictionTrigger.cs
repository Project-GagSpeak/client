namespace GagSpeak.State.Models;

[Serializable]
public class RestrictionTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.Restriction;
    public Guid RestrictionId { get; set; } = Guid.Empty;
    public NewState RestrictionState { get; set; } = NewState.Enabled;

    public RestrictionTrigger()
    { }

    public RestrictionTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public RestrictionTrigger(RestrictionTrigger other, bool keepId)
        : base(other, keepId)
    {
        RestrictionId = other.RestrictionId;
        RestrictionState = other.RestrictionState;
    }

    public override RestrictionTrigger Clone(bool keepId)
        => new RestrictionTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not RestrictionTrigger rt)
            return;

        RestrictionId = rt.RestrictionId;
        RestrictionState = rt.RestrictionState;
        base.ApplyChanges(other);
    }
}
