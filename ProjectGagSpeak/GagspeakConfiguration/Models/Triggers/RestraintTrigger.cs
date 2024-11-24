using GagspeakAPI.Enums;
using GagspeakAPI.Data;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// Trigger fired when a restraint set goes into a specific state.
/// </summary>
[Serializable]
public record RestraintTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.RestraintSet;

    // the kind of restraint set that will invoke this trigger's execution
    public Guid RestraintSetId { get; set; } = Guid.Empty;

    // the new state of it that will trigger the execution
    public NewState RestraintState { get; set; } = NewState.Enabled;

    public override RestraintTrigger DeepClone()
    {
        return new RestraintTrigger
        {
            TriggerIdentifier = TriggerIdentifier,
            Enabled = Enabled,
            Priority = Priority,
            Name = Name,
            Description = Description,
            StartAfter = StartAfter,
            EndAfter = EndAfter,
            TriggerActionKind = TriggerActionKind,
            TriggerAction = TriggerAction,
            ShockTriggerAction = ShockTriggerAction,
            RestraintTriggerAction = RestraintTriggerAction,
            GagTypeAction = GagTypeAction,
            MoodlesIdentifier = MoodlesIdentifier,
            RestraintSetId = RestraintSetId,
            RestraintState = RestraintState
        };
    }
}
