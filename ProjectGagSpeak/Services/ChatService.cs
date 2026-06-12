using CkCommons;
using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.GameInternals.Agents;
using GagSpeak.Services.Mediator;
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
        Svc.Chat.LogMessage += OnLogMessage;
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Logger.LogInformation("Disposing ChatService and unsubscribing from events.");
        Svc.Chat.LogMessage -= OnLogMessage;
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
    /// Handles the `LogMessage` event, parsing incoming chat messages and invoking specific checks
    /// for relevant in-game activities such as deathroll or PvP activity.
    /// </summary>
    /// <param name="message">
    /// The chat message provided by the game's logging system, containing details to be processed.
    /// </param>
    private void OnLogMessage(ILogMessage message)
    {
        if (!MainHub.IsConnected || !PlayerData.Available)
            return; // Process as normal.
        
        // Check for things that are pushed to LogMessages.
        CheckForDeathroll(message);
        CheckForPvpActivity(message);
    }

    /// <summary>
    /// Generates the full entity name including the world name if available.
    /// </summary>
    /// <param name="entity">The log message entity containing the name and home world information.</param>
    /// <returns>
    /// A string representing the entity name combined with the world name, or an empty string if the entity is null.
    /// </returns>
    private static string CalculateEntityNameWithWorld(ILogMessageEntity? entity)
    {
        return entity is null ? string.Empty : $"{entity.Name}@{entity.HomeWorld.ValueNullable?.Name.ToString()}";
    }
    
    /// <summary>
    ///     Detects any desired activity from PVP interactions.
    /// </summary>
    private void CheckForPvpActivity(ILogMessage message)
    {
        // Pvp defeat log messages are of type SystemError.
        if (!PlayerData.InPvP || message.LogMessageId is not 557)
            return;

        // If we got a kill, fore achievement.
        var sourceName = CalculateEntityNameWithWorld(message.SourceEntity);
        if (!PlayerData.IsDead && sourceName == PlayerData.Name)
        {
            Logger.LogInformation("We just killed someone in PvP!", LoggerType.Achievements);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PvpPlayerSlain);
        }
    }

    /// <summary>
    ///     Handle Deathroll Checks (/random)
    ///     MessageId 856 -> /random
    ///     MessageId 3887 -> /random 1000
    /// </summary>
    private void CheckForDeathroll(ILogMessage message)
    {
        Logger.LogDebug("Checking for Deathroll Message.", LoggerType.Triggers);
        Logger.LogDebug($"Message LogId: {message.LogMessageId}, Source: {message.SourceEntity?.Name}, Parameters: {message.ParameterCount}", LoggerType.Triggers);
        // Only care about /random messages
        if (message.LogMessageId is not (856 or 3887))
            return;
        
        Logger.LogDebug("Handling Deathroll Message.", LoggerType.Triggers);
        var world = message.SourceEntity?.HomeWorld.ValueNullable?.Name.ToString();
        var sender = message.Parameters[0].StringValue;
        var nameWithWorld = $"{sender}@{world}";
        var rolled = message.Parameters[1].UIntValue;
        Logger.LogDebug($"Received Deathroll Message from {nameWithWorld}", LoggerType.Triggers);
        
        // Check for a number cap. If not present, default to 999.
        var cap = message.ParameterCount > 2 ? message.Parameters[2].UIntValue : 0;
        
        Logger.LogDebug($"Rolled {rolled} with cap {cap}", LoggerType.Triggers);
        // Clamp and validate values.
        var rollResult = rolled > 999 ? -1 : (int)rolled;
        var capResult = cap is 0 or > 999 ? -1 : (int)cap;
        
        Logger.LogDebug($"Validated Deathroll: Roll {rollResult}, Cap {capResult}", LoggerType.Triggers);
        Mediator.Publish(new DeathrollMessage(nameWithWorld, rollResult, capResult));
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
    ///     The filters to apply when sanitizing a chat message we are sending off.
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
