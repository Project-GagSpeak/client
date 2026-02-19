using CkCommons;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using Lumina.Excel.Sheets;


namespace GagSpeak.Services;

/// <summary>
///     Centralized Message dispatcher and informant for chat related activities. <para />
///     Chat Messages are parsed into a friendly format that can be passed through the mediator with essential data parsed.
/// </summary>
public class ChatService : DisposableMediatorSubscriberBase
{
    // Internal queue for sending backlogged messages.
    public static readonly ConcurrentQueue<string> _messagesToSend = new();
    // A helpful timer to make our performed messages seem realistic and not instantanious.
    // Could probably remove this.
    private readonly Stopwatch _delayTimer = new();

    public ChatService(ILogger<ChatService> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
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

    /// <summary>
    ///     Process the requested queue of messages to send.
    /// </summary>
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
        if (!MainHub.IsConnected || !PlayerData.Available)
            return; // Process as normal.

        var senderPayload = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        var senderName = senderPayload?.PlayerName ?? PlayerData.Name;
        var senderWorld = senderPayload?.World.Value.Name.ToString() ?? PlayerData.HomeWorldName;

        // Check for things we dont need the player payload for.
        CheckForPvpActivity(type, msg);
        CheckForDeathroll(type, msg);

        // If the chat is not a normal chat channel do not process.
        if(ChatLogAgent.FromXivChatType(type) is not { } channel)
            return;

        Mediator.Publish(new GameChatMessage(channel, $"{senderName}@{senderWorld}", msg));
    }

    /// <summary>
    ///     Detects any desired activity from PVP interactions.
    /// </summary>
    private void CheckForPvpActivity(XivChatType type, SeString msg)
    {
        if (!PlayerData.InPvP || type is not (XivChatType)2874)
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
                Mediator.Publish(new DeathrollMessage(type, $"{otherPlayer.PlayerName}@{otherPlayer.World.Value.Name.ToString()}", msg));
            else
                Mediator.Publish(new DeathrollMessage(type, PlayerData.NameWithWorld, msg));
        }
    }

    /// <summary>
    ///     Allows other sources to Enqueue a message to send without adding the service.
    /// </summary>
    public static void EnqueueMessage(string message)
        => _messagesToSend.Enqueue(message);

    public static void SendCommand(string command)
        => SendMessage($"/{command}");

    public static void SendGeneralActionCommand(uint actionId)
        => SendCommand($"generalaction \"{Svc.Data.GetExcelSheet<GeneralAction>().GetRowOrDefault(actionId)?.Name}\"");

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
        catch (Bagagwa exception)
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

