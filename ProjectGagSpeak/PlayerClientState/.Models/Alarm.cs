using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public class Alarm
{
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset SetTimeUTC { get; set; } = DateTimeOffset.MinValue;
    public Guid PatternToPlay { get; set; } = Guid.Empty;
    public TimeSpan PatternStartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PatternDuration { get; set; } = TimeSpan.Zero;
    public List<DayOfWeek> RepeatFrequency { get; set; } = [];

    internal Alarm() { }

    public Alarm(Alarm other, bool copyIdentifier)
    {
        if (copyIdentifier)
            Identifier = other.Identifier;

        Enabled = other.Enabled;
        Label = other.Label;
        SetTimeUTC = other.SetTimeUTC;
        PatternToPlay = other.PatternToPlay;
        PatternStartPoint = other.PatternStartPoint;
        PatternDuration = other.PatternDuration;
        RepeatFrequency = other.RepeatFrequency;
    }

    public LightAlarm ToLightData()
        => new LightAlarm(Identifier, Label, SetTimeUTC, PatternToPlay);
}
