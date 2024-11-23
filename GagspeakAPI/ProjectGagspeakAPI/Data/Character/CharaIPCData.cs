using MessagePack;
#pragma warning disable CS0659

namespace GagspeakAPI.Data.Character;

/// <summary>
/// Stores Information about the Player's Moodles Data
/// - Stores Status Manager String
/// - Stores all active Moodle Statuses on the player, with the status info.
/// - Stores the GUID's of their Presets and the associated Moodles Statuses of those presets by GUID.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaIPCData
{
    /// <summary> Last Known StatusManager compressed in base64 string. (for easy setting) </summary>
    public string MoodlesData { get; set; } = string.Empty;

    /// <summary> The list of Active Statuses in the StatusManager, non-serialized. </summary>
    public List<MoodlesStatusInfo> MoodlesDataStatuses { get; set; } = new();

    /// <summary> A list of the player's Statuses. </summary>
    public List<MoodlesStatusInfo> MoodlesStatuses { get; set; } = new();

    /// <summary> List of player's Presets, and the Status GUID's the preset contains </summary>
    public List<(Guid, List<Guid>)> MoodlesPresets { get; set; } = new();

    /// <summary> Equals Override to ensure things are the same. </summary>
    public override bool Equals(object? obj)
    {
        // return false if object is not characterIPCData
        if (obj is CharaIPCData data)
        {
            return MoodlesData == data.MoodlesData
                && MoodlesDataStatuses.SequenceEqual(data.MoodlesDataStatuses)
                && MoodlesStatuses.SequenceEqual(data.MoodlesStatuses)
                && MoodlesPresets.SequenceEqual(data.MoodlesPresets);
        }
        return false;
    }
}
#pragma warning restore CS0659
