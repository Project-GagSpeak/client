using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Watchers;
using GagSpeak.WebAPI;

namespace GagSpeak.State.Listeners;

internal record PlayerHealth(IEnumerable<HealthPercentTrigger> triggers)
{
    public uint LastHp { get; set; }
    public uint LastMaxHp { get; set; }
};

/// <summary>
///     Tracks the player health of the characters we want to modify, 
///     and notifies trigger applier when threshold is met.
/// </summary>
public sealed class PlayerHpListener : DisposableMediatorSubscriberBase
{
    private readonly TriggerManager _manager;
    private readonly TriggerActionService _service;
    private readonly CharaObjectWatcher _watcher;

    public PlayerHpListener(ILogger<PlayerHpListener> logger, GagspeakMediator mediator,
        TriggerManager manager, TriggerActionService service, CharaObjectWatcher watcher)
        : base(logger, mediator)
    {
        _manager = manager;
        _service = service;
        _watcher = watcher;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => UpdateTriggerMonitors());

        Mediator.Subscribe<WatchedObjectCreated>(this, _ => { });
        // Bomb them from the monitors when they leave the area.
        Mediator.Subscribe<WatchedObjectDestroyed>(this, _ => Monitored.Remove(_.Address));

        Svc.Framework.Update += OnTick;
    }

    private readonly Dictionary<nint, PlayerHealth> Monitored = [];

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.Framework.Update -= OnTick;
    }

    // Change this eventually to only ever update when the triggers modify in the trigger manager, and on
    // character created / deleted events.
    private unsafe void UpdateTriggerMonitors()
    {
        if (_manager.Storage.HealthPercent.Any())
        {
            Monitored.Clear();
            return;
        }

        // Grab all health triggers of the category health percent, grouped by their monitored name.
        var triggers = _manager.Storage.HealthPercent
            .GroupBy(t => t.PlayerNameWorld)
            .ToDictionary(g => g.Key, g => new PlayerHealth(g.AsEnumerable()));

        // Iterate over the current rendered characters tracked by the watcher.
        // Take each as a character model and add them to the list of visible characters.
        List<nint> visibleMatches = CharaObjectWatcher.Rendered
            .Where(chara => triggers.ContainsKey(((Character*)chara)->GetNameWithWorld()))
            .ToList();

        // Get the addresses to remove. (Again, we wont need to do this if we bind it to the manager / watcher)
        var toRemove = Monitored.Keys.Except(visibleMatches);

        // Add those who are not yet monitored.
        var toAdd = visibleMatches.Except(Monitored.Keys);

        foreach (var addr in toRemove)
            Monitored.Remove(addr);

        foreach (var player in toAdd)
            if (triggers.TryGetValue(((Character*)player)->GetNameWithWorld(), out var item))
                Monitored.Add(player, item);
    }

    private unsafe  void OnTick(IFramework _)
    {
        if (!Monitored.Any())
            return;

        // Handle updating the monitored players.
        foreach (var (addr, pHealth) in Monitored)
        {
            Character* chara = (Character*)addr;
            var curHp = chara->CharacterData.Health;
            var curMaxHp = chara->CharacterData.MaxHealth;

            // if no hp changed, continue.
            if (curHp == pHealth.LastHp || curMaxHp == pHealth.LastMaxHp)
                continue;

            // Calculate health percentages once per player to avoid redundancies.
            var percentageHP = curHp * 100f / curMaxHp;
            var previousPercentageHP = pHealth.LastHp * 100f / pHealth.LastMaxHp;

            // scan the playerHealth values for trigger change conditions.
            foreach (var trigger in pHealth.triggers)
            {
                var isValid = false;

                // Check if health thresholds are met based on trigger type
                if (trigger.PassKind == ThresholdPassType.Under)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP > trigger.ThresholdMinValue && percentageHP <= trigger.ThresholdMinValue) ||
                            (previousPercentageHP > trigger.ThresholdMaxValue && percentageHP <= trigger.ThresholdMaxValue)
                        : (pHealth.LastHp > trigger.ThresholdMinValue && curHp <= trigger.ThresholdMinValue) ||
                            (pHealth.LastHp > trigger.ThresholdMaxValue && curHp <= trigger.ThresholdMaxValue);
                }
                else if (trigger.PassKind == ThresholdPassType.Over)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP < trigger.ThresholdMinValue && percentageHP >= trigger.ThresholdMinValue) ||
                            (previousPercentageHP < trigger.ThresholdMaxValue && percentageHP >= trigger.ThresholdMaxValue)
                        : (pHealth.LastHp < trigger.ThresholdMinValue && curHp >= trigger.ThresholdMinValue) ||
                            (pHealth.LastHp < trigger.ThresholdMaxValue && curHp >= trigger.ThresholdMaxValue);
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

        if (await _service.HandleActionAsync(trigger.InvokableAction, MainHub.UID, ActionSource.TriggerAction))
            GagspeakEventManager.AchievementEvent(UnlocksEvent.TriggerFired);
    }
}
