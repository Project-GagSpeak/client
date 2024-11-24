using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class ServerTagConfig : IGagspeakConfiguration
{
    public ServerTagStorage ServerTagStorage { get; set; }
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
