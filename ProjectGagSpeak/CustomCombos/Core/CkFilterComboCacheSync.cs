using ImGuiNET;
using OtterGui.Classes;
// Credit to OtterGui for the original implementation.
// These implementations are modified versions designed to fit both ClientSide updates and Callback-Dependent updates.

namespace GagSpeak.CustomCombos;

/// <summary> Variant that doesn't update CurrentSelectionIdx & CurrentSelection on selection. Instead, item selection invokes OnItemSelected() </summary>
/// <remarks> This should only ever be callbed by client-side selections to synced data without a button. (Currently just Gags) </remarks>
public abstract class CkFilterComboCacheSync<T> : CkFilterComboBase<T>
{
    /// <summary> The selected item in non-index format. </summary>
    public T? CurrentSelection { get; protected set; }

    /// <summary> A Cached List of the generated items. </summary>
    /// <remarks> Items are regenerated every time a cleanup is called. </remarks>
    private readonly ICachingList<T> _items;
    protected int CurrentSelectionIdx = -1;

    /// <summary> The condition that is met whenever the CachingList <typeparamref name="T"/> has finished caching the generated item function. </summary>
    protected bool IsInitialized
        => _items.IsInitialized;

    protected CkFilterComboCacheSync(IEnumerable<T> items, ILogger log)
        : base(new TemporaryList<T>(items), log)
    {
        CurrentSelection = default;
        _items = (ICachingList<T>)Items;
    }

    protected CkFilterComboCacheSync(Func<IReadOnlyList<T>> generator, ILogger log)
        : base(new LazyList<T>(generator), log)
    {
        CurrentSelection = default;
        _items = (ICachingList<T>)Items;
    }

    /// <summary> Triggers our Caching list to regenerate its passed in item list. </summary>
    /// <remarks> Call this whenever the source of our list updates to keep it synced. </remarks>
    protected override void Cleanup()
        => _items.ClearList();

    /// <summary> Draws the list and updates the selection in the filter cache if needed. </summary>
    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            if(!ReferenceEquals(CurrentSelection, Items[NewSelection.Value]))
                OnItemSelected(Items[NewSelection.Value]);
    }

    /// <summary> Called upon whenever we selected an item from the list. </summary>
    /// <remarks> Parent function should send the result to the server, & have its callback update here. </remarks>
    protected abstract void OnItemSelected(T? newSelection);

    /// <summary> The main Draw function that should be used for any parenting client side FilterCombo's of all types. </summary>
    /// <remarks> Any selection, or any change, will be stored into the CurrentSelectionIdx. </remarks>
    public bool Draw(string label, string preview, string tooltip, float previewWidth, float itemHeight,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => Draw(label, preview, tooltip, ref CurrentSelectionIdx, previewWidth, itemHeight, flags);

    /// <summary> Fires an event that gives us the previous and new selection. </summary>
    public event Action<T?, T?>? SelectionChanged;
}
