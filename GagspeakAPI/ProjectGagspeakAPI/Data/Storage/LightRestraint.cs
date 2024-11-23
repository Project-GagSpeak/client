using GagspeakAPI.Data.Struct;
using MessagePack;

namespace GagspeakAPI.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record LightRestraintData
{
    /// <summary>
    /// The Unique Identifier of the restraint set.
    /// </summary>
    public Guid Identifier { get; set; } = Guid.Empty;

    /// <summary>
    /// The name given to the restraint set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The Slots that this set affects when enabled. 
    /// Will only contain information for slots marked as enabled or with a set item.
    /// </summary>
    public List<AppliedSlot> AffectedSlots { get; set; } = new List<AppliedSlot>();

    /// <summary>
    /// Hardcore Traits associated per user.
    /// </summary>
    public Dictionary<string, HardcoreTraits> HardcoreTraits { get; set; } = new Dictionary<string, HardcoreTraits>();
}
