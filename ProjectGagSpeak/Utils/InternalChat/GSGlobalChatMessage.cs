using CkCommons.Chat;
using GagspeakAPI.Data;

namespace GagSpeak.Utils; 
public record GagSpeakChatMessage(UserData UserData, string Name, string Message) 
    : CkChatMessage(Name, Message, DateTime.UtcNow)
{
    public override string UID => UserData.UID ?? base.UID;
    public CkSupporterTier Tier => UserData.Tier ?? CkSupporterTier.NoRole;
}
