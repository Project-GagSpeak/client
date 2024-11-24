using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class GagStorageConfig : IGagspeakConfiguration
{
    public GagStorage GagStorage { get; set; } = new GagStorage();
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
