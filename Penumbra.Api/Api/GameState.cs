using Penumbra.Api.Enums;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the currently tracked game state. </summary>
public interface IPenumbraApiGameState
{
    /// <param name="drawObject"></param>
    /// <returns>The game object associated with the given <paramref name="drawObject">draw object</paramref>
    /// and the GUID and name of the collection associated with this game object.</returns>
    public (nint GameObject, (Guid Id, string Name) Collection) GetDrawObjectInfo(nint drawObject);

    /// <summary>
    /// Obtain the parent game object index for an unnamed cutscene actor by its <paramref name="actorIdx">index</paramref>.
    /// </summary>
    /// <param name="actorIdx"></param>
    /// <returns>The parent game object index.</returns>
    public int GetCutsceneParentIndex(int actorIdx);

    /// <summary>
    /// Set the cutscene parent of <paramref name="copyIdx"/> in Penumbras internal state to a new value.
    /// </summary>
    /// <param name="copyIdx"> The index of the cutscene actor to be changed. </param>
    /// <param name="newParentIdx"> The new index of the cutscene actors parent or -1 for no parent. </param>
    /// <returns> Success when the new parent could be set, or InvalidArgument if either index is out of its respective range. </returns>
    /// <remarks>
    /// Checks that the new parent exists as a game object if the value is not -1 before assigning. If it does not, InvalidArgument is given, too.
    /// Please only use this for good reason and if you know what you are doing, probably only for actor copies you actually create yourself.
    /// </remarks>
    public PenumbraApiEc SetCutsceneParentIndex(int copyIdx, int newParentIdx);

    /// <summary>
    /// Triggered when a character base is created and a corresponding gameObject could be found,
    /// before the Draw Object is actually created, so customize and equipdata can be manipulated beforehand.
    /// </summary>
    /// <returns><inheritdoc cref="CreatingCharacterBaseDelegate"/></returns>
    public event CreatingCharacterBaseDelegate? CreatingCharacterBase;

    /// <summary>
    /// Triggered after a character base was created if a corresponding gameObject could be found,
    /// so you can apply flag changes after finishing.
    /// </summary>
    /// <returns><inheritdoc cref="CreatedCharacterBaseDelegate"/></returns>
    public event CreatedCharacterBaseDelegate? CreatedCharacterBase;

    /// <summary>
    /// Triggered whenever a resource is redirected by Penumbra for a specific, identified game object.
    /// Does not trigger if the resource is not requested for a known game object.
    /// </summary>
    /// <returns><inheritdoc cref="GameObjectResourceResolvedDelegate"/></returns>
    public event GameObjectResourceResolvedDelegate? GameObjectResourceResolved;
}
