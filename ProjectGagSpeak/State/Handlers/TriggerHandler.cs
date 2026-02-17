using CkCommons;
using CkCommons.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.GameInternals.Structs;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagspeakAPI.Data;

namespace GagSpeak.State.Handlers;

/// <summary>
///     Handles incoming invokations for various triggers to in turn execute them <para />
///     May or may not have future integration weaved into this listener for the vibe server lobby system.
/// </summary>
public class TriggerHandler
{
    private readonly ILogger<TriggerHandler> _logger;
    private readonly MainConfig _config;
    private readonly KinksterManager _kinksters;
    private readonly PuppeteerManager _puppeteer;
    private readonly TriggerManager _triggers;
    private readonly TriggerActionService _triggerService;

    public TriggerHandler(ILogger<TriggerHandler> logger, MainConfig config, 
        KinksterManager kinksters, PuppeteerManager aliases, TriggerManager triggers,
        TriggerActionService service)
    {
        _logger = logger;
        _config = config;
        _kinksters = kinksters;
        _puppeteer = aliases;
        _triggers = triggers;
        _triggerService = service;
    }
    
    public async void CheckChatForTrigger(string name, string world, SeString msg)
    {
        // Fail if we are not connected with a valid globals (REMOVE LATER)
        if (ClientData.Globals is not { } globals)
            return;

        // Fail if puppeteer is not active
        if (!globals.PuppeteerEnabled)
            return;

        // Check global context first.
        if (!string.IsNullOrWhiteSpace(globals.TriggerPhrase))
        {
            var gTriggers = globals.TriggerPhrase.Split(',').ToList();
            if (IsValidTriggerWord(gTriggers, msg, false, out var matched))
            {
                var uid = GetUidFromNameWorld(name, world);
                var aliases = _puppeteer.GetGlobalAliases().ToList();
                var context = new PuppetMsgContext($"{name}@{world}", uid, matched, aliases, globals.PuppetPerms, null, null);
                await InvokeTrigger(context, msg);
                return;
            }
        }
        // Try for paired kinkster
        if (_puppeteer.GetPuppeteerUid(name, world) is { } matchedUID)
        {
            // Ensure still paired (avoid stalking abuse)
            if (_kinksters.TryGetKinkster(new(matchedUID), out var k))
            {
                var ignoreCase = k.OwnPerms.IgnoreTriggerCase;
                var pTriggers = k.OwnPerms.TriggerPhrase.Split(',').ToList();
                if (IsValidTriggerWord(pTriggers, msg, ignoreCase, out var matched))
                {
                    var shared = _puppeteer.GetAliasesForPuppeteer(matchedUID).ToList();
                    var context = new PuppetMsgContext(k.GetDisplayName(), matchedUID, matched, shared, k.OwnPerms.PuppetPerms, k.OwnPerms.StartChar, k.OwnPerms.EndChar);
                    await InvokeTrigger(context, msg);
                }
            }
        }
    }

    /// <summary>
    ///     Sees if the trigger phases for a global or pair that matched from a chat message is valid.
    /// </summary>
    private bool IsValidTriggerWord(List<string> triggerPhrases, SeString chatMessage, bool ignoreCase, out string matchedTrigger)
    {
        matchedTrigger = string.Empty;
        foreach (var triggerWord in triggerPhrases)
        {
            if (triggerWord.IsNullOrWhitespace())
                continue;

            if (!RegexEx.TryMatchTriggerWord(chatMessage.TextValue, triggerWord, ignoreCase).Success)
                continue;

            _logger.LogTrace("Matched trigger word: " + triggerWord, LoggerType.Puppeteer);
            matchedTrigger = triggerWord;
            return true;
        }
        return false;
    }

    /// <summary>
    ///     Attempts to get if this person was a kinkster, global or per pair.
    /// </summary>
    private string? GetUidFromNameWorld(string name, string world)
    {
        // First see if they exist as a puppeteer,
        if (_puppeteer.GetPuppeteerUid(name, world) is { } puppeteerUid)
            return puppeteerUid;
        // Otherwise, try to get from current visible.
        foreach (var k in _kinksters.DirectPairs.Where(k => k.IsRendered && string.Equals(k.PlayerNameWorld, $"{name}@{world}", StringComparison.OrdinalIgnoreCase)))
            return k.UserData.UID;
        return null;
    }

    private async Task InvokeTrigger(PuppetMsgContext context, SeString msg)
    {
        _logger.LogTrace($"Trigger ({context.Trigger}) detected in: [ {msg} ]", LoggerType.Puppeteer);
        // Trim everything before the trigger
        SeString scoped = msg.TextValue.Substring(msg.TextValue.IndexOf(context.Trigger) + context.Trigger.Length).Trim();
        
        // Scope to brackets if possible.
        scoped = scoped.GetSubstringWithinParentheses(context.StartChar ?? '(', context.EndChar ?? ')');
        _logger.LogTrace($"Scoped message: {scoped}", LoggerType.Puppeteer);

        // if the final scoped message is empty return
        if (string.IsNullOrWhiteSpace(scoped.TextValue))
            return;
        
        scoped = scoped.ConvertSquareToAngleBrackets();

        // Alias execution
        if (context.PuppetPerms.HasAny(PuppetPerms.Alias))
        {
            var found = await HandleAliases(scoped, context.Aliases, ActionSource.PairAlias, context.UID).ConfigureAwait(false);
            if (found > 0)
            {
                _logger.LogDebug($"Puppeteered by {context.DisplayName} with an [ALIAS] message. (Handled {found} Aliases)", LoggerType.Puppeteer);
                IncrementStats(context, PuppetPerms.Alias, found);
                return;
            }
        }
        // Fallback: sit / emote / ALL
        InvokeByCriteria(context, scoped);
    }

    private void IncrementStats(PuppetMsgContext context, PuppetPerms stat, int count, uint emoteRow = uint.MaxValue)
    {
        // Trigger regardless
        GagspeakEventManager.AchievementEvent(UnlocksEvent.OrderRecieved, context.UID ?? string.Empty, stat, count, emoteRow);

        // No need to do stats if the UID is null or the data is not found
        if (context.UID is null)
            return;
        if (!_puppeteer.Puppeteers.TryGetValue(context.UID, out var data))
            return;

        data.OrdersRecieved += count;
        switch (stat)
        {
            case PuppetPerms.Alias:     data.AliasOrders += count;  break;
            case PuppetPerms.Emotes:    data.EmoteOrders += count;  break;
            case PuppetPerms.Sit:       data.SitOrders += count;    break;
            case PuppetPerms.All:       data.OtherOrders += count;  break;
        }
        // Save after incrementing
        _puppeteer.Save();
    }

    /// <summary> 
    ///     Converts the input commands to the output commands from the alias list if any. <para />
    ///     Only valid aliases should be passed in, or else this will cause errors.
    /// </summary>
    private async Task<int> HandleAliases(SeString aliasMsg, IEnumerable<AliasTrigger> items, ActionSource source, string? enactor)
    {
        _logger.LogTrace($"Found {items.Count()} aliases for to check", LoggerType.Puppeteer);
        // sort by descending length so that shorter equivalents to not override longer variants.
        var sortedAliases = items.OrderByDescending(alias => alias.InputCommand.Length);
        // see if our message contains any of the alias strings. For it to match, it must match the full alias string.
        foreach (var alias in items)
        {
            if (!aliasMsg.TextValue.Contains(alias.InputCommand))
                continue;

            _logger.LogTrace($"Alias found: {alias.InputCommand}", LoggerType.Puppeteer);
            // fire and forget to not hang the chat listener on limbo.
            if(await _triggerService.HandleMultiActionAsync(alias.Actions, source, enactor))
            {
                return 1; // for now do this, maybe allow multiple executions later though.
            }
        }
        // Found nothing
        return 0;
    }

    /// <summary>
    ///     Determines if the message meets the criteria for the sender.
    /// </summary>
    public void InvokeByCriteria(PuppetMsgContext context, SeString message)
    {
        // ALL permission (non-emote)
        if (context.PuppetPerms.HasAny(PuppetPerms.All) && !IsEmoteMatch(message, out var _))
        {
            _logger.LogDebug($"Puppeteered by {context.DisplayName} with an [ALL] message.", LoggerType.Puppeteer);
            ChatService.EnqueueMessage($"/{message.TextValue}");
            IncrementStats(context, PuppetPerms.All, 1);
            return;
        }

        // EMOTES
        if (context.PuppetPerms.HasAny(PuppetPerms.Emotes) && IsEmoteMatch(message, out var emoteRow))
        {
            _logger.LogDebug($"Puppeteered by {context.DisplayName} with an [EMOTE] message.", LoggerType.Puppeteer);
            ChatService.EnqueueMessage($"/{message.TextValue}");
            IncrementStats(context, PuppetPerms.Emotes, 1, emoteRow);
            return;
        }

        // SIT / CPOSE
        if (context.PuppetPerms.HasAny(PuppetPerms.Sit))
        {
            // Match SIT emotes (50, 52)
            var sitEmote = EmoteEx.SittingEmotes().FirstOrDefault(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                _logger.LogDebug($"Puppeteered by {context.DisplayName} with an [SIT] message.", LoggerType.Puppeteer);
                ChatService.EnqueueMessage($"/{message.TextValue}");
                IncrementStats(context, PuppetPerms.Sit, 1, sitEmote.RowId);
                return;
            }
            // Match CPOSE emotes (90)
            if (EmoteService.ValidLightEmoteCache.Where(e => e.RowId == 90).Any(e => message.TextValue.Contains(e.Name.Replace(" ", "").ToLower())))
            {
                _logger.LogDebug($"Puppeteered by {context.DisplayName} with a [CPOSE] message.", LoggerType.Puppeteer);
                ChatService.EnqueueMessage($"/{message.TextValue}");
                IncrementStats(context, PuppetPerms.Sit, 1, 90);
            }
        }

        // --- Local helper to check for emote match ---
        bool IsEmoteMatch(SeString msg, out uint rowId)
        {
            var emote = EmoteService.ValidLightEmoteCache.FirstOrDefault(e => e.EmoteCommands.Any(c => string.Equals(msg.TextValue, c.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)));
            rowId = !string.IsNullOrWhiteSpace(emote.Name) ? emote.RowId : uint.MaxValue;
            return rowId != uint.MaxValue;
        }
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
                _logger.LogTrace($"Source:{srcCharaStr}, Target: {tgtCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                    $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
            }

            CheckSpellActionTriggers(actionEffect).ConfigureAwait(false);
        }
    }

    private async Task CheckSpellActionTriggers(ActionEffectEntry actEff)
    {
        _logger.LogTrace($"SourceID ({actEff.SourceID} | TargetID: {actEff.TargetID} | ActionID: {actEff.ActionID} | Type: {actEff.Type} | Damage: {actEff.Damage}", LoggerType.Triggers);

        // please for the love of god find a better way to handle this.
        var relevantTriggers = _triggers.Storage.SpellAction
            .Where(t => t.ActionKind == actEff.Type && t.GetStoredIds().Contains(actEff.ActionID) && t.ActionKind == actEff.Type )
            .ToList();

        if (!relevantTriggers.Any())
        {
            _logger.LogDebug("No relevant triggers found for this spell/action", LoggerType.Triggers);
            return;
        }

        foreach (var trigger in relevantTriggers)
        {
            _logger.LogTrace("Checking Trigger: " + trigger.Label, LoggerType.Triggers);
            if (!IsDirectionMatch(trigger.Direction, PlayerData.GameObjectId, actEff.SourceID, actEff.TargetID))
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

            if (isDamageRelated && !IsDamageWithinThreshold(actEff.Damage, trigger.ThresholdMinValue, trigger.ThresholdMaxValue))
            {
                _logger.LogTrace($"Was ActionKind [{actEff.Type}], but its damage ({actEff.Damage}) wasn't " +
                    $"between ({trigger.ThresholdMinValue}) & ({trigger.ThresholdMaxValue})", LoggerType.Triggers);
                continue;
            }

            // Execute trigger action if all conditions are met
            _logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
                + trigger.InvokableAction.ActionType.ToName(), LoggerType.Triggers);

            if (await _triggerService.HandleActionAsync(trigger.InvokableAction, ActionSource.TriggerAction))
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
