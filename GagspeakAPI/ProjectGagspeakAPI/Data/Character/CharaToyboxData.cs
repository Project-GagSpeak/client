using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data.Character;

/// <summary>
/// CharacterData class stores all of the user's settings, permissions, and appearance data
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public class CharaToyboxData
{
    /// <summary>
    /// the identifier being changed if used in an update call.
    /// </summary>
    public Guid InteractionId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets the GUID of the active pattern.
    /// </summary>
    public Guid ActivePatternId { get; set; } = Guid.Empty;

    /// <summary>
    /// Gets the GUID's of all alarms currently active.
    /// </summary>
    public List<Guid> ActiveAlarms { get; set; } = new List<Guid>();

    /// <summary>
    /// Gets the GUID's of all active triggers in the toybox.
    /// </summary>
    public List<Guid> ActiveTriggers { get; set; } = new List<Guid>();
}
