using GagspeakAPI.Data;

namespace GagSpeak.Utils.ChatLog;
public record struct InternalChatMessage
{
    public UserData UserData { get; init; }
    public string Name { get; init; }
    public string Message { get; init; }
    public DateTime TimeStamp { get; init; }

    public string UID => UserData.UID ?? "UNK";
    public CkSupporterTier Tier => UserData.Tier ?? CkSupporterTier.NoRole;

    public InternalChatMessage(UserData userData, string name, string message)
    {
        UserData = userData;
        Name = name;
        Message = message;
        TimeStamp = DateTime.Now;
    }
}
