using GagSpeak.GagspeakConfiguration.Models;

namespace GagSpeak.GagspeakConfiguration.Configurations;

public class TriggerConfig : IGagspeakConfiguration
{
    public static int CurrentVersion => 1;
    public int Version { get; set; } = CurrentVersion;

    /// <summary> The Trigger Storage for the toybox. </summary>
    public TriggerStorage Storage { get; set; } = new TriggerStorage();
}
