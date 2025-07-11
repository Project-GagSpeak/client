using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

[Serializable]
public class Pattern : IEditableStorageItem<Pattern>
{
    public Guid Identifier { get; internal set; } = Guid.NewGuid();
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PlaybackDuration { get; set; } = TimeSpan.Zero;
    public bool ShouldLoop { get; set; } = false;
    public FullPatternData PlaybackData { get; set; } = new();

    public Pattern()
    { }

    public Pattern(Pattern other, bool copyIdentifier = true)
    {
        Identifier = copyIdentifier ? other.Identifier : Guid.NewGuid();
        ApplyChanges(other);
    }

    public static Pattern AsEmpty() => new Pattern { Identifier = Guid.Empty };
    public Pattern Clone(bool keepId) => new Pattern(this, keepId);

    public void ApplyChanges(Pattern other)
    {
        Label = other.Label;
        Description = other.Description;
        Duration = other.Duration;
        StartPoint = other.StartPoint;
        PlaybackDuration = other.PlaybackDuration;
        ShouldLoop = other.ShouldLoop;
        PlaybackData = other.PlaybackData;

    }

    public LightPattern ToLightPattern() 
        => new LightPattern(Identifier, Label, Description, Duration, ShouldLoop);

    public JObject Serialize()
    {
        return new JObject()
        {
            ["Identifier"] = Identifier,
            ["Label"] = Label,
            ["Description"] = Description,
            ["Duration"] = Duration,
            ["StartPoint"] = StartPoint,
            ["PlaybackDuration"] = PlaybackDuration,
            ["ShouldLoop"] = ShouldLoop,
            ["PlaybackData"] = PlaybackData.ToCompressedBase64(),
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        Identifier = Guid.TryParse(jsonObject["Identifier"]?.Value<string>(), out var guid) ? guid : throw new Exception("Invalid GUID Data!");
        Label = jsonObject["Label"]?.Value<string>() ?? string.Empty;
        Description = jsonObject["Description"]?.Value<string>() ?? string.Empty;
        Duration = TimeSpan.TryParse(jsonObject["Duration"]?.Value<string>(), out var duration) ? duration : TimeSpan.Zero;
        StartPoint = TimeSpan.TryParse(jsonObject["StartPoint"]?.Value<string>(), out var startPoint) ? startPoint : TimeSpan.Zero;
        PlaybackDuration = TimeSpan.TryParse(jsonObject["PlaybackDuration"]?.Value<string>(), out var playbackDuration) ? playbackDuration : TimeSpan.Zero;
        ShouldLoop = jsonObject["ShouldLoop"]?.Value<bool>() ?? false;

        // Deserialize the FullPatternData.
        var patternDataString = jsonObject["PlaybackData"]?.Value<string>();
        PlaybackData = string.IsNullOrEmpty(patternDataString) ? FullPatternData.Empty : FullPatternData.FromCompressedBase64(patternDataString);
    }
}
