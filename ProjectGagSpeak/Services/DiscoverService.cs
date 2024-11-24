using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Events;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
using GagSpeak.Utils;
using GagSpeak.Utils.ChatLog;
using GagspeakAPI.Enums;
using ImGuiNET;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace GagSpeak.Services;

// handles the global chat and pattern discovery social features.
public class DiscoverService : DisposableMediatorSubscriberBase
{
    private readonly MainMenuTabs _tabMenu;
    private readonly PairManager _pairManager;
    private readonly string _configDirectory;
    private const string _chatlogFile = "global-chat-recent.log";
    private string ChatLogFilePath => Path.Combine(_configDirectory, _chatlogFile);
    public DiscoverService(string configDirectory, ILogger<DiscoverService> logger, 
        GagspeakMediator mediator, MainMenuTabs tabMenu, PairManager pairManager) : base(logger, mediator)
    {
        _configDirectory = configDirectory;
        _tabMenu = tabMenu;
        _pairManager = pairManager;

        // Create a new chat log
        GlobalChat = new ChatLog(Mediator);

        // Load the chat log
        LoadChatLog(GlobalChat);

        Mediator.Subscribe<GlobalChatMessage>(pairManager, (msg) => AddChatMessage(msg));
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, (msg) => 
        {
            if (msg.NewTab is MainMenuTabs.SelectedTab.GlobalChat)
            {
                GlobalChat.ShouldScrollToBottom = true;
                NewMessages = 0;
            }
        });
    }
    public static ChatLog GlobalChat { get; private set; }
    public static bool CreatedSameDay => DateTime.UtcNow.DayOfYear == GlobalChat.TimeCreated.DayOfYear;
    public static int NewMessages { get; private set; } = 0;

    protected override void Dispose(bool disposing)
    {
        // Save the chat log prior to disposal.
        SaveChatLog();
        base.Dispose(disposing);
    }

    private void AddWelcomeMessage()
    {
        GlobalChat.AddMessage(new ChatMessage(new("System"), "System", "Welcome to the GagSpeak Global Chat!. " +
            "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi!"));
    }

    private void AddChatMessage(GlobalChatMessage msg)
    {
        if (_tabMenu.TabSelection is not MainMenuTabs.SelectedTab.GlobalChat)
            NewMessages++;

        string SenderName = "Kinkster-" + msg.ChatMessage.MessageSender.UID.Substring(msg.ChatMessage.MessageSender.UID.Length - 3);


        // extract the user data from the message
        var userData = msg.ChatMessage.MessageSender;
        // grab the list of our currently online pairs.
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == userData.UID);

        // determine the display name
        if (msg.FromSelf) 
            SenderName = msg.ChatMessage.MessageSender.AliasOrUID;

        if (matchedPair != null) 
            SenderName = matchedPair.GetNickAliasOrUid();

        // if the supporter role is the highest role, give them a special label.
        if (userData.SupporterTier is CkSupporterTier.KinkporiumMistress)
            SenderName = $"Mistress Cordy";

        // construct the chat message struct to add.
        ChatMessage msgToAdd = new ChatMessage(userData, SenderName, msg.ChatMessage.Message);

        GlobalChat.AddMessage(msgToAdd);
    }

    private void SaveChatLog()
    {
        // Capture up to the last 500 messages
        var messagesToSave = GlobalChat.Messages.TakeLast(500).ToList();
        var logToSave = new SerializableChatLog(GlobalChat.TimeCreated, messagesToSave);

        // Serialize the item to JSON
        string json = JsonConvert.SerializeObject(logToSave);

        // Compress the JSON string
        var compressed = json.Compress(6);

        // Encode the compressed string to base64
        string base64ChatLogData = Convert.ToBase64String(compressed);
        // Take this base64data and write it out to the json file.
        File.WriteAllText(ChatLogFilePath, base64ChatLogData);
    }

    public void LoadChatLog(ChatLog chatLog)
    {
        // if the file does not exist, return
        if (!File.Exists(ChatLogFilePath))
        {
            // Add the basic welcome message and return.
            Logger.LogInformation("Chat log file does not exist. Adding welcome message.", LoggerType.GlobalChat);
            AddWelcomeMessage();
            return;
        }

        // Attempt Deserialization.
        SerializableChatLog savedChatlog = new SerializableChatLog();
        try
        {
            // The file was valid, so attempt to load in the data.
            string base64logFile = File.ReadAllText(ChatLogFilePath);
            // Decompress the log data
            var bytes = Convert.FromBase64String(base64logFile);
            // decompress it from string into the format we want.
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            // Deserialize the JSON string back to the object
            savedChatlog = JsonConvert.DeserializeObject<SerializableChatLog>(decompressed);

            // if any user datas are null, throw an exception.
            if (savedChatlog.Messages.Any(m => m.UserData is null))
                throw new Exception("One or more user datas are null in the chat log.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load chat log.");
            AddWelcomeMessage();
            return;
        }

        // If the de-serialized date is not the same date as our current date, do not restore the data.
        if (savedChatlog.DateStarted.DayOfYear != DateTime.Now.DayOfYear)
        {
            Logger.LogInformation("Chat log is from a different day. Not restoring.", LoggerType.GlobalChat);
            AddWelcomeMessage();
            return;
        }

        // The date is the same, so instead, let's load in the chat messages into the buffer and not add a welcome message.
        chatLog.AddMessageRange(savedChatlog.Messages);
        Logger.LogInformation($"Loaded {savedChatlog.Messages.Count} messages from the chat log.", LoggerType.GlobalChat);
    }
}

public struct SerializableChatLog
{
    public DateTime DateStarted { get; set; }
    public List<ChatMessage> Messages { get; set; }

    public SerializableChatLog(DateTime dateStarted, List<ChatMessage> messages)
    {
        DateStarted = dateStarted;
        Messages = messages;
    }
}
