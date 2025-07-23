using GagSpeak.Services;
using InteropGenerator.Runtime;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using System.Collections.ObjectModel;

namespace GagSpeak.State.Caches;

/// <summary>
///     Represents a cache for the Glamour Actor's state.
/// </summary>
/// <remarks> Useful for storing unrestricted states to restore slots when removed. </remarks>
public struct GlamourActorState
{
    private JObject? State;
    public JToken? Equipment => State?["Equipment"];
    public JToken? Customize => State?["Customize"];
    public JToken? Parameters => State?["Parameters"];

    // This will hold the parsed equipment for all slots
    public readonly Dictionary<EquipSlot, EquipItem> ParsedEquipment;

    public GlamourActorState(JObject? state)
    {
        State = state;
        ParsedEquipment = new Dictionary<EquipSlot, EquipItem>();
        ParseEquipments(Equipment);
    }

    public static GlamourActorState Empty => new GlamourActorState(null);

    public static GlamourActorState Clone(GlamourActorState other)
    {
        var clone = new GlamourActorState(other.State?.DeepClone() as JObject);
        foreach (var kvp in other.ParsedEquipment)
            clone.ParsedEquipment[kvp.Key] = kvp.Value;
        return clone;
    }

    /// <summary>
    ///     Attempts to update the active Glamour Actors state with its most recent data.
    ///     Current bound state is passed in so that we can run a comparison against the slots.
    /// </summary>
    public void UpdateEquipment(JObject newState, IReadOnlyDictionary<EquipSlot, EquipItem> boundState)
    {
        // Update object entirely if it was null before.
        if (State is null)
        {
            State = newState;
            ParseEquipments(Equipment);
            return;
        }

        // Otherwise, update the state conditionally.
        if (newState?["Customize"] is JToken customize)
            State["Customize"] = customize;

        if (newState?["Parameters"] is JToken parameters)
            State["Parameters"] = parameters;

        // Update Equipment Conditionally.
        if (newState?["Equipment"] is JToken equipment)
        {
            // Foreach slot in the currently parsed equipment.
            foreach (var slot in EquipSlotExtensions.EqdpSlots)
            {
                // Resolve the slot token.
                var slotToken = equipment[slot.ToString()];
                // look inside and grab its custom ID.
                var customId = slotToken?["ItemId"]?.Value<ulong>() ?? ulong.MaxValue;
                // Attempt to resolve the item.
                var newItem = ItemSvc.Resolve(slot, customId);
                // IF the item is the same as the current bound state, do NOT set it.
                if (boundState.TryGetValue(slot, out var boundItem) && boundItem.Equals(newItem))
                {
                    Svc.Logger.Information($"[GlamourActorState] Skipping update for slot {slot} as it matches the current bound state.");
                    continue;
                }

                // Otherwise, set the parsed equipment for this slot.
                State["Equipment"]![slot.ToString()] = slotToken;
                ParsedEquipment[slot] = newItem;
            }
        }
    }

    private void ParseEquipments(JToken? equipmentToken)
    {
        if (equipmentToken is not JObject equipmentObj)
            return;

        foreach (var slot in EquipSlotExtensions.EqdpSlots)
        {
            var slotToken = equipmentObj[slot.ToString()];
            var customId = slotToken?["ItemId"]?.Value<ulong>() ?? ulong.MaxValue;
            // set the item in the parsed equipment.
            ParsedEquipment[slot] = ItemSvc.Resolve(slot, customId);
        }
    }

    public bool RecoverSlot(EquipSlot slot, out ulong customItemId, out byte stain, out byte stain2)
    {
        if (Equipment is null || !EquipSlotExtensions.EqdpSlots.Contains(slot))
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
