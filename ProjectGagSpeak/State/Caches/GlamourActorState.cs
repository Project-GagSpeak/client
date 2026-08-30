using CkCommons.Classes;
using GagSpeak.State.Models;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.State.Caches;

/// <summary>
///   Represents a cache for the Glamour Actor's state.
/// </summary>
/// <remarks> Useful for storing unrestricted states to restore slots when removed. </remarks>
public struct GlamourActorState
{
    public JObject? State;
    public JToken? Equipment => State?["Equipment"];
    public JToken? Customize => State?["Customize"];
    public JToken? Parameters => State?["Parameters"];

    // This will hold the parsed equipment for all slots
    public readonly Dictionary<EquipSlot, EquipItem> ParsedEquipment;
    public MetaDataStruct MetaStates = MetaDataStruct.Empty;

    public GlamourActorState(JObject? state)
    {
        State = state;
        ParsedEquipment = new Dictionary<EquipSlot, EquipItem>();
        ParseEquipments(Equipment);
        ParseMeta(Equipment);
    }

    public bool IsEmpty => State is null && ParsedEquipment.Count == 0 && MetaStates.IsEmpty;

    public static GlamourActorState Empty => new GlamourActorState(null);

    public static GlamourActorState Clone(GlamourActorState other)
    {
        // Handle this properly later, should not be cloning a struct.
        var clone = new GlamourActorState(other.State?.DeepClone() as JObject);
        foreach (var kvp in other.ParsedEquipment)
            clone.ParsedEquipment[kvp.Key] = kvp.Value;
        clone.MetaStates = new(other.MetaStates.Headgear, other.MetaStates.Visor, other.MetaStates.Weapon);
        return clone;
    }

    /// <summary>
    ///   Reads a single equipment slot out of a raw Glamourer state object.
    /// </summary>
    /// <remarks> Lets us compare against Glamourer's live state without parsing the whole object. </remarks>
    public static bool TryReadSlot(JObject? state, EquipSlot slot, out ulong customItemId, out byte stain, out byte stain2)
    {
        customItemId = ulong.MaxValue;
        stain = 0;
        stain2 = 0;

        if (state?["Equipment"] is not JToken equipment || !EquipSlotExtensions.EqdpSlots.Contains(slot))
            return false;

        customItemId = equipment[slot.ToString()]?["ItemId"]?.Value<ulong>() ?? 4294967164;
        stain = equipment[slot.ToString()]?["Stain"]?.Value<byte>() ?? 0;
        stain2 = equipment[slot.ToString()]?["Stain2"]?.Value<byte>() ?? 0;
        return true;
    }

    /// <summary>
    ///   Reads the Hat/Visor/Weapon metadata out of a raw Glamourer state object.
    /// </summary>
    public static bool TryReadMeta(JObject? state, out MetaDataStruct meta)
    {
        meta = MetaDataStruct.Empty;
        if (state?["Equipment"] is not JToken equipment)
            return false;

        meta = ReadMeta(equipment);
        return true;
    }

    /// <summary>
    ///   Attempts to update the active Glamour Actors state with its most recent data. <para />
    ///   Current bound state is passed in so that we can run a comparison against the slots. <para />
    ///   However, do not pass in the FinalMeta, as we should cache the latest metadata state in accordance to base game.
    /// </summary>
    public void UpdateEquipment(JObject newState, IReadOnlyDictionary<EquipSlot, EquipItem> boundState)
    {
        // Update object entirely if it was null before.
        if (State is null)
        {
            State = newState;
            ParseEquipments(Equipment);
            ParseMeta(Equipment);
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
                    Svc.Logger.Verbose($"[GlamourActorState] Skipping update for slot {slot} as it matches the current bound state.");
                    continue;
                }

                // Otherwise, set the parsed equipment for this slot.
                State["Equipment"]![slot.ToString()] = slotToken;
                ParsedEquipment[slot] = newItem;
            }
        }
    }

    /// <summary> Only updates metadata when no flags for a particular metastate are occupied by bound items. </summary>
    public void UpdateMetaCheckBinds(JObject newState, bool anyHat, bool anyVisor, bool anyWep)
    {
        // Update object entirely if it was null before.
        if (State is null)
            State = newState;

        if (newState?["Equipment"] is not JToken equipment)
            return;

        // Only refresh a metastate that no bound item is currently occupying, otherwise
        // we would cache our own enforced value as the base to restore back to later.
        var latest = ReadMeta(equipment);
        if (!anyHat)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.HatState, latest.Headgear);
        if (!anyVisor)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.VisorState, latest.Visor);
        if (!anyWep)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.WeaponState, latest.Weapon);
    }

    /// <summary> Forcibly updates all metastates to the latest JObject state. </summary>
    public void UpdateMetaWithLatest(JObject newState)
    {
        if (newState?["Equipment"] is not JToken equipment)
            return;

        // parse the metadata.
        ParseMeta(equipment);
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

    private void ParseMeta(JToken? equipmentToken)
    {
        if (equipmentToken is not JObject equipmentObj)
            return;

        var latest = ReadMeta(equipmentObj);
        MetaStates = MetaStates
            .WithMetaIfDifferent(MetaIndex.HatState, latest.Headgear)
            .WithMetaIfDifferent(MetaIndex.VisorState, latest.Visor)
            .WithMetaIfDifferent(MetaIndex.WeaponState, latest.Weapon);
    }

    /// <summary> Maps Glamourer's (shown, applied) pairs onto our TriStateBool metadata. </summary>
    /// <remarks> A state Glamourer is not applying is <see cref="TriStateBool.Null"/>, meaning 'untouched'. </remarks>
    private static MetaDataStruct ReadMeta(JToken equipment)
        => new(ToTriState(equipment["Hat"]?["Show"], equipment["Hat"]?["Apply"]),
               ToTriState(equipment["Visor"]?["IsToggled"], equipment["Visor"]?["Apply"]),
               ToTriState(equipment["Weapon"]?["Show"], equipment["Weapon"]?["Apply"]));

    private static TriStateBool ToTriState(JToken? shown, JToken? applied)
        => (shown?.Value<bool>() ?? false, applied?.Value<bool>() ?? false) switch
        {
            (true, true) => TriStateBool.True,
            (false, true) => TriStateBool.False,
            _ => TriStateBool.Null,
        };

    public bool RecoverSlot(EquipSlot slot, out ulong customItemId, out byte stain, out byte stain2)
        => TryReadSlot(State, slot, out customItemId, out stain, out stain2);
}
