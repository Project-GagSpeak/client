using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using System.Threading;

namespace GagSpeak.Achievements;

public partial class AchievementManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _mainHub;
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientData _playerData;
    private readonly PairManager _pairManager;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateService _kinkPlateService;
    private readonly VibratorService _vibeService;
    private readonly UnlocksEventManager _eventManager;
    private readonly INotificationManager _notify;
    private readonly IDutyState _dutyState;

    // Token used for updating achievement data.
    private CancellationTokenSource? _saveDataUpdateCTS;
    private CancellationTokenSource? _achievementCompletedCTS;

    // Our Main SaveData instance.
    public static AchievementSaveData SaveData { get; private set; } = new AchievementSaveData();

    public AchievementManager(ILogger<AchievementManager> logger, GagspeakMediator mediator, 
        MainHub mainHub, ClientConfigurationManager clientConfigs, ClientData playerData, 
        PairManager pairManager, ClientMonitorService clientService, OnFrameworkService frameworkUtils, 
        CosmeticService cosmetics, KinkPlateService kinkPlateService, VibratorService vibeService, 
        UnlocksEventManager eventManager, INotificationManager notifs, IDutyState dutyState) : base(logger, mediator)
    {
        _mainHub = mainHub;
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _pairManager = pairManager;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;
        _cosmetics = cosmetics;
        _kinkPlateService = kinkPlateService;
        _vibeService = vibeService;
        _eventManager = eventManager;
        _notify = notifs;
        _dutyState = dutyState;

        Logger.LogInformation("Initializing Achievement Save Data Achievements", LoggerType.Achievements);
        SaveData = new AchievementSaveData();
        InitializeAchievements();

        Logger.LogInformation("Achievement Save Data Initialized, default saveData string stored in template.", LoggerType.Achievements);

        // Check for when we are connected to the server, use the connection DTO to load our latest stored save data.
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => OnServerConnection());
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ =>
        {
            // Revert SaveDataLoaded to false incase we get a disconnect before we can even run the onServerConnection function.
            SaveDataLoaded = false;
            ContainsValidSaveData = false;
            _saveDataUpdateCTS?.Cancel();
        });

        SubscribeToEvents();
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        _saveDataUpdateCTS?.Cancel();
        _saveDataUpdateCTS?.Dispose();
        _achievementCompletedCTS?.Cancel();
        _achievementCompletedCTS?.Dispose();
        UnsubscribeFromEvents();
    }

    // Public accessor helpers.
    public static int Total => SaveData.Achievements.Count;
    public static int Completed => SaveData.Achievements.Values.Count(a => a.IsCompleted);
    public static List<AchievementBase> AllBase => SaveData.Achievements.Values.Cast<AchievementBase>().ToList();
    public static List<AchievementBase> CompletedAchievements => SaveData.Achievements.Values.Where(a => a.IsCompleted).ToList();
    public static string GetTitleById(int id) => SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)?.Title ?? "No Title Set";
    public static List<AchievementBase> GetAchievementsForModule(AchievementModuleKind module) => SaveData.Achievements.Values.Where(a => a.Module == module).ToList();
    public static bool TryGetAchievement(int id, out AchievementBase achievement)
    {
        achievement = SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)!;
        return achievement is not null;
    }

    /// <summary>
    /// Required to be true in order for any save data uploads to occur.
    /// </summary>
    public static bool CanUploadSaveData => SaveDataLoaded && ContainsValidSaveData;

    // Marked as false if at any point during connection a loading issue occurs
    private static bool ContainsValidSaveData = false;

    // Only set to true after the saveData has been loaded completely.
    private static bool SaveDataLoaded = false;

    // Stores the time of the last unhandled disconnect, so we know if we should load previous data in or not.
    public static DateTime LastUnhandledDisconnect = DateTime.MinValue;

    private void OnServerConnection()
    {
        // Initial Assumption: SaveData is not loaded or valid.
        SaveDataLoaded = false;
        ContainsValidSaveData = false;

        // Cautionary Case 1: the connection DTO is null.
        // -------------------------------------------
        //    CONCERN: If null, it means we have no way to fetch what our stored AchievementData is from the server.
        //     ACTION: Avoid starting any achievement data updates and return early.
        //   CONSIDER: The SaveData will still contain blank achievement data state after returning. Meaning our saveData
        //             is still technically valid, but we dont want to allow this.
        // PREVENTION: Performing the early return will make SaveData updates invalid as ContainsValidSaveData will be false.
        if (MainHub.ConnectionDto is null)
        {
            Logger.LogError("At the time of processing this function, your ConnectionDto was null." + Environment.NewLine +
                "To prevent your SaveData from being reset or glitched or broken, until a reconnect " +
                "with a valid ConnectionDto no achievement updates will be made.", LoggerType.Achievements);
            return;
        }

        // Cautionary Case 2: We reconnected after unhandled exception / timeout.
        // ------------------------------------------------------------------
        //    CONCERN: If we reconnected after an unhandled exception, any changes made between the last save and the unhandled
        //             disconnect will be lost.
        //     ACTION: Load in the stored SaveData instead of the one from the connectionDTO by starting the saveCycle prior to
        //             loading in or initializing any achievement data, and doing an early return.
        //   CONSIDER: There is a possibility that the stored saveData could also not be valid data.
        // PREVENTION: Ensure the stored saveData is valid before loading it in.
        if (LastUnhandledDisconnect != DateTime.MinValue)
        {
            Logger.LogInformation("Our Last Disconnect was due to an exception, loading from existing/stored SaveData (if valid) instead.", LoggerType.Achievements);
            // set the unhandled disconnect back to its default value so the next save after is accepted.
            LastUnhandledDisconnect = DateTime.MinValue;
            Logger.LogInformation("Starting SaveCycle by uploading previous AchievementData String:\n" + GetSaveDataDtoString());
            // we can run the assumption here that the saveData is loaded in and valid.
            SaveDataLoaded = true;
            ContainsValidSaveData = true;
            BeginAchievementSaveCycle();
            return;
        }

        // Things have loaded in fine, handle how we import the AchievementSaveData.
        if (string.IsNullOrEmpty(MainHub.ConnectionDto.UserAchievements))
        {
            Logger.LogInformation("User has empty achievement Save Data. Creating new Save Data.", LoggerType.Achievements);
            SaveData = new AchievementSaveData();
            InitializeAchievements();
            Logger.LogDebug("Fresh Achievement Data Created!", LoggerType.Achievements);
            // we can mark the saveData as 'loaded' and 'valid' since we have no reason to believe it is not.
            SaveDataLoaded = true;
            ContainsValidSaveData = true;
        }
        else
        {
            Logger.LogInformation("Loading in AchievementData from ConnectionDto", LoggerType.Achievements);
            // See Inner Function for additional cautionary details:
            LoadSaveDataDto(MainHub.ConnectionDto.UserAchievements);
        }
        // begin the achievement save cycle.
        BeginAchievementSaveCycle();
    }

    private void BeginAchievementSaveCycle()
    {
        // Begin the save cycle.
        _saveDataUpdateCTS?.Cancel();
        _saveDataUpdateCTS?.Dispose();
        _saveDataUpdateCTS = new CancellationTokenSource();
        _ = AchievementDataPeriodicUpdate(_saveDataUpdateCTS.Token);
    }

    /// <summary>
    /// Send an update to our achievement data every 20-30 minutes.
    /// </summary>
    private async Task AchievementDataPeriodicUpdate(CancellationToken ct)
    {
        Logger.LogInformation("Starting SaveData Update Loop", LoggerType.Achievements);
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Logger.LogInformation("Achievement SaveData Update processing...");
                // break out of the loop and stop the update cycle if we no longer meet requirements for updating.
                if (!CanUploadSaveData)
                {
                    Logger.LogWarning("SaveData was either not loaded in or no longer valid, exiting loop to prevent potential data resets!");
                    _saveDataUpdateCTS?.Cancel();
                    return;
                }

                // otherwise, send the updated achievement information to the server.
                await SendUpdatedDataToServer();
            }
            catch (Exception)
            {
                Logger.LogDebug("Not sending Achievement SaveData due to disconnection canceling the loop.");
            }

            // Determine how long we should wait before the next update Randomly between 20 and 30 minutes
            int delayMinutes = random.Next(20, 31);
            Logger.LogInformation("SaveData Update Task Completed, Firing Again in " + delayMinutes + " Minutes");
            // Wait for the next update cycle.
            await Task.Delay(TimeSpan.FromMinutes(delayMinutes), ct).ConfigureAwait(false);
        }
    }

    private void LoadSaveDataDto(string Base64saveDataToLoad)
    {
        try
        {
            var bytes = Convert.FromBase64String(Base64saveDataToLoad);
            var version = bytes[0];
            version = bytes.DecompressToString(out var decompressed);
            LightSaveDataDto item = SaveDataDeserialize(decompressed) ?? throw new Exception("Failed to deserialize.");

            // Update the local achievement data by loading from the light save data.
            SaveData.LoadFromLightSaveDataDto(item);
            Logger.LogInformation("Achievement Data Loaded from Server", LoggerType.Achievements);
            Logger.LogInformation("Achievement Data String Loaded:\n" + Base64saveDataToLoad);
            // Assuming we have not hit the catch statement at this point, we can mark that ContainsValidSaveData is true.
            ContainsValidSaveData = true;
        }
        catch (Exception ex)
        {
            // Cautionary Case 3: Failed to load Achievement Data from server.
            // --------------------------------------------------------------
            //    CONCERN: If at any point the data loaded in from the server fails to deserialize, we should ensure that
            //             the SaveCycle is broken and future SaveData updates are prevented to preserve existing data.
            //     ACTION: Set the ContainsValidSaveData to false, so that even when SaveDataLoaded is true, no updates will be made.
            //   CONSIDER: The SaveDataLoaded will still be true after this finishes. Need a way to make sure both have to be true
            //             to allow upload.
            // PREVENTION: Make a seperate Boolean that => SaveDataLoaded && ContainsValidSaveData to determine if we can upload.
            Logger.LogError("Failed to load Achievement Data from server. Setting [HadFailedAchievementDataLoad] to true, " +
                "preventing any further uploads to keep your old data intact. If you wish to pull a manual reset and do not" +
                "believe this to be a bug, press the reset button, then reconnect.\n[REASON]: " + ex.Message);
            ContainsValidSaveData = false;
        }
        finally
        {
            // Ensure that SaveDataLoaded is set to true, so that the SaveCycle can begin.
            SaveDataLoaded = true;
        }
    }

    public async Task ResetAchievementData()
    {
        // Reset SaveData
        SaveData = new AchievementSaveData();
        InitializeAchievements();
        Logger.LogInformation("Reset Achievement Data Completely!", LoggerType.Achievements);
        // Send this off to the server.
        await SendUpdatedDataToServer();
    }

    // Your existing method to send updated data to the server
    private async Task SendUpdatedDataToServer()
    {
        var saveDataString = GetSaveDataDtoString();
        Logger.LogInformation("Sending updated achievement data to the server", LoggerType.Achievements);

        // Prevent uploads if CanUploadSaveData is not true.
        if (!CanUploadSaveData)
        {
            Logger.LogWarning("Failed to send Achievement SaveData to the server, CanUploadSaveData is not true, meaning " +
                "either the SaveData is not yet loaded, or that it was not valid.");
            return;
        }

        // Logic to send base64Data to the server
        Logger.LogInformation("Connected with AchievementData String:\n" + saveDataString);
        await _mainHub.UserUpdateAchievementData(new((MainHub.PlayerUserData), saveDataString));
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

    #region CompletingAchievements
    public async Task WasCompleted(int id, string title)
    {
        Logger.LogInformation("Achievement Completed: " + title, LoggerType.Achievements);
        // publish the award notification to the notification manager regardless of if we get inturrupted or not.
        _notify.AddNotification(new Notification()
        {
            Title = "Achievement Completed!",
            Content = "Completed: " + title,
            Type = NotificationType.Info,
            Icon = INotificationIcon.From(FontAwesomeIcon.Award),
            Minimized = false,
            InitialDuration = TimeSpan.FromSeconds(10)
        });

        // past this point, we should cancel the token and create a new one, to stop any currently running kinkplate setter tasks.
        Logger.LogInformation("Cancelling previous Achievement Completed Timer", LoggerType.Achievements);
        _achievementCompletedCTS?.Cancel();
        _achievementCompletedCTS = new CancellationTokenSource();

        // Then assign/reassign the upload task to the function.
        await UpdateTotalEarnedAchievementsAsync(_achievementCompletedCTS.Token);
    }

    private async Task UpdateTotalEarnedAchievementsAsync(CancellationToken token)
    {
        try
        {
            // Grab our latest content from our KinkPlate™ Service, Otherwise, fetch from the hub directly.
            KinkPlateContent clientPlateContent;
            if (_kinkPlateService.TryGetClientKinkPlateContent(out var plateContent))
            {
                clientPlateContent = plateContent;
            }
            else
            {
                var profileData = await _mainHub.UserGetKinkPlate(new UserDto(MainHub.PlayerUserData));
                clientPlateContent = profileData.Info;
            }

            // Update kinkplate with the new achievement count.
            Logger.LogDebug("Updating KinkPlate™ with new Achievement Completion count: " + clientPlateContent.CompletedAchievementsTotal, LoggerType.Achievements);
            clientPlateContent.CompletedAchievementsTotal = Completed;

            // update the plateContent to the server.
            await _mainHub.UserSetKinkPlateContent(new(MainHub.PlayerUserData, clientPlateContent));
            Logger.LogInformation("Updated KinkPlate™ with latest achievement count total.", LoggerType.Achievements);
        }
        catch (TaskCanceledException)
        {
            Logger.LogTrace("Halted Uploading of total completed achievements do to another being completed during the process.", LoggerType.Achievements);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to update KinkPlate™ with latest achievement count: {ex}", LoggerType.Achievements);
        }
    }
    #endregion CompletingAchievements

    private void SubscribeToEvents()
    {
        Logger.LogInformation("Player Logged In, Subscribing to Events!");
        _eventManager.Subscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _eventManager.Subscribe<bool, GagLayer, GagType, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _eventManager.Subscribe<bool, GagLayer, GagType, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _eventManager.Subscribe<bool, GagLayer, Padlocks, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _eventManager.Subscribe<bool, GagLayer, Padlocks, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _eventManager.Subscribe(UnlocksEvent.GagUnlockGuessFailed, () => (SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _eventManager.Subscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _eventManager.Subscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _eventManager.Subscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _eventManager.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _eventManager.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);

        _eventManager.Subscribe(UnlocksEvent.SoldSlave, () => (SaveData.Achievements[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.AuctionedOff, () => (SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

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

        _eventManager.Subscribe<InteractionType, NewState, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

        _eventManager.Subscribe(UnlocksEvent.RemoteOpened, () => (SaveData.Achievements[Achievements.JustVibing.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.VibeRoomCreated, () => (SaveData.Achievements[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe<NewState>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _eventManager.Subscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _eventManager.Subscribe(UnlocksEvent.ClientSlain, () => (SaveData.Achievements[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe(UnlocksEvent.ClientOneHp, () => (SaveData.Achievements[Achievements.BoundgeeJumping.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe<XivChatType>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _eventManager.Subscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _eventManager.Subscribe(UnlocksEvent.TutorialCompleted, () => (SaveData.Achievements[Achievements.TutorialComplete.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _eventManager.Subscribe(UnlocksEvent.PresetApplied, () => (SaveData.Achievements[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.GlobalSent, () => (SaveData.Achievements[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Subscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _eventManager.Subscribe<string>(UnlocksEvent.EasterEggFound, OnIconClicked);
        _eventManager.Subscribe(UnlocksEvent.ChocoboRaceFinished, () => (SaveData.Achievements[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _eventManager.Subscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (SaveData.Achievements[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _eventManager.Subscribe(UnlocksEvent.CutsceneInturrupted, () => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Mediator.Subscribe<PlayerLatestActiveItems>(this, (msg) => OnCharaOnlineCleanupForLatest(msg.User, msg.GagInfo, msg.ActiveRestraint));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, _ => OnPairVisible());
        Mediator.Subscribe<CommendationsIncreasedMessage>(this, (msg) => OnCommendationsGiven(msg.amount));
        Mediator.Subscribe<PlaybackStateToggled>(this, (msg) => (SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());

        Mediator.Subscribe<SafewordUsedMessage>(this, _ => (SaveData.Achievements[Achievements.KnowsMyLimits.Id] as ProgressAchievement)?.IncrementProgress());

        Mediator.Subscribe<GPoseStartMessage>(this, _ => (SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.BeginConditionalTask());
        Mediator.Subscribe<GPoseEndMessage>(this, _ => (SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.FinishConditionalTask());
        Mediator.Subscribe<CutsceneBeginMessage>(this, _ => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.BeginConditionalTask()); // starts Timer
        Mediator.Subscribe<CutsceneEndMessage>(this, _ => (SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.FinishConditionalTask()); // ends/completes progress.

        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => CheckOnZoneSwitchStart(msg.prevZone));
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, _ => CheckOnZoneSwitchEnd());

        Mediator.Subscribe<JobChangeMessage>(this, (msg) => OnJobChange(msg.jobId));

        ActionEffectMonitor.ActionEffectEntryEvent += OnActionEffectEvent;
        _dutyState.DutyStarted += OnDutyStart;
        _dutyState.DutyCompleted += OnDutyEnd;
    }

    private void UnsubscribeFromEvents()
    {
        _eventManager.Unsubscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _eventManager.Unsubscribe<bool, GagLayer, GagType, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _eventManager.Unsubscribe<bool, GagLayer, GagType, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _eventManager.Unsubscribe<bool, GagLayer, Padlocks, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _eventManager.Unsubscribe<bool, GagLayer, Padlocks, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _eventManager.Unsubscribe(UnlocksEvent.GagUnlockGuessFailed, () => (SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _eventManager.Unsubscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _eventManager.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _eventManager.Unsubscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _eventManager.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _eventManager.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);
        _eventManager.Unsubscribe(UnlocksEvent.SoldSlave, () => (SaveData.Achievements[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _eventManager.Unsubscribe(UnlocksEvent.AuctionedOff, () => (SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

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

        _eventManager.Unsubscribe<InteractionType, NewState, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

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
        Mediator.Unsubscribe<JobChangeMessage>(this);

        ActionEffectMonitor.ActionEffectEntryEvent -= OnActionEffectEvent;
        _dutyState.DutyStarted -= OnDutyStart;
        _dutyState.DutyCompleted -= OnDutyEnd;
    }
}
