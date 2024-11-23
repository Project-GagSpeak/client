using Newtonsoft.Json.Linq;
using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to the tracking of resources in use by actors. </summary>
public interface IPenumbraApiResourceTree
{
    /// <summary>
    /// Get the given game objects' resources, as dictionaries of actual paths (that may be FS paths for redirected resources, or game paths for swapped or vanilla resources) to game paths.
    /// </summary>
    /// <param name="gameObjects"> The game object indices for which to get the resources. </param>
    /// <returns> An array of resource path dictionaries, of the same length and in the same order as the given game object index array. </returns>
    /// <remarks> This function is best called right after the game objects are redrawn, as it may fail to resolve paths if relevant mod settings have changed since then. </remarks>
    public Dictionary<string, HashSet<string>>?[] GetGameObjectResourcePaths(params ushort[] gameObjects);

    /// <summary>
    /// Get the player and player-owned game objects' resources, as dictionaries of actual paths (that may be FS paths for redirected resources, or game paths for swapped or vanilla resources) to game paths.
    /// </summary>
    /// <returns> A dictionary of game object indices to resource path dictionaries. </returns>
    /// <remarks> This function is best called right after the game objects are redrawn, as it may fail to resolve paths if relevant mod settings have changed since then. </remarks>
    public Dictionary<ushort, Dictionary<string, HashSet<string>>> GetPlayerResourcePaths();

    /// <summary>
    /// Get the given game objects' resources of a given type, as dictionaries of resource handles to actual paths and, optionally, names and icons.
    /// </summary>
    /// <param name="type"> Type of the resources to get, for example <see cref="ResourceType.Mtrl"/> for materials. </param>
    /// <param name="withUiData"> Whether to get names and icons along with the paths. </param>
    /// <param name="gameObjects"> The game object indices for which to get the resources. </param>
    /// <returns> An array of resource information dictionaries, of the same length and in the same order as the given game object index array. </returns>
    /// <remarks>
    /// It is the caller's responsibility to make sure the returned resource handles are still in use on the game object's draw object before using them. <para />
    /// Also, callers should not use UI data for non-UI purposes.
    /// </remarks>
    public GameResourceDict?[] GetGameObjectResourcesOfType(ResourceType type, bool withUiData,
        params ushort[] gameObjects);

    /// <summary>
    /// Get the player and player-owned game objects' resources of a given type, as dictionaries of resource handles to actual paths and, optionally, names and icons.
    /// </summary>
    /// <param name="type"> Type of the resources to get, for example <see cref="ResourceType.Mtrl"/> for materials. </param>
    /// <param name="withUiData"> Whether to get names and icons along with the paths. </param>
    /// <returns> A dictionary of game object indices to resource information dictionaries. </returns>
    /// <remarks>
    /// It is the caller's responsibility to make sure the returned resource handles are still in use on the game object's draw object before using them. <para />
    /// Also, callers should not use UI data for non-UI purposes.
    /// </remarks>
    public Dictionary<ushort, GameResourceDict> GetPlayerResourcesOfType(ResourceType type, bool withUiData);

    /// <summary>
    /// Get the given game objects' resource tree.
    /// </summary>
    /// <param name="withUiData"> Whether to get names and icons along with the paths. </param>
    /// <param name="gameObjects"> The game object indices for which to get the resources. </param>
    /// <returns> An array of resource tree JObjects, of the same length and in the same order as the given game object index array. </returns>
    /// <remarks>
    /// It is the caller's responsibility to make sure the returned resource handles are still in use on the game object's draw object before using them. <para />
    /// Also, callers should not use UI data for non-UI purposes.
    /// </remarks>
    public JObject?[] GetGameObjectResourceTrees(bool withUiData, params ushort[] gameObjects);

    /// <summary>
    /// Get the player and player-owned game objects' resource trees.
    /// </summary>
    /// <param name="withUiData"> Whether to get names and icons along with the paths. </param>
    /// <returns> A dictionary of game object indices to resource trees. </returns>
    /// <remarks>
    /// It is the caller's responsibility to make sure the returned resource handles are still in use on the game object's draw object before using them. <para />
    /// Also, callers should not use UI data for non-UI purposes.
    /// </remarks>
    public Dictionary<ushort, JObject> GetPlayerResourceTrees(bool withUiData);
}
