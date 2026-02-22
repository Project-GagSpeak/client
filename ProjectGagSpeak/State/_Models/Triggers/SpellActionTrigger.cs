using CkCommons;
using GagSpeak.Utils;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

[Serializable]
public class SpellActionTrigger : Trigger, IThresholdContainer
{
    public override TriggerKind Type => TriggerKind.SpellAction;

    // Should the Action/Spell originate from you, or another target?
    public TriggerDirection Direction { get; set; } = TriggerDirection.Self;

    // If the target should be spesified, who should it be? (maybe make contentID idk)
    public string PlayerNameWorld { get; set; } = string.Empty;

    // If we want to detect generic filter types or not. True == LimitedActionEffectType, false == actionID's.
    public bool IsGenericDetection { get; set; } = false;

    // the type of action we are scanning for.
    public LimitedActionEffectType ActionKind { get; set; } = LimitedActionEffectType.Damage;

    // The actions that we want to detect, sorted by jobId.
    // This is done by having a SpellActionService that can fetch the correct cached lists at runtime for display.
    // Additionally, by only storing small dataTypes, values can be efficiently fetched.
    public Dictionary<JobType, List<uint>> StoredActions { get; set; } = new Dictionary<JobType, List<uint>>();

    // the threshold value that must be healed/dealt to trigger the action (-1 = full, 0 = onAction)
    public int ThresholdMinValue { get; set; } = -1;
    public int ThresholdMaxValue { get; set; } = 10000000;

    public SpellActionTrigger()
    { }

    public SpellActionTrigger(Trigger baseTrigger, bool keepId) 
        : base(baseTrigger, keepId)
    { }

    public SpellActionTrigger(SpellActionTrigger other, bool keepId) 
        : base(other, keepId)
    {
        Direction = other.Direction;
        PlayerNameWorld = other.PlayerNameWorld;
        IsGenericDetection = other.IsGenericDetection;
        ActionKind = other.ActionKind;
        StoredActions = new Dictionary<JobType, List<uint>>(other.StoredActions);
        ThresholdMinValue = other.ThresholdMinValue;
        ThresholdMaxValue = other.ThresholdMaxValue;
    }

    public override SpellActionTrigger Clone(bool keepId) => new SpellActionTrigger(this, keepId);

    // This can either get very optimial or very cancerous with no inbetween, try to find a better approach to this.
    public IEnumerable<uint> GetStoredIds() => StoredActions.Values.SelectMany(_ => _);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not SpellActionTrigger sat)
            return;

        Direction = sat.Direction;
        PlayerNameWorld = sat.PlayerNameWorld;
        IsGenericDetection = sat.IsGenericDetection;
        ActionKind = sat.ActionKind;
        StoredActions = new Dictionary<JobType, List<uint>>(sat.StoredActions);
        ThresholdMinValue = sat.ThresholdMinValue;
        ThresholdMaxValue = sat.ThresholdMaxValue;
    }
}
