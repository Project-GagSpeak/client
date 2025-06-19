using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GagSpeak.CkCommons;
using GagSpeak.GameInternals;
using GagSpeak.GameInternals.Addons;
using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.PlayerClient;

/// <summary>
///     The listener hears updates called to the GagspeakEventManager and updates the achievements accordingly.
/// </summary>
/// <remarks> Additionally, it listens for <see cref="OnServerConnection(string?)"/> to initialize the save data.</remarks>
public partial class AchievementListener : DisposableMediatorSubscriberBase
{
    private readonly ClientAchievements _saveData;
    private readonly PairManager _pairs;
    private readonly GagspeakEventManager _events;
    private readonly GagRestrictionManager _gags;
    private readonly RestraintManager _restraints;
    private readonly OnFrameworkService _frameworkUtils;

    private DateTime _lastCheck = DateTime.MinValue;
    private DateTime _lastPlayerCheck = DateTime.MinValue;
    private bool _clientWasDead = false;
    private int _lastPlayerCount = 0;

    private Task? _updateLoopTask = null;
    private CancellationTokenSource? _updateLoopCTS = new();

    public AchievementListener(ILogger<AchievementListener> logger, GagspeakMediator mediator,
        ClientAchievements saveData, PairManager pairs, GagspeakEventManager events,
        GagRestrictionManager gags, RestraintManager restraints, OnFrameworkService frameworkUtils)
        : base(logger, mediator)
    {
        _saveData = saveData;
        _pairs = pairs;
        _events = events;
        _gags = gags;
        _restraints = restraints;
        _frameworkUtils = frameworkUtils;

        SubscribeToEvents();
        BeginSaveCycle();

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => OnFrameworkCheck());
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ => _updateLoopCTS?.Cancel());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        UnsubscribeFromEvents();
        _updateLoopCTS?.CancelDispose();
    }

    private void BeginSaveCycle()
    {
        Logger.LogInformation("Beginning Achievement Save Cycle", LoggerType.Achievements);
        _updateLoopCTS = _updateLoopCTS?.CancelRecreate();
        _updateLoopTask = RunPeriodicUpdate(_updateLoopCTS!.Token);
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

    #region Framework Checks
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
        if (PlayerContent.TerritoryID is 144)
        {
            // Check Chocobo Racing Achievement.
            if (PlayerData.IsChocoboRacing)
            {
                var resultMenu = (AtkUnitBase*)AtkHelper.GetAddonByName("RaceChocoboResult");
                if (resultMenu != null)
                {
                    if (resultMenu->RootNode->IsVisible())
                        GagspeakEventManager.AchievementEvent(UnlocksEvent.ChocoboRaceFinished);
                }
            }
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
    #endregion Framework Checks


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

    private void SubscribeToEvents()
    {
        Logger.LogInformation("Player Logged In, Subscribing to Events!");
        _events.Subscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _events.Subscribe<int, GagType, bool, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _events.Subscribe<int, Padlocks, bool, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _events.Subscribe<int, Padlocks, bool, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _events.Subscribe(UnlocksEvent.GagUnlockGuessFailed, () => (ClientAchievements.SaveData[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _events.Subscribe<Guid, bool, string>(UnlocksEvent.RestrictionStateChange, OnRestrictionStateChange); // Apply on US
        _events.Subscribe<Guid, bool, string, string>(UnlocksEvent.PairRestrictionStateChange, OnPairRestrictionStateChange);
        _events.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestrictionLockStateChange, OnRestrictionLock); // Lock on US
        _events.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestrictionLockStateChange, OnPairRestrictionLockChange);


        _events.Subscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _events.Subscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _events.Subscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _events.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _events.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);

        _events.Subscribe(UnlocksEvent.SoldSlave, () => (ClientAchievements.SaveData[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.AuctionedOff, () => (ClientAchievements.SaveData[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _events.Subscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _events.Subscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _events.Subscribe<int>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _events.Subscribe<PuppetPerms>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _events.Subscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _events.Subscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _events.Subscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _events.Subscribe(UnlocksEvent.DeathRollCompleted, () => (ClientAchievements.SaveData[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<bool>(UnlocksEvent.AlarmToggled, _ => (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe(UnlocksEvent.ShockSent, OnShockSent);
        _events.Subscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _events.Subscribe<HardcoreSetting, bool, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

        _events.Subscribe(UnlocksEvent.RemoteOpened, () => (ClientAchievements.SaveData[Achievements.JustVibing.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.VibeRoomCreated, () => (ClientAchievements.SaveData[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe<bool>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _events.Subscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _events.Subscribe(UnlocksEvent.ClientSlain, () => (ClientAchievements.SaveData[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe(UnlocksEvent.ClientOneHp, () => (ClientAchievements.SaveData[Achievements.BoundgeeJumping.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<InputChannel>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _events.Subscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _events.Subscribe(UnlocksEvent.TutorialCompleted, () => (ClientAchievements.SaveData[Achievements.TutorialComplete.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _events.Subscribe(UnlocksEvent.PresetApplied, () => (ClientAchievements.SaveData[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.GlobalSent, () => (ClientAchievements.SaveData[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _events.Subscribe(UnlocksEvent.ChocoboRaceFinished, () => (ClientAchievements.SaveData[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (ClientAchievements.SaveData[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _events.Subscribe(UnlocksEvent.CutsceneInturrupted, () => (ClientAchievements.SaveData[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Mediator.Subscribe<PlayerLatestActiveItems>(this, (msg) => OnCharaOnlineCleanupForLatest(msg.User, msg.GagsInfo, msg.RestrictionsInfo, msg.RestraintInfo));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, _ => OnPairVisible());
        Mediator.Subscribe<CommendationsIncreasedMessage>(this, (msg) => OnCommendationsGiven(msg.amount));
        Mediator.Subscribe<PlaybackStateToggled>(this, (msg) => (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());

        Mediator.Subscribe<SafewordUsedMessage>(this, _ => (ClientAchievements.SaveData[Achievements.KnowsMyLimits.Id] as ProgressAchievement)?.IncrementProgress());

        Mediator.Subscribe<GPoseStartMessage>(this, _ => (ClientAchievements.SaveData[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.BeginConditionalTask());
        Mediator.Subscribe<GPoseEndMessage>(this, _ => (ClientAchievements.SaveData[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.FinishConditionalTask());
        Mediator.Subscribe<CutsceneBeginMessage>(this, _ => (ClientAchievements.SaveData[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.BeginConditionalTask()); // starts Timer
        Mediator.Subscribe<CutsceneEndMessage>(this, _ => (ClientAchievements.SaveData[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.FinishConditionalTask()); // ends/completes progress.

        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => CheckOnZoneSwitchStart(msg.prevZone));
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, _ => CheckOnZoneSwitchEnd());

        Svc.ClientState.ClassJobChanged += OnJobChange;
        Svc.DutyState.DutyStarted += OnDutyStart;
        Svc.DutyState.DutyCompleted += OnDutyEnd;
    }

    private void UnsubscribeFromEvents()
    {
        _events.Unsubscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _events.Unsubscribe<int, GagType, bool, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _events.Unsubscribe<int, Padlocks, bool, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _events.Unsubscribe<int, Padlocks, bool, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _events.Unsubscribe(UnlocksEvent.GagUnlockGuessFailed, () => (ClientAchievements.SaveData[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _events.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestrictionStateChange, OnRestrictionStateChange); // Apply on US
        _events.Unsubscribe<Guid, bool, string, string>(UnlocksEvent.PairRestrictionStateChange, OnPairRestrictionStateChange);
        _events.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestrictionLockStateChange, OnRestrictionLock); // Lock on US
        _events.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestrictionLockStateChange, OnPairRestrictionLockChange);

        _events.Unsubscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _events.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _events.Unsubscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _events.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _events.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);
        _events.Unsubscribe(UnlocksEvent.SoldSlave, () => (ClientAchievements.SaveData[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.AuctionedOff, () => (ClientAchievements.SaveData[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _events.Unsubscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _events.Unsubscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _events.Unsubscribe<int>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _events.Unsubscribe<PuppetPerms>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _events.Unsubscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _events.Unsubscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _events.Unsubscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _events.Unsubscribe(UnlocksEvent.DeathRollCompleted, () => (ClientAchievements.SaveData[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<bool>(UnlocksEvent.AlarmToggled, _ => (ClientAchievements.SaveData[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.ShockSent, OnShockSent);
        _events.Unsubscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _events.Unsubscribe<HardcoreSetting, bool, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

        _events.Unsubscribe(UnlocksEvent.RemoteOpened, () => (ClientAchievements.SaveData[Achievements.JustVibing.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.VibeRoomCreated, () => (ClientAchievements.SaveData[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe<bool>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _events.Unsubscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _events.Unsubscribe(UnlocksEvent.ClientSlain, () => (ClientAchievements.SaveData[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<InputChannel>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _events.Unsubscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _events.Unsubscribe(UnlocksEvent.TutorialCompleted, () => (ClientAchievements.SaveData[Achievements.TutorialComplete.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _events.Unsubscribe(UnlocksEvent.PresetApplied, () => (ClientAchievements.SaveData[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.GlobalSent, () => (ClientAchievements.SaveData[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _events.Unsubscribe(UnlocksEvent.ChocoboRaceFinished, () => (ClientAchievements.SaveData[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (ClientAchievements.SaveData[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _events.Unsubscribe(UnlocksEvent.CutsceneInturrupted, () => (ClientAchievements.SaveData[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Svc.ClientState.ClassJobChanged -= OnJobChange;
        Svc.DutyState.DutyStarted -= OnDutyStart;
        Svc.DutyState.DutyCompleted -= OnDutyEnd;
    }
}
