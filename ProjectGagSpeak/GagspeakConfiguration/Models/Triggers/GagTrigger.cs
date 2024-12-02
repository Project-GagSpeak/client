using GagspeakAPI.Enums;
using GagspeakAPI.Enums;
using GagspeakAPI.Data;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
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
            Name = Name,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone(),
            Gag = Gag,
            GagState = GagState
        };
    }
}
