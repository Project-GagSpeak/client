using GagspeakAPI.Data;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public class Alarm : IEditableStorageItem<Alarm>
{
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset SetTimeUTC { get; set; } = DateTimeOffset.MinValue;
    public Guid PatternToPlay { get; set; } = Guid.Empty;
    public TimeSpan PatternStartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PatternDuration { get; set; } = TimeSpan.Zero;
    public List<DayOfWeek> RepeatFrequency { get; set; } = [];

    public Alarm()
    { }

    public Alarm(Alarm other, bool copyIdentifier = true)
    {
        Identifier = copyIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public Alarm Clone(bool keepId) => new Alarm(this, keepId);

    public void ApplyChanges(Alarm changedItem)
    {
        Enabled = changedItem.Enabled;
        Label = changedItem.Label;
        SetTimeUTC = changedItem.SetTimeUTC;
        PatternToPlay = changedItem.PatternToPlay;
        PatternStartPoint = changedItem.PatternStartPoint;
        PatternDuration = changedItem.PatternDuration;
        RepeatFrequency = changedItem.RepeatFrequency;
    }

    public void Deserialize(JToken alarmToken)
    {
        if(alarmToken is not JObject alarmObject)
            return;

        Identifier = alarmObject["Identifier"]?.ToObject<Guid>() ?? throw new Exception("Failed to load alarm identifier.");
        Enabled = alarmObject["Enabled"]?.Value<bool>() ?? false;
        Label = alarmObject["Label"]?.Value<string>() ?? string.Empty;
        if (DateTimeOffset.TryParse(alarmObject["SetTimeUTC"]?.ToString(), out DateTimeOffset parsedTime))
        {
            SetTimeUTC = parsedTime;
        }
        else
        {
            // Handle any parsing errors, fallback to a known default (e.g., MinValue).
            SetTimeUTC = DateTimeOffset.MinValue;
        }
        PatternToPlay = alarmObject["PatternToPlay"]?.ToObject<Guid>() ?? Guid.Empty;
        PatternStartPoint = alarmObject["PatternStartPoint"]?.Value<TimeSpan>() ?? TimeSpan.Zero;
        PatternDuration = alarmObject["PatternDuration"]?.Value<TimeSpan>() ?? TimeSpan.Zero;
        RepeatFrequency = alarmObject["RepeatFrequency"]?.ToObject<List<DayOfWeek>>() ?? [];
    }

    public LightAlarm ToLightAlarm()
        => new LightAlarm(Identifier, Label, SetTimeUTC, PatternToPlay);
}
