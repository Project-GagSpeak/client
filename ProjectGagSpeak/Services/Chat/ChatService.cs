using CkCommons;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Chat;
using GagspeakAPI.Hub;
using GagspeakAPI.User;
using System.Text.RegularExpressions;
using GagSpeak.State.Managers;
using GagspeakAPI.Extensions;
using RichFilter = CkCommons.RichText.RichTextFilter;
using SeTextPayload = Dalamud.Game.Text.SeStringHandling.Payloads.TextPayload;

// Some of the below signatures were referenced by:
// https://git.anna.lgbt/anna/ChatTwo and https://git.anna.lgbt/anna/ExtraChat

namespace GagSpeak.Services;

public class ChatService : DisposableMediatorSubscriberBase
{
    internal static RichFilter AllowedTypes = RichFilter.Emotes | RichFilter.Color | RichFilter.Paragraph | RichFilter.Line;
    
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly ChatConfig _chatConfig;
    private readonly GlobalChatLog _globalChat;
    private readonly ChatFactory _factory;
    private readonly KinksterManager _kinksters;
    private readonly PairService _pairService;
    private readonly GagRestrictionManager _gags;
    private readonly MufflerService _garbler;
    

    private ChatlogId _overrideChatlogId = ChatlogId.Invalid;
    private readonly Dictionary<ChatlogId, DMChatLog> _dmChats = [];

    private uint ShowUidID = 0;
    private uint OpenGlobalChatID = 1;
    private DalamudLinkPayload ShowUID;
    private DalamudLinkPayload OpenRadarChat;

    public ChatService(ILogger<ChatService> logger, GagspeakMediator mediator,
        MainHub hub, MainConfig config, ChatConfig chatConfig, GlobalChatLog radarChat,
        ChatFactory factory, KinksterManager kinksters, GagRestrictionManager gags,
        MufflerService garbler, PairService pairService)
        : base(logger, mediator)
    {
        _hub = hub;
        _config = config;
        _chatConfig = chatConfig;
        _globalChat = radarChat;
        _factory = factory;
        _kinksters = kinksters;
        _pairService = pairService;
        _gags = gags;
        _garbler = garbler;

        ShowUID = Svc.Chat.AddChatLinkHandler(ShowUidID, OnShowUID);
        OpenRadarChat = Svc.Chat.AddChatLinkHandler(OpenGlobalChatID, OnOpenGlobalChat);

        // Track all messages from all types.
        Mediator.Subscribe<ChatReceivedMessage>(this, _ => OnChatMessageRecieved(_.Message));
        Mediator.Subscribe<ChatHistoryDownloaded>(this, _ => OnChatHistoryDownloaded(_.ChatId, _.ChatHistory));
    }

    internal IReadOnlyDictionary<ChatlogId, DMChatLog> DMChats => _dmChats;
    public int AllUnreadMentions()
    {
        var unread = 0;
        // Disable all mention bubbles if we disabled it via the chat.
        if (!_chatConfig.Data.AlertKind.HasAny(AlertKind.Bubble))
            return unread;
        // It was enabled, so display the rest.
        return unread + _dmChats.Values.Sum(dm => dm.UnreadMentions) + _globalChat.UnreadMentions;
    }

    public static Regex? MentionRegex { get; private set; } = null;

    /// <summary> The current ChatLogId overriding the chatlog label. </summary>
    internal ChatlogId ChatlogOverride
    {
        get => _overrideChatlogId;
        set
        {
            _overrideChatlogId = value;
            ReloadOverrideColor();
            UpdateChat();
        }
    }

    internal NativeUiColor OverrideColor { get; private set; } = new();

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Remove all registered chatlink handlers.
        Svc.Chat.RemoveChatLinkHandler();
    }

    #region Event Calls
    internal static void UpdateMentionRegex()
    {
        var validIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { MainHub.UID, MainHub.OwnUserData.AnonTag };
        if (!string.IsNullOrWhiteSpace(MainHub.OwnUserData?.Alias))
            validIds.Add(MainHub.OwnUserData.Alias);
        if (!string.IsNullOrWhiteSpace(MainHub.OwnUserData?.VanityName))
            validIds.Add(MainHub.OwnUserData.VanityName);

        var escapedIds = validIds.Select(Regex.Escape);
        var pattern = $@"(?<![\w-])@({string.Join("|", escapedIds)})(?![\w-])";
        // Assign the newly compiled regex to the global static property.
        MentionRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private void OnShowUID(uint id, SeString message)
    {
        var mousePos = ImGui.GetMousePos();
        if (_pairService.ResolveChatName(message.TextValue, StringComparison.Ordinal) is not { } userData)
            return;
        Mediator.Publish(new DalamudLinkPayloadHitUID(mousePos, userData));
    }

    private void OnOpenGlobalChat(uint id, SeString message)
        => Mediator.Publish(new DalamudLinkPayloadHitGlobalChat(ImGui.GetMousePos()));

    private void OnChatHistoryDownloaded(ChatlogId chatID, List<ChatlogMessage> history)
    {
        // This could be for any chat ID so we should determine what to do here....

        //var sChatLog = GetOrCreateSanctionLog(sGroup.Info.Sanction, sGroup.ChatId);
        //sChatLog.LoadHistory(history);
        Logger.LogDebug($"Loaded ChatHistory for {chatID.ChatId} with {history.Count} messages.", LoggerType.GlobalChat);
    }

    internal void OnChatMessageRecieved(ChatlogMessage logMsg)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogWarning("Received chat message before connection data was synced. Ignoring message.");
            return;
        }
        // Handle DirectMessages
        if (logMsg.Chatlog.Kind is GsChatKind.Direct)
        {
            // Isolate the DM chatlog by parsing the chatId ("UIDA-UIDB")
            var ownUser = MainHub.OwnUserData;
            var parts = logMsg.Chatlog.ChatId.Split('-');
            if (parts.Length != 2)
            {
                Logger.LogError($"Received DM with invalid ChatId format: {logMsg.Chatlog.ChatId}");
                return;
            }

            var otherUser = new UserData(parts[0] == ownUser.UID ? parts[1] : parts[0]);
            var targetUser = logMsg.Sender.UID == ownUser.UID ? otherUser : ownUser;
            // Ensure they allow messaging as well.
            if (targetUser.UID != ownUser.UID && !_pairService.IsValidDMChatUser(targetUser))
                return;
            // Valid Chatter, so get or create the log.
            var dmLog = GetOrCreateDMLog(targetUser, logMsg.Chatlog);
            dmLog.ProcessChatMessage(logMsg, true);
            PrintGsTell(logMsg, targetUser.UID != ownUser.UID, ownUser, otherUser);
        }
        else if (logMsg.Chatlog.Kind is GsChatKind.Global)
        {
            _globalChat.ProcessChatMessage(logMsg);
            PrintGlobalChatMessage(logMsg);
        }
    }

    private unsafe static void UpdateChat()
    {
        var agent = UIModule.Instance()->GetAgentModule()->GetAgentByInternalId(AgentId.ChatLog);
        agent->VirtualTable->Update(agent, 0);
        // Logger.LogInformation($"Updated chat to reflect override channel change (Kind: {ChatlogOverride.Kind} - ID: {ChatlogOverride.ChatId})");
    }
    #endregion

    #region Getters
    internal DMChatLog GetOrCreateDMLog(UserData otherUser, ChatlogId logId)
    {
        if (_dmChats.TryGetValue(logId, out var existing))
            return existing;
        // Otherwise, create the log, and append it to the chats.
        var newLog = _factory.CreateDMChat(otherUser, logId, 500);
        _dmChats.Add(logId, newLog);
        return newLog;
    }
    #endregion

    #region Resolvers
    public string ResolveOverrideName()
    {
        var logId = ChatlogOverride;
        if (logId.Kind is GsChatKind.Global)
            return "GlobalChat";
        if (logId.Kind is GsChatKind.Direct)
        {
            var parts = logId.ChatId.Split('-');
            if (parts.Length == 2)
            {
                var targetUid = parts[0] == MainHub.UID ? parts[1] : parts[0];
                var chatName = _pairService.GetChatNameLabel(new(targetUid));
                return $"GagSpeakDM >> {chatName}";
            }
        }
        // Otherwise return UNK
        return "UNK CHAT OVERRIDE";
    }

    // Updates the color 
    public void ReloadOverrideColor()
    {
        if (ChatlogOverride.Equals(ChatlogId.Invalid))
        {
            Logger.LogDebug("[ReloadOverrideColor] ChatlogOverride is Invalid. Resetting color.", LoggerType.GlobalChat);
            OverrideColor = new();
            return;
        }

        // Start with a default empty color
        var newColor = new NativeUiColor();

        switch (ChatlogOverride.Kind)
        {
            case GsChatKind.Direct:
                if (_chatConfig.Data.DMTextColor != default)
                {
                    Logger.LogDebug("[ReloadOverrideColor] Applying custom Direct Message text color.", LoggerType.GlobalChat);
                    newColor = _chatConfig.Data.DMTextColor;
                }
                break;
            case GsChatKind.Global:
                if (_chatConfig.Data.ChatColor != default)
                {
                    Logger.LogDebug("[ReloadOverrideColor] Applying custom Global chat color.", LoggerType.GlobalChat);
                    newColor = _chatConfig.Data.ChatColor;
                }
                break;
        }
        // Apply the resolved color once at the end
        OverrideColor = newColor;
    }

    public ChatlogId ResolveAlias(GsChatKind kind, string argument)
    {
        if (!MainHub.IsConnectionDataSynced)
            return ChatlogId.Invalid;

        // For SanctionGroups, joining a chat immidiately retrieves its history, and we get it on login.
        // This means the chat service will always have a valid LogId for every chat.
        // We can use this to perform a quick O(1) lookup, or O(n) at worst for displaynames.
        if (kind is GsChatKind.Direct)
        {
            if (_pairService.ResolveChatName(argument, StringComparison.OrdinalIgnoreCase) is { } match)
            {
                var dmChatId = string.CompareOrdinal(MainHub.UID, match.UID) < 0 ? $"{MainHub.UID}-{match.UID}" : $"{match.UID}-{MainHub.UID}";
                return new ChatlogId(kind, dmChatId);
            }
        }
        // Ret an invalid chatlog.
        return ChatlogId.Invalid;
    }
    #endregion

    #region ChatMessage Sending
    internal void SendTell(UserData recipient, string message)
    {
        var msgBytes = new SeStringBuilder().Add(new SeTextPayload(message)).Encode();
        SendDMInternal(recipient, message, msgBytes);
    }

    internal unsafe void SendTellNative(UserData recipient, byte[] msgBytes, Utf8String* nativeMsg)
    {
        var message = SeString.Parse(msgBytes);
        SendDMInternal(recipient, message.TextValue, msgBytes);
    }

    internal async void SendGlobalChatMessage(ChatlogId log, string message)
    {
        var msgBytes = new SeStringBuilder().Add(new SeTextPayload(message)).Encode();
        var msgDto = new SentMessage(log, MainHub.OwnUserData, message, msgBytes, _config.Data.UseLegacyAnonName, _chatConfig.Data.ChatPerms);
        SendChatInternal(log, msgDto);
    }

    internal unsafe void SendMessageNative(ChatlogId log, byte[] msgBytes, Utf8String* nativeMsg)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;

        var message = SeString.Parse(msgBytes);
        if (log.Kind is GsChatKind.Direct)
            SendDMNative(log, msgBytes, nativeMsg, message);
        else if (log.Kind is GsChatKind.Global)
            SendGlobalChatNative(log, msgBytes, nativeMsg, message);
    }

    private unsafe void SendDMNative(ChatlogId log, byte[] msgBytes, Utf8String* nativeMsg, SeString msgSeStr)
    {
        // Dont.
        return;
        // Get the labelName
        var parts = log.ChatId.Split('-');
        var targetUid = parts[0] == MainHub.UID ? parts[1] : parts[0];
        var msgDto = new SentMessage(log, MainHub.OwnUserData, msgSeStr.TextValue, msgBytes, _config.Data.UseLegacyAnonName, _chatConfig.Data.ChatPerms);
        SendChatInternal(log, msgDto);
    }

    private unsafe void SendGlobalChatNative(ChatlogId log, byte[] msgBytes, Utf8String* nativeMsg, SeString msgSeStr)
    {
        var msgDto = new SentMessage(log, MainHub.OwnUserData, msgSeStr.TextValue, msgBytes, _config.Data.UseLegacyAnonName, _chatConfig.Data.ChatPerms);
        SendChatInternal(log, msgDto);
    }

    private async void SendDMInternal(UserData recipient, string message, byte[] msgBytes)
    {
        if (!MainHub.IsConnectionDataSynced)
            return;
        // Dont
        return;
        // Ensure the correct log by running a comparer against sender and recipient.
        var dmChatId = string.CompareOrdinal(MainHub.UID, recipient.UID) < 0 ? $"{MainHub.UID}-{recipient.UID}" : $"{recipient.UID}-{MainHub.UID}";
        var dmChatlogId = new ChatlogId(GsChatKind.Direct, dmChatId);
        SendChatInternal(dmChatlogId, new(dmChatlogId, MainHub.OwnUserData, message, msgBytes, _config.Data.UseLegacyAnonName, _chatConfig.Data.ChatPerms));
    }

    private async void SendChatInternal(ChatlogId id, SentMessage messageDto)
    {
        Logger.LogInformation($"Sending off message to [{id.Kind}]({id.ChatId})", LoggerType.ChatHooks);
        
        // this will need to be moved or changed for future planned chat updates
        SentMessage finalMessage;
        if ((_gags.ServerGagData?.IsGagged() ?? true) && (ClientData.Globals?.ChatGarblerActive ?? false))
        {
            var garbledMsg = _garbler.GarbleMessage(messageDto.Message, true);
            // hacky, but just recreate the message dto with the garbler data replacing the original message
            // because we can't edit it after it's created
            finalMessage = messageDto with { Sender = MainHub.OwnUserData, Message = garbledMsg };
        }
        else
        {
            finalMessage = messageDto;
        }

        var result = await _hub.UserSendChat(finalMessage).ConfigureAwait(false);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to send message to [{id.Kind}]({id.ChatId}) through the hub! Error code: {result.ErrorCode}");
    }
    #endregion

    #region InGame-Chat Message Handling
    internal void PrintGsTell(ChatlogMessage msg, bool isSender, UserData clientUser, UserData otherUser)
    {
        // Exit early if we dont want to ouput DM's to the native chatbox.
        if (!_chatConfig.Data.ShowDMsInChatbox)
            return;
        // parse the byte contents.
        var seStringMsg = SeString.Parse(msg.Contents);

        // Construct the SeString
        var prefix = new SeStringBuilder();
        // Create GagSpeaks custom payload tag.
        prefix.Add(ChatHelpers.CreateGsChatIdPayload(msg.Chatlog.ChatId));
        prefix.Add(RawPayload.LinkTerminator);

        var prefixFG = _chatConfig.Data.DMPrefixColor.Foreground;
        var prefixEdge = _chatConfig.Data.DMPrefixColor.Glow;

        if (prefixFG != default) prefix.BeginForegroundColor(prefixFG);
        if (prefixEdge != default) prefix.BeginGlowColor(prefixEdge);

        var displayName = _pairService.GetChatNameLabel(otherUser);

        prefix.Add(ShowUID).AddText(displayName).Add(RawPayload.LinkTerminator);
        prefix.Add(new IconPayload(BitmapFontIcon.CrossWorld));
        prefix.AddText("GagSpeak");

        if (prefixEdge != default) prefix.EndGlowColor();
        if (prefixFG != default) prefix.EndForegroundColor();

        // Contents
        var contents = new SeStringBuilder();
        var textBg =  _chatConfig.Data.DMTextColor.Foreground;
        var textEdge = _chatConfig.Data.DMTextColor.Glow;

        if (textBg != default) contents.BeginForegroundColor(textBg);
        if (textEdge != default) contents.BeginGlowColor(textEdge);

        // Add the parsed payload message:
        foreach (var payload in seStringMsg.Payloads)
            contents.Add(payload);

        if (textEdge != default) contents.EndGlowColor();
        if (textBg != default) contents.EndForegroundColor();

        // Finally, print the entry through dalamud, to avoid printing it to the game network.
        Svc.Chat.Print(new XivChatEntry
        {
            Name = prefix.Build(),
            Message = contents.Build(),
            Type = isSender ? XivChatType.TellOutgoing : XivChatType.TellIncoming,
        });
    }

    internal void PrintGlobalChatMessage(ChatlogMessage msg)
    {
        // Grab the preferences.
        if (!_chatConfig.Data.UseNativeChat)
            return;

        var seStringMsg = SeString.Parse(msg.Contents);
        var prefix = new SeStringBuilder();
        prefix.Add(ChatHelpers.CreateGsChatIdPayload(msg.Chatlog.ChatId));
        prefix.Add(RawPayload.LinkTerminator);

        var color = _chatConfig.Data.ChatColor;

        if (color.Foreground != default) prefix.BeginForegroundColor(color.Foreground);
        if (color.Glow != default) prefix.BeginGlowColor(color.Glow);

        prefix.AddText("[").Add(OpenRadarChat).AddText("GlobalChat").Add(RawPayload.LinkTerminator).AddText("]<");
        prefix.Add(ShowUID).AddText(_globalChat.GetChatName(msg.Sender)).Add(RawPayload.LinkTerminator).AddText("> ");

        if (color.Glow != default) prefix.EndGlowColor();
        if (color.Foreground != default) prefix.EndForegroundColor();

        var contents = new SeStringBuilder();

        if (color.Foreground != default) contents.BeginForegroundColor(color.Foreground);
        if (color.Glow != default) contents.BeginGlowColor(color.Glow);

        foreach (var payload in seStringMsg.Payloads)
            contents.Add(payload);

        if (color.Glow != default) contents.EndGlowColor();
        if (color.Foreground != default) contents.EndForegroundColor();

        if (_chatConfig.Data.ChatType is XivChatType.Debug)
        {
            Svc.Chat.Print(new XivChatEntry
            {
                Name = "",
                Message = prefix.Append(contents.BuiltString).BuiltString,
                Type = _chatConfig.Data.ChatType,
            });
        }
        else
        {
            Svc.Chat.Print(new XivChatEntry
            {
                Name = prefix.BuiltString,
                Message = contents.BuiltString,
                Type = _chatConfig.Data.ChatType,
            });
        }
    }

    #endregion

    // Should definitely change how this is routed.
    private void PrintAnnouncement(string message)
    {
        // This is a TODO feature, but we can have a dummy sample for now.
        Svc.Chat.Print(new XivChatEntry { Type = XivChatType.Notice, Message = $"[GagSpeak] {message}" });
    }
}
