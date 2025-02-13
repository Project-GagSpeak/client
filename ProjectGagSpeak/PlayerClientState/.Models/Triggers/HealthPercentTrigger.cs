namespace GagSpeak.PlayerState.Models;

[Serializable]
public record HealthPercentTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.HealthPercent;

    // Player Name to monitor the health % of. use format Player Name@World
    public string PlayerToMonitor { get; set; } = string.Empty;

    // if allowing percentageHealth
    public bool UsePercentageHealth { get; set; } = false;

    // what threshold pass to listen to.
    public ThresholdPassType PassKind { get; set; } = ThresholdPassType.Under;

    // the minValue to display (can either be in percent or normal numbers, based on above option)
    public int MinHealthValue { get; set; } = 0;

    // the maxValue to display (can either be in percent or normal numbers, based on above option)
    public int MaxHealthValue { get; set; } = 10000000;

    public override HealthPercentTrigger DeepClone()
    {
        return new HealthPercentTrigger
        {
            Identifier = Identifier,
            Enabled = Enabled,
            Priority = Priority,
            Label = Label,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone(),
            PlayerToMonitor = PlayerToMonitor,
            UsePercentageHealth = UsePercentageHealth,
            PassKind = PassKind,
            MinHealthValue = MinHealthValue,
            MaxHealthValue = MaxHealthValue
        };
    }
}
