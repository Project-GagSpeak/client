using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class TriggerConfig : IGagspeakConfiguration
{
    /// <summary> The Trigger Storage for the toybox. </summary>
    public TriggerStorage TriggerStorage { get; set; } = new TriggerStorage();
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
}
