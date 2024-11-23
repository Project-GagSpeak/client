namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// The storage of all patterns
/// </summary>
[Serializable]
public class PatternStorage
{
    /// <summary> The storage of all patterns </summary>
    public List<PatternData> Patterns { get; set; } = new();
}
