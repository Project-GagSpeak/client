namespace GagSpeak.State.Models;

[Serializable]
public class SocialTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.SocialAction;

    public SocialGame Game { get; set; } = SocialGame.DeathRoll;

    public SocialGameResult Result { get; set; } = SocialGameResult.Loss;

    public SocialTrigger()
    { }

    public SocialTrigger(Trigger baseTrigger, bool keepId)
        : base(baseTrigger, keepId)
    { }

    public SocialTrigger(SocialTrigger other, bool clone)
        : base(other, clone)
    {
        Game = other.Game;
    }

    public override SocialTrigger Clone(bool keepId)
        => new SocialTrigger(this, keepId);

    public override void ApplyChanges(Trigger other)
    {
        base.ApplyChanges(other);
        if (other is not SocialTrigger st)
            return;

        Game = st.Game;
        Result = st.Result;
    }
}
