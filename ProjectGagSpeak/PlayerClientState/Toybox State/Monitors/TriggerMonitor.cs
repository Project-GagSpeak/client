using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text.RegularExpressions;

namespace GagSpeak.PlayerState.Listener;

/// <summary> Listens for incoming changes to Alarms, Patterns, Triggers, and WIP, Vibe Server Lobby System. </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public sealed class TriggerMonitor : DisposableMediatorSubscriberBase
{
    private readonly GlobalData _globals;
    private readonly PairManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly PuppeteerManager _aliasManager;
    private readonly TriggerManager _triggerManager;
    private readonly TriggerApplier _triggerApplier;
    private readonly ClientMonitor  _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;
    public TriggerMonitor(ILogger<TriggerMonitor> logger, GagspeakMediator mediator,
        GlobalData globals, PairManager pairs, GagRestrictionManager gags,
        PuppeteerManager aliasManager, TriggerManager triggerManager, TriggerApplier triggerApplier,
        ClientMonitor clientMonitor, OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _globals = globals;
        _pairs = pairs;
        _gags = gags;
        _aliasManager = aliasManager;
        _triggerManager = triggerManager;
        _triggerApplier = triggerApplier;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;

        ActionEffectMonitor.ActionEffectEntryEvent += OnActionEffectEvent;
        Mediator.Subscribe<ClientSentChat>(this, (msg) => OnClientChatSent(msg.Channel, msg.Message));
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateTriggerMonitors());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => UpdateTrackedPlayerHealth());
    }

    protected override void Dispose(bool disposing)
    {
        ActionEffectMonitor.ActionEffectEntryEvent -= OnActionEffectEvent;
        base.Dispose(disposing);
    }
    #region TextTriggers
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



    public async void ExecuteGlobalTrigger(string trigger, SeString chatMessage, PuppetPerms perms)
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
            if (await ConvertAliasCommandsIfAny(remainingMessage, _aliasManager.GlobalAliasStorage, MainHub.UID, ActionSource.GlobalAlias))
                return;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(perms, remainingMessage))
        {
            Logger.LogInformation("Your Global Trigger phrase was used to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return;
        }
        return;
    }

    public async void ExecutePairTrigger(string trigger, SeString chatMessage, string enactor, UserPairPermissions perms)
    {
        Logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        Logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);

        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses(perms.StartChar, perms.EndChar);
        Logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (perms.PuppetPerms.HasFlag(PuppetPerms.Alias))
            if (await ConvertAliasCommandsIfAny(remainingMessage, _aliasManager.PairAliasStorage[enactor].Storage, enactor, ActionSource.PairAlias))
                return;

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
            return;

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        // verify permissions are satisfied.
        if (MeetsSettingCriteria(perms.PuppetPerms, remainingMessage))
        {
            Logger.LogInformation("[" + enactor + "] used your trigger phase to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatMonitor.EnqueueMessage("/" + remainingMessage.TextValue);
            return;
        }
        return;
    }

    /// <summary> Converts the input commands to the output commands from the alias list if any. </summary>
    /// <remarks> Will also determine what kind of message to prepare for execution based on the alias. </remarks>
    public async Task<bool> ConvertAliasCommandsIfAny(SeString aliasMsg, List<AliasTrigger> AliasItems, string SenderUid, ActionSource source)
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

            Logger.LogTrace("Alias found: " + alias.InputCommand, LoggerType.Puppeteer);
            // fire and forget to not hang the chat listener on limbo.
            if(await _triggerApplier.HandleMultiActionAsync(alias.Actions, SenderUid, source))
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
            var sitEmote = EmoteExtensions.SittingEmotes().FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                Logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, sitEmote.RowId);
                return true;
            }
            if (EmoteService.ValidLightEmoteCache.Where(e => e.RowId is 90).Any(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower())))
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
    #endregion TextTriggers

    #region SpellAction
    private void CheckSpellActionTriggers(ActionEffectEntry actionEffect)
    {
        Logger.LogTrace("SourceID: " + actionEffect.SourceID + " TargetID: " + actionEffect.TargetID + " ActionID: " + actionEffect.ActionID + " Type: " + actionEffect.Type + " Damage: " + actionEffect.Damage, LoggerType.ToyboxTriggers);

        var relevantTriggers = _triggerManager.Storage.SpellAction
            .Where(t => t.GetStoredIds().Contains(actionEffect.ActionID) && t.ActionKind == actionEffect.Type )
            .ToList();

        if (!relevantTriggers.Any())
            Logger.LogDebug("No relevant triggers found for this spell/action", LoggerType.ToyboxTriggers);

        foreach (var trigger in relevantTriggers)
        {
            try
            {
                Logger.LogTrace("Checking Trigger: " + trigger.Label, LoggerType.ToyboxTriggers);
                // Determine if the direction matches
                var isSourcePlayer = _clientMonitor.ObjectId == actionEffect.SourceID;
                var isTargetPlayer = _clientMonitor.ObjectId == actionEffect.TargetID;

                Logger.LogTrace("Trigger Direction we are checking was: " + trigger.Direction, LoggerType.ToyboxTriggers);
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
                    Logger.LogDebug("Direction didn't match", LoggerType.ToyboxTriggers);
                    return; // Use return instead of continue in lambda expressions
                }

                Logger.LogTrace("Direction Matches, checking damage type", LoggerType.ToyboxTriggers);

                // Check damage thresholds for relevant action kinds
                var isDamageRelated = trigger.ActionKind is
                    LimitedActionEffectType.Heal or
                    LimitedActionEffectType.Damage or
                    LimitedActionEffectType.BlockedDamage or
                    LimitedActionEffectType.ParriedDamage;

                if (isDamageRelated && (actionEffect.Damage < trigger.ThresholdMinValue || actionEffect.Damage > (trigger.ThresholdMaxValue == -1 ? int.MaxValue : trigger.ThresholdMaxValue)))
                {
                    Logger.LogTrace($"Was ActionKind [" + actionEffect.Type + "], however, its damage (" + actionEffect.Damage + ") was not between (" + trigger.ThresholdMinValue +
                        ") and (" + trigger.ThresholdMaxValue + ")", LoggerType.ToyboxTriggers);
                    return; // Use return instead of continue in lambda expressions
                }

                // Execute trigger action if all conditions are met
                Logger.LogDebug($"{actionEffect.Type} Action Triggered", LoggerType.ToyboxTriggers);
                ExecuteTriggerAction(trigger);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing trigger");
            }
        };
    }
    #endregion SpellAction

    #region RestraintState
    public void CheckActiveRestraintTriggers(Guid setId, NewState state)
    {
        // make this only allow apply and lock, especially on the setup.
        var matchingTriggers = _triggerManager.Storage.RestraintState
            .Where(trigger => trigger.RestraintSetId == setId && trigger.RestraintState == state)
            .ToList();

        // if the triggers is not empty, perform logic, but return if there isnt any.
        if (!matchingTriggers.Any())
            return;

        // find the relevant trigger with the highest priority.
        var highestPriorityTrigger = matchingTriggers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        // execute this trigger action.
        if (highestPriorityTrigger != null)
            ExecuteTriggerAction(highestPriorityTrigger);
    }
    #endregion RestraintState

    #region RestrictionState
    public void CheckActiveRestrictionTriggers(Guid setId, NewState state)
    {
        // make this only allow apply and lock, especially on the setup.
        var matchingTriggers = _triggerManager.Storage.RestrictionState
            .Where(trigger => trigger.RestrictionId == setId && trigger.RestrictionState == state)
            .ToList();

        // if the triggers is not empty, perform logic, but return if there isnt any.
        if (!matchingTriggers.Any())
            return;

        // find the relevant trigger with the highest priority.
        var highestPriorityTrigger = matchingTriggers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        // execute this trigger action.
        if (highestPriorityTrigger != null)
            ExecuteTriggerAction(highestPriorityTrigger);
    }
    #endregion RestrictionState

    #region GagState
    private void CheckGagStateTriggers(GagType gagType, NewState newState)
    {
        // Check to see if any active gag triggers are in the message
        var matchingTriggers = _triggerManager.Storage.GagState
            .Where(x => x.Gag == gagType && x.GagState == newState)
            .ToList();

        // if the triggers is not empty, perform logic, but return if there isnt any.
        if (!matchingTriggers.Any())
            return;

        // find the relevant trigger with the highest priority.
        var highestPriorityTrigger = matchingTriggers
            .OrderByDescending(x => x.Priority)
            .FirstOrDefault();

        // execute this trigger action.
        if (highestPriorityTrigger != null)
            ExecuteTriggerAction(highestPriorityTrigger);
    }
    #endregion GagState

    #region HealthPercent
    private readonly Dictionary<IPlayerCharacter, PlayerHealth> MonitoredPlayers = [];
    private record PlayerHealth(IEnumerable<HealthPercentTrigger> triggers)
    {
        public uint LastHp { get; set; }
        public uint LastMaxHp { get; set; }
    };

    private void UpdateTriggerMonitors()
    {
        if (!_triggerManager.Storage.HealthPercent.Any())
        {
            MonitoredPlayers.Clear();
            return;
        }

        // Group triggers by the player being monitored.
        var playerTriggers = _triggerManager.Storage.HealthPercent
            .GroupBy(trigger => trigger.PlayerNameWorld)
            .ToDictionary(group => group.Key, group => new PlayerHealth(group.AsEnumerable()));

        // Get the visible characters.
        var visiblePlayerCharacters = _frameworkUtils.GetObjectTablePlayers()
            .Where(player => playerTriggers.Keys.Contains(player.NameWithWorld()));

        // Remove players from MonitoredPlayers who are no longer visible.
        var playersToRemove = MonitoredPlayers.Keys.Except(visiblePlayerCharacters);

        // Add Players that should be tracked that are now visible.
        var playersToAdd = visiblePlayerCharacters.Except(MonitoredPlayers.Keys);

        // remove all the non-visible players
        foreach (var player in playersToRemove)
            MonitoredPlayers.Remove(player);

        // add all the visible players
        foreach (var player in playersToAdd)
            if (playerTriggers.TryGetValue(player.NameWithWorld(), out var triggers))
                MonitoredPlayers.Add(player, triggers);
    }

    private void UpdateTrackedPlayerHealth()
    {
        if (!MonitoredPlayers.Any())
            return;

        // Handle updating the monitored players.
        foreach (var player in MonitoredPlayers)
        {
            // if no hp changed, continue.
            if (player.Key.CurrentHp == player.Value.LastHp || player.Key.MaxHp == player.Value.LastMaxHp)
                continue;

            // Calculate health percentages once per player to avoid redundancies.
            var percentageHP = player.Key.CurrentHp * 100f / player.Key.MaxHp;
            var previousPercentageHP = player.Value.LastHp * 100f / player.Value.LastMaxHp;

            // scan the playerHealth values for trigger change conditions.
            foreach (var trigger in player.Value.triggers)
            {
                var isValid = false;

                // Check if health thresholds are met based on trigger type
                if (trigger.PassKind == ThresholdPassType.Under)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP > trigger.ThresholdMinValue && percentageHP <= trigger.ThresholdMinValue) ||
                            (previousPercentageHP > trigger.ThresholdMaxValue && percentageHP <= trigger.ThresholdMaxValue)
                        : (player.Value.LastHp > trigger.ThresholdMinValue && player.Key.CurrentHp <= trigger.ThresholdMinValue) ||
                            (player.Value.LastHp > trigger.ThresholdMaxValue && player.Key.CurrentHp <= trigger.ThresholdMaxValue);
                }
                else if (trigger.PassKind == ThresholdPassType.Over)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP < trigger.ThresholdMinValue && percentageHP >= trigger.ThresholdMinValue) ||
                            (previousPercentageHP < trigger.ThresholdMaxValue && percentageHP >= trigger.ThresholdMaxValue)
                        : (player.Value.LastHp < trigger.ThresholdMinValue && player.Key.CurrentHp >= trigger.ThresholdMinValue) ||
                            (player.Value.LastHp < trigger.ThresholdMaxValue && player.Key.CurrentHp >= trigger.ThresholdMaxValue);
                }

                if (isValid)
                    ExecuteTriggerAction(trigger);
            }
        }
    }
    #endregion HealthPercent

    public async void ExecuteTriggerAction(Trigger trigger)
    {
        Logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
            + trigger.InvokableAction.ActionType.ToName(), LoggerType.ToyboxTriggers);

        if (await _triggerApplier.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
            UnlocksEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
    }

    public async void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!_clientMonitor.IsPresent || !_triggerManager.Storage.SpellAction.Any())
            return;

        // maybe run this in async but idk it could also be a very bad idea.
        // Only do if we know what we are doing and have performance issues.
        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            foreach (var actionEffect in actionEffects)
            {
                if (LoggerFilter.FilteredCategories.Contains(LoggerType.ActionEffects))
                {
                    // Perform logging and action processing for each effect
                    var sourceCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.SourceID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";
                    var targetCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.TargetID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";
                    
                    var actionStr = SpellActionService.AllActionsLookup.TryGetValue(actionEffect.ActionID, out var match) ? match.Name.ToString() : "UNKN ACT";

                    Logger.LogTrace($"Source:{sourceCharaStr}, Target: {targetCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                        $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
                }
                CheckSpellActionTriggers(actionEffect);
            };
        });
    }

}
