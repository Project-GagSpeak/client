using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Chat;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Interfaces;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace GagSpeak.PlayerData.Handlers;

public class PuppeteerHandler : DisposableMediatorSubscriberBase
{
    private readonly ActionExecutor _actionExecuter;
    private readonly ClientData _playerChara;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;

    // Handle excessive alias trigger firing.
    private const int _aliasTriggerThresholdPerCycle = 10;
    private int _aliasTriggerCount = 0;
    private DateTime _blockUntil = DateTime.MinValue;
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public PuppeteerHandler(ILogger<PuppeteerHandler> logger, GagspeakMediator mediator,
        ActionExecutor actionExecuter, ClientData playerChara, ClientConfigurationManager clientConfiguration, 
        PairManager pairManager) : base(logger, mediator)
    {
        _actionExecuter = actionExecuter;
        _clientConfigs = clientConfiguration;
        _playerChara = playerChara;
        _pairManager = pairManager;

        Mediator.Subscribe<UserPairSelected>(this, (newPair) =>
        {
            SelectedPair = newPair.Pair;
            CancelEditingList();
        });

        MonitorExcessiveTriggersTask(_cancellationTokenSource.Token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _cancellationTokenSource.Cancel();
        _cancellationTokenSource.Dispose();
    }

    public Pair? SelectedPair = null; // Selected Pair we are viewing for Puppeteer.

    // Store an accessor of the alarm being edited.
    public bool IsEditingList => ClonedAliasListForEdit is not null;
    public List<AliasTrigger>? ClonedAliasListForEdit { get; private set; } = null;
    public string UidOfStorage => SelectedPair?.UserData.UID ?? string.Empty;
    public bool MadeAliasChangeSinceLastEdit { get; set; } = false;
    public void StartEditingList(AliasStorage aliasStorage) => ClonedAliasListForEdit = aliasStorage.CloneAliasList();
    public void CancelEditingList()
    {
        ClonedAliasListForEdit = null;
        MadeAliasChangeSinceLastEdit = false;
    }
    public void SaveModifiedList()
    {
        if (ClonedAliasListForEdit is null || MadeAliasChangeSinceLastEdit is false)
        {
            ClonedAliasListForEdit = null;
            MadeAliasChangeSinceLastEdit = false;
            return;
        }

        _clientConfigs.UpdateAliasList(UidOfStorage, ClonedAliasListForEdit);
        // clear editing data.
        ClonedAliasListForEdit = null;
        MadeAliasChangeSinceLastEdit = false;
    }

    #region PuppeteerSettings
    public string? GetUIDMatchingSender(string nameWithWorld) => _clientConfigs.GetUidMatchingSender(nameWithWorld);
    public string ListenerNameForPair()
    {
        if (_clientConfigs.AliasConfig.AliasStorage.TryGetValue(UidOfStorage, out var aliasStorage))
            return aliasStorage.HasNameStored ? aliasStorage.CharacterNameWithWorld : "Not Yet Listening!";
        return "Not Yet Listening!";
    }
    public List<string> GetPlayersToListenFor() => _clientConfigs.GetPlayersToListenFor();
    public Pair? GetPairOfUid(string uid) => _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == uid);

    public void OnClientMessageContainsPairTrigger(string msg)
    {
        foreach (var pair in _pairManager.DirectPairs)
        {
            string[] triggers = pair.PairPerms.TriggerPhrase.Split("|").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            string? foundTrigger = triggers.FirstOrDefault(trigger => msg.Contains(trigger));

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
    }

    #endregion PuppeteerSettings

    public bool IsValidTriggerWord(List<string> triggerPhrases, SeString chatMessage, out string matchedTrigger)
    {
        // reject if on block timer.
        if (DateTime.UtcNow < _blockUntil)
        {
            Logger.LogTrace("Alias trigger inputs are currently blocked.", LoggerType.Puppeteer);
            matchedTrigger = string.Empty;
            return false;
        }

        matchedTrigger = string.Empty;
        foreach (string triggerWord in triggerPhrases)
        {
            if (string.IsNullOrWhiteSpace(triggerWord)) continue;

            var match = TryMatchTriggerWord(chatMessage.TextValue, triggerWord);
            if (!match.Success) continue;

            Logger.LogTrace("Matched trigger word: " + triggerWord, LoggerType.Puppeteer);
            matchedTrigger = triggerWord;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Parses and executes from a global trigger phrase.
    /// </summary>
    /// <returns>True if it was fired, false if not.</returns>
    public bool ParseOutputFromGlobalAndExecute(string trigger, SeString chatMessage, XivChatType type, bool sits, bool motions, bool alias, bool all)
    {
        Logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        Logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);
        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();
        Logger.LogTrace("Remaining message: " + remainingMessage, LoggerType.Puppeteer);

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses();
        Logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (alias) {
            // Alias's will dispatch multiple commands anyways, so we can return early
            Logger.LogTrace("Alias Permissions found, checking for Aliases");
            var wasAnAlias = ConvertAliasCommandsIfAny(remainingMessage, _clientConfigs.AliasConfig.GlobalAliasList, MainHub.UID);
            if (wasAnAlias)
                return true;
        }

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
        {
            Logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return false;
        }

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();
        if (alias) {
            // Alias's will dispatch multiple commands anyways, so we can return early
            Logger.LogTrace("Alias Permissions found, checking for Aliases");
            var wasAnAlias = ConvertAliasCommandsIfAny(remainingMessage, _clientConfigs.AliasConfig.GlobalAliasList, MainHub.UID);
            if (wasAnAlias)
                return true;
        }
        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(sits, motions, all, remainingMessage))
        {
            Logger.LogInformation("Your Global Trigger phrase was used to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatBoxMessage.EnqueueMessage("/" + remainingMessage.TextValue);
            return true;
        }
        return false;
    }

    public bool ParseOutputAndExecute(string trigger, SeString chatMessage, XivChatType type, Pair senderPair)
    {
        senderPair.OwnPerms.PuppetPerms(out bool sits, out bool motions, out bool aliases, out bool all, out char startChar, out char endChar);
        var SenderUid = senderPair.UserData.UID;
        Logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        Logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);
        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();
        Logger.LogTrace("Remaining message: " + remainingMessage, LoggerType.Puppeteer);

        // obtain the substring within the start and end char if provided.
        remainingMessage = remainingMessage.GetSubstringWithinParentheses(startChar, endChar);
        Logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (aliases) {
            // if any are found here, it will perform a call to the action executer, so we can return.
            Logger.LogTrace("Alias Permissions found, checking for Aliases");
            var wasAnAlias = ConvertAliasCommandsIfAny(remainingMessage, _clientConfigs.AliasConfig.AliasStorage[SenderUid].AliasList, SenderUid);
            if (wasAnAlias)
                return true;
        }

        // otherwise, proceed to parse normal message.
        if (remainingMessage.TextValue.IsNullOrEmpty())
        {
            Logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return false;
        }

        // apply bracket conversions.
        remainingMessage = remainingMessage.ConvertSquareToAngleBrackets();

        if (aliases) {
            Logger.LogTrace("Alias Permissions found, checking for Aliases");
            var wasAnAlias = ConvertAliasCommandsIfAny(remainingMessage, _clientConfigs.AliasConfig.AliasStorage[SenderUid].AliasList, SenderUid);
            if (wasAnAlias)
                return true;
        }

        // verify permissions are satisfied.
        if (MeetsSettingCriteria(sits, motions, all, remainingMessage))
        {
            Logger.LogInformation("[" + SenderUid + "] used your trigger phase to make you execute a message!", LoggerType.Puppeteer);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            ChatBoxMessage.EnqueueMessage("/" + remainingMessage.TextValue);
            return true;
        }

        return false;
    }

    public bool MeetsSettingCriteria(bool canSit, bool canEmote, bool canAll, SeString message)
    {
        if (canAll)
        {
            Logger.LogTrace("Accepting Message as you allow All Commands", LoggerType.Puppeteer);
            var emote = EmoteMonitor.EmoteCommandsWithId.FirstOrDefault(e => string.Equals(message.TextValue, e.Key.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(emote.Key))
            {
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)emote.Value);
            }
            return true;
        }

        if (canEmote)
        {
            var emote = EmoteMonitor.EmoteCommandsWithId
                .FirstOrDefault(e => string.Equals(message.TextValue, e.Key.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(emote.Key))
            {
                Logger.LogTrace("Valid Emote name: " + emote.Key.Replace(" ", "").ToLower() + ", RowID: " + emote.Value, LoggerType.Puppeteer);
                Logger.LogTrace("Accepting Message as you allow Motion Commands", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)emote.Value);
                return true;
            }
        }

        // 50 == Sit, 52 == Sit (Ground), 90 == Change Pose
        if (canSit)
        {
            Logger.LogTrace("Checking if message is a sit command", LoggerType.Puppeteer);
            var sitEmote = EmoteMonitor.SitEmoteComboList.FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                Logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)sitEmote.RowId);
                return true;
            }
            if (EmoteMonitor.EmoteCommandsWithId.Where(e => e.Value is 90).Any(e => message.TextValue.Contains(e.Key.Replace(" ", "").ToLower())))
            {
                Logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)90);
                return true;
            }
        }

        // Failure
        return false;
    }

    /// <summary>
    /// Converts the input commands to the output commands from the alias list if any.
    /// Will also determine what kind of message to prepare for execution
    /// </summary>
    public bool ConvertAliasCommandsIfAny(SeString messageWithAlias, List<AliasTrigger> AliasItems, string SenderUid)
    {
        bool wasAnAlias = false;
        Logger.LogTrace("Found " + AliasItems.Count + " alias triggers for this user", LoggerType.Puppeteer);

        // sort by descending length so that shorter equivalents to not override longer variants.
        var sortedAliases = AliasItems.OrderByDescending(alias => alias.InputCommand.Length);
        // see if our message contains any of the alias strings. For it to match, it must match the full alias string.
        foreach (AliasTrigger alias in AliasItems)
        {
            // if the alias is not enabled, skip!
            if (!alias.Enabled) 
                continue;

            // if the alias input does not exist, Skip!
            if (string.IsNullOrWhiteSpace(alias.InputCommand))
                continue;

            // if the alias input doesn't exist in the message inside the brackets, skip!
            if (!messageWithAlias.TextValue.Contains(alias.InputCommand))
                continue;

            // Increment the alias trigger count
            _aliasTriggerCount++;
            wasAnAlias = true;
            // Check if the threshold is exceeded
            if (_aliasTriggerCount > _aliasTriggerThresholdPerCycle)
            {
                _blockUntil = DateTime.UtcNow.AddSeconds(15); // Block for 15 seconds
                Logger.LogDebug("Alias trigger threshold exceeded. Blocking inputs for 15 seconds.");
                continue;
            }

            // the alias exists in this message, so go ahead and fore the multi-action execute!
            Logger.LogTrace("Alias found: " + alias.InputCommand, LoggerType.Puppeteer);
            _ = _actionExecuter.ExecuteMultiActionAsync(alias.Executions.Values.ToList(), SenderUid);
        }
        return wasAnAlias;
    }

    private Match TryMatchTriggerWord(string message, string triggerWord)
    {
        var triggerRegex = $@"(?<=^|\s){triggerWord}(?=[^a-z])";
        return Regex.Match(message, triggerRegex);
    }

    /// <summary>
    /// Monitors the abuse of excessive triggers throughout plugin lifetime, 
    /// setting temporary timeouts if exploited.
    /// </summary>
    private async void MonitorExcessiveTriggersTask(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(5000, cancellationToken); // Wait for 5 seconds
                _aliasTriggerCount = 0;
            }
        }
        catch (TaskCanceledException)
        {
            Logger.LogDebug("Alias trigger monitoring task has been cancelled.");
        }
    }

}
