using Dalamud.Plugin.Services;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage> where T : class where TStateStorage : struct
{
    public delegate void SelectionChangeDelegate(T? oldSelection, T? newSelection, in TStateStorage state);

    protected readonly HashSet<FileSystem<T>.IPath> _selectedPaths = [];

    // The currently selected leaf, if any.
    protected FileSystem<T>.Leaf? SelectedLeaf;

    // The currently selected value, if any.
    public T? Selected
        => SelectedLeaf?.Value;

    public IReadOnlySet<FileSystem<T>.IPath> SelectedPaths
        => _selectedPaths;

    // Fired after the selected leaf changed.
    public event SelectionChangeDelegate? SelectionChanged;
    private FileSystem<T>.Leaf?           _jumpToSelection;

    public void ClearSelection()
        => Select(null, AllowMultipleSelection);

    public void RemovePathFromMultiSelection(FileSystem<T>.IPath path)
    {
        _selectedPaths.Remove(path);
        if (_selectedPaths.Count == 1 && _selectedPaths.First() is FileSystem<T>.Leaf leaf)
            Select(leaf, true, GetState(leaf));
    }

    private void Select(FileSystem<T>.IPath? path, in TStateStorage storage, bool additional, bool all)
    {
        if (path == null)
        {
            Select(null, AllowMultipleSelection, storage);
        }
        else if (all && AllowMultipleSelection && SelectedLeaf != path)
        {
            var idxTo = _state.IndexOf(s => s.Path == path);
            var depth = _state[idxTo].Depth;
            if (SelectedLeaf != null && _selectedPaths.Count == 0)
            {
                var idxFrom = _state.IndexOf(s => s.Path == SelectedLeaf);
                (idxFrom, idxTo) = idxFrom > idxTo ? (idxTo, idxFrom) : (idxFrom, idxTo);
                if (_state.Skip(idxFrom).Take(idxTo - idxFrom + 1).All(s => s.Depth == depth))
                {
                    foreach (var p in _state.Skip(idxFrom).Take(idxTo - idxFrom + 1))
                        _selectedPaths.Add(p.Path);
                    Select(null, false);
                }
            }
        }
        else if (additional && AllowMultipleSelection)
        {
            if (SelectedLeaf != null && _selectedPaths.Count == 0)
                _selectedPaths.Add(SelectedLeaf);
            if (!_selectedPaths.Add(path))
                RemovePathFromMultiSelection(path);
            else
                Select(null, false);
        }
        else if (path is FileSystem<T>.Leaf leaf)
        {
            Select(leaf, AllowMultipleSelection, storage);
        }
    }

    protected virtual void Select(FileSystem<T>.Leaf? leaf, bool clear, in TStateStorage storage = default)
    {
        if (clear)
            _selectedPaths.Clear();

        var oldV = SelectedLeaf?.Value;
        var newV = leaf?.Value;
        if (oldV == newV)
            return;

        SelectedLeaf = leaf;
        SelectionChanged?.Invoke(oldV, newV, storage);
    }

    protected readonly FileSystem<T> FileSystem;

    public virtual ISortMode<T> SortMode
        => ISortMode<T>.Lexicographical;

    // Used by Add and AddFolder buttons.
    private string _newName = string.Empty;

    private readonly string _label = string.Empty;

    public string Label
    {
        get => _label;
        init
        {
            _label    = value;
            MoveLabel = $"{value}Move";
        }
    }

    // Default color for tree expansion lines.
    protected virtual uint FolderLineColor
        => 0xFFFFFFFF;

    // Default color for folder names.
    protected virtual uint ExpandedFolderColor
        => 0xFFFFFFFF;

    protected virtual uint CollapsedFolderColor
        => 0xFFFFFFFF;

    // Whether all folders should be opened by default or closed.
    protected virtual bool FoldersDefaultOpen
        => false;

    public readonly Action<Exception> ExceptionHandler;

    public readonly bool AllowMultipleSelection;

    protected readonly ILogger Log;

    public FileSystemSelector(FileSystem<T> fileSystem, IKeyState keyState, ILogger log,
        string label = "##FileSystemSelector", bool allowMultipleSelection = false)
    {
        FileSystem             = fileSystem;
        _state                 = new List<StateStruct>(FileSystem.Root.TotalDescendants);
        _keyState              = keyState;
        Label                  = label;
        AllowMultipleSelection = allowMultipleSelection;
        Log                    = log;

        InitDefaultContext();
        InitDefaultButtons();
        EnableFileSystemSubscription();
        ExceptionHandler = e => Log.LogWarning(e.ToString());
    }

    /// <summary> Customization point: Should always create any interactable item. Item will be monitored for selection. </summary>
    /// <param name="folder"> The folder to draw. </param>
    /// <param name="selected"> If the folder is selected. (This does NOT mean if it's expanded or not. </param>
    /// <remarks> Everything here is wrapped in a group. </remarks>
    protected virtual void DrawFolderName(FileSystem<T>.Folder folder, bool selected)
    {
        using var id = ImRaii.PushId((int)folder.Identifier);
        using var color = ImRaii.PushColor(ImGuiCol.Text, folder.State ? ExpandedFolderColor : CollapsedFolderColor);
        ImGui.Selectable(" ► " + folder.Name.Replace("%", "%%"), selected, ImGuiSelectableFlags.None);
    }



    /// <summary> Customization point: Should always create any interactable item. Item will be monitored for selection. </summary>
    /// <param name="leaf"> The leaf to draw. </param>
    /// <param name="state"> The state storage for the leaf. </param>
    /// <param name="selected"> Whether the leaf is currently selected. </param>
    /// <remarks> Can add additional icons or buttons if wanted. Everything drawn in here is wrapped in a group. </remarks>
    protected virtual void DrawLeafName(FileSystem<T>.Leaf leaf, in TStateStorage state, bool selected)
    {
        // Can add custom color application in any override.
        using var id = ImRaii.PushId((int)leaf.Identifier);
        ImGui.Selectable("○" + leaf.Name.Replace("%", "%%"), selected, ImGuiSelectableFlags.None);
    }

    public void Draw(float width)
    {
        try
        {
            DrawPopups();
            using var group = ImRaii.Group();
            if (DrawList(width))
            {
                ImGui.PopStyleVar();
                if (width < 0)
                    width = ImGui.GetWindowWidth() - width;
                DrawButtons(width);
            }
        }
        catch (Exception e)
        {
            throw new Exception("Exception during FileSystemSelector rendering:\n"
              + $"{_currentIndex} Current Index\n"
              + $"{_currentDepth} Current Depth\n"
              + $"{_currentEnd} Current End\n"
              + $"{_state.Count} Current State Count\n"
              + $"{_filterDirty} Filter Dirty", e);
        }
    }

    // Select a specific leaf in the file system by its value.
    // If a corresponding leaf can be found, also expand its ancestors.
    public void SelectByValue(T value)
    {
        var leaf = FileSystem.Root.GetAllDescendants(ISortMode<T>.Lexicographical).OfType<FileSystem<T>.Leaf>()
            .FirstOrDefault(l => l.Value == value);
        if (leaf != null)
            EnqueueFsAction(() =>
            {
                _filterDirty |= ExpandAncestors(leaf);
                Select(leaf, AllowMultipleSelection, GetState(leaf));
                _jumpToSelection = leaf;
            });
    }
}
