using Penumbra.Api.Enums;

namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to collection management. </summary>
public interface IPenumbraApiCollection
{
    /// <returns> A list of the GUIDs of all currently installed collections together with their display names, excluding the empty collection. </returns>
    public Dictionary<Guid, string> GetCollections();

    /// <summary> Returns all collections for which either
    /// <list type="number">
    ///     <item> the name is equal to the given identifier up to case, </item>
    ///     <item> the identifier is parsable to a GUID and the GUID corresponds to an existing collection, </item>
    ///     <item> or the identifier is at least 8 characters long and the GUID as a hex-string starts with the identifier. </item>
    /// </list>
    /// </summary>
    public List<(Guid Id, string Name)> GetCollectionsByIdentifier(string identifier);

    /// <returns>A dictionary of affected items in <paramref name="collectionId"/> via GUID and known objects or null.</returns>
    public Dictionary<string, object?> GetChangedItemsForCollection(Guid collectionId);

    /// <returns> The GUID and name of the collection assigned to the given <paramref name="type"/>, the empty GUID for the empty collection, or null if nothing is assigned. </returns>
    public (Guid Id, string Name)? GetCollection(ApiCollectionType type);

    /// <returns>Return whether the object at <paramref name="gameObjectIdx" /> produces a valid identifier, if the identifier has a collection assigned, and the collection that affects the object.</returns>
    public (bool ObjectValid, bool IndividualSet, (Guid Id, string Name) EffectiveCollection) GetCollectionForObject(int gameObjectIdx);

    /// <summary>
    /// Set a collection by GUID for a specific type.
    /// </summary>
    /// <param name="type">The collection type to set.</param>
    /// <param name="collectionId">The GUID of the collection to set it to, null to remove the association if allowed. </param>
    /// <param name="allowCreateNew">Allow only setting existing types or also creating an unset type.</param>
    /// <param name="allowDelete">Allow deleting existing collections if <paramref name="collectionId"/> is empty.</param>
    /// <returns>InvalidArgument if type is invalid,
    /// NothingChanged if the new collection is the same as the old,<br />
    /// AssignmentDeletionDisallowed if <paramref name="collectionId"/> is null and <paramref name="allowDelete"/> is false, and the assignment exists,<br />
    /// or if Default, Current or Interface would be deleted.<br />
    /// CollectionMissing if the new collection can not be found,<br />
    /// AssignmentCreationDisallowed if <paramref name="allowCreateNew"/> is false and the assignment does not exist,<br />
    /// or Success, as well as the GUID of the previous collection (empty if no assignment existed).
    /// </returns>
    public (PenumbraApiEc, (Guid Id, string Name)? OldCollection) SetCollection(ApiCollectionType type, Guid? collectionId, bool allowCreateNew,
        bool allowDelete);

    /// <summary>
    /// Set a collection by GUID for a specific game object.
    /// </summary>
    /// <param name="gameObjectIdx">The index of the desired game object in the object table.</param>
    /// <param name="collectionId">The GUID of the collection to set it to, null to remove the association if allowed. </param>
    /// <param name="allowCreateNew">Allow only setting existing individuals or also creating a new individual assignment.</param>
    /// <param name="allowDelete">Allow deleting existing individual assignments if <paramref name="collectionId"/> is null.</param>
    /// <returns>InvalidIdentifier if <paramref name="gameObjectIdx"/> does not produce an existing game object or the object is not identifiable,
    /// NothingChanged if the new collection is the same as the old,<br />
    /// AssignmentDeletionDisallowed if <paramref name="collectionId"/> is null and <paramref name="allowDelete"/> is false, and the assignment exists,<br />
    /// CollectionMissing if the new collection can not be found,<br />
    /// AssignmentCreationDisallowed if <paramref name="allowCreateNew"/> is false and the assignment does not exist,<br />
    /// or Success, as well as the name of the previous collection (empty if no assignment existed).</returns>
    public (PenumbraApiEc, (Guid Id, string Name)? OldCollection) SetCollectionForObject(int gameObjectIdx, Guid? collectionId, bool allowCreateNew,
        bool allowDelete);
}
