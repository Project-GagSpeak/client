using Penumbra.GameData.Enums;

namespace GagSpeak.PlayerState.Visual;

public struct GlamourCache
{
    private JObject? State { get; set; }
    public GlamourCache() { }
    public GlamourCache(JObject? stateObject) => State = stateObject;

    public JToken? Equipment => State?["Equipment"];
    public JToken? Customize => State?["Customize"];
    public JToken? Parameters => State?["Parameters"];

    public bool RecoverSlot(EquipSlot slot, out ulong customItemId, out byte stain, out byte stain2)
    {
        if(Equipment is null || !EquipSlotExtensions.EqdpSlots.Contains(slot))
        {
            customItemId = ulong.MaxValue;
            stain = 0;
            stain2 = 0;
            return false;
        }
        // Return the proper values for the slot.
        customItemId = Equipment?[slot.ToString()]?["ItemId"]?.Value<ulong>() ?? 4294967164;
        stain = Equipment?[slot.ToString()]?["Stain"]?.Value<byte>() ?? 0;
        stain2 = Equipment?[slot.ToString()]?["Stain2"]?.Value<byte>() ?? 0;

        return true;
    }
}
