using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerState.Listener;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;

namespace GagSpeak.UpdateMonitoring.Chat;

/// <summary>
/// This class handles incoming chat messages, combat messages, and other related Messages we care about. 
/// It will then trigger the appropriate chat classes to handle the message.
/// 
/// It is worth noting that this detection occurs after the message is sent to the server, and should not be
/// depended on for translation prior to sending.
/// </summary>
public class ChatMonitor : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly GlobalData _globals;
    private readonly ChatSender _chatSender;
    private readonly PuppeteerManager _manager;
    private readonly MiscellaneousListener _listener;
    private readonly ClientMonitor _clientMonitor;
    private readonly DeathRollService _deathRolls;
    private readonly IChatGui _chat;
    private Stopwatch messageTimer;

    /// <summary> This is the constructor for the OnChatMsgManager class. </summary>
    public ChatMonitor(ILogger<ChatMonitor> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, GlobalData globals, ChatSender chatSender,
        PuppeteerManager manager, MiscellaneousListener puppeteer, ClientMonitor client, 
        DeathRollService deathRolls, IChatGui chat) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _globals = globals;
        _chatSender = chatSender;
        _manager = manager;
        _listener = puppeteer;
        _clientMonitor = client;
        _deathRolls = deathRolls;
        _chat = chat;

        // set variables
        MessageQueue = new Queue<string>();
        messageTimer = new Stopwatch();
        // set up the event handlers
        _chat.ChatMessage += Chat_OnChatMessage;

        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
    }

    // the messages to send to the server.
    public static Queue<string> MessageQueue;
    // players to listen to messages from. (Format of NameWithWorld)
    public static IEnumerable<string> PlayersToListenFor;

    /// <summary> This is the disposer for the OnChatMsgManager class. </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _chat.ChatMessage -= Chat_OnChatMessage;
    }

    public static void EnqueueMessage(string message)
        => MessageQueue.Enqueue(message);

    private void Chat_OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        // Don't process messages if we are not visible or connected.
        if (_clientMonitor.ClientPlayer is null || MainHub.IsConnected is false)
            return;

        // If we have just recieved a message detailing if we just killed someone or if someone just killed us.
        if (_clientMonitor.InPvP && type is (XivChatType)2874)
        {
            // and if we are not dead, then it's our kill.
            if (!_clientMonitor.IsDead)
            {
                Logger.LogInformation("We just killed someone in PvP!", LoggerType.Achievements);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PvpPlayerSlain);
            }
        }

        // Handle the special case where we are checking a DeathRoll
        if (type == (XivChatType)2122 || type == (XivChatType)8266 || type == (XivChatType)4170)
        {
            var playerPayloads = message.Payloads.OfType<PlayerPayload>().ToList();
            if (playerPayloads.Any())
            {
                var firstPayload = playerPayloads.FirstOrDefault();
                if (firstPayload is not null)
                    _deathRolls.ProcessMessage(type, firstPayload.PlayerName + "@" + firstPayload.World.Value.Name.ToString(), message);
            }
            else
            {
                // Should check under our name if this isn't valid as someone elses player payload.
                Logger.LogDebug("Message was from self.", LoggerType.ToyboxTriggers);
                _deathRolls.ProcessMessage(type, _clientMonitor.ClientPlayer.NameWithWorld(), message);
            }
        }

        // get the player payload of the sender. If we are sending the message, this is null.
        var senderPlayerPayload = sender.Payloads.SingleOrDefault(x => x is PlayerPayload) as PlayerPayload;
        var senderName = "";
        var senderWorld = "";
        if (senderPlayerPayload == null)
        {
            senderName = _clientMonitor.ClientPlayer.Name.TextValue;
            senderWorld = _clientMonitor.ClientPlayer.HomeWorldName();
        }
        else
        {
            senderName = senderPlayerPayload.PlayerName;
            senderWorld = senderPlayerPayload.World.Value.Name.ToString();
        }

        // After this point we only check triggers, so if its not a valid trigger then dont worry about it.
        var channel = ChatChannel.GetChatChannelFromXivChatType(type);
        if (channel is null) return;

        // if we are the sender, return after checking if what we sent matches any of our pairs triggers.
        if (senderName + "@" + senderWorld == _clientMonitor.ClientPlayer.NameWithWorld())
        {
            Mediator.Publish(new ClientSentChat(channel.Value, message.TextValue));
            return;
        }

        // return if the message type is not in our valid chat channels for puppeteer.
        if (_globals.GlobalPerms is null || channel.Value.IsChannelEnabled(_mainConfig.Config.PuppeteerChannelsBitfield))
            return;

        // check for global puppeteer triggers
        var globalTriggers = _globals.GlobalPerms.TriggerPhrase.Split('|').ToList();
        if (IsValidTriggerWord(globalTriggers, message, out var globalMatch))
            if (_listener.ExecuteGlobalTrigger(globalMatch, message, _globals.GlobalPerms.PuppetPerms))
                return; // early return to prevent double trigger call.

        // check for puppeteer pair triggers
        if (_manager.TryGetListenerPairPerms(senderName, senderWorld, out var pair))
        {
            var triggers = pair.OwnPerms.TriggerPhrase.Split('|').ToList();
            if (IsValidTriggerWord(triggers, message, out var pairMatch))
            {
                // log success
                Logger.LogInformation(senderName + " used your pair trigger phrase to make you execute a message!");
                _listener.ExecutePairTrigger(pairMatch, message, pair.UserData.UID, pair.OwnPerms);
            }
        }
    }

    public bool IsValidTriggerWord(List<string> triggerPhrases, SeString chatMessage, out string matchedTrigger)
    {
        matchedTrigger = string.Empty;
        foreach (var triggerWord in triggerPhrases)
        {
            if (triggerWord.IsNullOrWhitespace())
                continue;

            if(!RegexEx.TryMatchTriggerWord(chatMessage.TextValue, triggerWord).Success)
                continue;

            Logger.LogTrace("Matched trigger word: " + triggerWord, LoggerType.Puppeteer);
            matchedTrigger = triggerWord;
            return true;
        }
        return false;
    }

    /// <summary> <b> SENDS A REAL CHAT MESSAGE TO THE SERVER </b></summary>
    public void SendRealMessage(string message)
        => Generic.ExecuteSafely(() => _chatSender.SendMessage(message));

    /// <summary> Sends messages to the server if there are any in the queue. </summary>
    private void FrameworkUpdate()
    {
        if (MessageQueue.Count <= 0)
            return;

        if (!messageTimer.IsRunning)
        {
            messageTimer.Start();
        }
        else
        {
            if (messageTimer.ElapsedMilliseconds > 500)
            {
                Generic.ExecuteSafely(() => _chatSender.SendMessage(MessageQueue.Dequeue()));
                messageTimer.Restart();
            }
        }
    }
}

