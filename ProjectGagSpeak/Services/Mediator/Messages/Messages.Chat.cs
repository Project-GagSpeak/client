using CkCommons.RichChat;
using GagspeakAPI.Chat;
using GagspeakAPI.User;

namespace GagSpeak.Services.Mediator;

public enum ChatFailType
{
    FeatureDisabled,
    MissingPermissions,
    InvalidChatLog,
    MissingArgument,
    TargetResolutionFailed
}

public record ChatHistoryDownloaded(ChatlogId ChatId, List<ChatlogMessage> ChatHistory) : MessageBase;
public record ChatReceivedMessage(ChatlogMessage Message) : MessageBase;
public record ChatCmdFailureMessage(GsChatKind? Kind, string Command, string Args, ChatFailType Reason, string Data = "") : MessageBase;
public record ChatOpenChatWindow(RichChatLog<NewGsChatMessage> ChatLog) : MessageBase;
public record DalamudLinkPayloadHitUID(Vector2 MousePos, UserData UserData) : MessageBase;
public record DalamudLinkPayloadHitGlobalChat(Vector2 MousePos) : MessageBase;
