using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;

namespace GagSpeak.PlayerState.Toybox;

public sealed class AlarmManager : AlarmEditor, IMediatorSubscriber, IDisposable
{
    private readonly PatternApplier _applier;
    public GagspeakMediator Mediator { get; }
    public AlarmManager(ILogger<AlarmManager> logger, GagspeakMediator mediator,
        GagspeakConfigService mainConfig, PatternConfigService patterns,
        AlarmConfigService alarms, PatternApplier patternApplier) : base(logger, mainConfig, patterns, alarms)
    {
        Mediator = mediator;
        _applier = patternApplier;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => MinutelyAlarmCheck());
    }

    private DateTime _lastAlarmCheck = DateTime.MinValue;

    public AlarmStorage Storage => _alarms.Current.Storage;

    public void Dispose() => Mediator.UnsubscribeAll(this);

    public void OnLogin() { }

    public void OnLogout() { }

    public Alarm CreateNew(string alarmName)
    {
        return new Alarm() { Label = alarmName };
    }

    public Alarm CreateClone(Alarm other, string newName)
    {
        return new Alarm() { Label = newName };
    }

    public void Delete(Alarm alarm)
    {

    }

    public void Rename(Alarm alarm, string newName)
    {
        var oldName = alarm.Label;
        if(oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        alarm.Label = newName;
    }

    public void StartEditing(Alarm alarm)
    {

    }

    public void StopEditing()
    {

    }

    public void AddFavorite(Alarm alarm)
    {

    }

    public void RemoveFavorite(Alarm alarm)
    {

    }

    public void ToggleAlarm(Guid alarmId, string enactor)
    {
        if (Storage.TryGetAlarm(alarmId, out var alarm))
        {
            alarm.Enabled = !alarm.Enabled;
            _alarms.Save();
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
            _alarms.Save();
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
            _alarms.Save();
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled, NewState.Disabled);
        }
    }

    public void MinutelyAlarmCheck()
    {
        if ((DateTime.Now - _lastAlarmCheck).TotalMinutes < 1)
            return;

        _lastAlarmCheck = DateTime.Now; // Update the last execution time
        _logger.LogTrace("Checking Alarms", LoggerType.ToyboxAlarms);

        // Iterate through each stored alarm
        foreach (var alarm in Storage)
        {
            if (!alarm.Enabled) continue;

            // grab the current day of the week in our local timezone
            var currentDay = DateTime.Now.DayOfWeek;

            // check if current day is in our frequency list
            if (!alarm.RepeatFrequency.Contains(currentDay))
                continue;

            var alarmTime = alarm.SetTimeUTC.ToLocalTime();
            // check if current time matches execution time and if so play
            if (DateTime.Now.TimeOfDay.Hours == alarmTime.TimeOfDay.Hours && 
                DateTime.Now.TimeOfDay.Minutes == alarmTime.TimeOfDay.Minutes)
            {
                _logger.LogInformation("Playing Pattern : " + alarm.PatternToPlay, LoggerType.ToyboxAlarms);
                // locate the pattern in the pattern storage that we need to play based on the alarms pattern to play.
                if (_patterns.Current.Storage.TryGetPattern(alarm.PatternToPlay, out var pattern))
                    _applier.StartPlayback(pattern, alarm.PatternStartPoint, alarm.PatternDuration);
            }
        }
    }
}

public class AlarmEditor(ILogger logger, GagspeakConfigService mainConfig,
    PatternConfigService patterns, AlarmConfigService alarms)
{
    protected readonly ILogger _logger = logger;
    protected readonly GagspeakConfigService _mainConfig = mainConfig;
    protected readonly PatternConfigService _patterns = patterns;
    protected readonly AlarmConfigService _alarms = alarms;

    // methods for the editor and stuff.
    public string GetAlarmFrequencyString(List<DayOfWeek> FrequencyOptions)
    {
        // if the alarm is empty, return "never".
        if (FrequencyOptions.Count == 0) return "Never";
        // if the alarm contains all days of the week, return "every day".
        if (FrequencyOptions.Count == 7) return "Every Day";
        // List size can contain multiple days, but cannot contain "never" or "every day".
        var result = "";
        foreach (var freq in FrequencyOptions)
        {
            switch (freq)
            {
                case DayOfWeek.Sunday: result += "Sun"; break;
                case DayOfWeek.Monday: result += "Mon"; break;
                case DayOfWeek.Tuesday: result += "Tue"; break;
                case DayOfWeek.Wednesday: result += "Wed"; break;
                case DayOfWeek.Thursday: result += "Thu"; break;
                case DayOfWeek.Friday: result += "Fri"; break;
                case DayOfWeek.Saturday: result += "Sat"; break;
            }
            result += ", ";
        }
        // remove the last comma and space.
        result = result.Remove(result.Length - 2);
        return result;
    }
}
