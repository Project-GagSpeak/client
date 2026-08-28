using CkCommons.RichChat;
using GagspeakAPI.Chat;
using GagspeakAPI.User;

namespace GagSpeak.Services;

public sealed class NewGsChatMessage : IChatMessage
{
    public string   MsgId           { get; }
    public UserData Sender          { get; }
    public DateTime TimestampUTC    { get; }
    public bool     FirstInMsgChain { get; }

    public string DisplayName  { get; set; } = string.Empty;
    public string Message      { get; set; } = string.Empty;
    public bool   WasMentioned { get; set; } = false;

    public string       SenderId => Sender.UID;
    public CkVanityTier Tier     => Sender.Tier;

    private NewGsChatMessage(string msgId, UserData sender, DateTime timestampUtc, string message, bool firstInMsgChain)
    {
        ArgumentNullException.ThrowIfNull(sender);
        FirstInMsgChain = firstInMsgChain;
        MsgId = msgId;
        Sender = sender;
        TimestampUTC = timestampUtc;
        Message = message;
        DisplayName = sender.AliasOrUID;
    }

    public NewGsChatMessage(ChatlogMessage msg, bool firstInMsgChain)
        : this(msg, msg.Message, firstInMsgChain)
    {
        DisplayName = Sender.AliasOrUID;
    }

    public NewGsChatMessage(ChatlogMessage msg, string sanitizedMsg, bool firstInMsgChain)
        : this(msg.MsgId, msg.Sender, msg.TimeSentUTC, sanitizedMsg, firstInMsgChain)
    {
        DisplayName = Sender.AliasOrUID;
    }

    public NewGsChatMessage(ChatlogMessage msg, string senderName, string sanitizedMsg, bool firstInMsgChain)
    : this(msg.MsgId, msg.Sender, msg.TimeSentUTC, sanitizedMsg, firstInMsgChain)
    {
        DisplayName = senderName;
    }
}
