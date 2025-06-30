using CkCommons.Helpers;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Agents;
using GagSpeak.GameInternals.Structs;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles incoming invokations for various triggers to in turn execute them
/// </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public class TriggerHandler
{
    private readonly ILogger<TriggerHandler> _logger;
    private readonly MainConfig _config;
    private readonly GlobalPermissions _globals;
    private readonly PuppeteerManager _aliases;
    private readonly TriggerManager _triggers;
    private readonly TriggerActionService _triggerService;
    private readonly OnFrameworkService _frameworkUtils;

    public TriggerHandler(ILogger<TriggerHandler> logger, MainConfig config,
        GlobalPermissions globals, PuppeteerManager aliases, TriggerManager triggers,
        TriggerActionService triggerService, OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _config = config;
        _globals = globals;
        _aliases = aliases;
        _triggers = triggers;
        _triggerService = triggerService;
        _frameworkUtils = frameworkUtils;
    }

    public bool PotentialGlobalTriggerMsg(string senderName, string senderWorld, InputChannel channel, SeString msg)
    {
        if (_globals.Current is not { } globals)
            return false;

        // Check for Global Triggers first.
        var globalTriggers = globals.TriggerPhrase.Split('|').ToList();
        if (IsValidTriggerWord(globalTriggers, msg, out var globalMatch))
        {
            ExecuteTrigger(globalMatch, msg, _globals.Current.PuppetPerms, _aliases.GlobalAliasStorage, ActionSource.GlobalAlias, MainHub.UID);
            return true;
        }
        return false;
    }

    public bool PotentialPairTriggerMsg(string sName, string sWorld, InputChannel channel, SeString msg, [NotNullWhen(true)] out Kinkster? kinksterMatch)
    {
        // Check for Pair Triggers.
        if (_aliases.TryGetListenerPairPerms(sName, sWorld, out kinksterMatch))
        {
            var triggers = kinksterMatch.OwnPerms.TriggerPhrase.Split('|').ToList();
            if (IsValidTriggerWord(triggers, msg, out var pairMatch))
            {
                var storage = _aliases.PairAliasStorage[kinksterMatch.UserData.UID].Storage;
                ExecuteTrigger(pairMatch, msg, kinksterMatch.OwnPerms.PuppetPerms, storage, ActionSource.PairAlias,
                    kinksterMatch.UserData.UID, kinksterMatch.OwnPerms.StartChar, kinksterMatch.OwnPerms.EndChar);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    ///     Called upon by the ActionEffectDetour
    /// </summary>
    public async void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!PlayerData.Available || !_triggers.Storage.SpellAction.Any())
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
                    var sourceCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.SourceID) as IPlayerCharacter)?.GetNameWithWorld() ?? "UNKN OBJ";
                    var targetCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.TargetID) as IPlayerCharacter)?.GetNameWithWorld() ?? "UNKN OBJ";

                    var actionStr = SpellActionService.AllActionsLookup.TryGetValue(actionEffect.ActionID, out var match) ? match.Name.ToString() : "UNKN ACT";

                    _logger.LogTrace($"Source:{sourceCharaStr}, Target: {targetCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                        $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
                }
                CheckSpellActionTriggers(actionEffect).ConfigureAwait(false);
            };
        });
    }


    /// <summary>
    ///     Sees if the trigger phases for a global or pair that matched from a chat message is valid.
    /// </summary>
    private bool IsValidTriggerWord(List<string> triggerPhrases, SeString chatMessage, out string matchedTrigger)
    {
        matchedTrigger = string.Empty;
        foreach (var triggerWord in triggerPhrases)
        {
            if (triggerWord.IsNullOrWhitespace())
                continue;

            if (!RegexEx.TryMatchTriggerWord(chatMessage.TextValue, triggerWord).Success)
                continue;

            _logger.LogTrace("Matched trigger word: " + triggerWord, LoggerType.Puppeteer);
            matchedTrigger = triggerWord;
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Handles Trigger Execution logic once we have determined one was found!
    /// </summary>
    private async void ExecuteTrigger(string trigger, SeString msg, PuppetPerms perms, AliasStorage storage, ActionSource source,
        string enactorUid, char startChar = '(', char endChar = ')')
    {
        _logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        _logger.LogTrace("Message we are checking for the trigger in: " + msg, LoggerType.Puppeteer);

        // obtain the substring that occurs in the message after the trigger.
        SeString finalMsg = msg.TextValue.Substring(msg.TextValue.IndexOf(trigger) + trigger.Length).Trim();

        // obtain the substring within the start and end char if provided.
        finalMsg = finalMsg.GetSubstringWithinParentheses(startChar, endChar);
        _logger.LogTrace("Remaining message after brackets: " + finalMsg);

        // If it was for an alias, and not a text insstruction, handle the alias and return early.
        if (perms.HasAny(PuppetPerms.Alias))
            if (await ConvertAliasCommandsIfAny(finalMsg, storage.Items, enactorUid, ActionSource.PairAlias))
                return;

        // Otherwise, handle the final message accordingly.
        if (finalMsg.TextValue.IsNullOrEmpty())
            return;

        // apply bracket conversions.
        finalMsg = finalMsg.ConvertSquareToAngleBrackets();

        // verify permissions are satisfied.
        if (MeetsSettingCriteria(perms, finalMsg))
        {
            var enclosedText = (enactorUid == MainHub.UID) ? "A GlobalTrigger" : enactorUid;
            _logger.LogInformation($"[{enclosedText}] made you execute a message!", LoggerType.Puppeteer);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatService.EnqueueMessage("/" + finalMsg.TextValue);
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
            if(await _triggerService.HandleMultiActionAsync(alias.Actions, SenderUid, source))
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
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, sitEmote.RowId);
                return true;
            }
            if (EmoteService.ValidLightEmoteCache.Where(e => e.RowId is 90).Any(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower())))
            {
                _logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, 90);
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
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, emote.RowId);
                return true;
            }
            return false;
        }
    }

    private async Task CheckSpellActionTriggers(ActionEffectEntry actionEffect)
    {
        _logger.LogTrace("SourceID: " + actionEffect.SourceID + " TargetID: " + actionEffect.TargetID + " ActionID: " + actionEffect.ActionID + " Type: " + actionEffect.Type + " Damage: " + actionEffect.Damage, LoggerType.Triggers);

        // please for the love of god find a better way to handle this.
        var relevantTriggers = _triggers.Storage.SpellAction
            .Where(t => t.ActionKind == actionEffect.Type && t.GetStoredIds().Contains(actionEffect.ActionID) && t.ActionKind == actionEffect.Type )
            .ToList();

        if (!relevantTriggers.Any())
        {
            _logger.LogDebug("No relevant triggers found for this spell/action", LoggerType.Triggers);
            return;
        }

        foreach (var trigger in relevantTriggers)
        {
            _logger.LogTrace("Checking Trigger: " + trigger.Label, LoggerType.Triggers);
            if (!IsDirectionMatch(trigger.Direction, PlayerData.Object?.GameObjectId ?? 0, actionEffect.SourceID, actionEffect.TargetID))
            {
                _logger.LogDebug("Direction didn't match", LoggerType.Triggers);
                continue;
            }

            _logger.LogTrace("Direction Matches, checking damage type", LoggerType.Triggers);
            var isDamageRelated = trigger.ActionKind is
                LimitedActionEffectType.Heal or
                LimitedActionEffectType.Damage or
                LimitedActionEffectType.BlockedDamage or
                LimitedActionEffectType.ParriedDamage;

            if (isDamageRelated && !IsDamageWithinThreshold(actionEffect.Damage, trigger.ThresholdMinValue, trigger.ThresholdMaxValue))
            {
                _logger.LogTrace($"Was ActionKind [{actionEffect.Type}], but its damage ({actionEffect.Damage}) wasn't " +
                    $"between ({trigger.ThresholdMinValue}) & ({trigger.ThresholdMaxValue})", LoggerType.Triggers);
                continue;
            }

            // Execute trigger action if all conditions are met
            _logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
                + trigger.InvokableAction.ActionType.ToName(), LoggerType.Triggers);

            if (await _triggerService.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
                GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
        };
    }

    private bool IsDirectionMatch(TriggerDirection direction, ulong playerId, uint sourceId, ulong targetId)
    {
        var isSourcePlayer = playerId == sourceId;
        var isTargetPlayer = playerId == targetId;
        return direction switch
        {
            TriggerDirection.Self => isSourcePlayer,
            TriggerDirection.SelfToOther => isSourcePlayer && !isTargetPlayer,
            TriggerDirection.Other => !isSourcePlayer,
            TriggerDirection.OtherToSelf => !isSourcePlayer && isTargetPlayer,
            TriggerDirection.Any => true,
            _ => false,
        };
    }

    private bool IsDamageWithinThreshold(uint damage, int min, int max)
        => damage >= min && (max == -1 || damage <= max);


}
