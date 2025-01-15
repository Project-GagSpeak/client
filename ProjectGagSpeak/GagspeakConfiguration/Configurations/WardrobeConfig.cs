using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class WardrobeConfig : IGagspeakConfiguration
{
    /// <summary> The GagSpeak Clients Pattern Storage </summary>
    public WardrobeStorage WardrobeStorage { get; set; } = new WardrobeStorage();
    // Using Static to view version during initial load for migrations.
    public static int CurrentVersion => 1;
    public int Version { get; set; } = CurrentVersion;

    public WardrobeConfig() { }
}
