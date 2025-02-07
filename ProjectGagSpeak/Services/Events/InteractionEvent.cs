using GagspeakAPI.Data;

namespace GagSpeak.Services.Events;

/// <summary>
/// Outline of the NotificationService Event record.
/// These Events help detail who did what to you and when it happened relative to your local time.
/// </summary>
public record InteractionEvent
{
    /// <summary>
    /// The time this event occured.
    /// </summary>
    public DateTime EventTime { get; }
    /// <summary>
    /// Who sent this update to you
    /// </summary>
    public string ApplierNickAliasOrUID { get; set; }
    
    /// <summary>
    /// Store the Raw UID so we can search it regardless. This is grouped with ApplyerNickAliasOrUID when in a filter.
    /// </summary>
    public string ApplierUID { get; } 

    /// <summary>
    /// What type of update it was.
    /// </summary>
    public InteractionType InteractionType { get; } // What type of update it was.

    /// <summary>
    /// Additional Information about the content update.
    /// </summary>
    public string InteractionContent { get; }

    public InteractionEvent(string applierUID, InteractionType type, string details)
    {
        EventTime = DateTime.Now;
        ApplierNickAliasOrUID = string.Empty;
        ApplierUID = applierUID;
        InteractionType = type;
        InteractionContent = details;
    }

    public InteractionEvent(string applierNickAliasOrUID, string applierUID, InteractionType type, string details)
    {
        EventTime = DateTime.Now;
        ApplierNickAliasOrUID = applierNickAliasOrUID;
        ApplierUID = applierUID;
        InteractionType = type;
        InteractionContent = details;
    }

    public override string ToString()
        => "[" + ApplierNickAliasOrUID + " Performed Action on you: " + InteractionType + " with details: " + InteractionContent + "]";
}
