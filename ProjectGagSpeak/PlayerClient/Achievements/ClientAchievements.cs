using CkCommons;
using System.Net.WebSockets;

namespace GagSpeak.PlayerClient;

public class ClientAchievements
{
    public const int Version = 4;

    private readonly ILogger<ClientAchievements> _logger;

    private static Dictionary<int, AchievementBase> _saveData = new();

    public ClientAchievements(ILogger<ClientAchievements> logger)
    {
        _logger = logger;
    }

    /// <summary> Publically accessible read-only scope of our current SaveData. </summary>
    public static IReadOnlyDictionary<int, AchievementBase> SaveData => _saveData;

    /// <summary> If the save data loaded was considered valid. </summary>
    public static bool HasValidData { get; private set; } = false;

    /// <summary> Flagged upon any unhandled disconnect. (Protects unhandled disconnects) </summary>
    public static bool HadUnhandledDC { get; set; } = false;

    public static int Total => _saveData.Count;
    public static int Completed => _saveData.Values.Count(x => x.IsCompleted);
    public static IEnumerable<AchievementBase> CompletedAchievements => _saveData.Values.Where(a => a.IsCompleted);
    public static string GetTitleById(int id) => _saveData.TryGetValue(id, out var item) ? item.Title : "No Title Set";
    public static IEnumerable<AchievementBase> GetByModule(AchievementModuleKind m) => _saveData.Values.Where(a => a.Module == m);

    public void HadUnhandledDisconnect(WebSocketException ex)
    {
        _logger.LogWarning("System closed unexpectedly, flagging Achievement Manager to not set data on reconnection.");
        HadUnhandledDC = true;
    }

    public void OnConnection(string? compressedBase64Data)
    {
        if (HadUnhandledDC && HasValidData)
        {
            _logger.LogWarning("Reconnection detected after an unhandled disconnect. Not storing retrieved data.");
            // Simply toggle this back off, and continue.
            HadUnhandledDC = false;
            return;
        }

        // if the string was null or empty, we should upload fresh data instead!
        if (string.IsNullOrEmpty(compressedBase64Data))
        {
            _logger.LogWarning("Received empty achievement data from server, uploading fresh data instead.");
            // Serialize and upload fresh data.
            // Note that this is ok to simply return, since on every disconnect
            // and startup, the achievements are already re-initialized.
            HasValidData = true;
        }
        else
        {
            _logger.LogInformation("Received achievement data from server, attempting to deserialize.");
            // Handles the end state of HasValidData internally.
            DeserializeData(compressedBase64Data);
        }
    }


    public void ResetAchievements(bool invalidate = true)
    {
        _saveData.Clear();
        HasValidData = !invalidate;
        _logger.LogInformation("All SaveData was cleared.");
    }

    /// <summary> Serializes the current achievement data into a compressed Base64 string. </summary>
    /// <returns> The compressed base64 string. </returns>
    /// <remarks> Even if achievement data changes, the raw json should remain parsable for deserialization. </remarks>
    public string SerializeData()
    {
        // compile everything to light savedata.
        var lightData = new List<LightAchievement>();
        foreach (var item in _saveData.Values)
        {
            lightData.Add(new LightAchievement
            {
                Type = item.GetAchievementType(),
                AchievementId = item.AchievementId,
                IsCompleted = item.IsCompleted,
                Progress = GetProgress(item),
                ConditionalTaskBegun = item is ConditionalProgressAchievement cpa ? cpa.ConditionalTaskBegun : false,
                StartTime = GetStartTime(item),
                RecordedDateTimes = item is TimedProgressAchievement tpa ? tpa.ProgressTimestamps : new List<DateTime>(),
                ActiveItems = item is DurationAchievement da ? da.ActiveItems : new List<TrackedItem>()
            });
        }

        // create an optimized compressed string.
        var rawJson = new JObject
        {
            ["Version"] = Version,
            ["LightAchievementData"] = JArray.FromObject(lightData),
        }.ToString(Formatting.Indented);

        // return the compressed version.
        var compressed = rawJson.Compress(6);
        var base64Data = Convert.ToBase64String(compressed);
        return base64Data;
    }

    /// <summary>
    ///     Deserializes the provided compressed Base64 string into the achievement data.
    /// </summary>
    public void DeserializeData(string compressedBase64Data)
    {
        try
        {
            // get the bytes.
            var bytes = Convert.FromBase64String(compressedBase64Data);
            var version = bytes[0];
            // get the version and the decompressed string.
            version = bytes.DecompressToString(out var decompressed);
            // Deserialize internally.
            var dataJObject = JObject.Parse(decompressed);

            // Support any version migration in the future here to correct deserializations.

            // Handle data setting. Exception can be thrown if the load fails here.
            SetSaveData(dataJObject["LightAchievementData"]);

            // Reaching here implies success.
            _logger.LogInformation("Achievement Data Loaded from Server", LoggerType.Achievements);
            _logger.LogDebug("Achievement Data String Loaded:\n" + compressedBase64Data);
            HasValidData = true;
        }
        catch (Bagagwa ex)
        {
            _logger.LogError("Failed to load Achievement Data from server. Blocking all further AchievementAutoSaves." +
                $"This is done to help keep old data intact. \n[REASON]: {ex.Message}");
            HasValidData = false;
        }
    }

    public void AddProgress(AchievementModuleKind module, AchievementInfo info, int goal, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new ProgressAchievement(module, info, goal, onCompleted, prefix, suffix, isSecret));

    public void AddConditional(AchievementModuleKind module, AchievementInfo info, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new ConditionalAchievement(module, info, cond, onCompleted, prefix, suffix, isSecret));

    public void AddThreshold(AchievementModuleKind module, AchievementInfo info, int goal, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new ThresholdAchievement(module, info, goal, onCompleted, prefix, suffix, isSecret));

    public void AddDuration(AchievementModuleKind module, AchievementInfo info, TimeSpan duration, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new DurationAchievement(module, info, duration, onCompleted, timeUnit, prefix, suffix, isSecret));

    public void AddRequiredTimeConditional(AchievementModuleKind module, AchievementInfo info, TimeSpan duration, Func<bool> cond, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new TimeRequiredConditionalAchievement(module, info, duration, cond, onCompleted, timeUnit, prefix, suffix, isSecret));

    public void AddTimeLimitedConditional(AchievementModuleKind module, AchievementInfo info, TimeSpan dur, Func<bool> cond, DurationTimeUnit timeUnit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new TimeLimitConditionalAchievement(module, info, dur, cond, onCompleted, timeUnit, prefix, suffix, isSecret));

    public void AddConditionalProgress(AchievementModuleKind module, AchievementInfo info, int goal, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool reqBeginAndFinish = true, bool isSecret = false)
        => _saveData.Add(info.Id, new ConditionalProgressAchievement(module, info, goal, cond, onCompleted, reqBeginAndFinish, prefix, suffix, isSecret));

    public void AddConditionalThreshold(AchievementModuleKind module, AchievementInfo info, int goal, Func<bool> cond, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new ConditionalThresholdAchievement(module, info, goal, cond, onCompleted, prefix, suffix, isSecret));

    public void AddTimedProgress(AchievementModuleKind module, AchievementInfo info, int goal, TimeSpan timeLimit, Action<int, string> onCompleted, string suffix = "", string prefix = "", bool isSecret = false)
        => _saveData.Add(info.Id, new TimedProgressAchievement(module, info, goal, timeLimit, onCompleted, prefix, suffix, isSecret));

    /// <summary> Attempts to set the SaveData from the provided JToken. </summary>
    /// <remarks> Throws exception if an ID exists in the AchievementMap but not <see cref="_saveData"/>, or if not given JArray. </remarks>
    /// <exception cref="Exception"> Thrown if an ID exists in the achievement map but not in the save data. </exception>
    private void SetSaveData(JToken? token)
    {
        if(token is not JArray lightDataArray)
            throw new ArgumentException("The provided JToken was not a JArray of LightAchievement data.");

        // Set the data.
        foreach(var light in lightDataArray)
        {
            // get the Id from the lightData.
            var id = light["AchievementId"]?.Value<int>() ?? -1;

            // Check and correct achievement data against AchievementMap
            if (!Achievements.AchievementMap.ContainsKey(id))
            {
                _logger.LogWarning($"The achievementId [{id}] is no longer in GagSpeak's AchievementMap. Skipping over it.");
                continue; // move to next one, it could just be a discontinued one.
            }

            // attempt to get and update the achievement.
            if(!_saveData.TryGetValue(id, out var dataAbstract))
                throw new ArgumentNullException($"Achievement with ID [{id}] was in GagSpeak's AchievementMap but not the SaveData. Something went wrong!");

            // Set the new data based on what type it was.
            switch (dataAbstract)
            {
                case ProgressAchievement pa:
                    pa.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    pa.Progress = light["Progress"]?.Value<int>() ?? 0;
                    break;

                case ConditionalAchievement ca:
                    ca.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    break;

                case ThresholdAchievement ta:
                    ta.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    break;

                case DurationAchievement da:
                    da.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    da.ActiveItems = light["ActiveItems"]?.ToObject<List<TrackedItem>>() ?? new List<TrackedItem>();
                    break;

                case ConditionalThresholdAchievement cta:
                    cta.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    break;

                case ConditionalProgressAchievement cpa:
                    cpa.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    cpa.Progress = light["Progress"]?.Value<int>() ?? 0;
                    cpa.ConditionalTaskBegun = light["ConditionalTaskBegun"]?.Value<bool>() ?? false;
                    break;

                case TimedProgressAchievement tpa:
                    tpa.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    tpa.ProgressTimestamps = light["RecordedDateTimes"]?.ToObject<List<DateTime>>() ?? new List<DateTime>();
                    break;

                case TimeLimitConditionalAchievement tla:
                    tla.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    tla.StartPoint = light["StartTime"]?.Value<DateTime>() ?? DateTime.MinValue;
                    break;

                case TimeRequiredConditionalAchievement tra:
                    tra.IsCompleted = light["IsCompleted"]?.Value<bool>() ?? false;
                    tra.StartPoint = light["StartTime"]?.Value<DateTime>() ?? DateTime.MinValue;
                    break;
            }
        };
    }

    // Helpers Below.
    private int GetProgress(AchievementBase achievement)
        => achievement switch
        {
            ConditionalProgressAchievement cpa => cpa.Progress,
            ProgressAchievement pa => pa.Progress,
            _ => 0
        };

    private DateTime GetStartTime(AchievementBase achievement)
        => achievement switch
        {
            TimeLimitConditionalAchievement tl => tl.StartPoint,
            TimeRequiredConditionalAchievement tr => tr.StartPoint,
            _ => DateTime.MinValue
        };
}
