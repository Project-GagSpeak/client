using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using System.Linq;

namespace GagSpeak.PlayerState.Toybox;

public sealed class AlarmManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly PatternManager _patterns;
    private readonly PatternApplier _applier;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<Alarm> _itemEditor = new();
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

    public AlarmStorage Storage { get; private set; } = new AlarmStorage();
    public Alarm? ItemInEditor => _itemEditor.ItemInEditor;
    public IEnumerable<Alarm> ActiveAlarms => Storage.Where(x => x.Enabled);

    public Alarm CreateNew(string alarmName)
    {
        alarmName = RegexEx.EnsureUniqueName(alarmName, Storage, t => t.Label);
        var newAlarm = new Alarm() { Label = alarmName };
        Storage.Add(newAlarm);
        _saver.Save(this);

        Mediator.Publish(new ConfigAlarmChanged(StorageChangeType.Created, newAlarm, null));
        return newAlarm;
    }

    public Alarm CreateClone(Alarm other, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, t => t.Label);
        var clonedItem = new Alarm(other, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);

        Logger.LogDebug($"Cloned alarm {other.Label} to {newName}.");
        Mediator.Publish(new ConfigAlarmChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Rename(Alarm alarm, string newName)
    {
        if (Storage.Contains(alarm))
        {
            var prevName = alarm.Label;
            Logger.LogDebug($"Storage contained alarm, renaming {alarm.Label} to {newName}.");
            newName = RegexEx.EnsureUniqueName(newName, Storage, (t) => t.Label);
            alarm.Label = newName;
            _saver.Save(this);

            Mediator.Publish(new ConfigAlarmChanged(StorageChangeType.Renamed, alarm, prevName));
        }
    }

    public void Delete(Alarm alarm)
    {
        if (Storage.Remove(alarm))
        {
            Logger.LogDebug($"Deleted alarm {alarm.Label}.");
            Mediator.Publish(new ConfigAlarmChanged(StorageChangeType.Deleted, alarm, null));
            _saver.Save(this);
        }

    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(Alarm item) => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogDebug($"Storage updated changes to alarm {sourceItem.Label}.");
            Mediator.Publish(new ConfigAlarmChanged(StorageChangeType.Modified, sourceItem, null));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the alarm as a favorite. </summary>
    public bool AddFavorite(Alarm a) => _favorites.TryAddRestriction(FavoriteIdContainer.Alarm, a.Identifier);

    /// <summary> Attempts to remove the alarm as a favorite. </summary>
    public bool RemoveFavorite(Alarm a) => _favorites.RemoveRestriction(FavoriteIdContainer.Alarm, a.Identifier);

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
        var alarmItems = JArray.FromObject(Storage.Select(a => a.Serialize()));
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
        Mediator.Publish(new ReloadFileSystem(Module.Alarm));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray alarms)
            return;

        foreach (var alarmToken in alarms)
        {
            try
            {
                var newAlarm = Alarm.FromToken(alarmToken, _patterns);
                Logger.LogDebug("Loaded Alarm: " + newAlarm.ToString());
                Storage.Add(newAlarm);
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

            // check if the alarm is set to repeat on this day
            if(!alarm.DaysToFire.HasAny(DateTime.Now.DayOfWeek.ToFlagVariant()))
            {
                var alarmTime = alarm.SetTimeUTC.ToLocalTime();
                // check if current time matches execution time and if so play
                if (DateTime.Now.TimeOfDay.Hours == alarmTime.TimeOfDay.Hours 
                 && DateTime.Now.TimeOfDay.Minutes == alarmTime.TimeOfDay.Minutes)
                {
                    Logger.LogInformation($"Playing Alarm: {alarm.PatternRef.Label} ({alarm.PatternRef.Identifier})", LoggerType.ToyboxAlarms);
                    _applier.StartPlayback(alarm.PatternRef, alarm.PatternStartPoint, alarm.PatternDuration);
                }
            }
        }
    }
}
