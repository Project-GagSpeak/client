namespace GagSpeak.PlayerState.Models;

[Serializable]
public record SocialTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.SocialAction;

    // the social action to monitor.
    public SocialActionType SocialType { get; set; } = SocialActionType.DeathRollLoss;

    public SocialTrigger() { }

    public SocialTrigger(Trigger baseTrigger, bool keepId) : base(baseTrigger, keepId) { }

    public SocialTrigger(SocialTrigger other, bool clone) : base(other, clone)
    {
        SocialType = other.SocialType;
    }
}
