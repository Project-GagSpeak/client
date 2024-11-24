using GagspeakAPI.Data;
using GagspeakAPI.Enums;

namespace GagSpeak.Utils.ChatLog;
public record struct ChatMessage
{
    public UserData UserData { get; init; }
    public string Name { get; init; }
    public string Message { get; init; }
    public DateTime TimeStamp { get; init; }

    public string UID => UserData.UID ?? "Unknown";
    public CkSupporterTier SupporterTier => UserData.SupporterTier ?? CkSupporterTier.NoRole;

    public ChatMessage(UserData userData, string name, string message)
    {
        UserData = userData;
        Name = name;
        Message = message;
        TimeStamp = DateTime.Now;
    }
}
