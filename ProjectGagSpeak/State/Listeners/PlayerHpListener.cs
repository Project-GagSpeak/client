using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Tracks the player health of the characters we want to modify, 
///     and notifies trigger applier when threshold is met.
/// </summary>
public sealed class PlayerHpListener : DisposableMediatorSubscriberBase
{
    private readonly TriggerManager _manager;
    private readonly TriggerActionService _actionService;
    private readonly OnFrameworkService _frameworkUtils;
    public PlayerHpListener(
        ILogger<PlayerHpListener> logger,
        GagspeakMediator mediator,
        TriggerManager manager,
        TriggerActionService actionService,
        OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _manager = manager;
        _actionService = actionService;
        _frameworkUtils = frameworkUtils;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateTriggerMonitors());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => UpdateTrackedPlayerHealth());
    }

    private readonly Dictionary<IPlayerCharacter, PlayerHealth> MonitoredPlayers = [];
    private record PlayerHealth(IEnumerable<HealthPercentTrigger> triggers)
    {
        public uint LastHp { get; set; }
        public uint LastMaxHp { get; set; }
    };

    private void UpdateTriggerMonitors()
    {
        if (_manager.Storage.HealthPercent.Any())
        {
            MonitoredPlayers.Clear();
            return;
        }

        // Group triggers by the player being monitored.
        var playerTriggers = _manager.Storage.HealthPercent
            .GroupBy(trigger => trigger.PlayerNameWorld)
            .ToDictionary(group => group.Key, group => new PlayerHealth(group.AsEnumerable()));

        // Get the visible characters.
        var visiblePlayerCharacters = _frameworkUtils.GetObjectTablePlayers()
            .Where(player => playerTriggers.Keys.Contains(player.GetNameWithWorld()));

        // Remove players from MonitoredPlayers who are no longer visible.
        var playersToRemove = MonitoredPlayers.Keys.Except(visiblePlayerCharacters);

        // Add Players that should be tracked that are now visible.
        var playersToAdd = visiblePlayerCharacters.Except(MonitoredPlayers.Keys);

        // remove all the non-visible players
        foreach (var player in playersToRemove)
            MonitoredPlayers.Remove(player);

        // add all the visible players
        foreach (var player in playersToAdd)
            if (playerTriggers.TryGetValue(player.GetNameWithWorld(), out var triggers))
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

    public async void ExecuteTriggerAction(HealthPercentTrigger trigger)
    {
        Logger.LogInformation("Your Trigger With Name " + trigger.Label + " and priority " + trigger.Priority + " triggering action "
            + trigger.InvokableAction.ActionType.ToName(), LoggerType.Triggers);

        if (await _actionService.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
            GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
    }
}
