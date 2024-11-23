using Glamourer.Api.Enums;
using Newtonsoft.Json.Linq;

namespace Glamourer.Api.Api;

/// <summary> Any functions related to Glamourer's state tracking. </summary>
public interface IGlamourerApiState
{
    /// <summary> Get the current Glamourer state of an actor. </summary>
    /// <param name="objectIndex"> The game object index of the desired actor. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <returns> ActorNotFound, InvalidKey or Success, and the state on success. </returns>
    /// <remarks> The actor does not need to have a prior Glamourer state as long as it can be found. </remarks>
    public (GlamourerApiEc, JObject?) GetState(int objectIndex, uint key);

    /// <summary> Get the current Glamourer state of a player character. </summary>
    /// <param name="playerName"> The name of the desired player. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <returns> ActorNotFound, InvalidKey or Success, and the state on success. </returns>
    /// <remarks>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.
    /// Only players are checked for name equality, no NPCs.
    /// If multiple players of the same name are found, the first is returned.
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public (GlamourerApiEc, JObject?) GetStateName(string playerName, uint key);

    /// <inheritdoc cref="GetState"/>
    public (GlamourerApiEc, string?) GetStateBase64(int objectIndex, uint key);

    /// <inheritdoc cref="GetStateName"/>
    public (GlamourerApiEc, string?) GetStateBase64Name(string objectName, uint key);

    /// <summary> Apply a supplied state to an actor. </summary>
    /// <param name="applyState"> The state, which can be either a Glamourer-supplied JObject or a Base64 string. </param>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the application. Respects Once, Equipment, Customization and Lock (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, ActorNotHuman, Success. </returns>
    public GlamourerApiEc ApplyState(object applyState, int objectIndex, uint key, ApplyFlag flags);

    /// <summary> Apply a supplied state to players. </summary>
    /// <param name="applyState"> The state, which can be either a Glamourer-supplied JObject or a Base64 string. </param>
    /// <param name="playerName"> The name of the player to be manipulated. </param>
    /// <param name="key"> A key to unlock or lock the state if necessary. </param>
    /// <param name="flags"> The flags used for the application. Respects Once, Equipment, Customization and Lock (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, ActorNotHuman, Success. </returns>
    /// <remarks>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are manipulated.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc ApplyStateName(object applyState, string playerName, uint key, ApplyFlag flags);

    /// <summary> Revert the Glamourer state of an actor to Game state. </summary>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Equipment and Customization (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, Success, NothingDone. </returns>
    public GlamourerApiEc RevertState(int objectIndex, uint key, ApplyFlag flags);

    /// <summary> Revert the Glamourer state of players to game state. </summary>
    /// <param name="playerName"> The name of the players to be reverted. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Equipment and Customization (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, Success, NothingDone. </returns>
    /// /// <remarks>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are reverted.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc RevertStateName(string playerName, uint key, ApplyFlag flags);

    /// <summary> Unlock the Glamourer state of an actor with a key. </summary>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="key"> A key to unlock the state. </param>
    /// <returns> ActorNotFound, InvalidKey, Success, NothingDone. </returns>
    public GlamourerApiEc UnlockState(int objectIndex, uint key);

    /// <summary> Unlock the Glamourer state of players with a key. </summary>
    /// <param name="playerName"> The name of the players to be unlocked. </param>
    /// <param name="key"> A key to unlock the state. </param>
    /// <returns> InvalidKey, Success, NothingDone. </returns>
    public GlamourerApiEc UnlockStateName(string playerName, uint key);

    /// <summary> Unlock all active glamourer states with a key. </summary>
    /// <param name="key"> The key to unlock states with. </param>
    /// <returns> The number of unlocked states. </returns>
    public int UnlockAll(uint key);

    /// <summary> Revert the Glamourer state of an actor to automation state. </summary>
    /// <param name="objectIndex"> The game object index of the actor to be manipulated. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once and Lock (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, Success, NothingDone. </returns>
    public GlamourerApiEc RevertToAutomation(int objectIndex, uint key, ApplyFlag flags);

    /// <summary> Revert the Glamourer state of players to automation state. </summary>
    /// <param name="playerName"> The name of the players to be reverted. </param>
    /// <param name="key"> A key to unlock the state if necessary. </param>
    /// <param name="flags"> The flags used for the reversion. Respects Once and Lock (see <see cref="ApplyFlag"/>.) </param>
    /// <returns> ActorNotFound, InvalidKey, Success, NothingDone. </returns>
    /// /// <remarks>
    /// The player does not have to be currently available as long as he has a persisted Glamourer state.<br/>
    /// Only players are checked for name equality, no NPCs.<br/>
    /// If multiple players of the same name are found, all of them are reverted.<br/>
    /// Prefer to use the index-based function unless you need to get the state of someone currently unavailable.
    /// </remarks>
    public GlamourerApiEc RevertToAutomationName(string playerName, uint key, ApplyFlag flags);

    /// <summary> Invoked with the game object pointer (if available) whenever an actors tracked state changes. </summary>
    public event Action<nint> StateChanged;

    /// <summary> Invoked with the game object pointer (if available) whenever an actors tracked state changes, with the type of change. </summary>
    public event Action<nint, StateChangeType> StateChangedWithType;

    /// <summary> Invoked when the player enters or leaves GPose (true => entered GPose, false => left GPose). </summary>
    public event Action<bool>? GPoseChanged;
}
