namespace GagSpeak.PlayerClient;

public interface IEditableStorage<T> where T : class, IEditableStorageItem<T>
{

    /// <summary> Replace the original item with a new one (used for abstract type changes). </summary>
    /// <param name="oldItem"> The item to be replaced. </param>
    /// <param name="newItem"> The new item to replace the old one with. </param>
    /// <returns> True if the item was replaced, false if it was not. </returns>
    public virtual T? ReplaceSource(T oldItem, T newItem) => null;

    /// <summary> Apply changes from changedItem to the original source item (in-place update). </summary>
    /// <remarks> This will keep the source item as a reference, and only updates internals. </remarks>
    public bool TryApplyChanges(T sourceItem, T changedItem);
}
