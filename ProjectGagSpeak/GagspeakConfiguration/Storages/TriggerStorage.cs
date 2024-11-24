namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// Trigger Storage contains an abstract list of triggers.
/// The Trigger Object is a base abstract class for the other types of triggers.
/// So any trigger type can be stored here.
/// </summary>
[Serializable]
public record TriggerStorage
{
    public List<Trigger> Triggers { get; set; } = [];
}
