namespace GagSpeak.PlayerState.Models;

[Serializable]
public record SocialTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.SocialAction;

    // the social action to monitor.
    public SocialActionType SocialType { get; set; } = SocialActionType.DeathRollLoss;

    public override SocialTrigger DeepClone()
    {
        return new SocialTrigger
        {
            Identifier = Identifier,
            Enabled = Enabled,
            Priority = Priority,
            Label = Label,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone(),
            SocialType = SocialType
        };
    }
}
