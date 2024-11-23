using MessagePack;

namespace GagspeakAPI.Data;

/// <summary>
/// Generic Pattern Info retrieved from search results.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record ServerPatternInfo
{
    public Guid Identifier = Guid.Empty;

    public string Name = "UNK PATTERN NAME";

    public string Description = string.Empty;
    
    public string Author = string.Empty;

    public List<string> Tags = new List<string>();

    public int Downloads = 0;

    public int Likes = 0;

    public bool Looping = false;

    public TimeSpan Length = TimeSpan.Zero;

    public DateTime UploadedDate = DateTime.MinValue;

    public bool UsesVibrations = false;

    public bool UsesRotations = false;
    
    public bool UsesOscillation = false;

    public bool HasLiked = false;
}
