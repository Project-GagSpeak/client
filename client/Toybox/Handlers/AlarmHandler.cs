using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerData.Handlers;

/// <summary>
/// A Handler that manages how we modify our alarm information for edits.
/// For Toggling states and updates via non-direct edits, see ToyboxManager.
/// </summary>
public class AlarmHandler : MediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ToyboxManager _toyboxStateManager;
    private DateTime _lastExecutionTime;

    public AlarmHandler(ILogger<AlarmHandler> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, ToyboxManager toyboxStateManager)
        : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _toyboxStateManager = toyboxStateManager;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => DelayedFrameworkAlarmCheck());
    }

    public List<Alarm> Alarms => _clientConfigs.AlarmConfig.AlarmStorage.Alarms;
    public int AlarmCount => _clientConfigs.AlarmConfig.AlarmStorage.Alarms.Count;

    public Alarm? ClonedAlarmForEdit { get; private set; } = null;

    public void StartEditingAlarm(Alarm alarm)
    {
        ClonedAlarmForEdit = alarm.DeepCloneAlarm();
        Guid originalID = alarm.Identifier; // Prevent storing the alarm ID by reference.
        ClonedAlarmForEdit.Identifier = originalID; // Ensure the ID remains the same here.
    }

    public void CancelEditingAlarm() => ClonedAlarmForEdit = null;

    public void SaveEditedAlarm()
    {
        if (ClonedAlarmForEdit is null)
            return;
        // locate the restraint set that contains the matching guid.
        var setIdx = _clientConfigs.AlarmConfig.AlarmStorage.Alarms
            .FindIndex(x => x.Identifier == ClonedAlarmForEdit.Identifier);

        // update that set with the new cloned set.
        _clientConfigs.UpdateAlarm(ClonedAlarmForEdit, setIdx);
        // make the cloned set null again.
        ClonedAlarmForEdit = null;
    }

    public void AddNewAlarm(Alarm newPattern) => _clientConfigs.AddNewAlarm(newPattern);
    public void RemoveAlarm(Alarm alarmToRemove)
    {
        _clientConfigs.RemoveAlarm(alarmToRemove);
        CancelEditingAlarm();
    }

    public void EnableAlarm(Alarm alarm)
    => _toyboxStateManager.EnableAlarm(alarm.Identifier);

    public void DisableAlarm(Alarm alarm)
        => _toyboxStateManager.DisableAlarm(alarm.Identifier);

    public string PatternName(Guid patternId)
        => _clientConfigs.PatternConfig.PatternStorage.Patterns.FirstOrDefault(p => p.UniqueIdentifier == patternId)?.Name ?? "Unknown";

    public TimeSpan GetPatternLength(Guid guid)
        => _clientConfigs.PatternConfig.PatternStorage.Patterns.FirstOrDefault(p => p.UniqueIdentifier == guid)?.Duration ?? TimeSpan.Zero;

    public string GetAlarmFrequencyString(List<DayOfWeek> FrequencyOptions)
    {
        // if the alarm is empty, return "never".
        if (FrequencyOptions.Count == 0) return "Never";
        // if the alarm contains all days of the week, return "every day".
        if (FrequencyOptions.Count == 7) return "Every Day";
        // List size can contain multiple days, but cannot contain "never" or "every day".
        string result = "";
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

    public void DelayedFrameworkAlarmCheck()
    {
        if ((DateTime.Now - _lastExecutionTime).TotalSeconds < 60)
        {
            return; // Exit if less than 60 seconds have passed since the last execution
        }

        _lastExecutionTime = DateTime.Now; // Update the last execution time

        Logger.LogTrace("Checking Alarms", LoggerType.ToyboxAlarms);

        // Iterate through each stored alarm
        foreach (var alarm in Alarms)
        {
            // if the alarm is not enabled, continue
            if (!alarm.Enabled)
                continue;

            // grab the current day of the week in our local timezone
            DateTime now = DateTime.Now;
            DayOfWeek currentDay = now.DayOfWeek;

            // check if current day is in our frequency list
            if (!alarm.RepeatFrequency.Contains(currentDay))
                continue;

            // convert execution time from UTC to our local timezone
            DateTimeOffset alarmTime = alarm.SetTimeUTC.ToLocalTime();

            // check if current time matches execution time and if so play
            if (now.Hour == alarmTime.Hour && now.Minute == alarmTime.Minute)
            {
                Logger.LogInformation("Playing Pattern : " + alarm.PatternToPlay, LoggerType.ToyboxAlarms);
                _toyboxStateManager.FireAlarmPattern(alarm);
            }
        }
    }
}
