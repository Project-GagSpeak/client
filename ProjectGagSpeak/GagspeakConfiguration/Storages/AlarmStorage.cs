namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// The container of all stored alarms currently saved for the client.
/// </summary>
[Serializable]
public record AlarmStorage
{
    public List<Alarm> Alarms { get; set; } = [];
}
