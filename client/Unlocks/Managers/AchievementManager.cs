using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.User;

// if present in diadem /(https://github.com/Infiziert90/DiademCalculator/blob/d74a22c58840a864cda12131fe2646dfc45209df/DiademCalculator/Windows/Main/MainWindow.cs#L12)

namespace GagSpeak.Achievements;
public partial class AchievementManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _mainHub;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _playerData;
    private readonly PairManager _pairManager;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly CosmeticService _cosmetics;
    private readonly VibratorService _vibeService;
    private readonly UnlocksEventManager _eventManager;
    private readonly INotificationManager _notify;
    private readonly IDutyState _dutyState;

    // Token used for updating achievement data.
    private CancellationTokenSource? _saveDataUpdateCTS;
    private CancellationTokenSource? _achievementCompletedCTS;
    // Stores the last time we disconnected via an exception. Controlled via HubFactory/ApiController.
    public static DateTime _lastDisconnectTime = DateTime.MinValue;
    public static bool HadFailedAchievementDataLoad { get; private set; } = false;

    // Dictates if our connection occurred after an exception (within 5 minutes).
    private bool _reconnectedAfterException => DateTime.UtcNow - _lastDisconnectTime < TimeSpan.FromMinutes(5);
    public AchievementManager(ILogger<AchievementManager> logger, GagspeakMediator mediator, MainHub mainHub,
        ClientConfigurationManager clientConfigs, PlayerCharacterData playerData, PairManager pairManager,
        ClientMonitorService clientService, OnFrameworkService frameworkUtils, CosmeticService cosmetics, 
        VibratorService vibeService, UnlocksEventManager eventManager, INotificationManager notifs, 
        IDutyState dutyState) : base(logger, mediator)
    {
        _mainHub = mainHub;
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _pairManager = pairManager;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;
        _cosmetics = cosmetics;
        _vibeService = vibeService;
        _eventManager = eventManager;
        _notify = notifs;
        _dutyState = dutyState;

        SaveData = new AchievementSaveData();
        Logger.LogInformation("Initializing Achievement Save Data Achievements", LoggerType.Achievements);
        InitializeAchievements();
        Logger.LogInformation("Achievement Save Data Initialized", LoggerType.Achievements);

        // Check for when we are connected to the server, use the connection DTO to load our latest stored save data.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => OnConnection());
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ => _saveDataUpdateCTS?.Cancel());

        // initial subscribe
        SubscribeToEvents();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _saveDataUpdateCTS?.Cancel();
        _saveDataUpdateCTS?.Dispose();

        UnsubscribeFromEvents();
    }

    public static AchievementSaveData SaveData { get; private set; } = new();

    public static int Total => SaveData.Achievements.Count;
    public static int Completed => SaveData.Achievements.Values.Count(a => a.IsCompleted);
    public static List<AchievementBase> AllBase => SaveData.Achievements.Values.Cast<AchievementBase>().ToList();
    public static List<AchievementBase> CompletedAchievements => SaveData.Achievements.Values.Where(a => a.IsCompleted).ToList();
    public static string GetTitleById(int id) => SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)?.Title ?? "No Title Set";
    public static bool TryGetAchievement(int id, out AchievementBase achievement)
    {
        achievement = SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)!;
        return achievement is not null;
    }

    public static List<AchievementBase> GetAchievementForModule(AchievementModuleKind module)
        => SaveData.Achievements.Values.Where(a => a.Module == module).ToList();

    /// <summary>
    /// Fired whenever we complete an achievement.
    /// </summary>
    public async Task WasCompleted(int id, string title)
    {
        // do anything we need to with the ID here, such as updating the list, our profile, ext.
        Logger.LogInformation("Achievement Completed: " + title, LoggerType.Achievements);
        // publish the award notification to the notification manager.
        _notify.AddNotification(new Notification()
        {
            Title = "Achievement Completed!",
            Content = "Completed: " + title,
            Type = NotificationType.Info,
            Icon = INotificationIcon.From(FontAwesomeIcon.Award),
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(10)
        });

        // Cancel the previous timer if it exists
        _achievementCompletedCTS?.Cancel();
        _achievementCompletedCTS = new CancellationTokenSource();

        // Set a timer to update the KinkPlate data after 30 seconds
        try
        {
            // Wait for 5 seconds or until the task is cancelled
            Logger.LogInformation("Waiting 5 seconds before updating KinkPlate™ with new Achievement Completion", LoggerType.Achievements);
            await Task.Delay(TimeSpan.FromSeconds(5), _achievementCompletedCTS.Token);
            var profile = await _mainHub.UserGetKinkPlate(new UserDto(MainHub.PlayerUserData)).ConfigureAwait(false);
            if (profile != null)
            {
                profile.Info.CompletedAchievementsTotal = Completed;
                // Update the KinkPlate info
                await _mainHub.UserSetKinkPlate(new(MainHub.PlayerUserData, profile.Info, profile.ProfilePictureBase64));
                Logger.LogInformation("Updated KinkPlate™ with latest achievement count total. with new Achievement Completion", LoggerType.Achievements);
            }
        }
        catch (TaskCanceledException)
        {
            // The task was canceled, which means another achievement was completed within the 30-second window
            Logger.LogInformation("Achievement Completed Timer was canceled due to another achievement being completed.", LoggerType.Achievements);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to update KinkPlate™ with latest achievement count total. with new Achievement Completion: " + ex);
        }
    }

    /// <summary>
    /// Updater that sends our latest achievement Data to the server every 30 minutes.
    /// </summary>
    private async Task AchievementDataPeriodicUpdate(CancellationToken ct)
    {
        Logger.LogInformation("Starting SaveData Update Loop", LoggerType.Achievements);
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            Logger.LogInformation("SaveData Update Task is running", LoggerType.Achievements);
            try
            {
                // wait randomly between 4 and 16 seconds before sending the data.
                // The 4 seconds gives us enough time to buffer any disconnects that interrupt connection.
                await Task.Delay(TimeSpan.FromSeconds(random.Next(4, 16)), ct).ConfigureAwait(false);
                if (HadFailedAchievementDataLoad)
                {
                    // cancel the token and exit the loop.
                    Logger.LogWarning("Failed to load Achievement Data from server. Cancelling SaveData Update Loop.");
                    _saveDataUpdateCTS?.Cancel();
                    return;
                }
                else
                {
                    await SendUpdatedDataToServer();
                }
            }
            catch (Exception)
            {
                Logger.LogDebug("Not sending Achievement SaveData due to disconnection canceling the loop.");
            }

            int delayMinutes = random.Next(20, 31); // Random delay between 20 and 30 minutes
            Logger.LogInformation("SaveData Update Task Completed, Firing Again in " + delayMinutes + " Minutes");
            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), ct).ConfigureAwait(false);
        }
    }

    public Task ResetAchievementData()
    {
        // Reset SaveData
        SaveData = new AchievementSaveData();
        InitializeAchievements();
        Logger.LogInformation("Reset Achievement Data Completely!", LoggerType.Achievements);
        // Send this off to the server.
        SendUpdatedDataToServer();
        return Task.CompletedTask;
    }

    // Your existing method to send updated data to the server
    private Task SendUpdatedDataToServer()
    {
        var saveDataString = GetSaveDataDtoString();
        // Logic to send base64Data to the server
        Logger.LogInformation("Sending updated achievement data to the server", LoggerType.Achievements);
        _mainHub.UserUpdateAchievementData(new((MainHub.PlayerUserData), saveDataString)).ConfigureAwait(false);
        Mediator.Publish(new AchievementDataUpdateMessage(saveDataString));
        return Task.CompletedTask;
    }

    public static string GetSaveDataDtoString()
    {
        // get the Dto-Ready data object of our saveData
        LightSaveDataDto saveDataDto = SaveData.ToLightSaveDataDto();

        // condense it into the json and compress it.
        string json = SaveDataSerialize(saveDataDto);
        var compressed = json.Compress(6);
        string base64Data = Convert.ToBase64String(compressed);
        return base64Data;
    }

    private void LoadSaveDataDto(string Base64saveDataToLoad)
    {
        try
        {
            Logger.LogDebug("Client Connected To Server with valid Achievement Data, loading in now!");
            var bytes = Convert.FromBase64String(Base64saveDataToLoad);
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            LightSaveDataDto item = SaveDataDeserialize(decompressed) ?? throw new Exception("Failed to deserialize.");

            // Update the local achievement data
            SaveData.LoadFromLightSaveDataDto(item);
            Logger.LogInformation("Achievement Data Loaded from Server", LoggerType.Achievements);
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to load Achievement Data from server. Setting [HadFailedAchievementDataLoad] to true, " +
                "preventing any further uploads to keep your old data intact. If you wish to pull a manual reset and do not" +
                "believe this to be a bug, press the reset button, then reconnect.\n[REASON]: " + ex.Message);
            HadFailedAchievementDataLoad = true;
        }
    }

    private void OnConnection()
    {
        // if our connection dto is null, return.
        if (MainHub.ConnectionDto is null)
        {
            Logger.LogError("Connection DTO is null. Cannot proceed with AchievementManager Service.", LoggerType.Achievements);
            return;
        }

        if (_reconnectedAfterException)
        {
            Logger.LogInformation("Our Last Disconnect was due to an exception, loading from stored SaveData instead.", LoggerType.Achievements);
            // May cause some bugs, fiddle around with it if it does.
            _lastDisconnectTime = DateTime.MinValue;
        }
        else
        {
            if (!string.IsNullOrEmpty(MainHub.ConnectionDto.UserAchievements))
            {
                Logger.LogInformation("Loading in AchievementData from ConnectionDto", LoggerType.Achievements);
                LoadSaveDataDto(MainHub.ConnectionDto.UserAchievements);
            }
            else
            {
                Logger.LogInformation("User has empty achievement Save Data. Creating new Save Data.", LoggerType.Achievements);
                SaveData = new AchievementSaveData();
                InitializeAchievements();
            }
        }

        // Begin the save cycle.
        _saveDataUpdateCTS?.Cancel();
        _saveDataUpdateCTS?.Dispose();
        _saveDataUpdateCTS = new CancellationTokenSource();
        _ = AchievementDataPeriodicUpdate(_saveDataUpdateCTS.Token);
    }

    private static string SaveDataSerialize(LightSaveDataDto lightSaveDataDto)
    {
        // Ensure to set the version and include all necessary properties.
        JObject saveDataJsonObject = new JObject
        {
            ["Version"] = lightSaveDataDto.Version,
            ["LightAchievementData"] = JArray.FromObject(lightSaveDataDto.LightAchievementData),
            ["EasterEggIcons"] = JObject.FromObject(lightSaveDataDto.EasterEggIcons),
            ["VisitedWorldTour"] = JObject.FromObject(lightSaveDataDto.VisitedWorldTour)
        };

        // Convert JObject to formatted JSON string
        return saveDataJsonObject.ToString(Formatting.Indented);
    }

    private static LightSaveDataDto SaveDataDeserialize(string jsonString)
    {
        // Parse the JSON string into a JObject
        JObject saveDataJsonObject = JObject.Parse(jsonString);

        // Extract and validate the version
        int version = saveDataJsonObject["Version"]?.Value<int>() ?? 3;

        // Apply migrations based on the version number
        if (version < 3)
        {
            // Example migration: Update structure for version 1 to version 2
            MigrateToVersion3(saveDataJsonObject);
            // update verion to 3
            version = 3;
        }

        // Extract and validate LightAchievementData
        JArray lightAchievementDataArray = saveDataJsonObject["LightAchievementData"] as JArray ?? new JArray();
        List<LightAchievement> lightAchievementDataList = new List<LightAchievement>();

        foreach (JObject achievement in lightAchievementDataArray)
        {
            int achievementId = achievement["AchievementId"]?.Value<int>() ?? 0;

            // Check and correct achievement data against AchievementMap
            if (!Achievements.AchievementMap.ContainsKey(achievementId))
            {
                StaticLogger.Logger.LogError("For some reason, your stored achievement ID ["+ achievementId + "] doesn't exist in the AchievementMap. Skipping over it.");
                continue; // Skip over achievements that failed to load.

            }

            // get the achievement based on the ID.
            if(TryGetAchievement(achievementId, out var achievementData))
            {
                var lightAchievement = new LightAchievement
                {
                    Type = achievementData.GetAchievementType(),
                    AchievementId = achievementData.AchievementId,
                    IsCompleted = achievement["IsCompleted"]?.Value<bool>() ?? false,
                    Progress = achievement["Progress"]?.Value<int>() ?? 0,
                    ConditionalTaskBegun = achievement["ConditionalTaskBegun"]?.Value<bool>() ?? false,
                    StartTime = achievement["StartTime"]?.Value<DateTime>() ?? DateTime.MinValue,
                    RecordedDateTimes = achievement["RecordedDateTimes"]?.ToObject<List<DateTime>>() ?? new List<DateTime>(),
                    ActiveItems = achievement["ActiveItems"]?.ToObject<List<TrackedItem>>() ?? new List<TrackedItem>()
                };

/*                // log a string that shows each active items key, and its respective values item and apply time.
                if(lightAchievement.Type is AchievementType.Duration && lightAchievement.ActiveItems != new List<TrackedItem>())
                    StaticLogger.Logger.LogDebug("ActiveItems:" + string.Join(", ", lightAchievement.ActiveItems.Select(x => x.Item + " (" + x.UIDAffected + " - " + x.TimeAdded + ")")));*/

                lightAchievementDataList.Add(lightAchievement);
            }
            else
            {
                StaticLogger.Logger.LogError("Failed to load Achievement with ID: " + achievementId);
            }

        }

        // Extract and validate EasterEggIcons
        JObject easterEggIconsObject = saveDataJsonObject["EasterEggIcons"] as JObject ?? new JObject();
        Dictionary<string, bool> easterEggIcons = easterEggIconsObject.ToObject<Dictionary<string, bool>>() ?? new Dictionary<string, bool>();

        // Extract and validate VisitedWorldTour
        JObject visitedWorldTourObject = saveDataJsonObject["VisitedWorldTour"] as JObject ?? new JObject();
        Dictionary<ushort, bool> visitedWorldTour = visitedWorldTourObject.ToObject<Dictionary<ushort, bool>>() ?? new Dictionary<ushort, bool>();

        // Create and return the LightSaveDataDto object
        LightSaveDataDto lightSaveDataDto = new LightSaveDataDto
        {
            Version = version,
            LightAchievementData = lightAchievementDataList,
            EasterEggIcons = easterEggIcons,
            VisitedWorldTour = visitedWorldTour
        };

        return lightSaveDataDto;
    }

    private static void MigrateToVersion3(JObject saveDataJsonObject)
    {
        // this made a successful update.
        JArray lightAchievementDataArray = saveDataJsonObject["LightAchievementData"] as JArray ?? new JArray();

        foreach (JObject achievement in lightAchievementDataArray)
        {
            // Check if ActiveItems is a dictionary
            if (achievement["ActiveItems"] is JObject activeItemsObject)
            {
                // Convert the dictionary to a list
                var activeItemsList = activeItemsObject.Properties()
                    .Select(p => new TrackedItem
                    {
                        Item = p.Name,
                        UIDAffected = p.Value["UIDAffected"]?.ToString() ?? string.Empty,
                        TimeAdded = p.Value["TimeAdded"]?.Value<DateTime>() ?? DateTime.MinValue
                    })
                    .ToList();

                // Replace the ActiveItems property with the new list
                achievement["ActiveItems"] = JToken.FromObject(activeItemsList);
            }
        }
    }

    private void SubscribeToEvents()
    {
        Logger.LogInformation("Player Logged In, Subscribing to Events!");
        _eventManager.Subscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _eventManager.Subscribe<GagLayer, GagType, bool>(UnlocksEvent.GagAction, OnGagApplied);
        _eventManager.Subscribe<GagLayer, GagType, bool>(UnlocksEvent.GagRemoval, OnGagRemoval);
        _eventManager.Subscribe<GagType>(UnlocksEvent.PairGagAction, OnPairGagApplied);
        _eventManager.Subscribe(UnlocksEvent.GagUnlockGuessFailed, () => (SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _eventManager.Subscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _eventManager.Subscribe<RestraintSet, bool, string>(UnlocksEvent.RestraintApplicationChanged, OnRestraintApplied); // Apply on US
        _eventManager.Subscribe<RestraintSet, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _eventManager.Subscribe(UnlocksEvent.SoldSlave, () => (SaveData.Achievements[Achievements.SoldSlave.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe(UnlocksEvent.AuctionedOff, () => (SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _eventManager.Subscribe<Guid, bool, string>(UnlocksEvent.PairRestraintApplied, OnPairRestraintApply);
        _eventManager.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);

        _eventManager.Subscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _eventManager.Subscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _eventManager.Subscribe<ushort>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _eventManager.Subscribe<bool>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _eventManager.Subscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _eventManager.Subscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _eventManager.Subscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _eventManager.Subscribe(UnlocksEvent.DeathRollCompleted, () => (SaveData.Achievements[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe<NewState>(UnlocksEvent.AlarmToggled, _ => (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe(UnlocksEvent.ShockSent, OnShockSent);
        _eventManager.Subscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _eventManager.Subscribe<HardcoreAction, NewState, string, string>(UnlocksEvent.HardcoreForcedPairAction, OnHardcoreForcedPairAction);

        _eventManager.Subscribe(UnlocksEvent.RemoteOpened, () => (SaveData.Achievements[Achievements.JustVibing.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.VibeRoomCreated, () => (SaveData.Achievements[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe<NewState>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _eventManager.Subscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _eventManager.Subscribe(UnlocksEvent.ClientSlain, () => (SaveData.Achievements[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe(UnlocksEvent.ClientOneHp, () => (SaveData.Achievements[Achievements.BoundgeeJumping.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe<XivChatType>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _eventManager.Subscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _eventManager.Subscribe(UnlocksEvent.TutorialCompleted, () => (SaveData.Achievements[Achievements.TutorialComplete.Id] as ProgressAchievement)?.CheckCompletion());
        _eventManager.Subscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _eventManager.Subscribe(UnlocksEvent.PresetApplied, () => (SaveData.Achievements[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.GlobalSent, () => (SaveData.Achievements[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _eventManager.Subscribe<string>(UnlocksEvent.EasterEggFound, OnIconClicked);
        _eventManager.Subscribe(UnlocksEvent.ChocoboRaceFinished, () => (SaveData.Achievements[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (SaveData.Achievements[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _eventManager.Subscribe(UnlocksEvent.CutsceneInturrupted, () => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Mediator.Subscribe<PlayerLatestActiveItems>(this, (msg) => OnCharaOnlineCleanupForLatest(msg.User, msg.ActiveGags, msg.ActiveRestraint));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, _ => OnPairVisible());
        Mediator.Subscribe<CommendationsIncreasedMessage>(this, (msg) => OnCommendationsGiven(msg.amount));
        Mediator.Subscribe<PlaybackStateToggled>(this, (msg) => (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());

        Mediator.Subscribe<SafewordUsedMessage>(this, _ => (SaveData.Achievements[Achievements.KnowsMyLimits.Id] as ProgressAchievement)?.IncrementProgress());

        Mediator.Subscribe<GPoseStartMessage>(this, _ => (SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.BeginConditionalTask());
        Mediator.Subscribe<GPoseEndMessage>(this, _ => (SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.FinishConditionalTask());
        Mediator.Subscribe<CutsceneBeginMessage>(this, _ => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.BeginConditionalTask()); // starts Timer
        Mediator.Subscribe<CutsceneEndMessage>(this, _ => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.FinishConditionalTask()); // ends/completes progress.

        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => CheckOnZoneSwitchStart(msg.prevZone));
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, _ =>
        {
            CheckOnZoneSwitchEnd();
            CheckDeepDungeonStatus();
        });

        IpcFastUpdates.GlamourEventFired += OnJobChange;
        ActionEffectMonitor.ActionEffectEntryEvent += OnActionEffectEvent;
        _dutyState.DutyStarted += OnDutyStart;
        _dutyState.DutyCompleted += OnDutyEnd;
    }

    private void UnsubscribeFromEvents()
    {
        _eventManager.Unsubscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _eventManager.Unsubscribe<GagLayer, GagType, bool>(UnlocksEvent.GagAction, OnGagApplied);
        _eventManager.Unsubscribe<GagLayer, GagType, bool>(UnlocksEvent.GagRemoval, OnGagRemoval);
        _eventManager.Unsubscribe<GagType>(UnlocksEvent.PairGagAction, OnPairGagApplied);
        _eventManager.Unsubscribe(UnlocksEvent.GagUnlockGuessFailed, () => (SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _eventManager.Unsubscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _eventManager.Unsubscribe<RestraintSet, bool, string>(UnlocksEvent.RestraintApplicationChanged, OnRestraintApplied); // Apply on US
        _eventManager.Unsubscribe<RestraintSet, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _eventManager.Unsubscribe(UnlocksEvent.SoldSlave, () => (SaveData.Achievements[Achievements.SoldSlave.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe(UnlocksEvent.AuctionedOff, () => (SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _eventManager.Unsubscribe<Guid, bool, string>(UnlocksEvent.PairRestraintApplied, OnPairRestraintApply);
        _eventManager.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);

        _eventManager.Unsubscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _eventManager.Unsubscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _eventManager.Unsubscribe<ushort>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _eventManager.Unsubscribe<bool>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _eventManager.Unsubscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _eventManager.Unsubscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _eventManager.Unsubscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _eventManager.Unsubscribe(UnlocksEvent.DeathRollCompleted, () => (SaveData.Achievements[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe<NewState>(UnlocksEvent.AlarmToggled, _ => (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe(UnlocksEvent.ShockSent, OnShockSent);
        _eventManager.Unsubscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _eventManager.Unsubscribe<HardcoreAction, NewState, string, string>(UnlocksEvent.HardcoreForcedPairAction, OnHardcoreForcedPairAction);

        _eventManager.Unsubscribe(UnlocksEvent.RemoteOpened, () => (SaveData.Achievements[Achievements.JustVibing.Id] as ProgressAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe(UnlocksEvent.VibeRoomCreated, () => (SaveData.Achievements[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe<NewState>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _eventManager.Unsubscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _eventManager.Unsubscribe(UnlocksEvent.ClientSlain, () => (SaveData.Achievements[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe<XivChatType>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _eventManager.Unsubscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _eventManager.Unsubscribe(UnlocksEvent.TutorialCompleted, () => (SaveData.Achievements[Achievements.TutorialComplete.Id] as ProgressAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _eventManager.Unsubscribe(UnlocksEvent.PresetApplied, () => (SaveData.Achievements[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Unsubscribe(UnlocksEvent.GlobalSent, () => (SaveData.Achievements[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Unsubscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _eventManager.Unsubscribe<string>(UnlocksEvent.EasterEggFound, OnIconClicked);
        _eventManager.Unsubscribe(UnlocksEvent.ChocoboRaceFinished, () => (SaveData.Achievements[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Unsubscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (SaveData.Achievements[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _eventManager.Unsubscribe(UnlocksEvent.CutsceneInturrupted, () => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Mediator.Unsubscribe<PlayerLatestActiveItems>(this);
        Mediator.Unsubscribe<PairHandlerVisibleMessage>(this);
        Mediator.Unsubscribe<CommendationsIncreasedMessage>(this);
        Mediator.Unsubscribe<PlaybackStateToggled>(this);
        Mediator.Unsubscribe<SafewordUsedMessage>(this);
        Mediator.Unsubscribe<GPoseStartMessage>(this);
        Mediator.Unsubscribe<GPoseEndMessage>(this);
        Mediator.Unsubscribe<CutsceneBeginMessage>(this);
        Mediator.Unsubscribe<CutsceneEndMessage>(this);
        Mediator.Unsubscribe<ZoneSwitchStartMessage>(this);
        Mediator.Unsubscribe<ZoneSwitchEndMessage>(this);

        IpcFastUpdates.GlamourEventFired -= OnJobChange;
        ActionEffectMonitor.ActionEffectEntryEvent -= OnActionEffectEvent;
        _dutyState.DutyStarted -= OnDutyStart;
        _dutyState.DutyCompleted -= OnDutyEnd;
    }
}
