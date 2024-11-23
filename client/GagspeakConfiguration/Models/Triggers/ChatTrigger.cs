using GagSpeak.ChatMessages;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public record ChatTrigger : Trigger
{
    public override TriggerKind Type => TriggerKind.Chat;

    // the chat text we are looking for
    public string ChatText { get; set; } = string.Empty;

    // (Optional) Who must say the text for it to trigger. (If left blank, accepts any)
    public string FromPlayerName { get; set; } = string.Empty;

    // The allowed channels this text can be scanned for in:
    public List<ChatChannel.Channels> AllowedChannels { get; set; } = [];

    public override ChatTrigger DeepClone()
    {
        return new ChatTrigger
        {
            TriggerIdentifier = TriggerIdentifier,
            Enabled = Enabled,
            Priority = Priority,
            Name = Name,
            Description = Description,
            StartAfter = StartAfter,
            EndAfter = EndAfter,
            TriggerActionKind = TriggerActionKind,
            TriggerAction = TriggerAction,
            ShockTriggerAction = ShockTriggerAction,
            RestraintTriggerAction = RestraintTriggerAction,
            GagTypeAction = GagTypeAction,
            MoodlesIdentifier = MoodlesIdentifier,
            ChatText = ChatText,
            FromPlayerName = FromPlayerName,
            AllowedChannels = AllowedChannels
        };
    }
}
