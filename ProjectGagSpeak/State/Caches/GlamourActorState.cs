using CkCommons.Classes;
using GagSpeak.State.Models;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.State.Caches;

/// <summary>
///     Represents a cache for the Glamour Actor's state.
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
    ///     Attempts to update the active Glamour Actors state with its most recent data. <para />
    ///     Current bound state is passed in so that we can run a comparison against the slots. <para />
    ///     However, do not pass in the FinalMeta, as we should cache the latest metadata state in accordance to base game.
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
    public void UpdateMetaCheckBinds(JObject newState, MetaDataStruct finalMeta, bool anyHat, bool anyVisor, bool anyWep)
    {
        // Update object entirely if it was null before.
        if (State is null)
            State = newState;

        if (newState?["Equipment"] is not JToken equipment)
            return;

        // parse the metadata.
        var sh = newState["Equipment"]?["Hat"]?["Show"]?.Value<bool>() ?? false;
        var ah = newState["Equipment"]?["Hat"]?["Apply"]?.Value<bool>() ?? false;
        var sv = newState["Equipment"]?["Visor"]?["IsToggled"]?.Value<bool>() ?? false;
        var av = newState["Equipment"]?["Visor"]?["Apply"]?.Value<bool>() ?? false;
        var sw = newState["Equipment"]?["Weapon"]?["Show"]?.Value<bool>() ?? false;
        var aw = newState["Equipment"]?["Weapon"]?["Apply"]?.Value<bool>() ?? false;
        //Svc.Logger.Verbose($"[GlamourActorState] Updating Meta: Hat({sh}, {ah}), Visor({sv}, {av}), Weapon({sw}, {aw})");
        // set the metadata based on the retrieved values.
        var hatState = (sh, ah) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        // If no hatstates are stored, just apply whatever was passed in.
        if (!anyHat)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.HatState, hatState);
        
        var visorState = (sv, av) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        // If no visor states are stored, just apply whatever was passed in.
        if (!anyVisor)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.VisorState, visorState);
       
        // If no weapon states are stored, just apply whatever was passed in.
        var weaponState = (sw, aw) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        if (!anyWep)
            MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.WeaponState, weaponState);
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

        // parse the metadata.
        var sh = equipmentObj?["Hat"]?["Show"]?.Value<bool>() ?? false;
        var ah = equipmentObj?["Hat"]?["Apply"]?.Value<bool>() ?? false;
        var sv = equipmentObj?["Visor"]?["IsToggled"]?.Value<bool>() ?? false;
        var av = equipmentObj?["Visor"]?["Apply"]?.Value<bool>() ?? false;
        var sw = equipmentObj?["Weapon"]?["Show"]?.Value<bool>() ?? false;
        var aw = equipmentObj?["Weapon"]?["Apply"]?.Value<bool>() ?? false;
        // set the metadata based on the retrieved values.
        var hatState = (sh, ah) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.HatState, hatState);
        var visorState = (sv, av) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.VisorState, visorState);
        var weaponState = (sw, aw) switch { (true, true) => TriStateBool.True, (false, true) => TriStateBool.False, _ => TriStateBool.Null };
        MetaStates = MetaStates.WithMetaIfDifferent(MetaIndex.WeaponState, weaponState);
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
