using CkCommons;
using CkCommons.Helpers;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Text.RegularExpressions;


namespace GagSpeak.Services;

/// <summary>
///     Handles all functionality related to chat message sending, intercepting, and post-process.
///     Note that this holes an internal queue 
/// </summary>
public class ChatService : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly KinksterManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly PuppeteerManager _puppetManager;
    private readonly TriggerHandler _triggerHandler;
    private readonly DeathRollService _deathRolls;

    // private variables for chat message handling 
    public static readonly ConcurrentQueue<string> _messagesToSend = new();
    private readonly Stopwatch _delayTimer = new();

    public ChatService(ILogger<ChatService> logger, GagspeakMediator mediator, MainConfig config, 
        KinksterManager pairs, GagRestrictionManager gags, PuppeteerManager puppetManager, 
        TriggerHandler triggerHandler, DeathRollService dr)
        : base(logger, mediator)
    {
        _config = config;
        _gags = gags;
        _pairs = pairs;
        _puppetManager = puppetManager;
        _triggerHandler = triggerHandler;
        _deathRolls = dr;

        _delayTimer.Start();
        Svc.Chat.ChatMessage += OnChatboxMessage;
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Logger.LogInformation("Disposing ChatService and unsubscribing from events.");
        Svc.Chat.ChatMessage -= OnChatboxMessage;
        _delayTimer?.Stop();
    }

    private void FrameworkUpdate()
    {
        if (_messagesToSend.IsEmpty || !_delayTimer.IsRunning)
            return;

        if (_delayTimer.ElapsedMilliseconds <= 500)
            return;

        if (!_messagesToSend.TryDequeue(out var message))
        {
            Logger.LogWarning("Failed to dequeue a message from the queue, this should not happen.");
            return;
        }

        SendMessage(message);
        _delayTimer.Restart();
    }

    /// <summary>
    ///     Handles incoming chat messages that have finished being processed by the server.
    /// </summary>
    private void OnChatboxMessage(XivChatType type, int ts, ref SeString sender, ref SeString msg, ref bool showInChatbox)
    {
        if (PlayerData.Object is null || MainHub.IsConnected is false)
            return; // Process as normal.

        // Check for things we dont need the player payload for.
        CheckForPvpActivity(type, msg);
        CheckForDeathroll(type, msg);

        // Extract the sender name & world from the sender payload, defaulting to the client player if not available.
        var senderPayload = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        var senderName = senderPayload?.PlayerName ?? PlayerData.Name;
        var senderWorld = senderPayload?.World.Value.Name.ToString() ?? PlayerData.HomeWorld;

        // If the chat is not a GsChatChannel, don't process anything more.
        if(ChatLogAgent.FromXivChatType(type) is not { } channel)
            return;

        Logger.LogTrace($"Chatbox Message Received: {senderName}@{senderWorld} in {channel} - {msg.TextValue}", LoggerType.ChatDetours);

        // if we are the sender, return after checking if what we sent matches any of our pairs triggers.
        if (senderName + "@" + senderWorld == PlayerData.NameWithWorld)
        {
            CheckOwnChatMessage(channel, msg.TextValue);
            Mediator.Publish(new ChatboxMessageFromSelf(channel, msg.TextValue));
            return;
        }

        // check for global puppeteer triggers
        if (_triggerHandler.PotentialGlobalTriggerMsg(senderName, senderWorld, channel, msg))
            return;

        // Check for local puppeteer triggers.
        if (_triggerHandler.PotentialPairTriggerMsg(senderName, senderWorld, channel, msg, out var kinkster))
        {
            // Let our mediator know a Kinkster sent a message.
            Mediator.Publish(new ChatboxMessageFromKinkster(kinkster, channel, msg.TextValue)); 
        }
    }

    /// <summary>
    ///     Detects any desired activity from PVP interactions.
    /// </summary>
    private void CheckForPvpActivity(XivChatType type, SeString msg)
    {
        if (!PlayerData.IsInPvP || type is not (XivChatType)2874)
            return;

        // If we got a kill, fore achievement.
        if (!PlayerData.IsDead)
        {
            Logger.LogInformation("We just killed someone in PvP!", LoggerType.Achievements);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PvpPlayerSlain);
        }
    }

    /// <summary>
    ///     Handle Deathroll Checks (/random or /dice)
    /// </summary>
    private void CheckForDeathroll(XivChatType type, SeString msg)
    {
        if (type is (XivChatType)2122 || type is (XivChatType)8266 || type is (XivChatType)4170)
        {
            if (msg.Payloads.OfType<PlayerPayload>().FirstOrDefault() is { } otherPlayer)
                _deathRolls.ProcessMessage(type, otherPlayer.PlayerName + "@" + otherPlayer.World.Value.Name.ToString(), msg);
            else
                _deathRolls.ProcessMessage(type, PlayerData.NameWithWorld, msg);
        }
    }

    /// <summary>
    ///     When a message processed by the chatbox was spesifically from us, do some unique
    ///     checks to help fulfill the conditoins for various Achievements.
    /// </summary>
    public void CheckOwnChatMessage(InputChannel channel, string msg)
    {
        // check if the message we sent contains any of our pairs triggers.
        foreach (var pair in _pairs.DirectPairs)
        {
            var triggers = pair.PairPerms.TriggerPhrase.Split("|").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            // This ensures it is a full word.
            var foundTrigger = triggers.FirstOrDefault(trigger
                => Regex.IsMatch(msg, $@"(?<!\w){Regex.Escape(trigger)}(?!\w)", RegexOptions.IgnoreCase));

            if (!string.IsNullOrEmpty(foundTrigger))
            {
                // This was a trigger message for the pair, so let's see what the pairs settings are for.
                var startChar = pair.PairPerms.StartChar;
                var endChar = pair.PairPerms.EndChar;

                // Get the string that exists beyond the trigger phrase found in the message.
                Logger.LogTrace("Sent Message with trigger phrase set by " + pair.GetNickAliasOrUid() + ". Gathering Results.", LoggerType.Puppeteer);
                SeString remainingMessage = msg.Substring(msg.IndexOf(foundTrigger) + foundTrigger.Length).Trim();

                // Get the substring within the start and end char if provided. If the start and end chars are not both present in the remaining message, keep the remaining message.
                remainingMessage.GetSubstringWithinParentheses(startChar, endChar);
                Logger.LogTrace("Remaining message after brackets: " + remainingMessage, LoggerType.Puppeteer);

                // If the string contains the word "grovel", fire the grovel achievement. (Fix these to be ID's not names.)
                if (remainingMessage.TextValue.Contains("grovel"))
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GrovelOrder);
                else if (remainingMessage.TextValue.Contains("dance"))
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.DanceOrder);
                else
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GenericOrder);
                return;
            }
        }
    }

    /// <summary>
    ///     Allows other sources to Enqueue a message to send without adding the service.
    /// </summary>
    public static void EnqueueMessage(string message)
        => _messagesToSend.Enqueue(message);

    public static void SendCommand(string command)
        => SendMessage($"/{command}");


    #region Helper Methods
    /// <summary>
    ///     A better way to handle sending a message safely now that it is integrated into XIVCLientStructs.
    /// </summary>
    private static void SendMessage(string message)
    {
        try
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            switch (bytes.Length)
            {
                case 0:
                    Svc.Logger.Warning("[ChatSender] Cannot Send Empty message!");
                    return;

                case > 500:
                    Svc.Logger.Warning("[ChatSender] Message exceeded maximum byte length!");
                    return;

                default:
                    SendMessageUnsafe(message);
                    break;
            }
        }
        catch (Exception exception)
        {
            Svc.Logger.Error($"[ChatSender] Could not send Message!: {exception}");
        }
    }

    /// <summary>
    ///     A better way to handle sending a message safely now that it is integrated into XIVCLientStructs.
    /// </summary>
    private static unsafe void SendMessageUnsafe(string message)
    {
        // Constructs the Utf8String from the message.
        var utf8Str = Utf8String.FromString(message);
        // Modern way of Sanitizing a string without direct Marshal pointer allocation.
        utf8Str->SanitizeString(SanatizeFilters, null);
        // Process the sanitized string into the chat box.
        UIModule.Instance()->ProcessChatBoxEntry(utf8Str);
        // Free the Utf8String memory to avoid memory leaks.
        utf8Str->Dtor(true);
    }

    /// <summary> 
    ///     The filters to apply when sanatizing a chat message we are sending off.
    /// </summary>
    private const AllowedEntities SanatizeFilters = 
        AllowedEntities.UppercaseLetters |
        AllowedEntities.LowercaseLetters |
        AllowedEntities.Numbers |
        AllowedEntities.SpecialCharacters |
        AllowedEntities.CharacterList |
        AllowedEntities.OtherCharacters |
        AllowedEntities.Payloads |
        AllowedEntities.Unknown8 |
        AllowedEntities.Unknown9;

    #endregion Helper Methods
}

