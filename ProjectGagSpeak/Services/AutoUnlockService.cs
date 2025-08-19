using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TerraFX.Interop.Windows;

namespace GagSpeak.Services;

/// <summary>
///     Dedicated to monitoring all current padlock states and timed events in hardcore
///     to perform server calls that will automatically unlock them. <para />
///     
///     <b> NOTICE FOR ACIEVEMENT SYNCRONIZATION: </b> <para />
///     When we restore backups, we will force it to re-trigger again. It is not prudent 
///     that we unlock them over and over until the server reconnects. What is important
///     is that if a unlock fails, that when the server does reconnect, the achievement 
///     that SHOULD HAVE fired, DOES fire. <para />
///     
///     So long as we can make that work, its golden.
/// </summary>
public sealed class AutoUnlockService : BackgroundService
{
    private readonly ILogger<AutoUnlockService> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly ClientData _clientData;
    private readonly MovementController _moveControl;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly PlayerControlHandler _hcHandler;
    private readonly DataDistributionService _dds;
    
    // the interval tasks to check for
    private readonly List<Task> _intervalTasks = [];
    
    // local helper values.
    private bool _clientWasDead = false;
    private int _lastPlayerCount = 0;

    public AutoUnlockService(ILogger<AutoUnlockService> logger, GagspeakMediator mediator, MainHub hub,
        ClientData clientData, MovementController moveControl, KinksterManager kinksters, 
        GagRestrictionManager gags, RestrictionManager restrictions, RestraintManager restraints, 
        CursedLootManager cursedLoot, PatternManager patterns, AlarmManager alarms,
        PlayerControlHandler hcHandler, DataDistributionService dds)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _clientData = clientData;
        _moveControl = moveControl;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _patterns = patterns;
        _alarms = alarms;
        _hcHandler = hcHandler;
        _dds = dds;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Unlock Service Started.");
        // Start all interval tasks
        _intervalTasks.Add(CheckOnInterval(1000, OnSecond, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(5000, OnFiveSeconds, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(15000, OnQuarterMinute, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(60000, OnMinute, stoppingToken));

        // Wait for all tasks to complete (which will be when stoppingToken is cancelled)
        await Task.WhenAll(_intervalTasks);
    }

    private async Task CheckOnInterval(int msDelay, Action checkFunc, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Generic.Safe(checkFunc);            
            await Task.Delay(msDelay, stoppingToken);
        }
    }

    // Anything that needs checks this fast is likely for padlocks.
    private async void OnSecond()
    {
        if (!MainHub.IsConnected)
            return;

        await CheckGags().ConfigureAwait(false);
        await CheckRestrictions().ConfigureAwait(false);
        await CheckRestraint().ConfigureAwait(false);
        await CheckCursedLoot().ConfigureAwait(false);
    }

    private unsafe void OnFiveSeconds()
    {
        // Don't rely on IsDead.
        var isDead = PlayerData.Health is 0;
        if (isDead && !_clientWasDead)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.ClientSlain);
        // update death state.
        _clientWasDead = isDead;

        // Detect chocobo race victory.
        if (PlayerContent.TerritoryID is 144 && PlayerData.IsChocoboRacing)
        {
            var resultMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("RaceChocoboResult").Address;
            if (resultMenu != null && resultMenu->RootNode->IsVisible())
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ChocoboRaceFinished);
        }
    }

    private void OnQuarterMinute()
    {
        // update tracked players.
        // we should get the current player object count that is within the range required for crowd pleaser.
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            var playersInRange = Svc.Objects.OfType<IPlayerCharacter>().Where(player => PlayerData.DistanceTo(player.Position) < 30f).Count();
            if (playersInRange != _lastPlayerCount)
            {
                _logger.LogTrace("(New Update) There are " + playersInRange + " Players nearby", LoggerType.AchievementInfo);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PlayersInProximity, playersInRange);
                _lastPlayerCount = playersInRange;
            }
        });
    }

    private async void OnMinute()
    {
        if (!MainHub.IsConnected)
            return;

        await CheckAlarms().ConfigureAwait(false);
    }

    // -------- Checker Methods Below.
    private async Task CheckGags()
    {
        if (_gags.ServerGagData is not { } gags || !gags.AnyGagLocked())
            return;

        foreach (var (gag, index) in gags.GagSlots.Select((slot, index) => (slot, index)))
        {
            if (!gag.Padlock.IsTimerLock()) continue;
            if (!gag.HasTimerExpired()) continue;

            _logger.LogTrace($"{gag.GagItem.GagName()}'s [{gag.Padlock}] Timer Expired!", LoggerType.Gags);
            // store backup state.
            var backup = gag;
            var dat = new ActiveGagSlot() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
            // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
            gag.Padlock = Padlocks.None;
            gag.Password = string.Empty;
            gag.Timer = DateTimeOffset.MinValue;
            gag.PadlockAssigner = string.Empty;
            if (await _dds.PushNewActiveGagSlot(index, dat, DataUpdateType.Unlocked).ConfigureAwait(false) is { } newData)
            {
                GagspeakEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, false, index, backup.Padlock, MainHub.UID);
                _mediator.Publish(new EventMessage(new("Auto-Unlock", MainHub.UID, InteractionType.UnlockGag, $"{gag.GagItem.GagName()}'s Timed Padlock Expired!")));
            }
            else
            {
                gag.Padlock = backup.Padlock;
                gag.Password = backup.Password;
                gag.Timer = backup.Timer;
                gag.PadlockAssigner = backup.PadlockAssigner;
            }
        }
    }

    private async Task CheckRestrictions()
    {
        if (_restrictions.ServerRestrictionData is not { } data || !data.Restrictions.Any(i => i.IsLocked()))
            return;

        foreach (var (item, index) in data.Restrictions.Select((slot, index) => (slot, index)))
        {
            if (!item.Padlock.IsTimerLock()) continue;
            if (!item.HasTimerExpired()) continue;

            _logger.LogTrace($"Restriction Layer {index + 1}'s [{item.Padlock}] Timer Expired!", LoggerType.Restrictions);
            // store backup state.
            var backup = item;
            var dat = new ActiveRestriction() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
            // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
            item.Padlock = Padlocks.None;
            item.Password = string.Empty;
            item.Timer = DateTimeOffset.MinValue;
            item.PadlockAssigner = string.Empty;
            if (await _dds.PushNewActiveRestriction(index, dat, DataUpdateType.Unlocked).ConfigureAwait(false) is { } newData)
            {
                // update was valid, so do whatever we would normally do in the manager.
                GagspeakEventManager.AchievementEvent(UnlocksEvent.RestrictionLockStateChange, false, index, backup.Padlock, MainHub.UID);
                _mediator.Publish(new EventMessage(new("Auto-Unlock", MainHub.UID, InteractionType.UnlockRestriction, $"Restriction Layer {index + 1}'s Timed Padlock Expired!")));
            }
            else
            {
                // Revert the values to prevent the update and trigger it again later. This helps to prevent false achievement triggering.
                item.Padlock = backup.Padlock;
                item.Password = backup.Password;
                item.Timer = backup.Timer;
                item.PadlockAssigner = backup.PadlockAssigner;
            }
        }
    }

    private async Task CheckRestraint()
    {
        if (_restraints.ServerData is not { } data || !data.IsLocked())
            return;

        if (!data.Padlock.IsTimerLock())
            return;

        if (!data.HasTimerExpired())
            return;
        
        _logger.LogTrace($"RestraintSet's [{data.Padlock.ToName()}] Timer Expired!", LoggerType.Restraints);
        // store backup state.
        var backup = data;
        var dat = new CharaActiveRestraint() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
        // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
        data.Padlock = Padlocks.None;
        data.Password = string.Empty;
        data.Timer = DateTimeOffset.MinValue;
        data.PadlockAssigner = string.Empty;
        if (await _dds.PushActiveRestraintUpdate(dat, DataUpdateType.Unlocked).ConfigureAwait(false) is { } newData)
        {
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, data.Identifier, backup.Padlock, false, MainHub.UID);
            // Sold slave is never valid here.            
            _mediator.Publish(new EventMessage(new("Auto-Unlock", MainHub.UID, InteractionType.UnlockRestraint, $"Active RestraintSet's Timed Padlock Expired!")));
        }
        else
        {
            // Revert the values to prevent the update and trigger it again later. This helps to prevent false achievement triggering.
            data.Padlock = backup.Padlock;
            data.Password = backup.Password;
            data.Timer = backup.Timer;
            data.PadlockAssigner = backup.PadlockAssigner;
        }
    }

    // Subject to change!
    private Task CheckCursedLoot()
    {
        if (_cursedLoot.Storage.ActiveItems.Count is 0)
            return Task.CompletedTask;

        // Otherwise iterate the items for unlocks.
        foreach (var item in _cursedLoot.Storage.ActiveItems)
        {
            if (item.ReleaseTime - DateTimeOffset.UtcNow > TimeSpan.Zero)
                continue;
            
            // Item should be unlocked.
            item.AppliedTime = DateTimeOffset.MinValue;
            item.ReleaseTime = DateTimeOffset.MinValue;
            _cursedLoot.ForceSave();
            
            // if it was a restriction manager, be sure to remove its item.
            if (item.RestrictionRef is RestrictionItem nonGagRestriction)
                _restrictions.TryRemoveRemoveOccupied(nonGagRestriction);
            
            // _managerCache.UpdateCache(AppliedCursedItems, _mainConfig.Current.CursedItemsApplyTraits);
        }
        return Task.CompletedTask;
    }

    // Check the hardcore timers and perform a hardcore change when a timer expires.
    // It it crucial here that we do NOT restore backup states if the timer fires,
    // but instead that we handle it as if we are disabling it regardless, and if a
    // failure occurred, then our fix will be re-instated upon next reconnect.
    private async Task CheckHardcoreTimers()
    {
        if (ClientData.Hardcore is not { } hcState)
            return;

        // forced follow is special in a sense.
        if (!string.IsNullOrEmpty(hcState.LockedFollowing) && MovementController.TimeIdleDuringFollow > TimeSpan.FromSeconds(6))
        {
            _logger.LogInformation("Standing Still for over 6 seconds during LockedFollow. Auto-Disabling!", LoggerType.HardcoreMovement);
            var enactor = hcState.LockedFollowing;
            // locally make the change first. (This includes the control reverts, must be careful!)
            _clientData.DisableHardcoreState(MainHub.PlayerUserData, HcAttribute.Follow);
            _moveControl.ResetTimeoutTracker();

            // Attempt the server-side call.
            if (await _hub.UserHardcoreAttributeExpired(new(HcAttribute.Follow, new(hcState.LockedFollowing))).ConfigureAwait(false) is { } newData)
            {
                // Call was a success, so perform the hardcore handler change.
                // DO THAT HERE.
            }
        }

        // Check Emote Timer.

        // Check Confinement Timer.

        // Check Imprisonment Timer.

        // Check Chat Boxes Hidden Timer.

        // Check Chat Input Hidden Timer.

        // Check Chat Input Blocked Timer.

        // Check Hypnotic Effect Timer. (I think this already auto-disables?)
    }


    private Task CheckAlarms()
    {
        if (!_alarms.ActiveAlarms.Any())
            return Task.CompletedTask;

        // Move through the active alarms to see if the alarm should be played.
        foreach (var alarm in _alarms.ActiveAlarms)
        {
            // If it shouldnt fire today, skip it.
            if (!alarm.DaysToFire.HasAny(DateTime.Now.DayOfWeek.ToFlagVariant()))
                continue;

            // get local time.
            var alarmTime = alarm.SetTimeUTC.ToLocalTime();
            // ensure current time matches set time.
            if (DateTime.Now.TimeOfDay.Hours != alarmTime.TimeOfDay.Hours)
                continue;

            // if the minutes match, play the alarm.
            if (DateTime.Now.TimeOfDay.Minutes != alarmTime.TimeOfDay.Minutes)
                continue;

            // Fire that alarm!
            _logger.LogInformation($"Alarm Triggered!: [{alarm.PatternRef.Label}] Playing Pattern ({alarm.PatternRef.Label})", LoggerType.Alarms);
            _patterns.SwitchPattern(alarm.PatternRef, alarm.PatternStartPoint, alarm.PatternDuration, MainHub.UID);
        }

        return Task.CompletedTask;
    }
}
