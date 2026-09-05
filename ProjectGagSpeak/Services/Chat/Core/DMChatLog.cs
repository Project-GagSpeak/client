using CkCommons.RichChat;
using CkCommons.RichText;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.User;

namespace GagSpeak.Services;

public class DMChatLog : RichChatLog<NewGsChatMessage>
{
    private readonly ILogger<DMChatLog> _logger;
    private readonly ChatConfig _chatConfig;
    private readonly ChatColors _chatColors;
    private readonly KinksterManager _kinksters;
    private readonly BlockService _blockedUsers;
    private readonly PairService _pairService;

    public DMChatLog(UserData targetUser, ChatlogId id, int capacity,
        ILogger<DMChatLog> logger, ChatConfig config, ChatColors chatColors, 
        KinksterManager kinksters, BlockService blockedUsers, PairService pairService)
        : base(id.ChatId, capacity)
    {
        _logger = logger;
        _chatConfig = config;
        _chatColors = chatColors;
        _kinksters = kinksters;
        _blockedUsers = blockedUsers;
        _pairService = pairService;

        TargetUser = targetUser;
        ChatID = id;
    }

    public ChatlogId ChatID { get; protected set; }

    public UserData TargetUser { get; init; }

    public void LoadHistory(IEnumerable<ChatlogMessage> messages)
    {
        ClearLog();
        // Load in all messages.
        foreach (var message in messages)
            ProcessChatMessage(message, false);
        MarkAsRead(true);
    }

    public void ClearHistory()
        => ClearLog();

    // Processes a external chat message that can be any unsanitized or modified type,
    // then converts its contents into NewGsChatMessage, adding it to the log.
    // This is where detections for mentions, highlights, or other nuances should be handled.
    public void ProcessChatMessage(ChatlogMessage msg, bool doPings = true)
    {
        _logger.LogTrace($"[DirectMsg] ({ID}) recieved msg from <{msg.Sender.UID}>: {msg.Message}", LoggerType.GsTells);
        // Wrap devs in special text.
        var ctx = msg.Sender.Tier is CkVanityTier.KinkporiumMistress
            ? $"[rawcolor={GsCol.ShopKeeperText.Uint()}]{msg.Message}[/rawcolor]"
            : NewRichText.StripDisallowedRichTags(msg.Message, ChatService.AllowedTypes);

        // Safely check if the sender is the local user to skip self-pings.
        if (msg.Sender.UID == MainHub.UID)
        {
            AddLogMessage(new NewGsChatMessage(msg, GetSenderName(msg), ctx, NewSenderSinceLastMsg(msg.Sender.UID)));
            return;
        }

        var finalMsgText = ProcessMentions(ctx, out bool wasMentioned);
        var gsMsg = new NewGsChatMessage(msg, GetSenderName(msg), finalMsgText, NewSenderSinceLastMsg(msg.Sender.UID))
        {
            WasMentioned = wasMentioned
        };
        AddLogMessage(gsMsg);
        if (!wasMentioned && !_chatConfig.Data.PingOnDM)
            return;
        if (doPings && !_blockedUsers.IsMuted(msg.Sender.UID) && _chatConfig.Data.AlertKind.HasAny(AlertKind.Audio))
            _chatConfig.PlaySound();
    }

    private string GetSenderName(ChatlogMessage msg)
    {
        var sender = msg.Sender;
        // Prioritize VanityName if set
        if (sender.VanityName is not null && msg.Flags.HasAny(ChatFlags.UseDisplayName))
            return $"${sender.VanityName}-{msg.KinksterTag}";
        // Fallback for pairs
        if (_kinksters.GetValueOrDefault(sender) is { } kinkster)
            return $"{kinkster.GetNickAliasOrUid()} ({msg.KinksterTag})";
        // Final fallback - AnonKinkster name.
        return $"Kinkster-{msg.KinksterTag}";
    }

    private string ProcessMentions(string sanitizedMessage, out bool wasMentioned)
    {
        wasMentioned = false;
        var regex = ChatService.MentionRegex;
        if (regex is null || !sanitizedMessage.Contains('@'))
            return sanitizedMessage;

        var match = regex.Match(sanitizedMessage);
        if (match.Success)
        {
            wasMentioned = true;
            if (_chatConfig.Data.MentionHighlights)
                return regex.Replace(sanitizedMessage, $"[rawcolor={_chatConfig.Data.MentionColor}]$0[/rawcolor]");
        }
        return sanitizedMessage;
    }
}
