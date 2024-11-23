using Penumbra.Api.Enums;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the redrawing of actors. </summary>
public interface IPenumbraApiRedraw
{
    /// <summary>
    /// Queue redrawing of the actor with the given object <paramref name="gameObjectIndex" />, if it exists, with the given RedrawType <paramref name="setting"/>.
    /// </summary>
    public void RedrawObject(int gameObjectIndex, RedrawType setting);

    /// <summary>
    /// Queue redrawing of all currently available actors with the given RedrawType <paramref name="setting"/>.
    /// </summary>
    public void RedrawAll(RedrawType setting);

    /// <summary>
    /// Triggered whenever a game object is redrawn via Penumbra.
    /// </summary>
    /// /<returns><inheritdoc cref="GameObjectRedrawnDelegate"/></returns>
    public event GameObjectRedrawnDelegate? GameObjectRedrawn;
}
