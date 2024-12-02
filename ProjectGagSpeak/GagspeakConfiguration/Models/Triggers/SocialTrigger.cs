namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
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
            Name = Name,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone(),
            SocialType = SocialType
        };
    }
}
