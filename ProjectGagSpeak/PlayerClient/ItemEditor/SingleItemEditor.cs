using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerClient;

#pragma warning disable IDE1006 // Naming Styles

/// <summary>
///     Handles editing of a single item, not tied to any storage.
/// </summary>
public sealed class SingleItemEditor<T> where T : class, IEditableStorageItem<T>
{
    /// <summary> The original item being edited. </summary>
    private T? SourceItem = null;

    /// <summary> The cloned copy for editing. </summary>
    public T? ItemInEditor { get; private set; } = null;

    /// <summary> Begin editing by cloning the source item. </summary>
    public void StartEditing(T source)
    {
        if (source is null || ItemInEditor is not null)
            return;

        SourceItem = source;
        ItemInEditor = source.Clone(true);
    }

    /// <summary> Cancel editing and discard changes. </summary>
    public void QuitEditing()
    {
        ItemInEditor = null;
        SourceItem = null;
    }

    /// <summary> Apply changes from the editor copy to the original item. </summary>
    public bool SaveAndQuitEditing([NotNullWhen(true)] out T? source)
    {
        source = null;
        if (ItemInEditor is null || SourceItem is null)
            return false;

        SourceItem.ApplyChanges(ItemInEditor);
        source = SourceItem;
        ItemInEditor = null;
        SourceItem = null;
        return true;
    }
}
#pragma warning restore IDE1006 // Naming Styles

