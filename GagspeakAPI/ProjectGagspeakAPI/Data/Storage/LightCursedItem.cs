using GagspeakAPI.Data.Struct;
using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record LightCursedItem
{
    /// <summary>
    /// The Unique Identifier of the cursed Item.
    /// </summary>
    public Guid Identifier { get; set; } = Guid.Empty;

    /// <summary>
    /// The name given to the restraint set.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The GagType that this cursed item applies if enabled.
    /// </summary>
    public GagType GagType { get; set; } = GagType.None;

    /// <summary>
    /// The Glamour Item affected by the cursed item. (not applicable if a gag item.
    /// </summary>
    public AppliedSlot AffectedSlot { get; set; } = new AppliedSlot();

    /// <summary>
    /// Determines if the cursed item is a GagItem or a Equipment Item.
    /// </summary>
    [IgnoreMember]
    public bool IsGag => GagType != GagType.None;
}