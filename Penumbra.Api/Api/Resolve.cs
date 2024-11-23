using Lumina.Data;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the resolving of paths. </summary>
public interface IPenumbraApiResolve
{
    /// <summary>
    /// Resolve a given <paramref name="gamePath" /> via Penumbra using the Base collection.
    /// </summary>
    /// <returns>The resolved path, or the given path if Penumbra would not manipulate it.</returns>
    public string ResolveDefaultPath(string gamePath);

    /// <summary>
    /// Resolve a given <paramref name="gamePath" /> via Penumbra using the Interface collection.
    /// </summary>
    /// <returns>The resolved path, or the given path if Penumbra would not manipulate it.</returns>
    public string ResolveInterfacePath(string gamePath);

    /// <summary>
    /// Resolve a given <paramref name="gamePath" /> via Penumbra using collection applying to the <paramref name="gameObjectIdx"/> 
    /// given by its index in the game object table.
    /// </summary>
    /// <remarks>If the object does not exist in the table, the default collection is used.</remarks>
    /// <returns>The resolved path, or the given path if Penumbra would not manipulate it.</returns>
    public string ResolveGameObjectPath(string gamePath, int gameObjectIdx);

    /// <summary>
    /// Resolve a given <paramref name="gamePath" /> via Penumbra using the collection currently applying to the player character.
    /// </summary>
    /// <returns>The resolved path, or the given path if Penumbra would not manipulate it.</returns>
    public string ResolvePlayerPath(string gamePath);

    /// <summary>
    /// Reverse resolves a given local <paramref name="moddedPath" /> into its replacement in form of all applicable game paths
    /// for the collection applying to the <paramref name="gameObjectIdx"/>th game object in the game object table.
    /// </summary>
    /// <remarks>If the object does not exist in the table, the default collection is used.</remarks>
    /// <returns>A list of game paths resolving to the modded path.</returns>
    public string[] ReverseResolveGameObjectPath(string moddedPath, int gameObjectIdx);

    /// <summary>
    /// Reverse resolves a given local <paramref name="moddedPath" /> into its replacement in form of all applicable game paths
    /// for the collection currently applying to the player character.
    /// </summary>
    /// <returns>A list of game paths resolving to the modded path.</returns>
    public string[] ReverseResolvePlayerPath(string moddedPath);

    /// <summary>
    /// Resolve all game paths in <paramref name="forward"/> and reserve all paths in <paramref name="reverse"/> at once.
    /// </summary>
    /// <param name="forward">Paths to forward-resolve.</param>
    /// <param name="reverse">Paths to reverse-resolve.</param>
    /// <returns>A pair of an array of forward-resolved single paths of the same length as <paramref name="forward"/> and an array of arrays of reverse-resolved paths.
    /// The outer array has the same length as <paramref name="reverse"/> while each inner array can have arbitrary length.</returns>
    public (string[], string[][]) ResolvePlayerPaths(string[] forward, string[] reverse);

    /// <summary>
    /// Resolve all game paths in <paramref name="forward"/> and reserve all paths in <paramref name="reverse"/> at once asynchronously.
    /// </summary>
    /// <inheritdoc cref="ResolvePlayerPaths"/>
    /// <remarks> Can be called from outside of framework. Can theoretically produce incoherent state when collections change during evaluation. </remarks>
    public Task<(string[], string[][])> ResolvePlayerPathsAsync(string[] forward, string[] reverse);
}
