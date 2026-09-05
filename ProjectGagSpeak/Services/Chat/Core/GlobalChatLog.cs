using CkCommons.RichChat;
using CkCommons.RichText;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.User;
using System.Text.RegularExpressions;

namespace GagSpeak.Services;

public class GlobalChatLog : RichChatLog<NewGsChatMessage>, IMediatorSubscriber, IDisposable
{
    private readonly ILogger<GlobalChatLog> _logger;
    private readonly ChatConfig _chatConfig;
    private readonly ChatColors _chatColors;
    private readonly KinksterManager _kinksters;
    private readonly BlockService _blockService;

    private Dictionary<UserData, (bool LegacyId, ChatFlags Flags)> _userMeta = new(UserDataComparer.Instance);
    // Used exclusively for radar mentions.
    private Regex? _mentionRegex;

    public GagspeakMediator Mediator { get; }

    public GlobalChatLog(ILogger<GlobalChatLog> logger, GagspeakMediator mediator,
        ChatConfig chatConfig, ChatColors colors, KinksterManager kinksters,
        BlockService blockService)
        : base("GlobalChat", 1500)
    {
        _logger = logger;
        Mediator = mediator;
        _chatConfig = chatConfig;
        _chatColors = colors;
        _kinksters = kinksters;
        _blockService = blockService;

        Mediator.Subscribe<ConnectedMessage>(this, _ => OnConnected());
    }

    public const string DisplayName = "Global Chat";
    public int Participants => _userMeta.Count;
    internal IReadOnlyDictionary<UserData, (bool LegacyId, ChatFlags Flags)> ChatUsers => _userMeta;

    public void Dispose()
    {
        Mediator.UnsubscribeAll(this);
        ClearLog();
    }

    // Update Mentions
    private void OnConnected()
    {
        var validIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { MainHub.OwnUserData.AnonName, MainHub.OwnUserData.AnonTag };
        if (!string.IsNullOrWhiteSpace(MainHub.OwnUserData.VanityName))
            validIds.Add(MainHub.OwnUserData.VanityName);
        
        var escapedIds = validIds.Select(Regex.Escape);
        var pattern = $@"(?<![\w-])@({string.Join("|", escapedIds)})(?![\w-])";
        _mentionRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public void LoadChatHistory(List<ChatlogMessage> chatHistory)
    {
        ClearLog();
        // Reload log, mark as read.
        foreach (var msg in chatHistory)
            ProcessChatMessage(msg, false);
        MarkAsRead(true);
        _logger.LogDebug("Loaded GlobalChat history, marked all as read.", LoggerType.GlobalChat);
    }


    // May not always be valid if the passed in user is just a UID.
    public string GetChatName(UserData user)
    {
        if (_kinksters.GetValueOrDefault(user) is { } kinkster)
            return $"{kinkster.GetNickAliasOrUid()} ({user.AnonTag})";
        if (_userMeta.TryGetValue(user, out var meta) && meta.Flags.HasAny(ChatFlags.UseDisplayName))
            return user.VanityOrAnonName;
        return user.AnonName;
    }

    public void AddUpdateMember(GlobalChatMember dto)
        => _userMeta[dto.User] = (dto.LegacyId, dto.Flags);

    public void ProcessChatMessage(ChatlogMessage msg, bool doPings = true)
    {
        _logger.LogDebug($"[RadarChat] {ID} recieved msg from {msg.Sender.AnonTag}", LoggerType.GlobalChat);
        _userMeta[msg.Sender] = (msg.LegacyId, msg.Flags);
        // Wrap devs in special text.
        var ctx = msg.Sender.Tier is CkVanityTier.KinkporiumMistress
            ? $"[rawcolor={GsCol.ShopKeeperText.Uint()}]{msg.Message}[/rawcolor]"
            : NewRichText.StripDisallowedRichTags(msg.Message, ChatService.AllowedTypes);

        if (msg.Sender.UID == MainHub.OwnUserData.UID)
        {
            AddLogMessage(new NewGsChatMessage(msg, GetSenderName(msg), ctx, NewSenderSinceLastMsg(msg.Sender.UID)));
            return;
        }

        // Process for mentions
        var finalMsgText = ProcessMentions(ctx, out bool wasMentioned);
        var gsMsg = new NewGsChatMessage(msg, GetSenderName(msg), finalMsgText, NewSenderSinceLastMsg(msg.Sender.UID))
        {
            WasMentioned = wasMentioned
        };
        AddLogMessage(gsMsg);

        if (!wasMentioned)
            return;
        if (doPings && !_blockService.IsMuted(msg.Sender.UID) && _chatConfig.Data.AlertKind.HasAny(AlertKind.Audio))
            _chatConfig.PlaySound();
    }

    // Move to message handler.
    private string GetSenderName(ChatlogMessage msg)
    {
        if (msg.Sender.Tier is CkVanityTier.KinkporiumMistress)
            return msg.Sender.VanityOrAnonName;

        // Otherwise parse normally.
        var sender = msg.Sender;
        // Prioritize VanityName if set
        if (sender.VanityName is not null && msg.Flags.HasAny(ChatFlags.UseDisplayName))
            return $"{sender.VanityName}-{msg.KinksterTag}";
        // Fallback for pairs
        if (_kinksters.GetValueOrDefault(sender) is { } kinkster)
            return $"{kinkster.GetNickAliasOrUid()} ({msg.KinksterTag})";
        // Final fallback - AnonKinkster name.
        return $"Kinkster-{msg.KinksterTag}";
    }

    private string ProcessMentions(string sanitizedMessage, out bool wasMentioned)
    {
        wasMentioned = false;
        var regex = _mentionRegex;
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
