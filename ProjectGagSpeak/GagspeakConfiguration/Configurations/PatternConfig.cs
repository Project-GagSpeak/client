using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class PatternConfig : IGagspeakConfiguration
{
    /// <summary> The GagSpeak Clients Pattern Stroage </summary>
    public PatternStorage PatternStorage { get; set; } = new();
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
