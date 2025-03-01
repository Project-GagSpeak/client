using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Linq;

namespace GagSpeak.PlayerState.Toybox;

public sealed class AlarmManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly PatternManager _patterns;
    private readonly PatternApplier _applier;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private DateTime _lastAlarmCheck = DateTime.MinValue;
    public AlarmManager(ILogger<AlarmManager> logger, GagspeakMediator mediator,
        PatternManager patterns, PatternApplier applier, FavoritesManager favorites,
        ConfigFileProvider fileNames, HybridSaveService saver) : base(logger, mediator)
    {
        _patterns = patterns;
        _applier = applier;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => MinutelyAlarmCheck());
    }

    // Cached Information
    public Alarm? ActiveEditorItem = null;
    public IEnumerable<Alarm> ActiveAlarms => Storage.Where(x => x.Enabled);

    // Storage
    public AlarmStorage Storage { get; private set; } = new AlarmStorage();

    public Alarm CreateNew(string alarmName)
    {
        var newAlarm = new Alarm() { Label = alarmName };
        Storage.Add(newAlarm);
        _saver.Save(this);
        Mediator.Publish(new ConfigAlarmChanged(StorageItemChangeType.Created, newAlarm, null));
        return newAlarm;
    }

    public Alarm CreateClone(Alarm other, string newName)
    {
        var clonedItem = new Alarm(other, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);
        Logger.LogDebug($"Cloned alarm {other.Label} to {newName}.");
        Mediator.Publish(new ConfigAlarmChanged(StorageItemChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Rename(Alarm alarm, string newName)
    {
        if (Storage.Contains(alarm))
        {
            Logger.LogDebug($"Storage contained alarm, renaming {alarm.Label} to {newName}.");
            var newNameReal = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
            alarm.Label = newNameReal;
            Mediator.Publish(new ConfigAlarmChanged(StorageItemChangeType.Renamed, alarm, newNameReal));
            _saver.Save(this);
        }
    }

    public void Delete(Alarm alarm)
    {
        if (ActiveEditorItem is null)
            return;

        if (Storage.Remove(alarm))
        {
            Logger.LogDebug($"Deleted alarm {alarm.Label}.");
            Mediator.Publish(new ConfigAlarmChanged(StorageItemChangeType.Deleted, alarm, null));
            _saver.Save(this);
        }

    }

    public void StartEditing(Alarm alarm)
    {
        if (Storage.Contains(alarm))
            ActiveEditorItem = alarm;
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => ActiveEditorItem = null;

    /// <summary> Injects all the changes made to the Restriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (ActiveEditorItem is null)
            return;

        if (Storage.ByIdentifier(ActiveEditorItem.Identifier) is { } item)
        {
            item = ActiveEditorItem;
            ActiveEditorItem = null;
            Mediator.Publish(new ConfigAlarmChanged(StorageItemChangeType.Modified, item, null));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the alarm as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool AddFavorite(Alarm alarm)
        => _favorites.TryAddRestriction(FavoriteIdContainer.Alarm, alarm.Identifier);

    /// <summary> Attempts to remove the alarm as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool RemoveFavorite(Alarm alarm)
        => _favorites.RemoveRestriction(FavoriteIdContainer.Alarm, alarm.Identifier);



    public void ToggleAlarm(Guid alarmId, string enactor)
    {
        if (Storage.TryGetAlarm(alarmId, out var alarm))
        {
            alarm.Enabled = !alarm.Enabled;
            _saver.Save(this);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled,
                alarm.Enabled ? NewState.Enabled : NewState.Disabled);
        }
    }

    public void EnableAlarm(Guid alarmId, string enactor)
    {
        // Locate the alarm in the storage to modify the properties of.
        if (Storage.TryGetAlarm(alarmId, out var alarm))
        {
            // set the data and save.
            alarm.Enabled = true;
            _saver.Save(this);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled, NewState.Enabled);
        }
    }

    public void DisableAlarm(Guid alarmId, string enactor)
    {
        // if this is false it means one is active for us to disable.
        if (Storage.TryGetAlarm(alarmId, out var alarm))
        {
            if(!alarm.Enabled)
                return;
            // set the data and save.
            alarm.Enabled = false;
            _saver.Save(this);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled, NewState.Disabled);
        }
    }

    #region HybridSavable
    public void Save() => _saver.Save(this);
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.Alarms).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // we need to iterate through our list of trigger objects and serialize them.
        var alarmItems = JArray.FromObject(Storage);
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Alarms"] = alarmItems,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.Alarms;
        Logger.LogInformation("Loading in Alarms Config for file: " + file);

        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Alarms Config file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);

        // Migrate the jObject if it is using the old format.
        if (jObject["AlarmStorage"] is JObject)
            jObject = ConfigMigrator.MigrateAlarmsConfig(jObject, _fileNames);

        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject["Alarms"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray alarms)
            return;

        foreach (var alarmToken in alarms)
        {
            try
            {
                var newAlarm = JsonConvert.DeserializeObject<Alarm>(alarmToken.ToString());
                if (newAlarm is Alarm)
                {
                    Logger.LogDebug("Loaded Alarm: " + newAlarm.ToString());
                    Storage.Add(newAlarm); // try and add it in.
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Error deserializing alarm: " + ex);
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable

    public void MinutelyAlarmCheck()
    {
        if ((DateTime.Now - _lastAlarmCheck).TotalMinutes < 1)
            return;

        _lastAlarmCheck = DateTime.Now; // Update the last execution time
        Logger.LogTrace("Checking Alarms", LoggerType.ToyboxAlarms);

        // Iterate through each stored alarm
        foreach (var alarm in Storage)
        {
            if (!alarm.Enabled)
                continue;

            // grab the current day of the week in our local timezone
            var currentDay = DateTime.Now.DayOfWeek;

            // check if current day is in our frequency list
            if (!alarm.RepeatFrequency.Contains(currentDay))
                continue;

            var alarmTime = alarm.SetTimeUTC.ToLocalTime();
            // check if current time matches execution time and if so play
            if (DateTime.Now.TimeOfDay.Hours == alarmTime.TimeOfDay.Hours && DateTime.Now.TimeOfDay.Minutes == alarmTime.TimeOfDay.Minutes)
            {
                Logger.LogInformation("Playing Alarm : " + alarm.PatternToPlay, LoggerType.ToyboxAlarms);
                // locate the alarm in the alarm storage that we need to play based on the alarms alarm to play.
                if (_patterns.Storage.TryGetPattern(alarm.PatternToPlay, out var pattern))
                    // Use the patternManager to switch the patterns if this is buggy.
                    _applier.StartPlayback(pattern, alarm.PatternStartPoint, alarm.PatternDuration);
            }
        }
    }
}
