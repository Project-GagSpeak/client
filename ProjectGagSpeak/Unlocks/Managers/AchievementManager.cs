using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using GagSpeak.ChatMessages;
using GagSpeak.CkCommons;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Network;

namespace GagSpeak.Achievements;

/// <summary> The current state of the achievement save data. </summary>
public class SaveDataCache
{
    /// <summary> Our Main SaveData instance. </summary>
    /// <remarks> Dependent on InitializeAchievements() after construction to be configured. </remarks>
    public AchievementSaveData SaveData { get; private set; } = new AchievementSaveData();

    /// <summary> Marked as false if at any point during connection a loading issue occurs </summary>
    public bool ContainsValidSaveData = false;

    /// <summary> Only set to true after the saveData has been loaded completely. </summary>
    public bool SaveDataLoaded = false;

    /// <summary> Flagged upon any unhandled disconnect. </summary>
    public DateTime LastUnhandledDisconnect = DateTime.MinValue;

    /// <summary> If the SaveData is loaded and valid, we can upload. </summary>
    public bool CanUpload() => SaveDataLoaded && ContainsValidSaveData;
}

public partial class AchievementManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _mainHub;
    private readonly KinksterRequests _playerData;
    private readonly PairManager _pairs;
    private readonly MainConfigService _mainConfig;
    private readonly ClientMonitor _clientMonitor;
    private readonly UnlocksEventManager _events;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly RestraintManager _restraints;
    private readonly CursedLootManager _cursedLoot;
    private readonly PatternManager _patterns;
    private readonly AlarmManager _alarms;
    private readonly TriggerManager _triggers;
    private readonly SexToyManager _sexToys;
    private readonly TraitAllowanceManager _traits;
    private readonly ItemService _items;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateService _kinkPlateService;
    private readonly INotificationManager _notify;
    private readonly IDutyState _dutyState;

    // Token used for updating achievement data.
    private CancellationTokenSource? _saveDataUpdateCTS;
    private CancellationTokenSource? _achievementCompletedCTS;

    public AchievementManager(
        ILogger<AchievementManager> logger,
        GagspeakMediator mediator, 
        MainHub mainHub,
        KinksterRequests playerData,
        MainConfigService mainConfig,
        PairManager pairs,
        ClientMonitor clientMonitor,
        UnlocksEventManager events,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PatternManager patterns,
        AlarmManager alarms,
        TriggerManager triggers,
        SexToyManager sexToys,
        TraitAllowanceManager traits,
        ItemService items,
        OnFrameworkService frameworkUtils,
        CosmeticService cosmetics,
        KinkPlateService kinkPlateService,
        INotificationManager notifs,
        IDutyState dutyState) : base(logger, mediator)
    {
        _mainHub = mainHub;
        _playerData = playerData;
        _pairs = pairs;
        _mainConfig = mainConfig;
        _clientMonitor = clientMonitor;
        _events = events;
        _gags = gags;
        _restrictions = restrictions;
        _restraints = restraints;
        _cursedLoot = cursedLoot;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
        _sexToys = sexToys;
        _traits = traits;
        _items = items;
        _frameworkUtils = frameworkUtils;
        _cosmetics = cosmetics;
        _kinkPlateService = kinkPlateService;
        _notify = notifs;
        _dutyState = dutyState;

        Logger.LogInformation("Initializing Achievement Save Data Achievements", LoggerType.Achievements);
        LatestCache = new SaveDataCache();
        InitializeAchievements();

        Logger.LogInformation("Achievement Save Data Initialized, default saveData string stored in template.", LoggerType.Achievements);

        // Check for when we are connected to the server, use the connection DTO to load our latest stored save data.
        Mediator.Subscribe<MainHubDisconnectedMessage>(this, _ =>
        {
            // if the lastUnhandled disconnect is MinValue, then we should reset the cache entirely.
            if (LatestCache.LastUnhandledDisconnect == DateTime.MinValue)
            {
                Logger.LogInformation("Unhandled Disconnect, resetting Achievement Data Cache.", LoggerType.Achievements);
                ReInitializeSaveData();
            }
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
    public static int Total => LatestCache.SaveData.Achievements.Count;
    public static int Completed => LatestCache.SaveData.Achievements.Values.Count(a => a.IsCompleted);
    public static List<AchievementBase> AllBase => LatestCache.SaveData.Achievements.Values.Cast<AchievementBase>().ToList();
    public static List<AchievementBase> CompletedAchievements => LatestCache.SaveData.Achievements.Values.Where(a => a.IsCompleted).ToList();
    public static string GetTitleById(int id) => LatestCache.SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)?.Title ?? "No Title Set";
    public static List<AchievementBase> GetAchievementsForModule(AchievementModuleKind module) => LatestCache.SaveData.Achievements.Values.Where(a => a.Module == module).ToList();
    public static bool TryGetAchievement(int id, out AchievementBase achievement)
    {
        achievement = LatestCache.SaveData.Achievements.Values.FirstOrDefault(a => a.AchievementId == id)!;
        return achievement is not null;
    }

    public static SaveDataCache LatestCache = new SaveDataCache();

    public void OnServerConnection(string? connectedAchievementString)
    {
        // if the last unhandled disconnect is any value besides MinValue, then load in that data.
        if(LatestCache.LastUnhandledDisconnect != DateTime.MinValue && LatestCache.CanUpload())
        {
            Logger.LogInformation("Last Disconnect was due to an exception, loading from existing/stored SaveData (if valid) instead.", LoggerType.Achievements);
            LatestCache.LastUnhandledDisconnect = DateTime.MinValue;
            BeginAchievementSaveCycle();
            return;
        }

        // Otherwise, we dont give a fuck what the SaveData was, we should try to reinitialize the data though.
        if(connectedAchievementString.IsNullOrEmpty())
        {
            // reinitialize and push update.
            Logger.LogInformation("User has empty achievement Save Data. Creating new Save Data.", LoggerType.Achievements);
            ReInitializeSaveData();
            Logger.LogDebug("Fresh Achievement Data Created!", LoggerType.Achievements);
            LatestCache.SaveDataLoaded = true;
            LatestCache.ContainsValidSaveData = true;
            // begin the achievement save cycle.
            BeginAchievementSaveCycle();
        }
        else // we have non-null string, so attempt to load it.
        {
            Logger.LogInformation("Loading in AchievementData from ConnectionResponse", LoggerType.Achievements);
            LoadSaveDataDto(connectedAchievementString);
            // if the cache is valid for uploads, begin the save cycle, otherwise do not ever allow it to run a save cycle.
            if (LatestCache.CanUpload())
                BeginAchievementSaveCycle();
        }
    }

    private void BeginAchievementSaveCycle()
    {
        // Begin the save cycle.
        _saveDataUpdateCTS?.Cancel();
        _saveDataUpdateCTS?.Dispose();
        _saveDataUpdateCTS = new CancellationTokenSource();
        _ = AchievementDataPeriodicUpdate(_saveDataUpdateCTS.Token);
    }

    /// <summary> Send an update to our achievement data every 20-30 minutes. </summary>
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
                if (LatestCache.CanUpload() is false)
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
            var delayMinutes = random.Next(20, 31);
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
            var item = SaveDataDeserialize(decompressed) ?? throw new Exception("Failed to deserialize.");

            // Update the local achievement data by loading from the light save data.
            LatestCache.SaveData.LoadFromLightSaveDataDto(item);
            Logger.LogInformation("Achievement Data Loaded from Server", LoggerType.Achievements);
            Logger.LogInformation("Achievement Data String Loaded:\n" + Base64saveDataToLoad);
            // Assuming we have not hit the catch statement at this point, we can mark that ContainsValidSaveData is true.
            LatestCache.ContainsValidSaveData = true;
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
            // PREVENTION: Make a separate Boolean that => SaveDataLoaded && ContainsValidSaveData to determine if we can upload.
            Logger.LogError("Failed to load Achievement Data from server. Setting [HadFailedAchievementDataLoad] to true, " +
                "preventing any further uploads to keep your old data intact. If you wish to pull a manual reset and do not" +
                "believe this to be a bug, press the reset button, then reconnect.\n[REASON]: " + ex.Message);
            LatestCache.ContainsValidSaveData = false;
        }
        finally
        {
            // Ensure that SaveDataLoaded is set to true, so that the SaveCycle can begin.
            LatestCache.SaveDataLoaded = true;
        }
    }

    public async Task ResetAchievementData()
    {
        // Reset SaveData
        ReInitializeSaveData();
        LatestCache.ContainsValidSaveData = true;
        LatestCache.SaveDataLoaded = true;
        Logger.LogInformation("Reset Achievement Data Completely!", LoggerType.Achievements);
        // Send this off to the server.
        await SendUpdatedDataToServer();
    }

    // Your existing method to send updated data to the server
    private async Task SendUpdatedDataToServer()
    {
        // Prevent uploads if CanUpload() is false.
        if (LatestCache.CanUpload() is false)
        {
            Logger.LogWarning("Failed to send Achievement SaveData to the server, CanUploadSaveData is not true, meaning " +
                "either the SaveData is not yet loaded, or that it was not valid.");
            return;
        }

        var saveDataString = GetSaveDataDtoString();
        Logger.LogInformation("Sending updated achievement data to the server", LoggerType.Achievements);

        // Logic to send base64Data to the server
        Logger.LogInformation("Connected with AchievementData String:\n" + saveDataString);
        await _mainHub.UserUpdateAchievementData(new((MainHub.PlayerUserData), saveDataString));
    }

    public static string GetSaveDataDtoString()
    {
        // get the Dto-Ready data object of our saveData
        var saveDataDto = LatestCache.SaveData.ToLightSaveDataDto();

        // condense it into the json and compress it.
        var json = SaveDataSerialize(saveDataDto);
        var compressed = json.Compress(6);
        var base64Data = Convert.ToBase64String(compressed);
        return base64Data;
    }

    private static string SaveDataSerialize(LightSaveDataDto lightSaveDataDto)
    {
        return new JObject
        {
            ["Version"] = lightSaveDataDto.Version,
            ["LightAchievementData"] = JArray.FromObject(lightSaveDataDto.LightAchievementData),
            ["VisitedWorldTour"] = JObject.FromObject(lightSaveDataDto.VisitedWorldTour)
        }.ToString(Formatting.Indented);
    }

    private static LightSaveDataDto SaveDataDeserialize(string jsonString)
    {
        // Parse the JSON string into a JObject
        var saveDataJsonObject = JObject.Parse(jsonString);

        // Extract and validate the version
        var version = saveDataJsonObject["Version"]?.Value<int>() ?? 3;

        // Apply migrations based on the version number
        if (version < 3)
        {
            // Example migration: Update structure for version 1 to version 2
            MigrateToVersion3(saveDataJsonObject);
            // update verion to 3
            version = 3;
        }

        // Extract and validate LightAchievementData
        var lightAchievementDataArray = saveDataJsonObject["LightAchievementData"] as JArray ?? new JArray();
        var lightAchievementDataList = new List<LightAchievement>();

        foreach (JObject achievement in lightAchievementDataArray)
        {
            var achievementId = achievement["AchievementId"]?.Value<int>() ?? 0;

            // Check and correct achievement data against AchievementMap
            if (!Achievements.AchievementMap.ContainsKey(achievementId))
            {
                GagSpeak.StaticLog.Error("For some reason, your stored achievement ID ["+ achievementId + "] doesn't exist in the AchievementMap. Skipping over it.");
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
                GagSpeak.StaticLog.Error("Failed to load Achievement with ID: " + achievementId);
            }

        }

        // Extract and validate VisitedWorldTour
        var visitedWorldTourObject = saveDataJsonObject["VisitedWorldTour"] as JObject ?? new JObject();
        var visitedWorldTour = visitedWorldTourObject.ToObject<Dictionary<ushort, bool>>() ?? new Dictionary<ushort, bool>();

        // Create and return the LightSaveDataDto object
        var lightSaveDataDto = new LightSaveDataDto
        {
            Version = version,
            LightAchievementData = lightAchievementDataList,
            VisitedWorldTour = visitedWorldTour
        };

        return lightSaveDataDto;
    }

    private static void MigrateToVersion3(JObject saveDataJsonObject)
    {
        // this made a successful update.
        var lightAchievementDataArray = saveDataJsonObject["LightAchievementData"] as JArray ?? new JArray();

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
            Icon = INotificationIcon.From(FAI.Award),
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
                var profileData = await _mainHub.UserGetKinkPlate(new KinksterBase(MainHub.PlayerUserData));
                clientPlateContent = profileData.Info;
            }

            // Update KinkPlate™ with the new achievement count.
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
        _events.Subscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _events.Subscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _events.Subscribe<int, GagType, bool, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _events.Subscribe<int, Padlocks, bool, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _events.Subscribe<int, Padlocks, bool, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _events.Subscribe(UnlocksEvent.GagUnlockGuessFailed, () => (LatestCache.SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _events.Subscribe<Guid, bool, string>(UnlocksEvent.RestrictionStateChange, OnRestrictionStateChange); // Apply on US
        _events.Subscribe<Guid, bool, string, string>(UnlocksEvent.PairRestrictionStateChange, OnPairRestrictionStateChange);
        _events.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestrictionLockStateChange, OnRestrictionLock); // Lock on US
        _events.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestrictionLockStateChange, OnPairRestrictionLockChange);


        _events.Subscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _events.Subscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _events.Subscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _events.Subscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _events.Subscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);

        _events.Subscribe(UnlocksEvent.SoldSlave, () => (LatestCache.SaveData.Achievements[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.AuctionedOff, () => (LatestCache.SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _events.Subscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _events.Subscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _events.Subscribe<int>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _events.Subscribe<PuppetPerms>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _events.Subscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _events.Subscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _events.Subscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _events.Subscribe(UnlocksEvent.DeathRollCompleted, () => (LatestCache.SaveData.Achievements[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<NewState>(UnlocksEvent.AlarmToggled, _ => (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe(UnlocksEvent.ShockSent, OnShockSent);
        _events.Subscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _events.Subscribe<InteractionType, NewState, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

        _events.Subscribe(UnlocksEvent.RemoteOpened, () => (LatestCache.SaveData.Achievements[Achievements.JustVibing.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.VibeRoomCreated, () => (LatestCache.SaveData.Achievements[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe<NewState>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _events.Subscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _events.Subscribe(UnlocksEvent.ClientSlain, () => (LatestCache.SaveData.Achievements[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe(UnlocksEvent.ClientOneHp, () => (LatestCache.SaveData.Achievements[Achievements.BoundgeeJumping.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<ChatChannel.Channels>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _events.Subscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _events.Subscribe(UnlocksEvent.TutorialCompleted, () => (LatestCache.SaveData.Achievements[Achievements.TutorialComplete.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _events.Subscribe(UnlocksEvent.PresetApplied, () => (LatestCache.SaveData.Achievements[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.GlobalSent, () => (LatestCache.SaveData.Achievements[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Subscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _events.Subscribe(UnlocksEvent.ChocoboRaceFinished, () => (LatestCache.SaveData.Achievements[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Subscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (LatestCache.SaveData.Achievements[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _events.Subscribe(UnlocksEvent.CutsceneInturrupted, () => (LatestCache.SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

        Mediator.Subscribe<PlayerLatestActiveItems>(this, (msg) => OnCharaOnlineCleanupForLatest(msg.User, msg.GagsInfo, msg.RestrictionsInfo, msg.RestraintInfo));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, _ => OnPairVisible());
        Mediator.Subscribe<CommendationsIncreasedMessage>(this, (msg) => OnCommendationsGiven(msg.amount));
        Mediator.Subscribe<PlaybackStateToggled>(this, (msg) => (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());

        Mediator.Subscribe<SafewordUsedMessage>(this, _ => (LatestCache.SaveData.Achievements[Achievements.KnowsMyLimits.Id] as ProgressAchievement)?.IncrementProgress());

        Mediator.Subscribe<GPoseStartMessage>(this, _ => (LatestCache.SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.BeginConditionalTask());
        Mediator.Subscribe<GPoseEndMessage>(this, _ => (LatestCache.SaveData.Achievements[Achievements.SayMmmph.Id] as ConditionalProgressAchievement)?.FinishConditionalTask());
        Mediator.Subscribe<CutsceneBeginMessage>(this, _ => (LatestCache.SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.BeginConditionalTask()); // starts Timer
        Mediator.Subscribe<CutsceneEndMessage>(this, _ => (LatestCache.SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.FinishConditionalTask()); // ends/completes progress.

        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (msg) => CheckOnZoneSwitchStart(msg.prevZone));
        Mediator.Subscribe<ZoneSwitchEndMessage>(this, _ => CheckOnZoneSwitchEnd());

        Mediator.Subscribe<JobChangeMessage>(this, (msg) => OnJobChange(msg.jobId));

        Signatures.ActionEffectEntryEvent += OnActionEffectEvent;
        _dutyState.DutyStarted += OnDutyStart;
        _dutyState.DutyCompleted += OnDutyEnd;
    }

    private void UnsubscribeFromEvents()
    {
        _events.Unsubscribe<OrderInteractionKind>(UnlocksEvent.OrderAction, OnOrderAction);
        _events.Unsubscribe<int, GagType, bool, string>(UnlocksEvent.GagStateChange, OnGagStateChanged);
        _events.Unsubscribe<int, GagType, bool, string, string>(UnlocksEvent.PairGagStateChange, OnPairGagStateChanged);
        _events.Unsubscribe<int, Padlocks, bool, string>(UnlocksEvent.GagLockStateChange, OnGagLockStateChange);
        _events.Unsubscribe<int, Padlocks, bool, string, string>(UnlocksEvent.PairGagLockStateChange, OnPairGagLockStateChange);
        _events.Unsubscribe(UnlocksEvent.GagUnlockGuessFailed, () => (LatestCache.SaveData.Achievements[Achievements.RunningGag.Id] as ConditionalAchievement)?.CheckCompletion());

        _events.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestrictionStateChange, OnRestrictionStateChange); // Apply on US
        _events.Unsubscribe<Guid, bool, string, string>(UnlocksEvent.PairRestrictionStateChange, OnPairRestrictionStateChange);
        _events.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestrictionLockStateChange, OnRestrictionLock); // Lock on US
        _events.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestrictionLockStateChange, OnPairRestrictionLockChange);

        _events.Unsubscribe<RestraintSet>(UnlocksEvent.RestraintUpdated, OnRestraintSetUpdated);
        _events.Unsubscribe<Guid, bool, string>(UnlocksEvent.RestraintStateChange, OnRestraintStateChange); // Apply on US
        _events.Unsubscribe<Guid, bool, string, string>(UnlocksEvent.PairRestraintStateChange, OnPairRestraintStateChange);
        _events.Unsubscribe<Guid, Padlocks, bool, string>(UnlocksEvent.RestraintLockChange, OnRestraintLock); // Lock on US
        _events.Unsubscribe<Guid, Padlocks, bool, string, string>(UnlocksEvent.PairRestraintLockChange, OnPairRestraintLockChange);
        _events.Unsubscribe(UnlocksEvent.SoldSlave, () => (LatestCache.SaveData.Achievements[Achievements.SoldSlave.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.AuctionedOff, () => (LatestCache.SaveData.Achievements[Achievements.AuctionedOff.Id] as ProgressAchievement)?.IncrementProgress());

        _events.Unsubscribe<PuppeteerMsgType>(UnlocksEvent.PuppeteerOrderSent, OnPuppeteerOrderSent);
        _events.Unsubscribe(UnlocksEvent.PuppeteerOrderRecieved, OnPuppeteerReceivedOrder);
        _events.Unsubscribe<int>(UnlocksEvent.PuppeteerEmoteRecieved, OnPuppeteerReceivedEmoteOrder);
        _events.Unsubscribe<PuppetPerms>(UnlocksEvent.PuppeteerAccessGiven, OnPuppetAccessGiven);

        _events.Unsubscribe<PatternInteractionKind, Guid, bool>(UnlocksEvent.PatternAction, OnPatternAction);

        _events.Unsubscribe(UnlocksEvent.DeviceConnected, OnDeviceConnected);
        _events.Unsubscribe(UnlocksEvent.TriggerFired, OnTriggerFired);
        _events.Unsubscribe(UnlocksEvent.DeathRollCompleted, () => (LatestCache.SaveData.Achievements[Achievements.KinkyGambler.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<NewState>(UnlocksEvent.AlarmToggled, _ => (LatestCache.SaveData.Achievements[Achievements.Experimentalist.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.ShockSent, OnShockSent);
        _events.Unsubscribe(UnlocksEvent.ShockReceived, OnShockReceived);

        _events.Unsubscribe<InteractionType, NewState, string, string>(UnlocksEvent.HardcoreAction, OnHardcoreAction);

        _events.Unsubscribe(UnlocksEvent.RemoteOpened, () => (LatestCache.SaveData.Achievements[Achievements.JustVibing.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.VibeRoomCreated, () => (LatestCache.SaveData.Achievements[Achievements.VibingWithFriends.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe<NewState>(UnlocksEvent.VibratorsToggled, OnVibratorToggled);

        _events.Unsubscribe(UnlocksEvent.PvpPlayerSlain, OnPvpKill);
        _events.Unsubscribe(UnlocksEvent.ClientSlain, () => (LatestCache.SaveData.Achievements[Achievements.BadEndHostage.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<ChatChannel.Channels>(UnlocksEvent.ChatMessageSent, OnChatMessage);
        _events.Unsubscribe<IGameObject, ushort, IGameObject>(UnlocksEvent.EmoteExecuted, OnEmoteExecuted);
        _events.Unsubscribe(UnlocksEvent.TutorialCompleted, () => (LatestCache.SaveData.Achievements[Achievements.TutorialComplete.Id] as ProgressAchievement)?.CheckCompletion());
        _events.Unsubscribe(UnlocksEvent.PairAdded, OnPairAdded);
        _events.Unsubscribe(UnlocksEvent.PresetApplied, () => (LatestCache.SaveData.Achievements[Achievements.BoundaryRespecter.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.GlobalSent, () => (LatestCache.SaveData.Achievements[Achievements.HelloKinkyWorld.Id] as ProgressAchievement)?.IncrementProgress());
        _events.Unsubscribe(UnlocksEvent.CursedDungeonLootFound, OnCursedLootFound);
        _events.Unsubscribe(UnlocksEvent.ChocoboRaceFinished, () => (LatestCache.SaveData.Achievements[Achievements.WildRide.Id] as ConditionalAchievement)?.CheckCompletion());
        _events.Unsubscribe<int>(UnlocksEvent.PlayersInProximity, (count) => (LatestCache.SaveData.Achievements[Achievements.CrowdPleaser.Id] as ConditionalThresholdAchievement)?.UpdateThreshold(count));
        _events.Unsubscribe(UnlocksEvent.CutsceneInturrupted, () => (LatestCache.SaveData.Achievements[Achievements.WarriorOfLewd.Id] as ConditionalProgressAchievement)?.StartOverDueToInturrupt());

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

        Signatures.ActionEffectEntryEvent -= OnActionEffectEvent;
        _dutyState.DutyStarted -= OnDutyStart;
        _dutyState.DutyCompleted -= OnDutyEnd;
    }
}
