using CkCommons;
using CkCommons.Chat;
using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.RichText;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using ImGuiNET;
using System.Globalization;

namespace GagSpeak.Utils;
public class GlobalChatLog : CkChatlog<GSGlobalChatMessage>, IMediatorSubscriber, IDisposable
{
    private static string RecentFile => Path.Combine(ConfigFileProvider.GagSpeakDirectory, "global-chat-recent.log");
    private static RichTextFilter AllowedTypes = RichTextFilter.Emotes;

    private readonly ILogger<GlobalChatLog> _logger;
    private readonly MainHub _hub;
    private readonly MainMenuTabs _tabMenu;
    private readonly MainConfig _config;
    private readonly GlobalPermissions _globals;
    private readonly GagRestrictionManager _gags;
    private readonly KinksterManager _kinksters;
    private readonly MufflerService _garbler;

    private string _requestMessage = string.Empty;
    private static int _newMessages = 0;

    public GlobalChatLog(ILogger<GlobalChatLog> logger, GagspeakMediator mediator,
        MainHub hub, MainMenuTabs tabs, MainConfig config, GlobalPermissions globals, 
        GagRestrictionManager gags, KinksterManager kinksters, MufflerService garbler) 
        : base(0, "Global Chat", 1000)
    {
        _logger = logger;
        Mediator = mediator;
        _hub = hub;
        _tabMenu = tabs;
        _globals = globals;
        _gags = gags;
        _kinksters = kinksters;
        _garbler = garbler;

        // Load the chat log from most recent session, if any.
        LoadChatLog();

        Mediator.Subscribe<GlobalChatMessage>(this, AddNetworkMessage);
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, (msg) =>
        {
            if (msg.NewTab is MainMenuTabs.SelectedTab.GlobalChat)
            {
                ShouldScrollToBottom = true;
                _newMessages = 0;
            }
        });
    }

    public GagspeakMediator Mediator { get; }
    public bool CreatedSameDay => DateTime.UtcNow.DayOfYear == TimeCreated.DayOfYear;
    public int NewMessages => _newMessages;

    void IDisposable.Dispose()
    {
        Mediator.UnsubscribeAll(this);
        // Save the chat log prior to disposal.
        SaveChatLog();

        GC.SuppressFinalize(this);
    }

    public void SetAutoScroll (bool newState)
        => DoAutoScroll = newState;

    protected override string ToTooltip(GSGlobalChatMessage message)
        => $"Sent @ {message.Timestamp.ToString("T", CultureInfo.CurrentCulture)}" +
        "--NL----COL--[Right-Click]--COL-- View Interactions" +
        "--NL----COL--[Middle-Click]--COL-- Open KinkPlate";

    private void AddNetworkMessage(GlobalChatMessage networkChat)
    {
        if (_tabMenu.TabSelection is not MainMenuTabs.SelectedTab.GlobalChat)
            _newMessages++;
        // Default sender name and tag, respects 3-4 character string.
        var userTagCode = networkChat.Message.UserTagCode;
        var SenderName = "Kinkster-" + userTagCode;
        // get the UserData from the GlobalChatMessage (may not be a pair)
        var userData = networkChat.Message.Sender;
        // Set the SenderName according to conditions.
        if (userData.Tier is CkSupporterTier.KinkporiumMistress)
            SenderName = $"Mistress Cordy";
        else if (_kinksters.DirectPairs.FirstOrDefault(p => p.UserData.UID == userData.UID) is { } match)
            SenderName = match.GetNickAliasOrUid() + " (" + userTagCode + ")";
        else if (networkChat.FromSelf)
            SenderName = userData.AliasOrUID + " (" + userTagCode + ")";
        // construct the chat message struct to add, and append it.
        AddMessage(new GSGlobalChatMessage(userData, SenderName, networkChat.Message.Message));
    }

    protected override void AddMessage(GSGlobalChatMessage newMsg)
    {
        // Cordy is special girl :3
        if (newMsg.Tier is CkSupporterTier.KinkporiumMistress)
        {
            // Force set the uid color to her favorite color.
            UserColors[newMsg.UID] = CkColor.CkMistressColor.Vec4();
            // allow any rich text tags, as she is a special case.
            var prefix = $"[img=RequiredImages\\Tier4Icon][rawcolor={CkColor.CkMistressColor.Uint()}]{newMsg.Name}[/rawcolor]: ";
            Messages.PushBack(newMsg with { Message = prefix + newMsg.Message });
            unreadSinceScroll++;
        }
        else
        {
            // Assign the sender color
            var col = ColorHelpers.RgbaVector4ToUint(AssignSenderColor(newMsg));
            // strip out the modifiers that are not allowed to prevent chaos in global chat.
            var sanitizedMsg = CkRichText.StripDisallowedRichTags(newMsg.Message, AllowedTypes);
            // append special formatting to the start of the message based on supporter type.
            var prefix = newMsg.Tier switch
            {
                CkSupporterTier.DistinguishedConnoisseur => $"[img=RequiredImages\\Tier3Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.EsteemedPatron => $"[img=RequiredImages\\Tier2Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.ServerBooster => $"[img=RequiredImages\\TierBoosterIcon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                CkSupporterTier.IllustriousSupporter => $"[img=RequiredImages\\Tier1Icon][rawcolor={col}]{newMsg.Name}[/rawcolor]: ",
                _ => $"[rawcolor={col}]{newMsg.Name}[/rawcolor]: "
            };
            Messages.PushBack(newMsg with { Message = prefix + sanitizedMsg });
            unreadSinceScroll++;
        }
    }

    protected override void OnMiddleClick(GSGlobalChatMessage message)
        => Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(message.UserData));

    protected override void OnSendMessage(string message)
    {
        shouldFocusChatInput = true;
        if (string.IsNullOrWhiteSpace(previewMessage))
            return;

        // Process message if gagged
        if ((_gags.ServerGagData?.IsGagged() ?? true) && (_globals.Current?.ChatGarblerActive ?? false))
            previewMessage = _garbler.ProcessMessage(previewMessage);

        // Send message to the server
        _logger.LogTrace($"Sending Message: {previewMessage}", LoggerType.GlobalChat);
        _hub.UserSendGlobalChat(new(MainHub.PlayerUserData, previewMessage, _config.Current.PreferThreeCharaAnonName)).ConfigureAwait(false);

        // Clear message and trigger achievement event
        previewMessage = string.Empty;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GlobalSent);
    }

    protected override void DrawPopupInternal()
    {
        var shiftHeld = KeyMonitor.ShiftPressed();
        var ctrlHeld = KeyMonitor.CtrlPressed();

        CkGui.FontText(LastInteractedMsg.Name, Svc.PluginInterface.UiBuilder.MonoFontHandle);
        ImGui.Separator();
        if (ImGui.Selectable("View Light KinkPlate") && LastInteractedMsg.UID != "System")
        {
            Mediator.Publish(new KinkPlateOpenStandaloneLightMessage(LastInteractedMsg.UserData));
            ClosePopupAndResetMsg();
        }
        CkGui.AttachToolTip($"Opens {LastInteractedMsg.Name}'s Light KinkPlate.");

        ImGui.Separator();
        using (ImRaii.Disabled(!shiftHeld))
            if (ImGui.Selectable("Send Kinkster Request"))
            {
                _hub.UserSendKinksterRequest(new(new(LastInteractedMsg.UID), _requestMessage)).ConfigureAwait(false);
                ClosePopupAndResetMsg();
            }
        CkGui.AttachToolTip(shiftHeld
            ? $"Sends a Kinkster Request to {LastInteractedMsg.Name}."
            : "Must be holding SHIFT to select.");

        ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - 20);
        ImGui.InputTextWithHint("##attachedPairMsg", "Attached Request Msg..", ref _requestMessage, 150);
        ImGui.Separator();

        using (ImRaii.Disabled(!ctrlHeld))
            if (ImGui.Selectable("Hide Messages from Kinkster") && LastInteractedMsg.UID != "System" && LastInteractedMsg.UID != MainHub.UID)
            {
                SilenceList.Add(LastInteractedMsg.UID);
                ClosePopupAndResetMsg();
            }
        CkGui.AttachToolTip(ctrlHeld
            ? $"Hides all messages from {LastInteractedMsg.Name} until plugin reload/restart."
            : "Must be holding CTRL to select.");
    }

    private void ClosePopupAndResetMsg()
    {
        LastInteractedMsg = new(new("System"), string.Empty, string.Empty);
        ImGui.CloseCurrentPopup();
    }

    private void SaveChatLog()
    {
        // Capture up to the last 500 messages
        var messagesToSave = Messages.TakeLast(500).ToList();
        var logToSave = new SerializableChatLog(TimeCreated, messagesToSave);

        // Serialize the item to JSON
        var json = JsonConvert.SerializeObject(logToSave);
        var compressed = json.Compress(6);
        var base64ChatLogData = Convert.ToBase64String(compressed);
        File.WriteAllText(RecentFile, base64ChatLogData);
    }

    public void LoadChatLog()
    {
        // if the file does not exist, return
        if (!File.Exists(RecentFile))
        {
            // Add the basic welcome message and return.
            _logger.LogInformation("Chat log file does not exist. Adding welcome message.", LoggerType.GlobalChat);
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat!.[line]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi!"));
            return;
        }

        // Attempt Deserialization.
        var savedChatlog = new SerializableChatLog();
        try
        {
            var base64logFile = File.ReadAllText(RecentFile);
            var bytes = Convert.FromBase64String(base64logFile);
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            savedChatlog = JsonConvert.DeserializeObject<SerializableChatLog>(decompressed);

            // if any user datas are null, throw an exception.
            if (savedChatlog.Messages.Any(m => m.UserData is null))
                throw new Exception("One or more user datas are null in the chat log.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load chat log.");
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat!.[line]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi!"));
            return;
        }

        // If the de-serialized date is not the same date as our current date, do not restore the data.
        if (savedChatlog.DateStarted.DayOfYear != DateTime.Now.DayOfYear)
        {
            _logger.LogInformation("Chat log is from a different day. Not restoring.", LoggerType.GlobalChat);
            AddMessage(new(new("System"), "System",
                "Welcome to the GagSpeak Global Chat!.[line]" +
                "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi!"));
            return;
        }

        // The date is the same, so instead, let's load in the chat messages into the buffer and not add a welcome message.
        AddMessages(savedChatlog.Messages);
        _logger.LogInformation($"Loaded {savedChatlog.Messages.Count} messages from the chat log.", LoggerType.GlobalChat);
    }

    internal struct SerializableChatLog
    {
        public DateTime DateStarted { get; set; }
        public List<GSGlobalChatMessage> Messages { get; set; }

        public SerializableChatLog(DateTime dateStarted, List<GSGlobalChatMessage> messages)
        {
            DateStarted = dateStarted;
            Messages = messages;
        }
    }
}
