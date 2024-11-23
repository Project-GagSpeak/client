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
    /// <summary> The name of the pattern </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary> The description of the pattern </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary> The author of the pattern (Anonymous by default) </summary>
    public string Author { get; set; } = "Anon. Kinkster";

    /// <summary> Tags for the pattern. 5 tags at most. </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary> The Total Overall Duration of the pattern </summary>
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;

    /// <summary> The start point of the pattern to play </summary>
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;

    /// <summary> The duration of the pattern to play (if 00:00, play full) </summary>
    public TimeSpan PlaybackDuration { get; set; } = TimeSpan.Zero;

    /// <summary> If the pattern is active </summary>
    public bool IsActive { get; set; } = false;

    /// <summary> If the pattern should loop </summary>
    public bool ShouldLoop { get; set; } = false;

    /// <summary> If the pattern is uploaded to the server. </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary> Let's us know who made the pattern. If empty, it was downloaded, if a string, it was one of our UID's. </summary>
    public string CreatorUID { get; set; } = string.Empty;

    /// <summary> The pattern byte data </summary>
    public List<byte> PatternByteData { get; set; } = new();

    public LightPattern ToLightData()
    {
        return new LightPattern()
        {
            Identifier = UniqueIdentifier,
            Name = Name,
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
            Author = Author,
            Tags = Tags,
            Duration = Duration,
            StartPoint = StartPoint,
            PlaybackDuration = PlaybackDuration,
            IsActive = IsActive,
            ShouldLoop = ShouldLoop,
            IsPublished = IsPublished,
            CreatorUID = CreatorUID,
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
            ["Author"] = Author,
            ["Tags"] = new JArray(Tags),
            ["Duration"] = Duration,
            ["StartPoint"] = StartPoint,
            ["PlaybackDuration"] = PlaybackDuration,
            ["IsActive"] = IsActive,
            ["ShouldLoop"] = ShouldLoop,
            ["IsPublished"] = IsPublished,
            ["CreatedByClient"] = CreatorUID,
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
            Author = jsonObject["Author"]?.Value<string>() ?? "Anon. Kinkster";

            // Deserialize the ViewAccess
            if (jsonObject["Tags"] is JArray viewAccessArray)
            {
                Tags = viewAccessArray.Select(x => x.Value<string>()).ToList()!;
            }

            Duration = TimeSpan.TryParse(jsonObject["Duration"]?.Value<string>(), out var duration) ? duration : TimeSpan.Zero;
            StartPoint = TimeSpan.TryParse(jsonObject["StartPoint"]?.Value<string>(), out var startPoint) ? startPoint : TimeSpan.Zero;
            PlaybackDuration = TimeSpan.TryParse(jsonObject["PlaybackDuration"]?.Value<string>(), out var playbackDuration) ? playbackDuration : TimeSpan.Zero;

            IsActive = jsonObject["IsActive"]?.Value<bool>() ?? false;
            ShouldLoop = jsonObject["ShouldLoop"]?.Value<bool>() ?? false;
            IsPublished = jsonObject["IsPublished"]?.Value<bool>() ?? false;
            CreatorUID = jsonObject["CreatedByClient"]?.Value<string>() ?? string.Empty; // prevent uploading if not failed.

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
