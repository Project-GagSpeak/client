using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using CkCommons;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.Toybox;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using Microsoft.Extensions.Hosting;
using GagSpeak.Utils;

namespace GagSpeak.Services;

/// <summary>
///     A service that manages achievements for the GagSpeak client.
/// </summary>
public class AchievementsService : DisposableMediatorSubscriberBase, IHostedService
{
    private readonly ClientAchievements _saveData;
    private readonly MainConfig _config;
    private readonly GlobalPermissions _globals;
    private readonly TraitsCache _traits;
    private readonly KinksterManager _pairs;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly AchievementEventHandler _handler;
    private readonly NotificationService _notifier;
    private readonly RemoteService _remoteService;
    private readonly OnFrameworkService _frameworkUtils;

    private DateTime _lastCheck = DateTime.MinValue;
    private DateTime _lastPlayerCheck = DateTime.MinValue;
    private bool _clientWasDead = false;
    private int _lastPlayerCount = 0;

    private Task? _updateLoopTask = null;
    private CancellationTokenSource? _updateLoopCTS = new();


    public AchievementsService(
        ILogger<AchievementsService> logger,
        GagspeakMediator mediator,
        ClientAchievements saveData,
        MainConfig config,
        GlobalPermissions globals,
        TraitsCache traits,
        KinksterManager pairs,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PatternManager patterns,
        AlarmManager alarms,
        TriggerManager triggers,
        AchievementEventHandler handler, 
        NotificationService notifier,
        RemoteService remoteService,
        OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _saveData = saveData;
        _config = config;
        _globals = globals;
        _traits = traits;
        _pairs = pairs;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        _handler = handler;
        _notifier = notifier;
        _remoteService = remoteService;
        _frameworkUtils = frameworkUtils;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => OnFrameworkCheck());
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ =>
        {
            _updateLoopCTS.SafeCancel();
            // if the lastUnhandled disconnect is MinValue, then we should reset the cache entirely.
            if (!ClientAchievements.HadUnhandledDC)
            {
                Logger.LogInformation("Had normal disconnect, cleaning up cache for next connection.", LoggerType.Achievements);
                ReInitializeAchievements(true);
            }
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _updateLoopCTS.SafeCancelDispose();
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("AchievementListener Starting");
        ReInitializeAchievements(true);
        _handler.SubscribeToEvents();
        Logger.LogInformation("AchievementListener Started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("AchievementListener Stopping");
        _handler.UnsubscribeFromEvents();
        return Task.CompletedTask;
    }
    public void OnServerConnection(string? connectedAchievementString)
    {
        // Process the internal process of handling the string upon connection.
        _saveData.OnConnection(connectedAchievementString);
        // if the last unhandled disconnect is any value besides MinValue, then load in that data.
        if (!ClientAchievements.HasValidData)
        {
            Logger.LogWarning("Achievement Save Data is invalid, cannot load achievements. Refusing the update.", LoggerType.Achievements);
            return;
        }

        // Otherwise we should begin the update loop.
        Logger.LogInformation("Achievement Save Data is valid, beginning save cycle.", LoggerType.Achievements);
        BeginSaveCycle();
    }

    private void BeginSaveCycle()
    {
        Logger.LogInformation("Beginning Achievement Save Cycle", LoggerType.Achievements);
        _updateLoopCTS = _updateLoopCTS.SafeCancelRecreate();
        _updateLoopTask = RunPeriodicUpdate(_updateLoopCTS!.Token);
    }


    private Task OnCompletion(int id, string title)
    {
        Logger.LogInformation("Achievement Completed: " + title, LoggerType.Achievements);
        // publish the award notification to the notification manager regardless of if we get inturrupted or not.
        _notifier.ShowCustomNotification(new()
        {
            Title = "Achievement Completed!",
            Content = "Completed: " + title,
            Type = NotificationType.Info,
            Icon = INotificationIcon.From(FAI.Award),
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(10)
        });

        Mediator.Publish(new UpdateCompletedAchievements());
        return Task.CompletedTask;
    }
    private unsafe void OnFrameworkCheck()
    {
        // Throttle to once every 5 seconds.
        var now = DateTime.UtcNow;
        if ((now - _lastCheck).TotalSeconds < 5)
            return;

        _lastCheck = now;

        var isCurrentlyDead = PlayerData.Health is 0;

        if (isCurrentlyDead && !_clientWasDead)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.ClientSlain);

        _clientWasDead = isCurrentlyDead;


        // check if in gold saucer (maybe do something better for this later.
        if (PlayerContent.TerritoryID is 144 && PlayerData.IsChocoboRacing)
        {
            var resultMenu = (AtkUnitBase*)AtkHelper.GetAddonByName("RaceChocoboResult");
            if (resultMenu != null && resultMenu->RootNode->IsVisible())
                GagspeakEventManager.AchievementEvent(UnlocksEvent.ChocoboRaceFinished);
        }

        // if 15 seconds has passed since the last player check, check the player.
        if ((now - _lastPlayerCheck).TotalSeconds < 15)
            return;

        // update player count
        _lastPlayerCheck = now;

        // we should get the current player object count that is within the range required for crowd pleaser.
        var playersInRange = _frameworkUtils.GetObjectTablePlayers()
            .Where(player => PlayerData.DistanceTo(player.Position) < 30f)
            .Count();

        if (playersInRange != _lastPlayerCount)
        {
            Logger.LogTrace("(New Update) There are " + playersInRange + " Players nearby", LoggerType.AchievementInfo);
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PlayersInProximity, playersInRange);
            _lastPlayerCount = playersInRange;
        }
    }

    private async Task RunPeriodicUpdate(CancellationToken ct)
    {
        Logger.LogInformation("Starting SaveData Update Loop", LoggerType.Achievements);
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            var minutesToNextCheck = 60;
            try
            {
                Logger.LogDebug("Achievement SaveData Update processing...", LoggerType.Achievements);

                if (!ClientAchievements.HasValidData)
                {
                    Logger.LogWarning("Had Invalid Save Data, refusing to Update.");
                    minutesToNextCheck = 5;
                }
                else if (ClientAchievements.HadUnhandledDC)
                {
                    Logger.LogWarning("Had Unhandled DC you have still not yet recovered from, refusing to Update.");
                    minutesToNextCheck = 1;
                }
                else
                {
                    Mediator.Publish(new SendAchievementData());
                    Logger.LogDebug("Achievement SaveData Update completed successfully.", LoggerType.Achievements);
                    minutesToNextCheck = random.Next(20, 31);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.LogDebug("SaveData Update loop canceled.", LoggerType.Achievements);
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unexpected error in SaveData Update loop.");
            }

            Logger.LogInformation($"Updating SaveData again in {minutesToNextCheck}minutes.");
            await Task.Delay(TimeSpan.FromMinutes(minutesToNextCheck), ct).ConfigureAwait(false);
        }
    }

    public void ReInitializeAchievements(bool invalidate)
    {
        Logger.LogInformation("Resetting achievements data.", LoggerType.Achievements);
        _saveData.ResetAchievements(invalidate);

        #region GAG MODULE
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.SelfApplied, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags Self-Applied");

        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilenceSlut, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.WatchYourTongue, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.TongueTamer, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.KinkyLibrarian, 500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.OrchestratorOfSilence, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags to Kinksters", "Applied");

        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilencedSlut, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.InDeepSilence, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.SilentObsessions, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.GoldenSilence, 500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.AKinkForDrool, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.ThePerfectGagSlut, 5000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "times by Kinksters", "Gagged");

        _saveData.AddThreshold(AchievementModuleKind.Gags, Achievements.ShushtainableResource, 3, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags Active at Once");

        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.OfVoicelessPleas, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.DefianceInSilence, 500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.MuffledResilience, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.TrainedInSubSpeech, 2500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.PublicSpeaker, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");
        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.FromCriesOfHumility, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Garbled Messages Sent");

        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.WhispersToWhimpers, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.OfMuffledMoans, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.SilentStruggler, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.QuietedCaptive, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hour Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.MessyDrooler, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.DroolingDiva, TimeSpan.FromHours(12), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hours Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.EmbraceOfSilence, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Day Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.SubjugationToSilence, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.SpeechSilverSilenceGolden, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days Gagged", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Gags, Achievements.TheKinkyLegend, TimeSpan.FromDays(14), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days Gagged", "Spent");

        _saveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SilentButDeadly, 10,
            () => _gags.ServerGagData is { } gagData && gagData.AnyGagActive(), (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Roulettes Completed");

        _saveData.AddTimedProgress(AchievementModuleKind.Gags, Achievements.ATrueGagSlut, 10, TimeSpan.FromHours(1), (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gags Received In Hour");

        _saveData.AddProgress(AchievementModuleKind.Gags, Achievements.GagReflex, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Gag Reflexes Experienced");

        _saveData.AddConditional(AchievementModuleKind.Gags, Achievements.QuietNowDear, () =>
        {
            var targetIsGagged = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == Svc.Targets.Target?.GameObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == Svc.Targets.Target?.GameObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    targetIsGagged = targetPair.LastGagData.GagSlots.Any(x => x.GagItem is not GagType.None);
                }
            }
            return targetIsGagged;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Kinkster Hushed");

        _saveData.AddConditional(AchievementModuleKind.Gags, Achievements.SilenceOfShame, () => _gags.ServerGagData?.IsGagged() ?? false, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Kinksters", "Hushed by");

        _saveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.YourFavoriteNurse, 20,
            () => _gags.ServerGagData is { } gagData && gagData.GagSlots.Any(x => x.GagItem == GagType.MedicalMask), (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Patients Serviced", reqBeginAndFinish: false);

        _saveData.AddConditionalProgress(AchievementModuleKind.Gags, Achievements.SayMmmph, 1, () => _gags.ServerGagData?.IsGagged() ?? false, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Photos Taken");
        #endregion GAG MODULE

        #region WARDROBE MODULE
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.FirstTiemers, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Applied");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.Cuffed19, 19, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Cuffs Applied");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.TheRescuer, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Unlocked");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.SelfBondageEnthusiast, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Applied");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.DiDEnthusiast, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Applied");

        _saveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe, Achievements.CrowdPleaser, 15,
            () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "People Nearby");
        _saveData.AddConditionalThreshold(AchievementModuleKind.Wardrobe, Achievements.Humiliation, 5,
            () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "GagSpeak Pairs Nearby");

        _saveData.AddTimedProgress(AchievementModuleKind.Wardrobe, Achievements.BondageBunny, 5, TimeSpan.FromHours(2), (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Received In 2 Hours");

        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.ToDyeFor, 5, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Dyed");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.DyeAnotherDay, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Dyed");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.DyeHard, 15, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restraints Dyed");

        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.RiggersFirstSession, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.MyLittlePlaything, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hour");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.SuitsYouBitch, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hours");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.TiesThatBind, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Day");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.SlaveTrainer, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.CeremonyOfEternalBondage, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days");

        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.FirstTimeBondage, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.AmateurBondage, TimeSpan.FromHours(1), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hour locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.ComfortRestraint, TimeSpan.FromHours(6), DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hours locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.YourBondageMaid, TimeSpan.FromDays(1), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Day locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.YourRubberMaid, TimeSpan.FromDays(4), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.TrainedBondageSlave, TimeSpan.FromDays(7), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.YourRubberSlut, TimeSpan.FromDays(14), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days locked up", "Spent");
        _saveData.AddDuration(AchievementModuleKind.Wardrobe, Achievements.ATrueBondageSlave, TimeSpan.FromDays(30), DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Days locked up", "Spent");

        _saveData.AddConditional(AchievementModuleKind.Wardrobe, Achievements.KinkyExplorer, () => _config.Current.CursedLootPanel, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Cursed Runs Started");
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.TemptingFatesTreasure, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Cursed Loot Discovered");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.BadEndSeeker, 25,
            () => _cursedLoot.LockChance <= 25, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.EverCursed, 100,
            () => _cursedLoot.LockChance <= 25, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Cursed Loot Discovered", reqBeginAndFinish: false);

        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.HealSlut, 1, () =>
        {
            var activeItems = 0;
            if (_gags.ServerGagData?.IsGagged() ?? false) activeItems++;
            if (_restraints.AppliedRestraint is not null) activeItems++;
            if (_remoteService.ClientIsBeingBuzzed) activeItems++;
            return activeItems >= 2;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Duties Completed");

        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.BondagePalace, 1, ()
            => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "FloorSets Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.HornyOnHigh, 1, ()
            => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "FloorSets Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.EurekaWhorethos, 1, ()
            => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "FloorSets Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.MyKinkRunsDeep, 1, ()
            => _restraints.AppliedRestraint is not null && _traits.FinalTraits != 0, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "FloorSets Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.MyKinksRunDeeper, 1, ()
            => _restraints.AppliedRestraint is not null && _traits.FinalTraits != 0, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "FloorSets Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.TrialOfFocus, 1, () =>
        {
            if (PlayerData.Level < 90)
                return false;
            return (_restraints.AppliedRestraint is not null && ArousalService.ArousalPercent > 0.5f) ? true : false;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");
        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.TrialOfDexterity, 1, () =>
        {
            if (PlayerData.Level < 90)
                return false;
            return (_restraints.AppliedRestraint is not null && (_traits.FinalTraits & Traits.BoundArms | Traits.BoundLegs) != 0) ? true : false;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");

        _saveData.AddConditionalProgress(AchievementModuleKind.Wardrobe, Achievements.TrialOfTheBlind, 1, () =>
        {
            if (PlayerData.Level < 90)
                return false;
            return (_restraints.AppliedRestraint is not null && (_traits.FinalTraits & Traits.Blindfolded) != 0) ? true : false;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Hardcore Trials Cleared");

        // While actively moving, incorrectly guess a restraint lock while gagged (Secret)
        _saveData.AddConditional(AchievementModuleKind.Wardrobe, Achievements.RunningGag, () =>
        {
            unsafe
            {
                var gameControl = FFXIVClientStructs.FFXIV.Client.Game.Control.Control.Instance();
                var movementByte = gameControl->IsWalking; // Marshal.ReadByte((nint)gameControl, 30243); If using this, its walking when 1?
                var movementDetection = AgentMap.Instance();
                var result = movementDetection->IsPlayerMoving;
                Svc.Logger.Information("IsPlayerMoving Result: " + result + " || IsWalking Byte: " + movementByte);
                return (_gags.ServerGagData?.IsGagged() ?? false) && _restraints.AppliedRestraint is not null && result && movementByte;
            }
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Funny Conditions Met");

        // Check this in the action function handler
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.AuctionedOff, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Auctioned Off", suffix: "Times");

        // Check this in the action function handler
        _saveData.AddProgress(AchievementModuleKind.Wardrobe, Achievements.SoldSlave, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Sold off in Bondage ", suffix: "Times");

        // Bondodge - Within 2 seconds of having a restraint set applied to you, remove it from yourself (might want to add a duration conditional but idk?)
        _saveData.AddTimeLimitedConditional(AchievementModuleKind.Wardrobe, Achievements.Bondodge,
            TimeSpan.FromSeconds(2), () => _restraints.AppliedRestraint is not null, DurationTimeUnit.Seconds, (id, name) => OnCompletion(id, name).ConfigureAwait(false));

        #endregion WARDROBE MODULE


        #region PUPPETEER MODULE
        // (can work both ways)
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.AnObedientPet, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Recieved", suffix: "Sit Orders");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.ControlMyBody, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.CompleteDevotion, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Granted", suffix: "Pairs Access");

        _saveData.AddTimedProgress(AchievementModuleKind.Puppeteer, Achievements.MasterOfPuppets, 10, TimeSpan.FromHours(1), (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Within the last Hour");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.KissMyHeels, 50, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Grovels");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.Ashamed, 5, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Ordered to sulk", suffix: "Times");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.HouseServant, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Ordered to sweep", suffix: "Times");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.AMaestroOfMyProperty, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Ordered", suffix: "Dances");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.OrchestratorsApprentice, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.NoStringsAttached, 25, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.PuppetMaster, 50, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.MasterOfManipulation, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.TheGrandConductor, 250, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.MaestroOfStrings, 500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.OfGrandiousSymphony, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.SovereignMaestro, 2500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.OrchestratorOfMinds, 5000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Gave", suffix: "Orders to Kinksters");

        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.WillingPuppet, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.AtYourCommand, 25, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.YourMarionette, 50, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.TheInstrument, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.AMannequinsMadness, 250, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.DevotedDoll, 500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.EnthralledDoll, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.ObedientDoll, 1750, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.ServiceDoll, 2500, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.MastersPlaything, 5000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.MistressesPlaything, 5000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        _saveData.AddProgress(AchievementModuleKind.Puppeteer, Achievements.ThePerfectDoll, 10000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Puppeteered", suffix: "Times");
        #endregion PUPPETEER MODULE

        #region TOYBOX MODULE
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.MyPleasantriesForAll, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.DeviousComposer, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Published", suffix: "Patterns");

        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.TasteOfTemptation, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.SeekerOfSensations, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.CravingPleasure, 30, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Downloaded", suffix: "Patterns");

        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.GoodVibes, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.DelightfulPleasures, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.PatternLover, 25, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.SensualConnoisseur, 50, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.PassionateAdmirer, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Liked", suffix: "Patterns");

        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.ALittleTease, TimeSpan.FromSeconds(20), DurationTimeUnit.Seconds, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Seconds", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.ShortButSweet, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.TemptingRythms, TimeSpan.FromMinutes(2), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.MyBuildingDesire, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.WithWavesOfSensation, TimeSpan.FromMinutes(10), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.WithHeightenedSensations, TimeSpan.FromMinutes(15), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.MusicalMoaner, TimeSpan.FromMinutes(20), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.StimulatingExperiences, TimeSpan.FromMinutes(30), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.EnduranceKing, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");
        _saveData.AddDuration(AchievementModuleKind.Toybox, Achievements.EnduranceQueen, TimeSpan.FromMinutes(59), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Vibrated for");

        _saveData.AddConditional(AchievementModuleKind.Toybox, Achievements.CollectorOfSinfulTreasures, () =>
        { return (_globals.Current?.HasValidShareCode() ?? false) || _remoteService.ClientIsBeingBuzzed; }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Devices Connected");

        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Toybox, Achievements.MotivationForRestoration, TimeSpan.FromMinutes(30),
            () => _patterns.ActivePattern is not null, DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), suffix: " Vibrated in Diadem");

        _saveData.AddConditional(AchievementModuleKind.Toybox, Achievements.VulnerableVibrations, () => _patterns.ActivePattern is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Staggers Performed");

        _saveData.AddConditional(AchievementModuleKind.Toybox, Achievements.KinkyGambler,
            () => _triggers.Storage.Social.Count() > 0, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "DeathRolls Gambled");

        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.SubtleReminders, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Triggers Fired");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.LostInTheMoment, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Triggers Fired");
        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.TriggerHappy, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Triggers Fired");

        _saveData.AddProgress(AchievementModuleKind.Toybox, Achievements.HornyMornings, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Alarms Went Off");
        #endregion TOYBOX MODULE

        #region HARDCORE MODULE
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.AllTheCollarsOfTheRainbow, 20, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Forced", suffix: "Pairs To Follow You");
        _saveData.AddConditionalProgress(AchievementModuleKind.Hardcore, Achievements.UCanTieThis, 1,
            () => _globals.Current?.HcFollowState() ?? false, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Completed", suffix: "Duties in ForcedFollow.");

        // Forced follow achievements
        _saveData.AddDuration(AchievementModuleKind.Hardcore, Achievements.ForcedFollow, TimeSpan.FromMinutes(1), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");
        _saveData.AddDuration(AchievementModuleKind.Hardcore, Achievements.ForcedWalkies, TimeSpan.FromMinutes(5), DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Minutes", "Leashed a Kinkster for");

        // Time for Walkies achievements
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.TimeForWalkies, TimeSpan.FromMinutes(1), () => _globals.Current?.HcFollowState() ?? false,
            DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Leashed", "Spent");
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.GettingStepsIn, TimeSpan.FromMinutes(5), () => _globals.Current?.HcFollowState() ?? false,
            DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Leashed", "Spent");
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.WalkiesLover, TimeSpan.FromMinutes(10), () => _globals.Current?.HcFollowState() ?? false,
            DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Leashed", "Spent");

        //Part of the Furniture - Be forced to sit for 1 hour or more
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.LivingFurniture, TimeSpan.FromHours(1), () => _globals.Current?.HcEmoteIsAnySitting() ?? false,
            DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), suffix: "Forced to Sit");
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.WalkOfShame, TimeSpan.FromMinutes(5),
        () =>
        {
            if (_restraints.AppliedRestraint is not null && (_traits.FinalTraits & Traits.Blindfolded) != 0 && (_globals.Current?.HcFollowState() ?? false))
                if (PlayerContent.InMainCity)
                    return true;
            return false;
        }, DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Walked for", suffix: "In a Major City");

        _saveData.AddConditional(AchievementModuleKind.Hardcore, Achievements.BlindLeadingTheBlind,
            () =>
            {
                // I still dont know how to fetch kinkster info reliably yet.

                // This is temporarily impossible until i can make fetching active traits from pairs less cancer to handle.
                /*                if ((_traits.FinalTraits & Traits.Blindfolded) != 0)
                                    if (_pairs.DirectPairs.Any(x => x.PairGlobals.IsFollowing() && x.LastLightStorage.IsBlindfolded()))
                                        return true;*/
                return false;
            }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Blind Pairs Led");

        _saveData.AddConditional(AchievementModuleKind.Hardcore, Achievements.WhatAView, () => (_traits.FinalTraits & Traits.Blindfolded) != 0, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Blind Lookouts Performed");

        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.WhoNeedsToSee, TimeSpan.FromHours(3), () => (_traits.FinalTraits & Traits.Blindfolded) != 0,
        DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Blindfolded for");

        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.OfDomesticDiscipline, TimeSpan.FromMinutes(30), () => (_globals.Current?.HcStayState() ?? false),
            DurationTimeUnit.Minutes, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Locked away for");
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.HomeboundSubmission, TimeSpan.FromHours(1), () => (_globals.Current?.HcStayState() ?? false),
            DurationTimeUnit.Hours, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Locked away for");
        _saveData.AddRequiredTimeConditional(AchievementModuleKind.Hardcore, Achievements.PerfectHousePet, TimeSpan.FromDays(1), () => (_globals.Current?.HcStayState() ?? false),
            DurationTimeUnit.Days, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Locked away for");

        // Shock-related achievements - Give out shocks
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.IndulgingSparks, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Sent");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ShockingTemptations, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Sent");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.TheCrazeOfShockies, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Sent");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.WickedThunder, 10000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Sent");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ElectropeHasNoLimits, 25000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Sent");

        // Shock-related achievements - Get shocked
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ElectrifyingPleasure, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ShockingExperience, 100, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.WiredForObedience, 1000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ShockAddiction, 10000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.SlaveToTheShock, 25000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        _saveData.AddProgress(AchievementModuleKind.Hardcore, Achievements.ShockSlut, 50000, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Shocks Received");
        #endregion HARDCORE MODULE

        #region REMOTES MODULE
        _saveData.AddProgress(AchievementModuleKind.Remotes, Achievements.JustVibing, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Remotes Opened");

        _saveData.AddProgress(AchievementModuleKind.Remotes, Achievements.DontKillMyVibe, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Vibes Killed");

        _saveData.AddProgress(AchievementModuleKind.Remotes, Achievements.VibingWithFriends, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Rooms Joined");
        #endregion REMOTES MODULE



        #region GENERIC MODULE
        _saveData.AddProgress(AchievementModuleKind.Generic, Achievements.TutorialComplete, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Tutorial Completed");

        _saveData.AddConditional(AchievementModuleKind.Generic, Achievements.KinkyNovice, () => _pairs.DirectPairs.Count > 0, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Pair Added");

        _saveData.AddProgress(AchievementModuleKind.Generic, Achievements.TheCollector, 20, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Pairs Added");

        _saveData.AddProgress(AchievementModuleKind.Generic, Achievements.BoundaryRespecter, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Presets Applied");

        _saveData.AddProgress(AchievementModuleKind.Generic, Achievements.HelloKinkyWorld, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Global Messages Sent");

        _saveData.AddProgress(AchievementModuleKind.Generic, Achievements.KnowsMyLimits, 1, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Safewords Used");
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.WarriorOfLewd, 1,
            () => (_gags.ServerGagData?.IsGagged() ?? false) && _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), suffix: "Cutscenes Watched Bound & Gagged");

        _saveData.AddConditional(AchievementModuleKind.Generic, Achievements.EscapingIsNotEasy,
            () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Escape Attempts Made");

        _saveData.AddConditional(AchievementModuleKind.Generic, Achievements.ICantBelieveYouveDoneThis,
            () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Slaps Received");

        _saveData.AddConditional(AchievementModuleKind.Generic, Achievements.WithAKissGoodbye, () =>
        {
            var targetIsImmobile = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == Svc.Targets.Target?.GameObjectId))
            {
                Logger.LogTrace("Target is visible in the pair manager, checking if they are gagged.", LoggerType.Achievements);
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == Svc.Targets.Target?.GameObjectId);
                if (targetPair is not null)
                {
                    Logger.LogTrace("Target is in the direct pairs, checking if they are gagged.", LoggerType.Achievements);
                    // store if they are stuck emoting.
                    targetIsImmobile = !targetPair.PairGlobals.ForcedEmoteState.IsNullOrWhitespace();
                    // TODO:
                    // we can add restraint trait alternatives later, but wait until later when we restructure how we manage pair information.
                }
            }
            return targetIsImmobile;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Dotes to Helpless Kinksters", "Gave");

        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ProlificPetter, 10, () =>
        {
            var targetIsImmobile = false;
            if (_pairs.GetVisiblePairGameObjects().Any(x => x.GameObjectId == Svc.Targets.Target?.GameObjectId))
            {
                var targetPair = _pairs.DirectPairs.FirstOrDefault(x => x.VisiblePairGameObject?.GameObjectId == Svc.Targets.Target?.GameObjectId);
                if (targetPair is not null)
                {
                    // store if they are stuck emoting.
                    targetIsImmobile = !targetPair.PairGlobals.ForcedEmoteState.IsNullOrWhitespace();
                    // TODO:
                    // we can add restraint trait alternatives later, but wait until later when we restructure how we manage pair information.
                }
            }
            return targetIsImmobile;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Helpless Kinksters", "Pet", false);

        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.EscapedPatient, 10, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundToKill, 25, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.TheShackledSlayer, 50, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.DangerousConvict, 100, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.OfUnyieldingForce, 200, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.StimulationOverdrive, 300, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.BoundYetUnbroken, 400, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        _saveData.AddConditionalProgress(AchievementModuleKind.Generic, Achievements.ChainsCantHoldMe, 500, () => PlayerData.IsInPvP && (_restraints.AppliedRestraint is not null || _remoteService.ClientIsBeingBuzzed),
            (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Frontline Players Slain", "", false);
        #endregion GENERIC MODULE

        #region SECRETS MODULE
        _saveData.AddProgress(AchievementModuleKind.Secrets, Achievements.HiddenInPlainSight, 7, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Found", suffix: "Easter Eggs", isSecret: true);

        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.Experimentalist, () =>
        {
            return _gags.ServerGagData is { } gags && gags.IsGagged() && _restraints.AppliedRestraint is not null && _patterns.ActivePattern is not null && _triggers.ActiveTriggers.Count() > 0 && _alarms.ActiveAlarms.Count() > 0 && _remoteService.ClientIsBeingBuzzed;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Conditions", isSecret: true);

        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.HelplessDamsel, () =>
        {
            return _gags.ServerGagData is { } gags && gags.IsGagged() && _restraints.AppliedRestraint is not null && _remoteService.ClientIsBeingBuzzed && _pairs.DirectPairs.Any(x => x.OwnPerms.InHardcore)
            && _globals.Current is { } globals && (!globals.ForcedFollow.IsNullOrWhitespace() || !globals.ForcedEmoteState.IsNullOrWhitespace());
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Met", suffix: "Hardcore Conditions", isSecret: true);

        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.GaggedPleasure, () => _remoteService.ClientIsBeingBuzzed && _gags.ServerGagData is { } gags && gags.IsGagged(), (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Pleasure Requirements Met", isSecret: true);
        _saveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.BondageClub, 8, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Club Members Gathered", isSecret: true);
        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BadEndHostage, () => _restraints.AppliedRestraint is not null && PlayerData.IsDead, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Encountered", suffix: "Bad Ends", isSecret: true);
        _saveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.TourDeBound, 11, () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Taken", suffix: "Tours in Bondage", isSecret: true);
        _saveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.MuffledProtagonist, 1, () => _gags.ServerGagData is { } gags && gags.IsGagged() && _globals.Current is { } globals && globals.ChatGarblerActive, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "MissTypes Made", isSecret: true);
        // The above is currently non functional as i dont have the data to know which chat message type contains these request tasks.

        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.BoundgeeJumping, () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), prefix: "Attempted", suffix: "Dangerous Acts", isSecret: true);
        _saveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyTeacher, 10, () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        _saveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyProfessor, 50, () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        _saveData.AddConditionalProgress(AchievementModuleKind.Secrets, Achievements.KinkyMentor, 100, () => _restraints.AppliedRestraint is not null, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Thanks Received", reqBeginAndFinish: false, isSecret: true);
        _saveData.AddThreshold(AchievementModuleKind.Secrets, Achievements.ExtremeBondageEnjoyer, 10, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Restriction Conditions Satisfied", isSecret: true);
        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.WildRide, () =>
        {
            var raceEndVisible = false;
            unsafe
            {
                var raceEnded = (AtkUnitBase*)AtkHelper.GetAddonByName("RaceChocoboResult");
                if (raceEnded != null)
                    raceEndVisible = raceEnded->RootNode->IsVisible();
            }
            ;
            return PlayerData.IsChocoboRacing && raceEndVisible && _restraints.AppliedRestraint is not null;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Races Won In Unusual Conditions", isSecret: true);

        _saveData.AddConditional(AchievementModuleKind.Secrets, Achievements.SlavePresentation, () =>
        {
            return _gags.ServerGagData is { } gags && gags.IsGagged() && _restraints.AppliedRestraint is not null;
        }, (id, name) => OnCompletion(id, name).ConfigureAwait(false), "Presentations Given on Stage", isSecret: true);
        #endregion SECRETS MODULE
    }
}
