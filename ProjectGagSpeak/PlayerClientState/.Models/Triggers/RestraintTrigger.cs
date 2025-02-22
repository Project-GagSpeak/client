namespace GagSpeak.PlayerState.Models;

[Serializable]
public record RestraintTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.RestraintSet;

    // the kind of restraint set that will invoke this trigger's execution
    public Guid RestraintSetId { get; set; } = Guid.Empty;

    // the new state of it that will trigger the execution
    public NewState RestraintState { get; set; } = NewState.Enabled;

    internal RestraintTrigger() { }

    public RestraintTrigger(Trigger baseTrigger, bool keepId) : base(baseTrigger, keepId) { }

    public RestraintTrigger(RestraintTrigger other, bool keepId) : base(other, keepId)
    {
        RestraintSetId = other.RestraintSetId;
        RestraintState = other.RestraintState;
    }
}

[Serializable]
public record RestrictionTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.Restriction;
    public Guid RestrictionId { get; set; } = Guid.Empty;
    public NewState RestrictionState { get; set; } = NewState.Enabled;

    internal RestrictionTrigger() { }

    public RestrictionTrigger(RestrictionTrigger other, bool keepId) : base(other, keepId)
    {
        RestrictionId = other.RestrictionId;
        RestrictionState = other.RestrictionState;
    }
}
