namespace GagSpeak.PlayerState.Models;

[Serializable]
public record GagTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.GagState;

    // the gag that must be toggled to execute the trigger
    public GagType Gag { get; set; } = GagType.None;
    
    // the state of the gag that invokes it.
    public NewState GagState { get; set; } = NewState.Enabled;

    internal GagTrigger() { }

    public GagTrigger(GagTrigger other, bool keepId) : base(other, keepId)
    {
        Gag = other.Gag;
        GagState = other.GagState;
    }
}
