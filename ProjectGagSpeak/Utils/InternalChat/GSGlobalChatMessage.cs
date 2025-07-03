using CkCommons.Chat;
using GagspeakAPI.Data;

namespace GagSpeak.Utils; 
public record GSGlobalChatMessage(UserData UserData, string ChatName, string Content) 
    : CkChatMessage(ChatName, Content, DateTime.UtcNow)
{
    public override string UID => UserData.UID ?? base.UID;
    public CkSupporterTier Tier => UserData.Tier ?? CkSupporterTier.NoRole;
}
