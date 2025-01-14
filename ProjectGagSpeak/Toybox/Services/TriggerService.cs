using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using GagSpeak.ChatMessages;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Extensions;
using GameAction = Lumina.Excel.Sheets.Action;

namespace GagSpeak.Toybox.Services;

// handles the management of the connected devices or simulated vibrator.
public class TriggerService : DisposableMediatorSubscriberBase
{
    private readonly ActionExecutor _actionExecuter;
    private readonly ToyboxFactory _playerMonitorFactory;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly UnlocksEventManager _eventManager;
    private readonly ClientMonitorService _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly VibratorService _vibeService;

    public TriggerService(ILogger<TriggerService> logger, GagspeakMediator mediator,
        ActionExecutor actionExecuter, ToyboxFactory playerMonitorFactory,
        ClientConfigurationManager clientConfigs, UnlocksEventManager eventManager,
        ClientMonitorService clientMonitor, OnFrameworkService frameworkUtils,
        VibratorService vibeService) : base(logger, mediator)
    {
        _actionExecuter = actionExecuter;
        _playerMonitorFactory = playerMonitorFactory;
        _clientConfigs = clientConfigs;
        _eventManager = eventManager;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;
        _vibeService = vibeService;

        ActionEffectMonitor.ActionEffectEntryEvent += OnActionEffectEvent;

        _eventManager.Subscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintApply); // Apply on US
        _eventManager.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateTriggerMonitors());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => UpdateTrackedPlayerHealth());
    }

    public static List<MonitoredPlayerState> MonitoredPlayers { get; private set; } = new List<MonitoredPlayerState>();
    private bool ShouldProcessActionEffectHooks => _clientConfigs.ActiveTriggers.Any(x => x.Type is TriggerKind.SpellAction);
    public VibratorMode CurrentVibratorModeUsed => _clientConfigs.GagspeakConfig.VibratorMode;

    protected override void Dispose(bool disposing)
    {
        _eventManager.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintApply);
        _eventManager.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock);
        ActionEffectMonitor.ActionEffectEntryEvent -= OnActionEffectEvent;
        base.Dispose(disposing);
    }

    private void OnRestraintApply(Guid restraintId, bool isEnabling, string enactor)
    {
        if (isEnabling) CheckActiveRestraintTriggers(restraintId, NewState.Enabled);
    }

    private void OnRestraintLock(Guid restraintId, Padlocks padlock, bool isLocking, string enactorUID)
    {
        if (isLocking) CheckActiveRestraintTriggers(restraintId, NewState.Locked);
    }

    private void CheckSpellActionTriggers(ActionEffectEntry actionEffect)
    {
        Logger.LogDebug("SourceID: " + actionEffect.SourceID + " TargetID: " + actionEffect.TargetID + " ActionID: " + actionEffect.ActionID + " Type: " + actionEffect.Type + " Damage: " + actionEffect.Damage, LoggerType.ToyboxTriggers);

        var relevantTriggers = _clientConfigs.ActiveSpellActionTriggers
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
                Logger.LogDebug("Checking Trigger: " + trigger.Name, LoggerType.ToyboxTriggers);
                // Determine if the direction matches
                var isSourcePlayer = _clientMonitor.ObjectId == actionEffect.SourceID;
                var isTargetPlayer = _clientMonitor.ObjectId == actionEffect.TargetID;

                Logger.LogDebug("Trigger Direction we are checking was: " + trigger.Direction, LoggerType.ToyboxTriggers);
                bool directionMatches = trigger.Direction switch
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
                bool isDamageRelated = trigger.ActionKind is
                    LimitedActionEffectType.Heal or
                    LimitedActionEffectType.Damage or
                    LimitedActionEffectType.BlockedDamage or
                    LimitedActionEffectType.ParriedDamage;

                if (isDamageRelated && (actionEffect.Damage < trigger.ThresholdMinValue || actionEffect.Damage > (trigger.ThresholdMaxValue == -1 ? int.MaxValue : trigger.ThresholdMaxValue)))
                {
                    Logger.LogDebug($"Was ActionKind [" + actionEffect.Type + "], however, its damage (" + actionEffect.Damage + ") was not between (" + trigger.ThresholdMinValue +
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
        var matchingTriggers = _clientConfigs.ActiveRestraintTriggers
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

    private void CheckGagStateTriggers(GagType gagType, NewState newState)
    {
        // Check to see if any active gag triggers are in the message
        var matchingTriggers = _clientConfigs.ActiveGagStateTriggers
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
        Logger.LogInformation("Your Trigger With Name " + trigger.Name + " and priority " + trigger.Priority + " triggering action "
            + trigger.GetTypeName().ToName(), LoggerType.ToyboxTriggers);

        switch (trigger.GetTypeName())
        {
            case ActionExecutionType.SexToy:
            case ActionExecutionType.ShockCollar:
            case ActionExecutionType.Restraint:
            case ActionExecutionType.Gag:
            case ActionExecutionType.Moodle:
                if (await _actionExecuter.ExecuteActionAsync(trigger.ExecutableAction, MainHub.UID))
                    UnlocksEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
                break;
        }
    }

    public async void OnActionEffectEvent(List<ActionEffectEntry> actionEffects)
    {
        if (!_clientMonitor.IsPresent || !ShouldProcessActionEffectHooks)
            return;

        await _frameworkUtils.RunOnFrameworkThread(() =>
        {
            foreach (var actionEffect in actionEffects)
            {
                if (LoggerFilter.FilteredCategories.Contains(LoggerType.ActionEffects))
                {
                    // Perform logging and action processing for each effect
                    var sourceCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.SourceID) as IPlayerCharacter)?.GetNameWithWorld() ?? "UNKN OBJ";
                    var targetCharaStr = (_frameworkUtils.SearchObjectTableById(actionEffect.TargetID) as IPlayerCharacter)?.GetNameWithWorld() ?? "UNKN OBJ";
                    string actionStr = "UNKN ACT";
                    if (_clientMonitor.TryGetAction(actionEffect.ActionID, out GameAction action)) actionStr = action.Name.ToString();
                    Logger.LogDebug($"Source:{sourceCharaStr}, Target: {targetCharaStr}, Action: {actionStr}, Action ID:{actionEffect.ActionID}, " +
                        $"Type: {actionEffect.Type.ToString()} Amount: {actionEffect.Damage}", LoggerType.ActionEffects);
                }
                CheckSpellActionTriggers(actionEffect);
            };
        });
    }

    private void UpdateTriggerMonitors()
    {
        if (_clientConfigs.ActiveHealthPercentTriggers.Any())
        {
            // get the list of players to monitor.
            var activeTriggerPlayersToMonitor = _clientConfigs.ActiveHealthPercentTriggers.Select(x => x.PlayerToMonitor).Distinct().ToList();
            var activelyMonitoredPlayers = MonitoredPlayers.Select(x => x.PlayerNameWithWorld).ToList();
            var visiblePlayerCharacters = _frameworkUtils.GetObjectTablePlayers().Where(player => activeTriggerPlayersToMonitor.Contains(player.GetNameWithWorld()));

            // if any of the monitored players are not visible, we should remove them from the monitored list.
            // in other words, remove all monitored players whose PlayerNameWithWorld is not a part of the visiblePlayerCharacters.
            MonitoredPlayers.RemoveAll(x => !visiblePlayerCharacters.Any(player => player.GetNameWithWorld() == x.PlayerNameWithWorld));

            // if any of the visible players are not being monitored, we should add them to the monitored list.
            // in other words, add all visible players whose PlayerNameWithWorld is not a part of the activelyMonitoredPlayers.
            var playersToAdd = visiblePlayerCharacters.Where(player => !activelyMonitoredPlayers.Contains(player.GetNameWithWorld()));
            foreach (var player in playersToAdd)
                MonitoredPlayers.Add(_playerMonitorFactory.CreatePlayerMonitor(player));
            // early return.
            return;
        }
        // Clean if none.
        MonitoredPlayers.Clear();
    }

    private void UpdateTrackedPlayerHealth()
    {
        if (!MonitoredPlayers.Any())
            return;

        // By grouping triggers into a dictionary where the key is the player name, we reduce the need to filter triggers multiple times.
        var triggersByPlayer = _clientConfigs.ActiveHealthPercentTriggers
            .GroupBy(t => t.PlayerToMonitor)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Each player is processed once per update, and triggers are checked efficiently using the pre-computed dictionary.
        foreach (var player in MonitoredPlayers)
        {
            if (!player.HasHpChanged()) continue;

            // Logger.LogDebug("Hp Changed from {PreviousHp} to {CurrentHp}", player.PreviousHp, player.CurrentHp);

            // Calculate health percentages once per player to avoid redundancies.
            float percentageHP = player.CurrentHp * 100f / player.MaxHp;
            float previousPercentageHP = player.PreviousHp * 100f / player.PreviousMaxHp;

            if (triggersByPlayer.TryGetValue(player.PlayerNameWithWorld, out var triggers))
            {
                // Process each trigger for this player
                foreach (var trigger in triggers)
                {
                    bool isValid = false;

                    // Check if health thresholds are met based on trigger type
                    if (trigger.PassKind == ThresholdPassType.Under)
                    {
                        isValid = trigger.UsePercentageHealth
                            ? (previousPercentageHP > trigger.MinHealthValue && percentageHP <= trigger.MinHealthValue) ||
                                (previousPercentageHP > trigger.MaxHealthValue && percentageHP <= trigger.MaxHealthValue)
                            : (player.PreviousHp > trigger.MinHealthValue && player.CurrentHp <= trigger.MinHealthValue) ||
                                (player.PreviousHp > trigger.MaxHealthValue && player.CurrentHp <= trigger.MaxHealthValue);
                    }
                    else if (trigger.PassKind == ThresholdPassType.Over)
                    {
                        isValid = trigger.UsePercentageHealth
                            ? (previousPercentageHP < trigger.MinHealthValue && percentageHP >= trigger.MinHealthValue) ||
                                (previousPercentageHP < trigger.MaxHealthValue && percentageHP >= trigger.MaxHealthValue)
                            : (player.PreviousHp < trigger.MinHealthValue && player.CurrentHp >= trigger.MinHealthValue) ||
                                (player.PreviousHp < trigger.MaxHealthValue && player.CurrentHp >= trigger.MaxHealthValue);
                    }

                    if (isValid)
                        ExecuteTriggerAction(trigger);
                }
            }
            // update the HP regardless of change.
            player.UpdateHpChange();
        }
    }
}





