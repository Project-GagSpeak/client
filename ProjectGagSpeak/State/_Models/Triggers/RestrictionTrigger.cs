namespace GagSpeak.PlayerState.Models;

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

    public override RestrictionTrigger Clone(bool keepId) => new RestrictionTrigger(this, keepId);

    public void ApplyChanges(RestrictionTrigger other)
    {
        RestrictionId = other.RestrictionId;
        RestrictionState = other.RestrictionState;
        base.ApplyChanges(other);
    }
}
