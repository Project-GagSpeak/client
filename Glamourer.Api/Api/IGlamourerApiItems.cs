using Glamourer.Api.Enums;

namespace Glamourer.Api.Api;

/// <summary> All functions related to items. </summary>
public interface IGlamourerApiItems
{
    /// <summary> Set a single item on an actor. </summary>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="slot"> The slot to apply the item to. </param>
    /// <param name="itemId"> The (Custom) ID of the item to apply. </param>
    /// <param name="stains"> The IDs of the stains to apply to the item. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> ItemInvalid, ActorNotFound, ActorNotHuman, InvalidKey, Success. </returns>
    /// <remarks> The item ID can be a custom item ID in Glamourer's format for models without an associated item, or a normal game item ID. </remarks>
    public GlamourerApiEc SetItem(int objectIndex, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ApplyFlag flags);

    /// <summary> Set a single item on players. </summary>
    /// <param name="playerName"> The name of the players to be manipulated. </param>
    /// <param name="slot"> The slot to apply the item to. </param>
    /// <param name="itemId"> The (Custom) ID of the item to apply. </param>
    /// <param name="stains"> The IDs of the stains to apply to the item. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> ItemInvalid, ActorNotFound, ActorNotHuman, InvalidKey, Success. </returns>
    /// <remarks>
    /// The item ID can be a custom item ID in Glamourer's format for models without an associated item, or a normal game item ID.<br/>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are modified.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc SetItemName(string playerName, ApiEquipSlot slot, ulong itemId, IReadOnlyList<byte> stains, uint key, ApplyFlag flags);

    /// <summary> Set a single bonus item on an actor. </summary>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="slot"> The bonus slot to apply the item to. </param>
    /// <param name="bonusItemId"> The bonus item sheet ID of the item to apply (including stain). </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> ItemInvalid, ActorNotFound, ActorNotHuman, InvalidKey, Success. </returns>
    /// <remarks> The bonus item ID can currently not be a custom item ID in Glamourer's format for models without an associated item. Use 0 to remove the bonus item. </remarks>
    public GlamourerApiEc SetBonusItem(int objectIndex, ApiBonusSlot slot, ulong bonusItemId, uint key, ApplyFlag flags);

    /// <summary> Set a single bonus item on an actor. </summary>
    /// <param name="playerName"> The game object index of the actor to be manipulated. </param>
    /// <param name="slot"> The bonus slot to apply the item to. </param>
    /// <param name="bonusItemId"> The bonus item sheet ID of the item to apply (including stain). </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> ItemInvalid, ActorNotFound, ActorNotHuman, InvalidKey, Success. </returns>
    /// <remarks>
    /// The bonus item ID can currently not be a custom item ID in Glamourer's format for models without an associated item. Use 0 to remove the bonus item. <br/>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are modified.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc SetBonusItemName(string playerName, ApiBonusSlot slot, ulong bonusItemId, uint key, ApplyFlag flags);
}
