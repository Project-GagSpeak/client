using Glamourer.Api.Enums;

namespace Glamourer.Api.Api;

/// <summary> All functions related to Glamourer designs. </summary>
public interface IGlamourerApiDesigns
{
    /// <summary> Obtain a list of all available designs. </summary>
    /// <returns> A dictionary of all designs from their GUID to their current display name. </returns>
    public Dictionary<Guid, string> GetDesignList();

    /// <summary> Apply an existing design to an actor.  </summary>
    /// <param name="designId"> The GUID of the design to apply. </param>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once, Equipment, Customization, Lock (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> DesignNotFound, ActorNotFound, InvalidKey, Success. </returns>
    public GlamourerApiEc ApplyDesign(Guid designId, int objectIndex, uint key, ApplyFlag flags);

    /// <summary> Apply an existing design to an actor.  </summary>
    /// <param name="designId"> The GUID of the design to apply. </param>
    /// <param name="playerName"> The name of the players to be manipulated. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once, Equipment, Customization, Lock (see <see cref="ApplyFlag"/>.)</param>
    /// <returns> DesignNotFound, ActorNotFound, InvalidKey, Success. </returns>
    /// /// <remarks>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are reverted.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc ApplyDesignName(Guid designId, string playerName, uint key, ApplyFlag flags);
}
