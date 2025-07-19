namespace GagSpeak.Utils;

public struct SerializableChatLog
{
    public DateTime DateStarted { get; set; }
    public List<GagSpeakChatMessage> Messages { get; set; }

    public SerializableChatLog(DateTime dateStarted, List<GagSpeakChatMessage> messages)
    {
        DateStarted = dateStarted;
        Messages = messages;
    }
}
