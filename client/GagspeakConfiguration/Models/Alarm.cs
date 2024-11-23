using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public record Alarm
{
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset SetTimeUTC { get; set; } = DateTimeOffset.MinValue;
    public Guid PatternToPlay { get; set; } = Guid.Empty;
    public TimeSpan PatternStartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PatternDuration { get; set; } = TimeSpan.Zero;
    public List<DayOfWeek> RepeatFrequency { get; set; } = [];

    public LightAlarm ToLightData()
        => new LightAlarm
        {
            Identifier = Identifier,
            Name = Name,
            SetTimeUTC = SetTimeUTC,
            PatternThatPlays = PatternToPlay.ToString()
        };

    public Alarm DeepCloneAlarm()
        => new Alarm()
        {
            // do not clone the identifier, as we want a new one.
            Enabled = Enabled,
            Name = Name,
            SetTimeUTC = SetTimeUTC,
            PatternToPlay = PatternToPlay,
            PatternStartPoint = PatternStartPoint,
            PatternDuration = PatternDuration,
            RepeatFrequency = new List<DayOfWeek>(RepeatFrequency)
        };
}
