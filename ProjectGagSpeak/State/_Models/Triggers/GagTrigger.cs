namespace GagSpeak.State.Models;

[Serializable]
public class GagTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.GagState;

    // the gag that must be toggled to execute the trigger
    public GagType Gag { get; set; } = GagType.None;
    
    // the state of the gag that invokes it.
    public NewState GagState { get; set; } = NewState.Enabled;

    public GagTrigger()
    { }

    public GagTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public GagTrigger(GagTrigger other, bool keepId)
        : base(other, keepId)
    {
        Gag = other.Gag;
        GagState = other.GagState;
    }

    public override GagTrigger Clone(bool keepId) => new GagTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not GagTrigger gt)
            return;

        Gag = gt.Gag;
        GagState = gt.GagState;
    }
}
