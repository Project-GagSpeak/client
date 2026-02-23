using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.Localization;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Watchers;
using TerraFX.Interop.Windows;
using TerraFX.Interop.WinRT;

namespace GagSpeak.State.Listeners;

internal sealed record PlayerHealth(string NameWithWorld)
{
    public HashSet<HealthPercentTrigger> Triggers { get; set; } = new();
    public uint LastHp { get; set; }
    public uint LastMaxHp { get; set; }
}

/// <summary>
///     Track the HP of rendered actors that we have for Health% Triggers.
/// </summary>
public sealed class HealthMonitor : DisposableMediatorSubscriberBase
{
    private readonly TriggerManager _manager;
    private readonly CharaObjectWatcher _watcher;
    
    private Dictionary<nint, PlayerHealth> Monitored = [];

    public HealthMonitor(ILogger<HealthMonitor> logger, GagspeakMediator mediator,
        TriggerManager manager, CharaObjectWatcher watcher)
        : base(logger, mediator)
    {
        _manager = manager;
        _watcher = watcher;

        // After connection, reload all the triggers into the monitors.
        Mediator.Subscribe<ConnectedMessage>(this, _ => UpdateTriggersForMonitors(manager.Storage.HealthPercent, true));

        // This could be removed entirely if we detect when triggers enable or disable but yeah.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => UpdateHpValues());

        Mediator.Subscribe<WatchedObjectCreated>(this, _ => AddMonitorsForAddr(_.Address));
        Mediator.Subscribe<WatchedObjectDestroyed>(this, _ => RemoveMonitorsForAddr(_.Address));

        // Update which triggers are monitored.
        Mediator.Subscribe<EnabledItemChanged>(this, _ =>
        {
            if (_.Module is GSModule.Trigger)
            {
                Logger.LogDebug($"EnabledItemChanged : {_.ItemId} to {_.NewState}", LoggerType.Triggers);
                UpdateTriggerForMonitors(_.ItemId, _.NewState);
            }
        });
        Mediator.Subscribe<EnabledItemsChanged>(this, _ =>
        {
            if (_.Module is GSModule.Trigger)
            {
                Logger.LogDebug($"EnabledItemsChanged : {_.Items.Count()} to {_.NewState}", LoggerType.Triggers);
                UpdateTriggersForMonitors(_.Items, _.NewState);
            }
        });
        Mediator.Subscribe<ConfigTriggerChanged>(this, _ =>
        {
            if (_.Item is not HealthPercentTrigger trigger)
                return;

            if (_.Type is FileSystems.StorageChangeType.Deleted)
            {
                UpdateTriggerForMonitors(trigger, false);
                return;
            }
            UpdateTriggerForMonitors(trigger, _.Item.Enabled);
        });

        GetInitialMonitors();
    }

    private unsafe void GetInitialMonitors()
    {
        foreach (var charaAddr in CharaObjectWatcher.Rendered)
        {
            var chara = (Character*)charaAddr;
            Monitored.Add(charaAddr, new PlayerHealth(chara->GetNameWithWorld())
            {
                LastHp = chara->CharacterData.Health,
                LastMaxHp = chara->CharacterData.MaxHealth,
            });
        }
    }

    private void UpdateTriggerForMonitors(Guid triggerId, bool newState)
    {
        if (_manager.Storage.OfType<HealthPercentTrigger>().FirstOrDefault(t => t.Identifier == triggerId) is { } trigger)
            UpdateTriggerForMonitors(trigger, newState);
    }

    private void UpdateTriggerForMonitors(HealthPercentTrigger trigger, bool newState)
    {
        // Only one player can be tracked by one trigger.
        foreach (var (addr, data) in Monitored)
        {
            // If the trigger already exists, remove it as an initial assumption for our update.
            data.Triggers.Remove(trigger);

            // If the new state is false, continue.
            if (!newState)
                continue;

            // Otherwise update based on match
            if (data.NameWithWorld == trigger.PlayerNameWorld)
                data.Triggers.Add(trigger);
        }
    }

    private void UpdateTriggersForMonitors(IEnumerable<Guid> triggerIds, bool newState)
    {
        var triggers = _manager.Storage.OfType<HealthPercentTrigger>().Where(t => triggerIds.Contains(t.Identifier));
        UpdateTriggersForMonitors(triggers, newState);
    }

    // might be buggy?
    private void UpdateTriggersForMonitors(IEnumerable<HealthPercentTrigger> triggers, bool newState)
    {
        var nameToKey = Monitored.ToDictionary(t => t.Value.NameWithWorld, t => t.Key);
        foreach (var (addr, data) in Monitored)
        {
            // Remove all changed as a cleanup call.
            data.Triggers.ExceptWith(triggers);

            // If the new state is false, continue.
            if (!newState)
                continue;

            // Otherwise update based on match
            data.Triggers.UnionWith(triggers.Where(t => t.PlayerNameWorld == data.NameWithWorld));
        }
    }

    private unsafe void AddMonitorsForAddr(nint addr)
    {
        var chara = (Character*)addr;
        var nameWorld = chara->GetNameWithWorld();

        // For any triggers associated with this user, add them in.
        var relevantTriggers = _manager.Storage.HealthPercent.Where(t => t.PlayerNameWorld == nameWorld);
        Logger.LogDebug($"Adding monitor for {addr:X} with {relevantTriggers.Count()} triggers.", LoggerType.Triggers);
        Monitored.Add(addr, new PlayerHealth(nameWorld)
        {
            Triggers = relevantTriggers.Any() ? relevantTriggers.ToHashSet() : [],
            LastHp = chara->CharacterData.Health,
            LastMaxHp = chara->CharacterData.MaxHealth
        });
    }

    private unsafe void RemoveMonitorsForAddr(nint addr)
    {
        if (Monitored.Remove(addr))
            Logger.LogDebug($"Removed monitor for {addr:X}", LoggerType.Triggers);
    }

    private unsafe void UpdateHpValues()
    {
        if (Monitored.Count is 0)
            return;

        // Handle updating the monitored players.
        foreach (var (addr, data) in Monitored)
        {
            // Dont update anyone that has no attached trigger.
            if (data.Triggers.Count is 0)
                continue;

            Character* chara = (Character*)addr;
            var curHp = chara->CharacterData.Health;
            var curMaxHp = chara->CharacterData.MaxHealth;

            // if no hp changed, continue.
            if (curHp == data.LastHp && curMaxHp == data.LastMaxHp)
                continue;

            // Calculate health percentages once per player to avoid redundancies.
            var percentageHP = curHp * 100f / curMaxHp;
            var previousPercentageHP = data.LastHp * 100f / data.LastMaxHp;

            // scan the playerHealth values for trigger change conditions.
            foreach (var trigger in data.Triggers)
            {
                var isValid = false;

                // Check if health thresholds are met based on trigger type
                if (trigger.PassKind == ThresholdPassType.Under)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP > trigger.ThresholdMinValue && percentageHP <= trigger.ThresholdMinValue) ||
                            (previousPercentageHP > trigger.ThresholdMaxValue && percentageHP <= trigger.ThresholdMaxValue)
                        : (data.LastHp > trigger.ThresholdMinValue && curHp <= trigger.ThresholdMinValue) ||
                            (data.LastHp > trigger.ThresholdMaxValue && curHp <= trigger.ThresholdMaxValue);
                }
                else if (trigger.PassKind == ThresholdPassType.Over)
                {
                    isValid = trigger.UsePercentageHealth
                        ? (previousPercentageHP < trigger.ThresholdMinValue && percentageHP >= trigger.ThresholdMinValue) ||
                            (previousPercentageHP < trigger.ThresholdMaxValue && percentageHP >= trigger.ThresholdMaxValue)
                        : (data.LastHp < trigger.ThresholdMinValue && curHp >= trigger.ThresholdMinValue) ||
                            (data.LastHp < trigger.ThresholdMaxValue && curHp >= trigger.ThresholdMaxValue);
                }

                if (isValid)
                {
                    Logger.LogInformation("HP Change met a valid trigger condition!");
                    Mediator.Publish(new HpMonitorTriggered(addr, trigger));
                }
            }

            // Update with latest.
            data.LastHp = curHp;
            data.LastMaxHp = curMaxHp;
        }
    }

    public unsafe void DrawDebug()
    {
        using var node = ImRaii.TreeNode($"HP Debugger");
        if (!node) return;

        using (var modReps = ImRaii.Table("health-debug", 7, ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.BordersOuter))
        {
            if (!modReps) return;
            ImGui.TableSetupColumn("Address");
            ImGui.TableSetupColumn("NameWorld");
            ImGui.TableSetupColumn("Last HP");
            ImGui.TableSetupColumn("Curr HP");
            ImGui.TableSetupColumn("Last MaxHp");
            ImGui.TableSetupColumn("Curr MaxHp");
            ImGui.TableSetupColumn("Triggers");
            ImGui.TableHeadersRow();

            foreach (var (addr, data) in Monitored)
            {
                ImGui.TableNextColumn();
                CkGui.ColorText($"{addr:X}", ImGuiColors.DalamudViolet);
                ImGui.TableNextColumn();

                var chara = (Character*)addr;
                ImGui.Text(chara->GetNameWithWorld());

                ImGui.TableNextColumn();
                ImGui.Text($"{data.LastHp} Hp");

                ImGui.TableNextColumn();
                ImGui.Text($"{chara->CharacterData.Health} Hp");
                
                ImGui.TableNextColumn();
                ImGui.Text($"{data.LastMaxHp} MaxHp");

                ImGui.TableNextColumn();
                ImGui.Text($"{chara->CharacterData.MaxHealth} MaxHp");

                ImGui.TableNextColumn();
                ImGui.Text(string.Join(", ", data.Triggers.Select(t => t.Label)));
                ImGui.TableNextRow();
            }
        }
    }
}
