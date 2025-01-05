using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public record PatternData
{
    public Guid UniqueIdentifier { get; set; } = Guid.Empty;

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;
    public TimeSpan PlaybackDuration { get; set; } = TimeSpan.Zero;
    public bool IsActive { get; set; } = false;
    public bool ShouldLoop { get; set; } = false;
    public List<byte> PatternByteData { get; set; } = new();

    public LightPattern ToLightData()
    {
        return new LightPattern()
        {
            Identifier = UniqueIdentifier,
            Name = Name,
            Description = Description,
            Duration = Duration,
            ShouldLoop = ShouldLoop,
        };
    }

    public PatternData DeepCloneData()
    {
        return new PatternData()
        {
            // do not clone the unique identifier
            Name = Name,
            Description = Description,
            Duration = Duration,
            StartPoint = StartPoint,
            PlaybackDuration = PlaybackDuration,
            IsActive = IsActive,
            ShouldLoop = ShouldLoop,
            PatternByteData = PatternByteData,
        };
    }


    public JObject Serialize()
    {
        // Convert _patternData to a comma-separated string
        string patternDataString = string.Join(",", PatternByteData);

        return new JObject()
        {
            ["UniqueIdentifier"] = UniqueIdentifier,
            ["Name"] = Name,
            ["Description"] = Description,
            ["Duration"] = Duration,
            ["StartPoint"] = StartPoint,
            ["PlaybackDuration"] = PlaybackDuration,
            ["IsActive"] = IsActive,
            ["ShouldLoop"] = ShouldLoop,
            ["PatternByteData"] = patternDataString,
        };
    }

    public void Deserialize(JObject jsonObject)
    {
        try
        {
            UniqueIdentifier = Guid.TryParse(jsonObject["UniqueIdentifier"]?.Value<string>(), out var guid) ? guid : Guid.Empty;
            Name = jsonObject["Name"]?.Value<string>() ?? string.Empty;
            Description = jsonObject["Description"]?.Value<string>() ?? string.Empty;
            Duration = TimeSpan.TryParse(jsonObject["Duration"]?.Value<string>(), out var duration) ? duration : TimeSpan.Zero;
            StartPoint = TimeSpan.TryParse(jsonObject["StartPoint"]?.Value<string>(), out var startPoint) ? startPoint : TimeSpan.Zero;
            PlaybackDuration = TimeSpan.TryParse(jsonObject["PlaybackDuration"]?.Value<string>(), out var playbackDuration) ? playbackDuration : TimeSpan.Zero;
            IsActive = jsonObject["IsActive"]?.Value<bool>() ?? false;
            ShouldLoop = jsonObject["ShouldLoop"]?.Value<bool>() ?? false;

            PatternByteData.Clear();
            var patternDataString = jsonObject["PatternByteData"]?.Value<string>();
            if (string.IsNullOrEmpty(patternDataString))
            {
                // If the string is null or empty, generate a list with a single byte of 0
                PatternByteData = new List<byte> { (byte)0 };
            }
            else
            {
                // Otherwise, split the string into an array and convert each element to a byte
                PatternByteData = patternDataString.Split(',')
                    .Select(byte.Parse)
                    .ToList();
            }
        }
        catch (System.Exception e) { throw new Exception($"{e} Error deserializing pattern data"); }
    }
}
