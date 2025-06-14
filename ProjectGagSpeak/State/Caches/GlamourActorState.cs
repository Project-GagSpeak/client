using Penumbra.GameData.Enums;

namespace GagSpeak.State.Caches;

/// <summary>
///     Represents a cache for the Glamour Actor's state.
/// </summary>
/// <remarks> Useful for storing unrestricted states to restore slots when removed. </remarks>
public struct GlamourActorState(JObject? stateObject)
{
    private JObject? State { get; set; } = stateObject;
    public JToken? Equipment => State?["Equipment"];

    // Has no purpose atm, maybe we can do something with materials later but idk.
    public JToken? Customize => State?["Customize"];

    // Has no purpose atm, maybe we can do something with parameters later but idk.
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

    public static GlamourActorState Empty => new GlamourActorState(null);
}
