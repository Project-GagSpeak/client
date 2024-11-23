using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class AlarmConfig : IGagspeakConfiguration
{
    /// <summary> AliasList Storage per-paired user. </summary>
    public AlarmStorage AlarmStorage { get; set; } = new AlarmStorage();
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
