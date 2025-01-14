namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
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

    public override EmoteTrigger DeepClone()
    {
        return new EmoteTrigger
        {
            EmoteID = EmoteID,
            EmoteDirection = EmoteDirection,
            EmotePlayerNameWorld = EmotePlayerNameWorld,
            Identifier = Identifier,
            Enabled = Enabled,
            Priority = Priority,
            Name = Name,
            Description = Description,
            ExecutableAction = ExecutableAction.DeepClone()
        };
    }
}
