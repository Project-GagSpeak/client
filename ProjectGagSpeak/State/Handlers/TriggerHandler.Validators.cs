using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.GameInternals.Structs;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;

namespace GagSpeak.State.Handlers;

/// <summary> Listens for incoming changes to Alarms, Patterns, Triggers, and WIP, Vibe Server Lobby System. </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public sealed partial class TriggerHandler
{
    private readonly ILogger<TriggerHandler> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly KinksterRequests _globals;
    private readonly PairManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly PuppeteerManager _aliases;
    private readonly TriggerManager _triggers;
    private readonly SexToyManager _toys;
    private readonly MoodleHandler _moodles;
    private readonly ClientMonitor  _player;
    private readonly OnFrameworkService _frameworkUtils;

    public TriggerHandler(
        ILogger<TriggerHandler> logger,
        GagspeakMediator mediator,
        MainHub hub,
        KinksterRequests globals,
        PairManager pairs,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        PuppeteerManager aliases,
        TriggerManager triggers,
        SexToyManager toys,
        MoodleHandler moodles,
        ClientMonitor player,
        OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _globals = globals;
        _pairs = pairs;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _aliases = aliases;
        _triggers = triggers;
        _toys = toys;
        _moodles = moodles;
        _player = player;
        _frameworkUtils = frameworkUtils;
    }

    public void CheckOwnChatMessage(ChatChannel.Channels channel, string msg)
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
                _logger.LogTrace("Sent Message with trigger phrase set by " + pair.GetNickAliasOrUid() + ". Gathering Results.", LoggerType.Puppeteer);
                SeString remainingMessage = msg.Substring(msg.IndexOf(foundTrigger) + foundTrigger.Length).Trim();

                // Get the substring within the start and end char if provided. If the start and end chars are not both present in the remaining message, keep the remaining message.
                remainingMessage.GetSubstringWithinParentheses(startChar, endChar);
                _logger.LogTrace("Remaining message after brackets: " + remainingMessage, LoggerType.Puppeteer);

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

        if (_gags.ServerGagData is not { } gagData)
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

    /// <summary>
    ///     Asyncronously checks all global triggers for a match, compares against the permissions given,
    ///     then enqueues the message to be sent to the chat, or resolves the alias command as nessisary.
    /// </summary>
    public async void ExecuteGlobalTrigger(string trigger, SeString chatMessage, PuppetPerms perms)
    {
        _logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        _logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);

        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();
        _logger.LogTrace("Remaining message: " + remainingMessage, LoggerType.Puppeteer);

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses();
        _logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (perms.HasAny(PuppetPerms.Alias))
            if (await ConvertAliasCommandsIfAny(remainingMessage, _aliases.GlobalAliasStorage, MainHub.UID, ActionSource.GlobalAlias))
                return;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(perms, remainingMessage))
        {
            _logger.LogInformation("Your Global Trigger phrase was used to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return;
        }
        return;
    }

    /// <summary>
    ///     Asyncronously checks all triggers for the provided pair permissions, then compares against the permissions given,
    ///     then enqueues the message to be sent to the chat, or resolves the alias command as nessisary.
    /// </summary>
    public async void ExecutePairTrigger(string trigger, SeString chatMessage, string enactor, PairPerms perms)
    {
        _logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        _logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);

        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses(perms.StartChar, perms.EndChar);
        _logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (perms.PuppetPerms.HasFlag(PuppetPerms.Alias))
            if (await ConvertAliasCommandsIfAny(remainingMessage, _aliases.PairAliasStorage[enactor].Storage, enactor, ActionSource.PairAlias))
                return;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // verify permissions are satisfied.
        if (MeetsSettingCriteria(perms.PuppetPerms, remainingMessage))
        {
            _logger.LogInformation("[" + enactor + "] used your trigger phase to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return;
        }
        return;
    }

    /// <summary> 
    ///     Converts the input commands to the output commands from the alias list if any.
    /// </summary>
    /// <remarks> Will also determine what kind of message to prepare for execution based on the alias. </remarks>
    private async Task<bool> ConvertAliasCommandsIfAny(SeString aliasMsg, List<AliasTrigger> AliasItems, string SenderUid, ActionSource source)
    {
        var wasAnAlias = false;
        _logger.LogTrace("Found " + AliasItems.Count + " alias triggers for this user", LoggerType.Puppeteer);

        // sort by descending length so that shorter equivalents to not override longer variants.
        var sortedAliases = AliasItems.OrderByDescending(alias => alias.InputCommand.Length);
        // see if our message contains any of the alias strings. For it to match, it must match the full alias string.
        foreach (var alias in AliasItems)
        {
            if (!alias.Enabled || alias.InputCommand.IsNullOrWhitespace() || !aliasMsg.TextValue.Contains(alias.InputCommand))
                continue;

            _logger.LogTrace("Alias found: " + alias.InputCommand, LoggerType.Puppeteer);
            // fire and forget to not hang the chat listener on limbo.
            if(await HandleMultiActionAsync(alias.Actions, SenderUid, source))
            {
                wasAnAlias = true;
                break;
            }
        }
        return wasAnAlias;
    }

    /// <summary> Determines if the message meets the criteria for the sender. </summary>
    public bool MeetsSettingCriteria(PuppetPerms perms, SeString message)
    {
        if (perms.HasFlag(PuppetPerms.All))
        {
            _logger.LogTrace("Accepting Message as you allow All Commands", LoggerType.Puppeteer);
            IsEmoteMatch(message);
            return true;
        }

        if (perms.HasFlag(PuppetPerms.Emotes))
            if (IsEmoteMatch(message))
                return true;

        // 50 == Sit, 52 == Sit (Ground), 90 == Change Pose
        if (perms.HasFlag(PuppetPerms.Sit))
        {
            _logger.LogTrace("Checking if message is a sit command", LoggerType.Puppeteer);
            var sitEmote = EmoteExtensions.SittingEmotes().FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                _logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, sitEmote.RowId);
                return true;
            }
            if (EmoteService.ValidLightEmoteCache.Where(e => e.RowId is 90).Any(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower())))
            {
                _logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, 90);
                return true;
            }
        }

        return false;

        // Helper function to check if the message matches any emote
        bool IsEmoteMatch(SeString msg)
        {
            var emote = EmoteService.ValidLightEmoteCache.FirstOrDefault(e
                => e.EmoteCommands.Any(c => string.Equals(msg.TextValue, c.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));

            if (!string.IsNullOrEmpty(emote.Name))
            {
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, emote.RowId);
                return true;
            }
            return false;
        }
    }

    public async void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!_player.IsPresent || !_triggers.Storage.SpellAction.Any())
            return;

        // maybe run this in async but idk it could also be a very bad idea.
        // Only do if we know what we are doing and have performance issues.
        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            foreach (var actionEffect in actionEffects)
            {
                if ((LoggerFilter.FilteredLogTypes & LoggerType.ActionEffects) != 0)
                {
                    // Perform logging and action processing for each effect
                    var sourceCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.SourceID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";
                    var targetCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.TargetID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";

                    var actionStr = SpellActionService.AllActionsLookup.TryGetValue(actionEffect.ActionID, out var match) ? match.Name.ToString() : "UNKN ACT";

                    _logger.LogTrace($"Source:{sourceCharaStr}, Target: {targetCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                        $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
                }
                CheckSpellActionTriggers(actionEffect).ConfigureAwait(false);
            }
            ;
        });
    }

    private async Task CheckSpellActionTriggers(ActionEffectEntry actionEffect)
    {
        _logger.LogTrace("SourceID: " + actionEffect.SourceID + " TargetID: " + actionEffect.TargetID + " ActionID: " + actionEffect.ActionID + " Type: " + actionEffect.Type + " Damage: " + actionEffect.Damage, LoggerType.Triggers);

        var relevantTriggers = _triggers.Storage.SpellAction
            .Where(t => t.GetStoredIds().Contains(actionEffect.ActionID) && t.ActionKind == actionEffect.Type )
            .ToList();

        if (!relevantTriggers.Any())
            _logger.LogDebug("No relevant triggers found for this spell/action", LoggerType.Triggers);

        foreach (var trigger in relevantTriggers)
        {
            try
            {
                _logger.LogTrace("Checking Trigger: " + trigger.Label, LoggerType.Triggers);
                // Determine if the direction matches
                var isSourcePlayer = _player.ObjectId == actionEffect.SourceID;
                var isTargetPlayer = _player.ObjectId == actionEffect.TargetID;

                _logger.LogTrace("Trigger Direction we are checking was: " + trigger.Direction, LoggerType.Triggers);
                var directionMatches = trigger.Direction switch
                {
                    TriggerDirection.Self => isSourcePlayer,
                    TriggerDirection.SelfToOther => isSourcePlayer && !isTargetPlayer,
                    TriggerDirection.Other => !isSourcePlayer,
                    TriggerDirection.OtherToSelf => !isSourcePlayer && isTargetPlayer,
                    TriggerDirection.Any => true,
                    _ => false,
                };

                if (!directionMatches)
                {
                    _logger.LogDebug("Direction didn't match", LoggerType.Triggers);
                    return; // Use return instead of continue in lambda expressions
                }

                _logger.LogTrace("Direction Matches, checking damage type", LoggerType.Triggers);

                // Check damage thresholds for relevant action kinds
                var isDamageRelated = trigger.ActionKind is
                    LimitedActionEffectType.Heal or
                    LimitedActionEffectType.Damage or
                    LimitedActionEffectType.BlockedDamage or
                    LimitedActionEffectType.ParriedDamage;

                if (isDamageRelated && (actionEffect.Damage < trigger.ThresholdMinValue || actionEffect.Damage > (trigger.ThresholdMaxValue == -1 ? int.MaxValue : trigger.ThresholdMaxValue)))
                {
                    _logger.LogTrace($"Was ActionKind [" + actionEffect.Type + "], however, its damage (" + actionEffect.Damage + ") was not between (" + trigger.ThresholdMinValue +
                        ") and (" + trigger.ThresholdMaxValue + ")", LoggerType.Triggers);
                    return; // Use return instead of continue in lambda expressions
                }

                // Execute trigger action if all conditions are met
                _logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
                    + trigger.InvokableAction.ActionType.ToName(), LoggerType.Triggers);

                if (await HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing trigger");
            }
        };
    }
}
