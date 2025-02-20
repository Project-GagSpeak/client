using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons.Text;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using System.Text.RegularExpressions;

namespace GagSpeak.PlayerState.Listener;

/// <summary> Listeners for components that are not in the toybox compartment nor are visual components. </summary>
/// <remarks> May be catagorized later, but are filtered into here for now. </remarks>
public sealed class MiscellaneousListener : DisposableMediatorSubscriberBase
{
    private readonly GlobalData _globals;
    private readonly PairManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly PuppeteerManager _aliasManager;
    private readonly TriggerApplier _actionInvoker;
    private readonly ClientMonitor  _clientMonitor;
    public MiscellaneousListener(
        ILogger<ToyboxStateListener> logger,
        GagspeakMediator mediator,
        GlobalData globals,
        PairManager pairs,
        GagRestrictionManager gags,
        PuppeteerManager aliasManager,
        TriggerApplier actionInvoker,
        ClientMonitor clientMonitor) : base(logger, mediator)
    {
        _globals = globals;
        _pairs = pairs;
        _gags = gags;
        _aliasManager = aliasManager;
        _actionInvoker = actionInvoker;
        _clientMonitor = clientMonitor;

        Mediator.Subscribe<ClientSentChat>(this, (msg) => OnClientChatSent(msg.Channel, msg.Message));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void OnClientChatSent(ChatChannel.Channels channel, string msg)
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

                // If the string contains the word "grovel", fire the grovel achievement.
                if (remainingMessage.TextValue.Contains("grovel"))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GrovelOrder);
                else if (remainingMessage.TextValue.Contains("dance"))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.DanceOrder);
                else
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GenericOrder);

                return;
            }
        }

        if (_gags.ActiveGagsData is not { } gagData)
            return;
        if (_globals.GlobalPerms is not { } globalPerms)
            return;

        // if our message is longer than 5 words, fire our on-chat-message achievement.
        if (gagData.IsGagged() && globalPerms.ChatGarblerActive && msg.Split(' ').Length > 5)
        {
            if (channel.IsChannelEnabled(_globals.GlobalPerms.ChatGarblerChannelsBitfield))
                UnlocksEventManager.AchievementEvent(UnlocksEvent.ChatMessageSent, channel);
        }
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            Mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }

    public void UpdateListener(string pairName, string listenerName)
    {
        _aliasManager.UpdateStoredAliasName(pairName, listenerName);
        PostActionMsg(pairName, InteractionType.ListenerName, $"Updated listener name to {listenerName} for {pairName}.");
    }

    public bool ExecuteGlobalTrigger(string trigger, SeString chatMessage, PuppetPerms perms)
    {
        Logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        Logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);
        
        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();
        Logger.LogTrace("Remaining message: " + remainingMessage, LoggerType.Puppeteer);

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses();
        Logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (perms.HasFlag(PuppetPerms.Alias))
            if(ConvertAliasCommandsIfAny(remainingMessage, _aliasManager.GlobalAliasStorage, MainHub.UID, ActionSource.GlobalAlias))
                return true;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return false;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(perms, remainingMessage))
        {
            Logger.LogInformation("Your Global Trigger phrase was used to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return true;
        }
        return false;
    }

    public bool ExecutePairTrigger(string trigger, SeString chatMessage, string enactor, UserPairPermissions perms)
    {
        Logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        Logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);

        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses(perms.StartChar, perms.EndChar);
        Logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (perms.PuppetPerms.HasFlag(PuppetPerms.Alias))
            if(ConvertAliasCommandsIfAny(remainingMessage, _aliasManager.PairAliasStorage[enactor].Storage, enactor, ActionSource.PairAlias))
                return true;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return false;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // verify permissions are satisfied.
        if (MeetsSettingCriteria(perms.PuppetPerms, remainingMessage))
        {
            Logger.LogInformation("[" + enactor + "] used your trigger phase to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return true;
        }
        return false;
    }

    /// <summary> Converts the input commands to the output commands from the alias list if any. </summary>
    /// <remarks> Will also determine what kind of message to prepare for execution based on the alias. </remarks>
    public bool ConvertAliasCommandsIfAny(SeString aliasMsg, List<AliasTrigger> AliasItems, string SenderUid, ActionSource source)
    {
        var wasAnAlias = false;
        Logger.LogTrace("Found " + AliasItems.Count + " alias triggers for this user", LoggerType.Puppeteer);

        // sort by descending length so that shorter equivalents to not override longer variants.
        var sortedAliases = AliasItems.OrderByDescending(alias => alias.InputCommand.Length);
        // see if our message contains any of the alias strings. For it to match, it must match the full alias string.
        foreach (var alias in AliasItems)
        {
            if (!alias.Enabled || alias.InputCommand.IsNullOrWhitespace() || !aliasMsg.TextValue.Contains(alias.InputCommand))
                continue;

            wasAnAlias = true;
            Logger.LogTrace("Alias found: " + alias.InputCommand, LoggerType.Puppeteer);
            // fire and forget to not hang the chat listener on limbo.
            _ = _actionInvoker.HandleMultiActionAsync(alias.Executions.Values, SenderUid, source);
        }
        return wasAnAlias;
    }

    /// <summary> Determines if the message meets the criteria for the sender. </summary>
    public bool MeetsSettingCriteria(PuppetPerms perms, SeString message)
    {
        if (perms.HasFlag(PuppetPerms.All))
        {
            Logger.LogTrace("Accepting Message as you allow All Commands", LoggerType.Puppeteer);
            IsEmoteMatch(message);
            return true;
        }

        if (perms.HasFlag(PuppetPerms.Emotes))
            if (IsEmoteMatch(message))
                return true;

        // 50 == Sit, 52 == Sit (Ground), 90 == Change Pose
        if (perms.HasFlag(PuppetPerms.Sit))
        {
            Logger.LogTrace("Checking if message is a sit command", LoggerType.Puppeteer);
            var sitEmote = EmoteMonitor.SitEmoteComboList.FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                Logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, sitEmote.RowId);
                return true;
            }
            if (EmoteMonitor.ValidEmotes.Where(e => e.RowId is 90).Any(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower())))
            {
                Logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, 90);
                return true;
            }
        }

        return false;

        // Helper function to check if the message matches any emote
        bool IsEmoteMatch(SeString msg)
        {
            var emote = EmoteMonitor.ValidEmotes.FirstOrDefault(e 
                => e.EmoteCommands.Any(c => string.Equals(msg.TextValue, c.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(emote.Name))
            {
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, emote.RowId);
                return true;
            }
            return false;
        }
    }
}
