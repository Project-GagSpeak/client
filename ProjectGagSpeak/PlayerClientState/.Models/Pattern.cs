using GagspeakAPI.Data.Character;

namespace GagSpeak.PlayerState.Models;

[Serializable]
public class Pattern
{
    public Guid Identifier { get; set; } = Guid.Empty;

    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PlaybackDuration { get; set; } = TimeSpan.Zero;
    public bool ShouldLoop { get; set; } = false;
    public List<byte> PatternData { get; set; } = new();

    public LightPattern ToLightData() => new LightPattern(Identifier, Label, Description, Duration, ShouldLoop);
    public JObject Serialize()
    {
        // Convert _patternData to a comma-separated string
        var patternDataString = string.Join(",", PatternData);

        return new JObject()
        {
            ["Identifier"] = Identifier,
            ["Label"] = Label,
            ["Description"] = Description,
            ["Duration"] = Duration,
            ["StartPoint"] = StartPoint,
            ["PlaybackDuration"] = PlaybackDuration,
            ["ShouldLoop"] = ShouldLoop,
            ["PatternByteData"] = patternDataString,
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        try
        {
            Identifier = Guid.TryParse(jsonObject["Identifier"]?.Value<string>(), out var guid) ? guid : Guid.Empty;
            Label = jsonObject["Label"]?.Value<string>() ?? string.Empty;
            Description = jsonObject["Description"]?.Value<string>() ?? string.Empty;
            Duration = TimeSpan.TryParse(jsonObject["Duration"]?.Value<string>(), out var duration) ? duration : TimeSpan.Zero;
            StartPoint = TimeSpan.TryParse(jsonObject["StartPoint"]?.Value<string>(), out var startPoint) ? startPoint : TimeSpan.Zero;
            PlaybackDuration = TimeSpan.TryParse(jsonObject["PlaybackDuration"]?.Value<string>(), out var playbackDuration) ? playbackDuration : TimeSpan.Zero;
            ShouldLoop = jsonObject["ShouldLoop"]?.Value<bool>() ?? false;

            PatternData.Clear();
            var patternDataString = jsonObject["PatternByteData"]?.Value<string>();
            if (string.IsNullOrEmpty(patternDataString))
            {
                // If the string is null or empty, generate a list with a single byte of 0
                PatternData = new List<byte> { (byte)0 };
            }
            else
            {
                // Otherwise, split the string into an array and convert each element to a byte
                PatternData = patternDataString.Split(',')
                    .Select(byte.Parse)
                    .ToList();
            }
        }
        catch (System.Exception e) { throw new Exception($"{e} Error deserializing pattern data"); }
    }
}
