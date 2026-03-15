namespace GagSpeak.PlayerClient;

/// <summary>
///     Signifies that this item can be used in an editor interface when inside a storage container.
/// </summary>
public interface IEditableStorageItem<T> where T : class
{
    /// <summary> 
    ///     Clones the item, creating a new instance of the item with the same values. <para />
    ///     KeepIdentifier allows you to choose if the GUID is copied or not.
    /// </summary>
    /// <returns> The new instance of the cloned item. </returns>
    T Clone(bool keepId = false);

    /// <summary> Apply changes from another item to this one (in-place update). </summary>
    /// <remarks> This will keep the source item as a reference, and only updates internals. </remarks>
    void ApplyChanges(T changedItem);
}
