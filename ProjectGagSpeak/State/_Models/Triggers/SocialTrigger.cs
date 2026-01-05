namespace GagSpeak.State.Models;

[Serializable]
public class SocialTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.SocialAction;

    public SocialActionType SocialType { get; set; } = SocialActionType.DeathRollLoss;

    public SocialTrigger()
    { }

    public SocialTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public SocialTrigger(SocialTrigger other, bool clone)
        : base(other, clone)
    {
        SocialType = other.SocialType;
    }

    public override SocialTrigger Clone(bool keepId) => new SocialTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not SocialTrigger st)
            return;

        SocialType = st.SocialType;
    }
}
