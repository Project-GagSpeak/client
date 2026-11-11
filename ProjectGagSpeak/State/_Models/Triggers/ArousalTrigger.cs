namespace GagSpeak.State.Models;

[Serializable]
public class ArousalTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.Arousal;

    // what threshold pass to listen to.
    public ThresholdPassType PassKind { get; set; } = ThresholdPassType.Over;

    // the arousal % that must be crossed (0-100).
    public int Threshold { get; set; } = 50;

    public ArousalTrigger()
    { }

    public ArousalTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public ArousalTrigger(ArousalTrigger other, bool keepId)
        : base(other, keepId)
    {
        PassKind = other.PassKind;
        Threshold = other.Threshold;
    }

    public override ArousalTrigger Clone(bool keepId) => new ArousalTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not ArousalTrigger at)
            return;

        PassKind = at.PassKind;
        Threshold = at.Threshold;
    }
}
