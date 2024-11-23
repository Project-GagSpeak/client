using MessagePack;

namespace GagspeakAPI.Data.Struct;

[MessagePackObject(keyAsPropertyName: true)]
public record AppliedSlot
{
    public byte Slot { get; set; } = 3; // 3 == Head
    public ulong CustomItemId { get; set; } = ulong.MaxValue;
    public string Tooltip { get; set; } = string.Empty;
}
