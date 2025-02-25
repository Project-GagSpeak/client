using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerState.Listener;

/// <summary> Listens for incoming changes to Alarms, Patterns, Triggers, and WIP, Vibe Server Lobby System. </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public sealed class ToyboxStateListener : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly PairManager           _pairs;

    // Managers:
    // - SexToyManager (Manages what kind of toy is connected (actual vs simulated), and handles various reactions.
    // MAYBE CONSIDER ADDING:
    // - VibeControlLobbyManager (Future WIP Section)
    // - SpatialAudioManager (Huge WIP, highly dependant on if SCD's data can be properly parsed out or whatever.
    //      I've tried to get it working for so long ive lost hope.)
    private readonly PatternManager _patternManager;
    private readonly AlarmManager   _alarmManager;
    private readonly TriggerManager _triggerManager;
    // private readonly VibeControlLobbyManager _vibeLobbyManager;
    private readonly SexToyManager _toyManager;
    // private readonly ShockCollarManager = _shockCollarManager;
    // private readonly SpatialAudioManager _spatialAudioManager;

    // If any of the managers need a seperate applier, append them here.
    private readonly TriggerApplier _triggerApplier;
    private readonly ClientMonitor  _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;
    public ToyboxStateListener(
        ILogger<ToyboxStateListener> logger,
        GagspeakMediator mediator,
        GagspeakConfigService mainConfig,
        PairManager pairs,
        PatternManager patternManager,
        AlarmManager alarmManager,
        TriggerManager triggerManager,
        SexToyManager toyManager,
/*        VibeControlLobbyManager vibeLobbyManager, */
        TriggerApplier triggerApplier,
        ClientMonitor clientMonitor,
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _pairs = pairs;
        _patternManager = patternManager;
        _alarmManager = alarmManager;
        _triggerManager = triggerManager;
        _triggerApplier = triggerApplier;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;

        ActionEffectMonitor.ActionEffectEntryEvent += OnActionEffectEvent;
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateTriggerMonitors());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => UpdateTrackedPlayerHealth());
    }

    protected override void Dispose(bool disposing)
    {
        ActionEffectMonitor.ActionEffectEntryEvent -= OnActionEffectEvent;
        base.Dispose(disposing);
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            Mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }

    public void PatternSwitched(Guid newPattern, string enactor)
    {
        _patternManager.SwitchPattern(newPattern, enactor);
        PostActionMsg(enactor, InteractionType.SwitchPattern, "Pattern Switched");

    }

    public void PatternStarted(Guid patternId, string enactor)
    {
        _patternManager.EnablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StartPattern, "Pattern Enabled");
    }

    public void PatternStopped(Guid patternId, string enactor)
    {
        _patternManager.DisablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StopPattern, "Pattern Disabled");
    }

    public void AlarmToggled(Guid alarmId, string enactor)
    {
        _alarmManager.ToggleAlarm(alarmId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleAlarm, "Alarm Toggled");
    }

    public void TriggerToggled(Guid triggerId, string enactor)
    {
        _triggerManager.ToggleTrigger(triggerId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleTrigger, "Trigger Toggled");
    }

    private void CheckSpellActionTriggers(ActionEffectEntry actionEffect)
    {
        Logger.LogTrace("SourceID: " + actionEffect.SourceID + " TargetID: " + actionEffect.TargetID + " ActionID: " + actionEffect.ActionID + " Type: " + actionEffect.Type + " Damage: " + actionEffect.Damage, LoggerType.ToyboxTriggers);

        var relevantTriggers = _triggerManager.Storage.SpellAction
            .Where(trigger =>
                (trigger.ActionID == uint.MaxValue || trigger.ActionID == actionEffect.ActionID) &&
                trigger.ActionKind == actionEffect.Type)
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

    public async void ExecuteTriggerAction(Trigger trigger)
    {
        Logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
            + trigger.InvokableAction.ExecutionType.ToName(), LoggerType.ToyboxTriggers);

        if (await _triggerApplier.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
            UnlocksEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
    }


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
            .GroupBy(trigger => trigger.PlayerToMonitor)
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
                        ? (previousPercentageHP > trigger.MinHealthValue && percentageHP <= trigger.MinHealthValue) ||
                            (previousPercentageHP > trigger.MaxHealthValue && percentageHP <= trigger.MaxHealthValue)
                        : (player.Value.LastHp > trigger.MinHealthValue && player.Key.CurrentHp <= trigger.MinHealthValue) ||
                            (player.Value.LastHp > trigger.MaxHealthValue && player.Key.CurrentHp <= trigger.MaxHealthValue);
                }
                else if (trigger.PassKind == ThresholdPassType.Over)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP < trigger.MinHealthValue && percentageHP >= trigger.MinHealthValue) ||
                            (previousPercentageHP < trigger.MaxHealthValue && percentageHP >= trigger.MaxHealthValue)
                        : (player.Value.LastHp < trigger.MinHealthValue && player.Key.CurrentHp >= trigger.MinHealthValue) ||
                            (player.Value.LastHp < trigger.MaxHealthValue && player.Key.CurrentHp >= trigger.MaxHealthValue);
                }

                if (isValid)
                    ExecuteTriggerAction(trigger);
            }
        }
    }


    public async void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!_clientMonitor.IsPresent || !_triggerManager.Storage.SpellAction.Any())
            return;

        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            foreach (var actionEffect in actionEffects)
            {
                if (LoggerFilter.FilteredCategories.Contains(LoggerType.ActionEffects))
                {
                    // Perform logging and action processing for each effect
                    var sourceCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.SourceID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";
                    var targetCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.TargetID) as IPlayerCharacter)?.NameWithWorld() ?? "UNKN OBJ";
                    var actionStr = "UNKN ACT";
                    if (_clientMonitor.TryGetAction(actionEffect.ActionID, out var action)) actionStr = action.Name.ToString();
                    Logger.LogTrace($"Source:{sourceCharaStr}, Target: {targetCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                        $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
                }
                CheckSpellActionTriggers(actionEffect);
            };
        });
    }

}
