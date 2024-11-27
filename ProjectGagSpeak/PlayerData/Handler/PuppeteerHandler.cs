using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using System.Text.RegularExpressions;

namespace GagSpeak.PlayerData.Handlers;

public class PuppeteerHandler
{
    private readonly ILogger<PuppeteerHandler> _logger;
    private readonly ClientData _playerChara;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    public PuppeteerHandler(ILogger<PuppeteerHandler> logger, ClientData playerChara,
        ClientConfigurationManager clientConfiguration, PairManager pairManager)
    {
        _logger = logger;
        _clientConfigs = clientConfiguration;
        _playerChara = playerChara;
        _pairManager = pairManager;
    }

    public Pair? SelectedPair = null; // Selected Pair we are viewing for Puppeteer.

    // Store an accessor of the alarm being edited.
    public AliasStorage? ClonedAliasStorageForEdit { get; private set; } = null;
    public string UidOfStorage => SelectedPair?.UserData.UID ?? string.Empty;
    public bool IsModified { get; private set; } = false;

    public void StartEditingList(AliasStorage aliasStorage)
        => aliasStorage.CloneAliasList();

    public void CancelEditingSet()
        => ClonedAliasStorageForEdit = null;

    public void UpdatedEditedStorage()
    {
        if (ClonedAliasStorageForEdit is null)
            return;

        if (!IsModified)
        {
            _logger.LogTrace("No changes were made to the Alias Storage.", LoggerType.Puppeteer);
            return;
        }

        IsModified = false;
        _clientConfigs.UpdateAliasStorage(UidOfStorage, ClonedAliasStorageForEdit!);
    }

    public void MarkAsModified() => IsModified = true;

    #region PuppeteerSettings
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
    }


    public void UpdateDisplayForNewPair(Pair pair)
    {
        // for first time generations
        if (SelectedPair is null)
        {
            _logger.LogTrace($"Setting selected pair to " + pair.UserData.AliasOrUID, LoggerType.Puppeteer);
            SelectedPair = pair;
            //StartEditingSet(_clientConfigs.FetchAliasStorageForPair(pair.UserData.UID));
        }

        // for refreshing data once we switch pairs.
        if (SelectedPair.UserData.UID != pair.UserData.UID)
        {
            _logger.LogTrace($"Updating display to reflect pair " + pair.UserData.AliasOrUID, LoggerType.Puppeteer);
            SelectedPair = pair;
            //StartEditingSet(_clientConfigs.FetchAliasStorageForPair(pair.UserData.UID));
        }
        // log if the storage being edited is null.
        if (ClonedAliasStorageForEdit is null)
        {
            _logger.LogWarning($"Storage being edited is null for pair " + pair.UserData.AliasOrUID, LoggerType.Puppeteer);
        }
    }

    public AliasStorage GetAliasStorage(string pairUID)
        => _clientConfigs.FetchAliasStorageForPair(pairUID);

    public string? GetUIDMatchingSender(string nameWithWorld)
        => _clientConfigs.GetUidMatchingSender(nameWithWorld);

    public void AddAlias(AliasTrigger alias)
        => _clientConfigs.AddNewAliasTrigger(UidOfStorage, alias);
    public void RemoveAlias(AliasTrigger alias)
        => _clientConfigs.RemoveAliasTrigger(UidOfStorage, alias);

    #endregion PuppeteerSettings

    public List<string> GetPlayersToListenFor()
        => _clientConfigs.GetPlayersToListenFor();

    public Pair? GetPairOfUid(string uid)
        => _pairManager.DirectPairs.FirstOrDefault(p => p.UserData.UID == uid);

    public bool IsValidTriggerWord(List<string> triggerPhrases, SeString chatMessage, out string matchedTrigger)
    {
        matchedTrigger = string.Empty;
        foreach (string triggerWord in triggerPhrases)
        {
            if (string.IsNullOrWhiteSpace(triggerWord)) continue;

            var match = TryMatchTriggerWord(chatMessage.TextValue, triggerWord);
            if (!match.Success) continue;

            _logger.LogTrace("Matched trigger word: " + triggerWord, LoggerType.Puppeteer);
            matchedTrigger = triggerWord;
            return true;
        }
        return false;
    }

    public SeString GetMessageFromTrigger(string trigger, PuppeteerPerms perms, SeString chatMessage, XivChatType type, string? SenderUid = null)
    {
        _logger.LogTrace("Checking for trigger: " + trigger, LoggerType.Puppeteer);
        _logger.LogTrace("Message we are checking for the trigger in: " + chatMessage, LoggerType.Puppeteer);
        // obtain the substring that occurs in the message after the trigger.
        SeString remainingMessage = chatMessage.TextValue.Substring(chatMessage.TextValue.IndexOf(trigger) + trigger.Length).Trim();
        _logger.LogTrace("Remaining message: " + remainingMessage, LoggerType.Puppeteer);

        // obtain the substring within the start and end char if provided.
        remainingMessage.GetSubstringWithinParentheses(perms.StartChar, perms.EndChar);
        _logger.LogTrace("Remaining message after brackets: " + remainingMessage);

        if (!SenderUid.IsNullOrEmpty())
        {
            _logger.LogTrace("Checking for Aliases");
            remainingMessage = ConvertAliasCommandsIfAny(SenderUid, remainingMessage.TextValue);
        }

        _logger.LogTrace("Remaining message after aliases: " + remainingMessage, LoggerType.Puppeteer);

        // if the substring in the custom brackets is not null or empty, then convert brackets to angled and return.
        if (remainingMessage.TextValue.IsNullOrEmpty())
        {
            _logger.LogTrace("Message is empty after alias conversion.", LoggerType.Puppeteer);
            return new SeString();
        }

        remainingMessage.ConvertSquareToAngleBrackets();

        // only apply it if the message meets the criteria for the sender.
        if (MeetsSettingCriteria(perms, remainingMessage))
        {
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerOrderRecieved);
            return remainingMessage;
        }

        // return an empty SeString if we failed.
        _logger.LogDebug("Message did not meet the criteria for the sender", LoggerType.Puppeteer);
        return new SeString();
    }

    public bool MeetsSettingCriteria(PuppeteerPerms perms, SeString message)
    {
        if (perms.AllowAllRequests)
        {
            _logger.LogTrace("Accepting Message as you allow All Commands", LoggerType.Puppeteer);
            return true;
        }

        if (perms.AllowMotionRequests)
        {
            var emote = EmoteMonitor.EmoteCommandsWithId
                .FirstOrDefault(e => string.Equals(message.TextValue, e.Key.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(emote.Key))
            {
                _logger.LogTrace("Valid Emote name: " + emote.Key.Replace(" ", "").ToLower() + ", RowID: " + emote.Value, LoggerType.Puppeteer);
                _logger.LogTrace("Accepting Message as you allow Motion Commands", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)emote.Value);
                return true;
            }
        }

        // 50 == Sit, 52 == Sit (Ground), 90 == Change Pose
        if (perms.AllowSitRequests)
        {
            _logger.LogTrace("Checking if message is a sit command", LoggerType.Puppeteer);
            var sitEmote = EmoteMonitor.SitEmoteComboList.FirstOrDefault(e => message.TextValue.Contains(e.Name.ToString().Replace(" ", "").ToLower()));
            if (sitEmote.RowId is 50 or 52)
            {
                _logger.LogTrace("Message is a sit command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)sitEmote.RowId);
                return true;
            }
            if (EmoteMonitor.EmoteCommandsWithId.Where(e => e.Value is 90).Any(e => message.TextValue.Contains(e.Key.Replace(" ", "").ToLower())))
            {
                _logger.LogTrace("Message is a change pose command", LoggerType.Puppeteer);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PuppeteerEmoteRecieved, (ushort)90);
                return true;
            }
        }

        // Failure
        return false;
    }

    public SeString ConvertAliasCommandsIfAny(string SenderUid, SeString messageWithAlias)
    {
        // now we can use this index to scan our aliasLists
        List<AliasTrigger> Triggers = GetAliasStorage(SenderUid).AliasList;

        _logger.LogTrace("Found " + Triggers.Count + " alias triggers for this user", LoggerType.Puppeteer);

        // sort by descending length so that shorter equivalents to not override longer variants.
        var sortedAliases = Triggers.OrderByDescending(alias => alias.InputCommand.Length);
        // see if our message contains any of the alias strings. For it to match, it must match the full alias string.
        foreach (AliasTrigger alias in Triggers)
        {
            if (!alias.Enabled)
                continue;

            if (string.IsNullOrWhiteSpace(alias.InputCommand) || string.IsNullOrWhiteSpace(alias.OutputCommand))
                continue;

            // replace any alias with its corresponding output command.
            messageWithAlias = messageWithAlias.TextValue.Replace(alias.InputCommand, alias.OutputCommand);
        }
        return messageWithAlias;
    }

    private Match TryMatchTriggerWord(string message, string triggerWord)
    {
        var triggerRegex = $@"(?<=^|\s){triggerWord}(?=[^a-z])";
        return Regex.Match(message, triggerRegex);
    }

    public readonly struct PuppeteerPerms
    {
        public bool AllowSitRequests { get; init; }
        public bool AllowMotionRequests { get; init; }
        public bool AllowAllRequests { get; init; }
        public char StartChar { get; init; }
        public char EndChar { get; init; }
        public PuppeteerPerms(bool sit, bool motion, bool all, char sChar = '(', char eChar = ')')
        {
            AllowSitRequests = sit;
            AllowMotionRequests = motion;
            AllowAllRequests = all;
            StartChar = sChar;
            EndChar = eChar;
        }
    }
}
