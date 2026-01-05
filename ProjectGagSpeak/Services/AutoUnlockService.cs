using CkCommons;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Controller;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using Microsoft.Extensions.Hosting;

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
    private readonly ImprisonmentController _cageControl;
    private readonly MovementController _moveControl;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly VisualStateListener _visuals;
    private readonly PlayerCtrlHandler _hcHandler;
    private readonly DistributorService _dds;
    
    // the interval tasks to check for
    private readonly List<Task> _intervalTasks = [];
    
    // local helper values.
    private bool _clientWasDead = false;
    private int _lastPlayerCount = 0;

    public AutoUnlockService(ILogger<AutoUnlockService> logger, GagspeakMediator mediator, MainHub hub,
        ClientData clientData, ImprisonmentController cageControl, MovementController moveControl, 
        KinksterManager kinksters, GagRestrictionManager gags, RestrictionManager restrictions, 
        RestraintManager restraints, CursedLootManager cursedLoot, PatternManager patterns, 
        AlarmManager alarms, VisualStateListener visuals, PlayerCtrlHandler hcHandler, 
        DistributorService dds)
    {
        _logger = logger;
        _mediator = mediator;
        _hub = hub;
        _clientData = clientData;
        _cageControl = cageControl;
        _moveControl = moveControl;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _patterns = patterns;
        _alarms = alarms;
        _visuals = visuals;
        _hcHandler = hcHandler;
        _dds = dds;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto-Unlock Service Started.");
        // Start all interval tasks
        _intervalTasks.Add(CheckOnInterval(1000, OnSecond, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(5000, OnFiveSeconds, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(15000, OnQuarterMinute, stoppingToken));
        _intervalTasks.Add(CheckOnInterval(60000, OnMinute, stoppingToken));

        // Wait for all tasks to complete (which will be when stoppingToken is cancelled)
        return Task.CompletedTask;
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
        // remove the stopwatch if it becomes excessive.
        var sw = Stopwatch.StartNew();
        await Task.WhenAll(
            CheckGags(),
            CheckRestrictions(),
            CheckRestraint(),
            CheckCursedLoot(),
            CheckHardcoreTimers()
        ).ConfigureAwait(false);
        sw.Stop();
        if (sw.ElapsedMilliseconds > 1)
            _logger.LogDebug($"Checked Padlock & Hardcore Timers in {sw.ElapsedMilliseconds}ms", LoggerType.AutoUnlocks);
    }

    private unsafe void OnFiveSeconds()
    {
        if (!MainHub.IsConnected)
            return;

        Svc.Framework.RunOnFrameworkThread(() =>
        {
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
        });
    }

    private void OnQuarterMinute()
    {
        if (!MainHub.IsConnected)
            return;
        // update tracked players.
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            var playersInRange = Svc.Objects.OfType<IPlayerCharacter>().Where(player => PlayerData.DistanceTo(player.Position) < 30f).Count();
            if (playersInRange != _lastPlayerCount)
            {
                _logger.LogTrace("(New Update) There are " + playersInRange + " Players nearby", LoggerType.AchievementInfo, LoggerType.AutoUnlocks);
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

            _logger.LogInformation($"{gag.GagItem.GagName()}'s [{gag.Padlock}] Timer Expired!", LoggerType.AutoUnlocks);
            // store backup state.
            var backup = gag;
            var dat = new ActiveGagSlot() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
            // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
            gag.Padlock = Padlocks.None;
            gag.Password = string.Empty;
            gag.Timer = DateTimeOffset.MinValue;
            gag.PadlockAssigner = string.Empty;
            // push update.
            if (await _dds.PushNewActiveGagSlot(index, dat, DataUpdateType.Unlocked).ConfigureAwait(false) is not null)
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

            _logger.LogInformation($"Restriction Layer {index + 1}'s [{item.Padlock}] Timer Expired!", LoggerType.Restrictions);
            // store backup state.
            var backup = item;
            var dat = new ActiveRestriction() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
            // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
            item.Padlock = Padlocks.None;
            item.Password = string.Empty;
            item.Timer = DateTimeOffset.MinValue;
            item.PadlockAssigner = string.Empty;
            if (await _dds.PushNewActiveRestriction(index, dat, DataUpdateType.Unlocked).ConfigureAwait(false) is not null)
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
        
        _logger.LogInformation($"RestraintSet's [{data.Padlock.ToName()}] Timer Expired!", LoggerType.AutoUnlocks);
        // store backup state.
        var backup = data;
        var dat = new CharaActiveRestraint() with { Padlock = backup.Padlock, Password = backup.Password, PadlockAssigner = backup.PadlockAssigner };
        // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
        data.Padlock = Padlocks.None;
        data.Password = string.Empty;
        data.Timer = DateTimeOffset.MinValue;
        data.PadlockAssigner = string.Empty;
        if (await _dds.PushNewActiveRestraint(dat, DataUpdateType.Unlocked).ConfigureAwait(false) is not null)
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
    private async Task CheckCursedLoot()
    {
        if (!_cursedLoot.Storage.AppliedLootUnsorted.Any())
            return;

        // Otherwise iterate the items for unlocks.
        foreach (var item in _cursedLoot.Storage.AppliedLootUnsorted)
        {
            if (item.ReleaseTime >= DateTimeOffset.UtcNow) continue;

            _logger.LogInformation($"CursedLoot Item [{item.Label}] Timer Expired!", LoggerType.AutoUnlocks);

            // store backup state.
            var backup = item;
            // Temporarily update the changes locally, to prevent excess Auto-unlock calls.
            item.AppliedTime = DateTimeOffset.MinValue;
            item.ReleaseTime = DateTimeOffset.MinValue;
            _cursedLoot.ForceSave();
            // Attempt to push the update.
            if (await _dds.PushActiveCursedLoot(_cursedLoot.Storage.AppliedLootIds.ToList(), item.Identifier, null).ConfigureAwait(false) is null)
            {
                // Revert the values to prevent the update and trigger it again later. This helps to prevent false achievement triggering.
                item.AppliedTime = backup.AppliedTime;
                item.ReleaseTime = backup.ReleaseTime;
                _cursedLoot.ForceSave();
            }
            else
            {
                // was successful, so make sure to remove it from viausals and cache manager.
                await _visuals.CursedItemRemoved(item.Identifier);
            }
        }
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
        if (hcState.LockedFollowing.Length > 0 && MovementController.TimeIdleDuringFollow > TimeSpan.FromSeconds(6))
        {
            _logger.LogInformation("Standing Still for over 6 seconds during LockedFollow. Auto-Disabling!", LoggerType.AutoUnlocks);
            var enactor = hcState.LockedFollowing.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.Follow);
            _moveControl.ResetTimeoutTracker();
            // then server-side.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.Follow, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"LockedFollow Timer Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.DisableLockedFollow(new(enactor), success);
        }

        // Check Emote Timer.
        if (hcState.LockedEmoteState.Length > 0 && hcState.EmoteExpireTime < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("LockedEmote Timer Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.LockedEmoteState.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.EmoteState);
            // then server-side.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.EmoteState, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"LockedEmote Timer Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.DisableLockedEmote(new(enactor), success);
        }

        // Check Confinement Timer.
        if (hcState.IndoorConfinement.Length > 0 && hcState.ConfinementTimer < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Confinement Timer Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.IndoorConfinement.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.Confinement);
            // then server-side.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.Confinement, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Confinement Timer Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.DisableConfinement(new(enactor), success);
        }

        // Check Imprisonment Timer.
        if (hcState.Imprisonment.Length > 0)
        {
            // if we should be imprisoned but are not (due to failed application)
            if ((_cageControl.ShouldBeImprisoned && !_cageControl.IsImprisoned) || hcState.ImprisonmentTimer < DateTimeOffset.UtcNow)
            {
                _logger.LogInformation("Imprisonment Timer Expired (or state was invalid!)", LoggerType.AutoUnlocks);
                var enactor = hcState.Imprisonment.Split('|')[0];
                // locally change first.
                _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.Imprisonment);
                // Attempt the server-side call.
                var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.Imprisonment, new(enactor))).ConfigureAwait(false) is not null;
                if (success)
                    _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Imprisonment Timer Expired!")));
                // Perform manipulations regardless. If the achievements fire depends on if it was successful.
                _hcHandler.DisableImprisonment(new(enactor), success);
            }
        }

        // Check Chat Boxes Hidden Timer.
        if (hcState.ChatBoxesHidden.Length > 0 && hcState.ChatBoxesHiddenTimer < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Hidden ChatBoxes Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.ChatBoxesHidden.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.HiddenChatBox);
            // Attempt the server-side call.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.HiddenChatBox, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Hidden ChatBoxes Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.DisableHiddenChatBoxes(new(enactor), success);
        }

        // Check Chat Input Hidden Timer.
        if (hcState.ChatInputHidden.Length > 0 && hcState.ChatInputHiddenTimer < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Hidden ChatInput Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.ChatInputBlocked.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.HiddenChatInput);
            // Attempt the server-side call.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.HiddenChatInput, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Hidden ChatInput Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.RestoreChatInputVisibility(new(enactor), success);
        }

        // Check Chat Input Blocked Timer.
        if (hcState.ChatInputBlocked.Length > 0 && hcState.ChatInputBlockedTimer < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Blocked ChatInput Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.ChatInputBlocked.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.BlockedChatInput);
            // Attempt the server-side call.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.BlockedChatInput, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Blocked ChatInput Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.UnblockChatInput(new(enactor), success);
        }

        // Check Hypnotic Effect Timer.
        if (hcState.HypnoticEffect.Length > 0 && hcState.HypnoticEffectTimer < DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Hypnotic Effect Timer Expired!", LoggerType.AutoUnlocks);
            var enactor = hcState.HypnoticEffect.Split('|')[0];
            // locally change first.
            _clientData.DisableHardcoreStatus(MainHub.OwnUserData, HcAttribute.HypnoticEffect);
            // Attempt the server-side call.
            var success = await _hub.UserHardcoreAttributeExpired(new(HcAttribute.HypnoticEffect, new(enactor))).ConfigureAwait(false) is not null;
            if (success)
                _mediator.Publish(new EventMessage(new("Self-Update", MainHub.UID, InteractionType.HardcoreStateChange, $"Hypnotic Effect Timer Expired!")));
            // Perform manipulations regardless. If the achievements fire depends on if it was successful.
            _hcHandler.RemoveHypnoEffect(new(enactor), success);
        }
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
