using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Log;

namespace OtterGui.Widgets;

/// <summary>
/// A wrapper around filterable combos that makes them work with temporary lists without taking permanent additional memory.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FilterComboCache<T> : FilterComboBase<T>
{
    public T? CurrentSelection { get; protected set; }

    private readonly ICachingList<T> _items;
    protected        int             CurrentSelectionIdx = -1;

    protected bool IsInitialized
        => _items.IsInitialized;

    protected FilterComboCache(IEnumerable<T> items, MouseWheelType allowMouseWheel, Logger log)
        : base(new TemporaryList<T>(items), false, log)
    {
        AllowMouseWheel  = allowMouseWheel;
        CurrentSelection = default;
        _items           = (ICachingList<T>)Items;
    }

    protected FilterComboCache(Func<IReadOnlyList<T>> generator, MouseWheelType allowMouseWheel, Logger log)
        : base(new LazyList<T>(generator), false, log)
    {
        AllowMouseWheel  = allowMouseWheel;
        CurrentSelection = default;
        _items           = (ICachingList<T>)Items;
    }

    protected override void Cleanup()
        => _items.ClearList();


    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            UpdateSelection(Items[NewSelection.Value]);
    }

    protected virtual void UpdateSelection(T? newSelection)
    {
        if (!ReferenceEquals(CurrentSelection, newSelection))
            SelectionChanged?.Invoke(CurrentSelection, newSelection);
        CurrentSelection = newSelection;
    }

    protected override void OnMouseWheel(string _1, ref int _2, int steps)
    {
        if (Items.Count <= 1)
            return;

        var mouseWheel = -steps % Items.Count;
        NewSelection = mouseWheel switch
        {
            < 0 when CurrentSelectionIdx < 0 => Items.Count - 1 + mouseWheel,
            < 0                              => (CurrentSelectionIdx + Items.Count + mouseWheel) % Items.Count,
            > 0 when CurrentSelectionIdx < 0 => mouseWheel,
            > 0                              => (CurrentSelectionIdx + mouseWheel) % Items.Count,
            _                                => null,
        };
        if (NewSelection != null && Items.Count > NewSelection.Value)
        {
            CurrentSelectionIdx = NewSelection.Value;
            UpdateSelection(Items[NewSelection.Value]);
        }

        Cleanup();
    }

    public bool Draw(string label, string preview, string tooltip, float previewWidth, float itemHeight,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => Draw(label, preview, tooltip, ref CurrentSelectionIdx, previewWidth, itemHeight, flags);

    public event Action<T?, T?>? SelectionChanged;
}
