using GagSpeak.PlayerState.Toybox;
using GagspeakAPI.Data;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public class Alarm : IEditableStorageItem<Alarm>
{
    public Guid Identifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public string Label { get; set; } = string.Empty;
    public DateTimeOffset SetTimeUTC { get; set; } = DateTimeOffset.MinValue;
    public Pattern PatternRef { get; set; } = Pattern.AsEmpty();
    public TimeSpan PatternStartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PatternDuration { get; set; } = TimeSpan.Zero;
    public DaysOfWeek DaysToFire { get; set; } = DaysOfWeek.None;

    public Alarm()
    { }

    public Alarm(Alarm other, bool copyIdentifier = true)
    {
        Identifier = copyIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public static Alarm AsEmpty() => new Alarm { Identifier = Guid.Empty };
    public Alarm Clone(bool keepId) => new Alarm(this, keepId);

    public void ApplyChanges(Alarm changedItem)
    {
        Enabled = changedItem.Enabled;
        Label = changedItem.Label;
        SetTimeUTC = changedItem.SetTimeUTC;
        PatternRef = changedItem.PatternRef;
        PatternStartPoint = changedItem.PatternStartPoint;
        PatternDuration = changedItem.PatternDuration;
        DaysToFire = changedItem.DaysToFire;
    }

    public JObject Serialize()
    {
        return new JObject()
        {
            ["Identifier"] = Identifier,
            ["Enabled"] = Enabled,
            ["Label"] = Label,
            ["SetTimeUTC"] = SetTimeUTC.ToString("o"),
            ["PatternRef"] = PatternRef.Identifier,
            ["PatternStartPoint"] = PatternStartPoint,
            ["PatternDuration"] = PatternDuration,
            ["DaysToFire"] = DaysToFire.ToString(),
        };
    }

    /// <summary> Attempts to load an alarm object from a JToken.</summary>
    /// <param name="alarmToken"> The token to load the alarm from.</param>
    /// <param name="patterns"> The pattern manager to use for loading the pattern reference.</param>
    /// <returns> The loaded alarm object.</returns>
    /// <exception cref="Exception"> When a GUID is invalid or not defined, this is thrown. </exception>
    /// <remarks><b>THIS CAN THROW AN EXCEPTION</b></remarks>
    public static Alarm FromToken(JToken alarmToken, PatternManager patterns)
    {
        if (alarmToken is not JObject alarmObject)
            return AsEmpty();

        // try to load the pattern ref.
        Pattern patternRef = Pattern.AsEmpty();
        if (Guid.TryParse(alarmObject["PatternRef"]?.ToString(), out var refGuid))
        {
            if (patterns.Storage.FirstOrDefault(p => p.Identifier == refGuid) is { } match)
                patternRef = match;
            else
                GagSpeak.StaticLog.Warning("Alarm Referenced Pattern no longer exists!");
        }
        else throw new Exception("Invalid GUID format in GUID field.");

        // Grab the setTimeUTC
        var setTime = DateTimeOffset.TryParse(alarmObject["SetTimeUTC"]?.ToString(), out DateTimeOffset parsedTime)
            ? parsedTime : DateTimeOffset.MinValue;
        
        var alarm = new Alarm()
        {
            Identifier = Guid.TryParse(alarmObject["Identifier"]?.ToString(), out var id) ? id : throw new Exception("Bad Identifier for Alarm!"),
            Enabled = alarmObject["Enabled"]?.Value<bool>() ?? false,
            Label = alarmObject["Label"]?.Value<string>() ?? string.Empty,
            SetTimeUTC = setTime,
            PatternRef = patternRef,
            PatternStartPoint = TimeSpan.TryParse(alarmObject["PatternStartPoint"]?.ToString(), out var startPoint)
                ? startPoint : TimeSpan.Zero,
            PatternDuration = TimeSpan.TryParse(alarmObject["PatternDuration"]?.ToString(), out var duration)
                ? duration : TimeSpan.Zero,
            DaysToFire = Enum.TryParse<DaysOfWeek>(alarmObject["DaysToFire"]?.ToObject<string>(), out var traits) 
                ? traits : DaysOfWeek.None,
        };
        return alarm;
    }

    public LightAlarm ToLightAlarm()
        => new LightAlarm(Identifier, Label, SetTimeUTC, PatternRef.Identifier);
}
