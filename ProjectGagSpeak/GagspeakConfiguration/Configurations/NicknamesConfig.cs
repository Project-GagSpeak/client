using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class NicknamesConfig : IGagspeakConfiguration
{
    public ServerNicknamesStorage ServerNicknames { get; set; }
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
