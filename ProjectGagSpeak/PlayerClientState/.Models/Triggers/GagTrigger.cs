namespace GagSpeak.PlayerState.Models;

[Serializable]
public record GagTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.GagState;

    // the gag that must be toggled to execute the trigger
    public GagType Gag { get; set; } = GagType.None;
    
    // the state of the gag that invokes it.
    public NewState GagState { get; set; } = NewState.Enabled;

    public override GagTrigger DeepClone()
    {
        return new GagTrigger
        {
            Identifier = Identifier,
            Enabled = Enabled,
            Priority = Priority,
            Label = Label,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone(),
            Gag = Gag,
            GagState = GagState
        };
    }
}
