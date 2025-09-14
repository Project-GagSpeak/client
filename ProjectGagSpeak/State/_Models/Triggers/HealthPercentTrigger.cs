using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

[Serializable]
public class HealthPercentTrigger : Trigger, IThresholdContainer
{
    public override TriggerKind Type => TriggerKind.HealthPercent;

    // Player Name to monitor the health % of. use format Player Name@World
    public string PlayerNameWorld { get; set; } = string.Empty;

    // if allowing percentageHealth
    public bool UsePercentageHealth { get; set; } = false;

    // what threshold pass to listen to.
    public ThresholdPassType PassKind { get; set; } = ThresholdPassType.Under;

    // the minValue to display (can either be in percent or normal numbers, based on above option)
    public int ThresholdMinValue { get; set; } = 0;

    // the maxValue to display (can either be in percent or normal numbers, based on above option)
    public int ThresholdMaxValue { get; set; } = 10000000;

    public HealthPercentTrigger()
    { }

    public HealthPercentTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public HealthPercentTrigger(HealthPercentTrigger other, bool keepId)
        : base(other, keepId)
    {
        PlayerNameWorld = other.PlayerNameWorld;
        UsePercentageHealth = other.UsePercentageHealth;
        PassKind = other.PassKind;
        ThresholdMinValue = other.ThresholdMinValue;
        ThresholdMaxValue = other.ThresholdMaxValue;
    }

    public override HealthPercentTrigger Clone(bool keepId) => new HealthPercentTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not HealthPercentTrigger hpt)
            return;

        PlayerNameWorld = hpt.PlayerNameWorld;
        UsePercentageHealth = hpt.UsePercentageHealth;
        PassKind = hpt.PassKind;
        ThresholdMinValue = hpt.ThresholdMinValue;
        ThresholdMaxValue = hpt.ThresholdMaxValue;
    }
}
