namespace GagSpeak.PlayerState.Models;

[Serializable]
public record EmoteTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.EmoteAction;

    // the emote ID to look for.
    public uint EmoteID { get; set; } = uint.MaxValue;

    // What state should the emote be in when triggering this?
    public TriggerDirection EmoteDirection { get; set; } = TriggerDirection.Self;

    // if the 'other' is player spesific, define the player here.
    public string EmotePlayerNameWorld { get; set; } = string.Empty;

    public EmoteTrigger() { }

    public EmoteTrigger(Trigger baseTrigger, bool keepId) : base(baseTrigger, keepId) { }

    public EmoteTrigger(EmoteTrigger other, bool keepId) : base(other, keepId)
    {
        EmoteID = other.EmoteID;
        EmoteDirection = other.EmoteDirection;
        EmotePlayerNameWorld = other.EmotePlayerNameWorld;
    }
}
