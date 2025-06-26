using GagSpeak.CkCommons;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Gui.Components;
using GagSpeak.Kinksters;
using GagSpeak.Utils.ChatLog;
using GagSpeak.WebAPI;
using GagSpeak.Services.Configs;

namespace GagSpeak.Services;

/// <summary>
///     This class isnt really a 'service' that much. Should probably have other handlers for which chat is displayed.
///     Polish this up more when we integrate the vibe rooms and stuff i guess.
/// </summary>
public class DiscoverService : DisposableMediatorSubscriberBase
{
    private readonly MainMenuTabs _tabMenu;
    private readonly KinksterManager _pairManager;
    private static string ChatFilePath => Path.Combine(ConfigFileProvider.GagSpeakDirectory, "global-chat-recent.log");
    public DiscoverService(ILogger<DiscoverService> logger, GagspeakMediator mediator, 
        MainHub hub, MainMenuTabs tabMenu, KinksterManager pairManager,
        CosmeticService cosmetics) : base(logger, mediator)
    {
        _tabMenu = tabMenu;
        _pairManager = pairManager;

        // Create a new chat log
        GlobalChat = new InternalChatlog(hub, Mediator, cosmetics);

        // Load the chat log
        LoadChatLog(GlobalChat);

        Mediator.Subscribe<GlobalChatMessage>(pairManager, AddChatMessage);
        Mediator.Subscribe<MainWindowTabChangeMessage>(this, (msg) => 
        {
            if (msg.NewTab is MainMenuTabs.SelectedTab.GlobalChat)
            {
                GlobalChat.ShouldScrollToBottom = true;
                NewMessages = 0;
            }
        });
    }
    public static InternalChatlog GlobalChat { get; private set; }
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
        GlobalChat.AddMessage(new InternalChatMessage(new("System"), "System", "Welcome to the GagSpeak Global Chat!. " +
            "Your Name in here is Anonymous to anyone you have not yet added. Feel free to say hi!"));
    }

    private void AddChatMessage(GlobalChatMessage msg)
    {
        if (_tabMenu.TabSelection is not MainMenuTabs.SelectedTab.GlobalChat)
            NewMessages++;

        var userTagCode = msg.Message.UserTagCode;
        var SenderName = "Kinkster-" + userTagCode;


        // extract the user data from the message
        var userData = msg.Message.Sender;
        // grab the list of our currently online pairs.
        var matchedPair = _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == userData.UID);

        // determine the display name
        if (msg.FromSelf) 
            SenderName = msg.Message.Sender.AliasOrUID + " (" + userTagCode + ")";

        if (matchedPair != null)
            SenderName = matchedPair.GetNickAliasOrUid() + " (" + userTagCode + ")";

        // if the supporter role is the highest role, give them a special label.
        if (userData.Tier is CkSupporterTier.KinkporiumMistress)
            SenderName = $"Mistress Cordy";

        // construct the chat message struct to add.
        var msgToAdd = new InternalChatMessage(userData, SenderName, msg.Message.Message);

        GlobalChat.AddMessage(msgToAdd);
    }

    private void SaveChatLog()
    {
        // Capture up to the last 500 messages
        var messagesToSave = GlobalChat.Messages.TakeLast(500).ToList();
        var logToSave = new SerializableChatLog(GlobalChat.TimeCreated, messagesToSave);

        // Serialize the item to JSON
        var json = JsonConvert.SerializeObject(logToSave);

        // Compress the JSON string
        var compressed = json.Compress(6);

        // Encode the compressed string to base64
        var base64ChatLogData = Convert.ToBase64String(compressed);
        // Take this base64data and write it out to the json file.
        File.WriteAllText(ChatFilePath, base64ChatLogData);
    }

    public void LoadChatLog(InternalChatlog chatLog)
    {
        // if the file does not exist, return
        if (!File.Exists(ChatFilePath))
        {
            // Add the basic welcome message and return.
            Logger.LogInformation("Chat log file does not exist. Adding welcome message.", LoggerType.GlobalChat);
            AddWelcomeMessage();
            return;
        }

        // Attempt Deserialization.
        var savedChatlog = new SerializableChatLog();
        try
        {
            // The file was valid, so attempt to load in the data.
            var base64logFile = File.ReadAllText(ChatFilePath);
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
    public List<InternalChatMessage> Messages { get; set; }

    public SerializableChatLog(DateTime dateStarted, List<InternalChatMessage> messages)
    {
        DateStarted = dateStarted;
        Messages = messages;
    }
}
