using CkCommons.DrawSystem;

namespace GagSpeak.DrawSystem;

public interface ISortableFolder<TLeaf> where TLeaf : class
{
    /// <summary>
    ///   The uniquely identifying ID of the folder, used for UI state like Drag & Drop.
    /// </summary>
    uint ID { get; }


    /// <summary>
    ///   The currently applied dynamicSorter in a readonly state.
    /// </summary>
    public IReadOnlyDynamicSorter<DynamicLeaf<TLeaf>> DynamicSorter { get; }

    /// <summary>
    ///   Manually updated on every sort order update. Boosts draw performance for the filter editor.
    /// </summary>
    public IReadOnlyList<ISortMethod<DynamicLeaf<TLeaf>>> UnusedSteps { get; }

    /// <summary>
    ///   Adds a new filter to the end of the active list, saves the configuration, and recalculates steps.
    /// </summary>
    bool AddFilter(ISortMethod<DynamicLeaf<TLeaf>> filter);

    /// <summary>
    ///   Removes a filter at the specified index, saves the configuration, and recalculates steps.
    /// </summary>
    bool RemoveFilter(int index);

    /// <summary>
    ///   Moves existing filters to a new index, saves the configuration, and recalculates steps.
    /// </summary>
    bool MoveFilters(int[] fromIndices, int toIndex);

    /// <summary>
    ///   Clears all filters from the active list, saves the configuration, and recalculates steps.
    /// </summary>
    bool ClearFilters();
}