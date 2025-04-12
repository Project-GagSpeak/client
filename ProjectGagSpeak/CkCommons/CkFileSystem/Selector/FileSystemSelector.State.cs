using Dalamud.Interface;
using Dalamud.Utility;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage> : IDisposable
{
    /// <summary> The state storage can contain arbitrary data for each visible node. </summary>
    /// <remarks> It also contains the path of the visible node itself as well as its depth in the file system tree. </remarks>
    private struct StateStruct
    {
        public TStateStorage         StateStorage;
        public CkFileSystem<T>.IPath Path;
        public byte                  Depth;
    }

    /// <summary> Only contains values not filtered out at any time. </summary>
    /// <remarks> This is, in other words, the current filtered State cache of all visable folders and files. </remarks>
    private readonly List<StateStruct> _state;

    private CkFileSystem<T>.Leaf? _singleLeaf = null;
    private int                   _leafCount  = 0;

    public virtual void Dispose()
    {
        CkFileSystem.Changed -= OnFileSystemChange;
    }

    /// <summary> The default filter string that is input. </summary>
    protected string FilterValue { get; private set; } = string.Empty;

    /// <summary> If the filter was changed, recompute the state before the next draw iteration. </summary>
    private bool _filterDirty = true;

    /// <summary> Set the filter as dirty, requiring a recompute of the state. </summary>
    public void SetFilterDirty()
        => _filterDirty = true;

    protected string FilterTooltip = string.Empty;


    /// <summary> Internal method to change the filter value. </summary>
    /// <param name="filterValue"> the new filter value. </param>
    /// <returns> If the filter was changed. </returns>
    private bool ChangeFilterInternal(string filterValue)
    {
        if (filterValue == FilterValue)
            return false;

        FilterValue = filterValue;
        return true;
    }

    /// <summary> Customization point to draw additional filters into the filter row. </summary>
    /// <param name="width"> The width of the filter row. </param>
    /// <remarks> Parameters are start position for the filter input field and selector width. </remarks>
    /// <returns> The remaining width for the text input. </returns>
    protected virtual float CustomFiltersWidth(float width)
        => width;

    protected virtual void DrawCustomFilters() { }

    /// <summary> Draw the default filter row of a given width. </summary>
    /// <param name="width"> The width of the filter row. </param>
    /// <remarks> Also contains the buttons to add new items and folders if desired. </remarks>
    public void DrawFilterRow(float width)
    {
        using var group = ImRaii.Group();
        var       searchW   = CustomFiltersWidth(width);
        var       tmp       = FilterValue;
        var       tooltip   = FilterTooltip.Length > 0 ? FilterTooltip : string.Empty;
        var       change    = DrawerHelpers.FancySearchFilter("Filter", width, tooltip, ref tmp, 128, width - searchW, DrawCustomFilters);

        // the filter box had its value updated.
        if (change)
        {
            if (ChangeFilterInternal(tmp))
                SetFilterDirty();
        }

        // Draw any popups that we may have clicked on.
        DrawPopups();
    }

    /// <summary> Customization point on how a path should be filtered. Checks whether the FullName contains the current string by default. </summary>
    /// <param name="path"> The path to check. </param>
    /// <remarks> Is not called directly, but through ApplyFiltersAndState, which can be overwritten separately. </remarks>
    /// <returns> If any filters matched for this path. </returns>
    protected virtual bool ApplyFilters(CkFileSystem<T>.IPath path)
        => FilterValue.Length != 0 && !path.FullName().Contains(FilterValue);

    /// <summary> Customization point to get the state associated with a given path. </summary>
    /// <param name="path"> The path to get the state for. </param>
    /// <remarks> Is not called directly, but through ApplyFiltersAndState, which can be overwritten separately. </remarks>
    /// <returns> the state storage of the path. </returns>
    protected virtual TStateStorage GetState(CkFileSystem<T>.IPath path)
        => default;

    /// <summary> If state and filtering are connected, you can overwrite this method. </summary>
    /// <param name="path"> The path to get the state storage from and apply filters too. </param>
    /// <param name="state"> The StateStorage of the path. </param>
    /// <returns> If the path is visible after filtering. </returns>
    protected virtual bool ApplyFiltersAndState(CkFileSystem<T>.IPath path, out TStateStorage state)
    {
        state = GetState(path);
        return ApplyFilters(path);
    }

    /// <summary> Recursively apply filters. Folders are explored on their current expansion state and filtered themselves. </summary>
    /// <param name="path"> The Parent Path to apply and add filters to. </param>
    /// <param name="idx"> the index in the statestorage the path is at. </param>
    /// <param name="currentDepth"> the depth in the hierarchy we are in. </param>
    /// <returns> If anything was filtered here. </returns>
    /// <remarks> But if any of a folders descendants is visible, the folder will also remain visible. </remarks>
    private bool ApplyFiltersAddInternal(CkFileSystem<T>.IPath path, ref int idx, byte currentDepth)
    {
        var filtered = ApplyFiltersAndState(path, out var state);
        _state.Insert(idx, new StateStruct()
        {
            Depth        = currentDepth,
            Path         = path,
            StateStorage = state,
        });

        if (path is CkFileSystem<T>.Folder f)
        {
            if (f.State)
                foreach (var child in f.GetChildren(SortMode))
                {
                    ++idx;
                    filtered &= ApplyFiltersAddInternal(child, ref idx, (byte)(currentDepth + 1));
                }
            else
                filtered = ApplyFiltersScanInternal(path);
        }
        else if (!filtered && _leafCount++ == 0)
        {
            _singleLeaf = path as CkFileSystem<T>.Leaf;
        }

        // Remove a completely filtered folder again.
        if (filtered)
            _state.RemoveAt(idx--);

        return filtered;
    }

    /// <summary> Scan for visible descendants of an uncollapsed folder. </summary>
    /// <returns> If any filters were applied after the scan. </returns>
    private bool ApplyFiltersScanInternal(CkFileSystem<T>.IPath path)
    {
        if (!ApplyFiltersAndState(path, out var state))
        {
            if (path is CkFileSystem<T>.Leaf l && _leafCount++ == 0)
                _singleLeaf = l;
            return false;
        }

        if (path is CkFileSystem<T>.Folder f)
            return f.GetChildren(ISortMode<T>.Lexicographical).All(ApplyFiltersScanInternal);


        return true;
    }

    /// <summary> Non-recursive entry point for recreating filters if dirty. </summary>
    private void ApplyFilters()
    {
        if (!_filterDirty)
            return;

        _leafCount = 0;
        _state.Clear();
        var idx = 0;
        foreach (var child in CkFileSystem.Root.GetChildren(SortMode))
        {
            ApplyFiltersAddInternal(child, ref idx, 0);
            ++idx;
        }

        if (_leafCount == 1 && _singleLeaf! != SelectedLeaf)
        {
            _filterDirty = ExpandAncestors(_singleLeaf!);
            Select(_singleLeaf, AllowMultipleSelection, GetState(_singleLeaf!));
        }
        else
        {
            _filterDirty = false;
        }
    }


    /// <summary> Adds or removes descendants of the given folder based on the affected change. </summary>
    /// <param name="folder"> The folder we are adding or removing descendants from. </param>
    private void AddOrRemoveDescendants(CkFileSystem<T>.Folder folder)
    {
        if (folder.State)
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => AddDescendants(folder, idx));
        }
        else
        {
            RemoveDescendants(_currentIndex);
        }
    }

    /// <summary> Given the cache-index to a folder, remove its descendants from the cache. </summary>
    /// <param name="parentIndex"> The index of the folder in the cache. -1 indicates the root. </param>
    /// <remarks> Used when folders are collapsed. </remarks>
    private void RemoveDescendants(int parentIndex)
    {
        var start = parentIndex + 1;
        var depth = parentIndex < 0 ? -1 : _state[parentIndex].Depth;
        var end   = start;
        for (; end < _state.Count; ++end)
        {
            if (_state[end].Depth <= depth)
                break;
        }

        _state.RemoveRange(start, end - start);
        _currentEnd -= end - start;
    }

    /// <summary> Given a folder and its cache-index, add all its expanded and unfiltered descendants to the cache. </summary>
    /// <param name="f"> the folder to add descendants from. </param>
    /// <param name="parentIndex"> the index of the folder in the cache. -1 indicates the root. </param>
    /// <remarks> Used when folders are expanded. </remarks>
    private void AddDescendants(CkFileSystem<T>.Folder f, int parentIndex)
    {
        var depth = (byte)(parentIndex == -1 ? 0 : _state[parentIndex].Depth + 1);
        foreach (var child in f.GetChildren(SortMode))
        {
            ++parentIndex;
            ApplyFiltersAddInternal(child, ref parentIndex, depth);
        }
    }

    /// <summary> Any file system change also sets the filters dirty. </summary>
    private void EnableFileSystemSubscription()
        => CkFileSystem.Changed += OnFileSystemChange;

    /// <summary> Processes what to do upon any change in the File System. </summary>
    /// <param name="type"> The type of change that occurred. </param>
    /// <param name="changedObject"> The object that was changed. </param>
    /// <param name="previousParent"> The previous parent the object belonged to. </param>
    /// <param name="newParent"> The new parent the object belongs to. </param>
    private void OnFileSystemChange(FileSystemChangeType type, CkFileSystem<T>.IPath changedObject, CkFileSystem<T>.IPath? previousParent,
        CkFileSystem<T>.IPath? newParent)
    {
        switch (type)
        {
            case FileSystemChangeType.ObjectMoved:
                EnqueueFsAction(() =>
                {
                    ExpandAncestors(changedObject);
                    SetFilterDirty();
                });
                break;
            case FileSystemChangeType.ObjectRemoved:
            case FileSystemChangeType.Reload:
                if (changedObject == SelectedLeaf)
                    ClearSelection();
                else if (AllowMultipleSelection)
                    _selectedPaths.Remove(changedObject);
                SetFilterDirty();
                break;
            default:
                SetFilterDirty();
                break;
        }
    }
}
