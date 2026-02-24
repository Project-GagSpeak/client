using CkCommons;
using CkCommons.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Structs;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.Watchers;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles incoming monitored or manually invoked updates to call respective invocations if valid.
/// </summary>
public class TriggerHandler : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly KinksterManager _kinksters;
    private readonly PuppeteerManager _puppeteer;
    private readonly TriggerManager _triggers;
    private readonly SelfBondageService _selfBondage;
    private readonly ReactionDistributor _processor;

    public TriggerHandler(ILogger<TriggerHandler> logger, GagspeakMediator mediator,
        MainConfig config, KinksterManager kinksters, PuppeteerManager aliases,
        TriggerManager triggers, SelfBondageService selfBondage, ReactionDistributor processor)
        : base(logger, mediator)
    {
        _config = config;
        _kinksters = kinksters;
        _puppeteer = aliases;
        _triggers = triggers;
        _selfBondage = selfBondage;
        _processor = processor;

        Mediator.Subscribe<GameChatMessage>(this, _ => OnGameChat(_.Channel, _.SenderNameWorld, _.Msg));
        Mediator.Subscribe<EmoteDetected>(this, _ => OnEmote(_.ID, _.CallerAddr, _.TargetAddr));
        Mediator.Subscribe<HpMonitorTriggered>(this, _ => OnHpTrigger(_.PlayerAddr, _.HpTrigger));
        Mediator.Subscribe<GagStateChanged>(this, _ => OnGagChange(_.State, _.Layer, _.Data, _.Enactor, _.Target));
        Mediator.Subscribe<RestrictionStateChanged>(this, _ => OnRestrictionChange(_.State, _.Layer, _.Data, _.Enactor, _.Target));
        Mediator.Subscribe<RestraintStateChanged>(this, _ => OnRestraintChange(_.State, _.Data, _.Enactor, _.Target));
        Mediator.Subscribe<DeathrollResult>(this, _ => OnSocialGameEnd(_.WinnerNameWorld, _.LoserNameWorld));
    }

    #region Handlers
    /// <summary>
    ///     Processes a game's chat message for trigger detection. This is independant of GagPlates.
    /// </summary>
    private void OnGameChat(InputChannel channel, string senderNameWorld, SeString msg)
    {
        Logger.LogTrace($"OnGameChat: [{channel}]{senderNameWorld} - {msg.TextValue}", LoggerType.ChatDetours);
        // If from ourselves run a check if we issued a puppeteer command on someone else.
        if (PlayerData.NameWithWorld == senderNameWorld)
        {
            ScanOwnChat(channel, msg);
            return;
        }

        // Otherwise it's a potential Puppeteer Command. Ignore if not valid channel.
        if (!_config.Current.PuppeteerChannelsBitfield.IsActiveChannel((int)channel))
            return;

        // Also ignore if we have no valid globals, or if our Puppeteer is not enabled.
        if (ClientData.Globals is not { } globals)
            return;
        if (!globals.PuppeteerEnabled)
            return;

        // Assume first that this was a GlobalTrigger
        if (!string.IsNullOrWhiteSpace(globals.TriggerPhrase))
        {
            var gTriggers = globals.TriggerPhrase.Split(',').ToList();
            if (GetValidTrigger(gTriggers, msg) is { } match)
            {
                var uid = GetUidFromNameWorld(senderNameWorld);
                var aliases = _puppeteer.GetGlobalAliases().ToList();
                // Create Puppeteer message context.
                var context = new PuppetMsgContext(senderNameWorld, uid, match, aliases, globals.PuppetPerms, null, null);
                // Process the invocation seperately, so we can handle result logic for it without concern
                ProcessPuppetMsg(context, msg);
                return;
            }
        }

        // Try for paired kinkster
        if (_puppeteer.GetPuppeteerUid(senderNameWorld) is { } matchedUID)
        {
            // Ensure still paired (avoid stalking abuse)
            if (_kinksters.TryGetKinkster(new(matchedUID), out var k))
            {
                var pTriggers = k.OwnPerms.TriggerPhrase.Split(',').ToList();
                if (GetValidTrigger(pTriggers, msg) is { } match)
                {
                    var shared = _puppeteer.GetAliasesForPuppeteer(matchedUID).ToList();
                    var context = new PuppetMsgContext(k.GetDisplayName(), matchedUID, match, shared, k.OwnPerms.PuppetPerms, k.OwnPerms.StartChar, k.OwnPerms.EndChar);
                    ProcessPuppetMsg(context, msg);
                }
            }
        }
    }

    /// <summary>
    ///     A SameThreadMessage from the Mediator fired whenever an emote used by anyone occurs. <para />
    ///     (Hopefully reduce heavy load on system or something with optimized call logic ;-;)
    /// </summary>
    private unsafe void OnEmote(uint emoteId, nint callerAddr, nint targetAddr)
    {
        // Caller must be something, (Target can be nothing)
        if (!CharaObjectWatcher.Rendered.Contains(callerAddr))
            return;

        // Filter based on the type.
        var isClientRendered = CharaObjectWatcher.LocalPlayerRendered;
        var clientIsCaller = isClientRendered && callerAddr == PlayerData.Address;
        var clientIsTarget = isClientRendered && targetAddr == PlayerData.Address;

        // Get the health triggers scoped down to this person we are monitoring.
        var emoteTriggers = _triggers.Storage.OfType<EmoteTrigger>()
            .Where(IsValidTrigger)
            .OrderByDescending(t => t.Priority);

        HandleTriggerCandidates(emoteTriggers);

        bool IsValidTrigger(EmoteTrigger trigger)
        {
            if (!trigger.Enabled || trigger.EmoteID != emoteId)
                return false;

            switch (trigger.EmoteDirection)
            {
                case TriggerDirection.Any:
                    return true;

                case TriggerDirection.OtherToSelf:
                    // Ensure valid states.
                    if (!(CharaObjectWatcher.Rendered.Contains(targetAddr) && !clientIsCaller && clientIsTarget))
                        return false;
                    // If the target was defined, ensure it matches.
                    return !string.IsNullOrEmpty(trigger.PlayerNameWorld)
                        ? ((Character*)callerAddr)->GetNameWithWorld() == trigger.PlayerNameWorld
                        : true;

                case TriggerDirection.Other:
                    // Ensure valid states.
                    if (!(CharaObjectWatcher.Rendered.Contains(targetAddr) && !clientIsCaller))
                        return false;
                    // If the target was defined, ensure it matches.
                    return !string.IsNullOrEmpty(trigger.PlayerNameWorld)
                        ? ((Character*)targetAddr)->GetNameWithWorld() == trigger.PlayerNameWorld
                        : true;

                case TriggerDirection.SelfToOther:
                    // Ensure valid states.
                    if (!(CharaObjectWatcher.Rendered.Contains(targetAddr) && clientIsCaller))
                        return false;
                    // If the target was defined, ensure it matches.
                    return !string.IsNullOrEmpty(trigger.PlayerNameWorld)
                        ? ((Character*)targetAddr)->GetNameWithWorld() == trigger.PlayerNameWorld
                        : true;

                case TriggerDirection.Self:
                    return clientIsCaller;

                default:
                    return false;
            }
        }
    }

    private unsafe void OnHpTrigger(nint playerAddr, HealthPercentTrigger trigger)
    {
        // Ensure they are visible and rendered
        if (!CharaObjectWatcher.TryGetValue(playerAddr, out Character* chara))
            return;

        // Get the health triggers scoped down to this person we are monitoring.
        var hpTriggers = _triggers.Storage.OfType<HealthPercentTrigger>()
            .Where(t => t.Enabled && t.PlayerNameWorld == chara->GetNameWithWorld())
            .OrderByDescending(t => t.Priority);

        HandleTriggerCandidates(hpTriggers);
    }

    private void OnGagChange(NewState state, int layer, ActiveGagSlot data, string enactor, string target)
    {
        // For triggers, if the target is not us, we do not care.
        if (!MainHub.IsConnected || MainHub.UID != target)
            return;
        // Filter down to the triggers matching our change type and other details.
        var gagTriggers = _triggers.Storage.OfType<GagTrigger>()
            .Where(t => t.Enabled && t.Gag == data.GagItem && t.GagState == state)
            .OrderByDescending(t => t.Priority);

        HandleTriggerCandidates(gagTriggers, enactor);
    }

    private void OnRestrictionChange(NewState state, int layer, ActiveRestriction data, string enactor, string target)
    {
        // For triggers, if the target is not us, we do not care.
        if (!MainHub.IsConnected || MainHub.UID != target)
            return;

        // Filter down to the triggers matching our change type and other details.
        var bindTriggers = _triggers.Storage.OfType<RestrictionTrigger>()
            .Where(t => t.Enabled && t.RestrictionId == data.Identifier && t.RestrictionState == state)
            .OrderByDescending(t => t.Priority);

        HandleTriggerCandidates(bindTriggers, enactor);
    }

    private void OnRestraintChange(NewState state, CharaActiveRestraint data, string enactor, string target)
    {
        // For triggers, if the target is not us, we do not care.
        if (!MainHub.IsConnected || MainHub.UID != target)
            return;

        // Filter down to the triggers matching our change type and other details.
        var restraintTriggers = _triggers.Storage.OfType<RestraintTrigger>()
            .Where(t => t.Enabled && t.RestraintSetId == data.Identifier && t.RestraintState == state)
            .OrderByDescending(t => t.Priority);

        HandleTriggerCandidates(restraintTriggers, enactor);
    }

    private void OnRestraintLayerChange(CharaActiveRestraint data, RestraintLayer added, RestraintLayer removed, string enactor, string target)
    {
        /* Nothing yet */
    }

    /// <summary>
    ///     Called upon by the ActionEffectDetour
    /// </summary>
    public unsafe void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!PlayerData.Available || !_triggers.Storage.SpellAction.Any())
            return;

        foreach (var actionEffect in actionEffects)
        {
            if ((LoggerFilter.FilteredLogTypes & LoggerType.ActionEffects) != 0)
            {
                // Perform logging and action processing for each effect
                var srcChara = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(actionEffect.SourceID);
                var tgtChara = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(actionEffect.TargetID);
                var srcCharaStr = (srcChara != null && srcChara->IsCharacter()) ? ((Character*)srcChara)->GetNameWithWorld() : "UNKN OBJ";
                var tgtCharaStr = (tgtChara != null && tgtChara->IsCharacter()) ? ((Character*)tgtChara)->GetNameWithWorld() : "UNKN OBJ";

                var actionStr = SpellActionService.AllActionsLookup.TryGetValue(actionEffect.ActionID, out var match) ? match.Name.ToString() : "UNKN ACT";
                Logger.LogTrace($"Source:{srcCharaStr}, Target: {tgtCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                    $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
            }

            CheckSpellActionTriggers(actionEffect);
        }
    }

    /// <summary>
    ///     The result of a /dr between two individuals
    /// </summary>
    private void OnSocialGameEnd(string winnerNameWorld, string loserNameWorld)
    {
        if (!PlayerData.Available)
            return;

        var clientNameWorld = PlayerData.NameWithWorld;
        // If not a participant, ignore.
        if (clientNameWorld != winnerNameWorld && clientNameWorld != loserNameWorld)
            return;

        // Get if we won or lost.
        var isWinner = clientNameWorld == winnerNameWorld;

        // Filter down to the triggers matching our change type and other details.
        var socialTriggers = _triggers.Storage.OfType<SocialTrigger>()
            .Where(t => t.Enabled
                && t.Game is SocialGame.DeathRoll
                && (isWinner ? t.Result == SocialGameResult.Win : t.Result == SocialGameResult.Loss))
            .OrderByDescending(t => t.Priority);

        // Dunno the enactor here, but could maybe see if possible to extract if we need it for achievements down the line or something.
        HandleTriggerCandidates(socialTriggers);
    }

    #endregion

    #region Helpers
    private IEnumerable<InvokableActionType> GetReactionTypes(AliasTrigger alias)
        => alias.Actions.Select(a => a.ActionType);

    private bool HasAsyncAction(IEnumerable<InvokableActionType> types)
        => types.Any(IsActionTypeAsync);

    private bool IsActionTypeAsync(InvokableActionType type) => type switch
    {
        InvokableActionType.TextOutput => false,
        InvokableActionType.Gag => true,
        InvokableActionType.Restriction => true,
        InvokableActionType.Restraint => true,
        InvokableActionType.Moodle => false,
        InvokableActionType.ShockCollar => false,
        InvokableActionType.SexToy => false,
        _ => false
    };

    /// <summary>
    ///     Personalized Achievement detection for our own chat messages.
    /// </summary>
    public void ScanOwnChat(InputChannel channel, SeString msg)
    {
        // There are some achievements involving us ordering others using Puppeteer.
        // Detect these here.
        var toCheck = _kinksters.DirectPairs.Where(p => p.PairPerms.IsMarionette());
        foreach (var marionette in toCheck)
        {
            var possibleTriggers = marionette.PairPerms.TriggerPhrase.Split(',').ToList();
            if (GetValidTrigger(possibleTriggers, msg) is not { } match)
                continue;

            // Trim everything before the trigger
            SeString scoped = msg.TextValue.Substring(msg.TextValue.IndexOf(match) + match.Length).Trim();
            // Scope to brackets if possible.
            scoped = scoped.GetSubstringWithinParentheses(marionette.PairPerms.StartChar, marionette.PairPerms.EndChar);

            // Detect triggers for spesific achievements. (Change these to ID's over names eventually)
            // Also probably run this in the achievement manager over firing a different event type for it every time.
            if (scoped.TextValue.Contains("grovel"))
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GrovelOrder);
            else if (scoped.TextValue.Contains("dance"))
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.DanceOrder);
            else
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderSent, PuppeteerMsgType.GenericOrder);
        }
    }

    /// <summary>
    ///     Attempt to locate the first valid trigger in a chat message.
    /// </summary>
    private string? GetValidTrigger(List<string> triggerPhrases, SeString chatMessage)
    {
        foreach (var triggerWord in triggerPhrases)
        {
            if (triggerWord.IsNullOrWhitespace())
                continue;
            if (!RegexEx.TryMatchTriggerWord(chatMessage.TextValue, triggerWord).Success)
                continue;
            Logger.LogTrace($"Matched trigger {{{triggerWord}}}", LoggerType.Puppeteer);
            return triggerWord;
        }

        return null;
    }

    /// <summary>
    ///     Attempts to get if this person was a kinkster, global or per pair.
    /// </summary>
    private string? GetUidFromNameWorld(string nameWithWorld)
    {
        // First see if they exist as a puppeteer,
        if (_puppeteer.GetPuppeteerUid(nameWithWorld) is { } puppeteerUid)
            return puppeteerUid;
        // Otherwise, try to get from current visible.
        foreach (var k in _kinksters.DirectPairs.Where(k => k.IsRendered && string.Equals(k.PlayerNameWorld, nameWithWorld, StringComparison.OrdinalIgnoreCase)))
            return k.UserData.UID;
        return null;
    }

    /// <summary>
    ///     Handles the candidates from a series of selected triggers to identify which should be executed.
    /// </summary>
    private async void HandleTriggerCandidates<T>(IEnumerable<T> candidates, string? enactor = null) where T : Trigger
    {
        // Iterate through all, ordered by priority.
        foreach (var trigger in candidates.ToList())
        {
            // See if the trigger is async or not
            if (IsActionTypeAsync(trigger.ActionType))
            {
                // If we cant execute, continue.
                if (!_selfBondage.CanExecute(trigger.ActionType))
                    continue;

                // Logger.LogInformation($"Executing async action of type {trigger.ActionType} for trigger {trigger.Label}.", LoggerType.Triggers);
                if (await _processor.HandleActionAsync(trigger.InvokableAction, enactor).ConfigureAwait(false))
                {
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
                    break;
                }
            }
            else
            {
                // Need some kind of fallback here to make sure that we try the next trigger if this one fails?
                if (_processor.HandleAction(trigger.InvokableAction))
                {
                    GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
                    break;
                }
            }
        }
    }


    /// <summary>
    ///     Parses out a valid puppeteer message using its context, then runs the associated handle command.
    /// </summary>
    private async void ProcessPuppetMsg(PuppetMsgContext context, SeString msg)
    {
        Logger.LogTrace($"Trigger ({context.Trigger}) detected in: [ {msg} ]", LoggerType.Puppeteer);
        // Trim everything before the trigger
        SeString scoped = msg.TextValue.Substring(msg.TextValue.IndexOf(context.Trigger) + context.Trigger.Length).Trim();
        scoped = scoped.GetSubstringWithinParentheses(context.StartChar ?? '(', context.EndChar ?? ')');
        Logger.LogTrace($"Scoped message: {scoped}", LoggerType.Puppeteer);

        // if the final scoped message is empty return
        if (string.IsNullOrWhiteSpace(scoped.TextValue))
            return;

        scoped = scoped.ConvertSquareToAngleBrackets();

        Logger.LogTrace($"Context for scoped message {scoped} is UID: {context.UID}, DisplayName: {context.DisplayName}, Trigger: {context.Trigger}, Aliases ({context.Aliases.Count}): {string.Join(", ", context.Aliases.Select(a => a.InputCommand))}, Perms: {context.PuppetPerms}", LoggerType.Puppeteer);

        // Alias execution
        if (context.PuppetPerms.HasAny(PuppetPerms.Alias) && GetValidAlias(context.Aliases, scoped) is { } match)
        {
            // It was a valid alias instruction, so we should run its reaction context.
            Logger.LogDebug($"Puppeteered by {context.DisplayName} with an [ALIAS] message.", LoggerType.Puppeteer);
            // Invoke if true only. (Only fails if the server return call fails to be honest)
            if (await _processor.HandleActionsAsync(match.Actions, context.UID).ConfigureAwait(false))
                _processor.IncrementStats(context, PuppetPerms.Alias);
        }
        else
        {
            // Fallback: sit / emote / ALL
            _processor.HandlePuppeteeredText(context, msg);
        }
    }

    private AliasTrigger? GetValidAlias(IEnumerable<AliasTrigger> candidates, SeString aliasMsg)
    {
        // Order by Aliases based on length. If a valid one is found, only return it if valid and can be executed.
        foreach (var alias in candidates.OrderByDescending(alias => alias.InputCommand.Length).ToList())
        {
            if (!aliasMsg.TextValue.Contains(alias.InputCommand, alias.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                Logger.LogTrace($"Alias not matched due to string contains: {alias.InputCommand}", LoggerType.Puppeteer);
                continue;
            }
            // We found a potential match, but we must make sure it is allowed to be executed.
            if (!_selfBondage.CanExecute(alias.Actions.Select(a => a.ActionType)))
            {
                Logger.LogTrace($"Alias found but cannot be executed: {alias.InputCommand} - actions: {string.Join(", ", alias.Actions.Select(a => a.ActionType.ToName()))}", LoggerType.Puppeteer);
                continue;
            }

            // We have a valid AliasTrigger whose reactions are all available to process.
            Logger.LogTrace($"Alias found: {alias.InputCommand}", LoggerType.Puppeteer);
            return alias;
        }
        return null;
    }

    private async void CheckSpellActionTriggers(ActionEffectEntry actEff)
    {
        Logger.LogTrace($"SourceID ({actEff.SourceID} | TargetID: {actEff.TargetID} | ActionID: {actEff.ActionID} | Type: {actEff.Type} | Damage: {actEff.Damage}", LoggerType.Triggers);

        // please for the love of god find a better way to handle this.
        var relevantTriggers = _triggers.Storage.SpellAction
            .Where(t => t.ActionKind == actEff.Type && (t.GetStoredIds().Contains(actEff.ActionID) || t.IsGenericDetection))
            .ToList();

        if (!relevantTriggers.Any())
        {
            Logger.LogDebug("No relevant triggers found for this spell/action", LoggerType.Triggers);
            return;
        }

        foreach (var trigger in relevantTriggers)
        {
            Logger.LogTrace("Checking Trigger: " + trigger.Label, LoggerType.Triggers);
            if (!IsDirectionMatch(trigger.Direction, PlayerData.GameObjectId, actEff.SourceID, actEff.TargetID))
            {
                Logger.LogDebug("Direction didn't match", LoggerType.Triggers);
                continue;
            }

            Logger.LogTrace("Direction Matches, checking damage type", LoggerType.Triggers);
            var isDamageRelated = trigger.ActionKind is
                LimitedActionEffectType.Heal or
                LimitedActionEffectType.Damage or
                LimitedActionEffectType.BlockedDamage or
                LimitedActionEffectType.ParriedDamage;

            if (isDamageRelated && !IsDamageWithinThreshold(actEff.Damage, trigger.ThresholdMinValue, trigger.ThresholdMaxValue))
            {
                Logger.LogTrace($"Was ActionKind [{actEff.Type}], but its damage ({actEff.Damage}) wasn't " +
                    $"between ({trigger.ThresholdMinValue}) & ({trigger.ThresholdMaxValue})", LoggerType.Triggers);
                continue;
            }

            // Execute trigger action if all conditions are met
            Logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
                + trigger.InvokableAction.ActionType.ToName(), LoggerType.Triggers);

            if (await _processor.HandleActionAsync(trigger.InvokableAction).ConfigureAwait(false))
                GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
        }
        ;
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

    #endregion Helpers
}
