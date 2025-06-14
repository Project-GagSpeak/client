using Glamourer.Api.Enums;

namespace GagSpeak.Interop;
public interface IGlamourer
{
    /// <summary> May not need to be static, but determines if any incoming StateChanges are accepted or not.</summary>
    /// <remarks> By default, this is true during any Glamourer Update or on Gearset change.</remarks>
    static bool BlockStateChangeEvent { get; set; } = false;

    /// <summary> Grabs the clients current Glamourer state.</summary>
    /// <remarks>This is primarily used to grab the cache of the player when state is finalized.</remarks>
    JObject? GetClientGlamourerState();

    /// <summary> Sets a singular item to the Client's equipment. </summary>
    /// <param name="slot">The slot to set the item to.</param>
    /// <param name="item">The item to set.</param>
    /// <param name="dye">The dye to set.</param>
    /// <param name="variant">The variant to set.</param>
    Task SetClientItemSlot(ApiEquipSlot slot, ulong item, IReadOnlyList<byte> dye, uint variant);

    /// <summary> Modifies the Meta State(s) to a new value. (Wetness, Visor, Helmet, Weapon)</summary>
    /// <param name="metaTypes">The Meta Flags to modify.</param>
    /// <param name="newValue">The new value to set the Meta Flags to.</param>
    /// <remarks> Because this is a Flag passed in, multiple types can be modified at once.</remarks>
    Task SetMetaStates(MetaFlag metaTypes, bool newValue);

    /// <summary> Sets the Customize of the Client. </summary>
    /// <param name="customizations">The customizations to set.</param>
    /// <param name="parameters">The parameters to set.</param>
    /// <remarks> This is primarily used to set the customization of the player in Glamourer cached in restraint sets. </remarks>
    Task SetCustomize(JToken customizations, JToken parameters);

    /// <summary> Sets the equipment from an Client's state in Glamourer, as the set slots in a Restraint Set. </summary>
    /// <param name="setToEdit">The Restraint Set to edit.</param>
    /// <returns>True if the equipment was set successfully, false otherwise.</returns>
    /// <remarks> This is primarily used to set the equipment from the current state of the player in Glamourer. </remarks>
    // bool SetRestraintEquipmentFromState(RestraintSet setToEdit);

    /// <summary> Sets the customize parameters from the Client's state in Glamourer, and stores it into the Restraint Set. </summary>
    // void SetRestraintCustomizationsFromState(RestraintSet setToEdit);
}
