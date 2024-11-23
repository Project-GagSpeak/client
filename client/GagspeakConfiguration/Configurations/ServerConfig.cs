using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.WebAPI;

namespace GagSpeak.GagspeakConfiguration.Configurations;

[Serializable]
public class ServerConfig : IGagspeakConfiguration
{
    /// <summary> The GagSpeak Server's server storage </summary>
    public ServerStorage ServerStorage { get; set; } = new()
    {
        ServerName = MainHub.MainServer,
        ServiceUri = MainHub.MainServiceUri,
    };

    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
