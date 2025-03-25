using Dalamud.Interface;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerState.Models;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage> where T : class where TStateStorage : struct
{
    public delegate void SelectionChangeDelegate(T? oldSelection, T? newSelection, in TStateStorage state);

    protected readonly HashSet<CkFileSystem<T>.IPath> _selectedPaths = [];

    // The currently selected leaf, if any.
    protected CkFileSystem<T>.Leaf? SelectedLeaf;

    // The currently selected value, if any.
    public T? Selected
        => SelectedLeaf?.Value;

    public IReadOnlySet<CkFileSystem<T>.IPath> SelectedPaths
        => _selectedPaths;

    // Fired after the selected leaf changed.
    public event SelectionChangeDelegate? SelectionChanged;
    private CkFileSystem<T>.Leaf?         _jumpToSelection;

    public void ClearSelection()
        => Select(null, AllowMultipleSelection);

    public void RemovePathFromMultiSelection(CkFileSystem<T>.IPath path)
    {
        _selectedPaths.Remove(path);
        if (_selectedPaths.Count == 1 && _selectedPaths.First() is CkFileSystem<T>.Leaf leaf)
            Select(leaf, true, GetState(leaf));
    }

    private void Select(CkFileSystem<T>.IPath? path, in TStateStorage storage, bool additional, bool all)
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
        else if (path is CkFileSystem<T>.Leaf leaf)
        {
            Select(leaf, AllowMultipleSelection, storage);
        }
    }

    protected virtual void Select(CkFileSystem<T>.Leaf? leaf, bool clear, in TStateStorage storage = default)
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

    protected readonly CkFileSystem<T> CkFileSystem;

    public virtual ISortMode<T> SortMode
        => ISortMode<T>.Lexicographical;

    // Used by Add and AddFolder buttons.
    protected string _newName = string.Empty;

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

    public CkFileSystemSelector(CkFileSystem<T> fileSystem, ILogger log, IKeyState keyState,
        string label = "##CkFileSystemSelector", bool allowMultipleSelection = false)
    {
        CkFileSystem             = fileSystem;
        _state                 = new List<StateStruct>(CkFileSystem.Root.TotalDescendants);
        _keyState              = keyState;
        Label                  = label;
        AllowMultipleSelection = allowMultipleSelection;
        Log                    = log;

        InitDefaultContext();
        EnableFileSystemSubscription();
        ExceptionHandler = e => Log.LogWarning(e.ToString());
    }

    /// <summary> Customization point: Should always create any interactable item. Item will be monitored for selection. </summary>
    /// <param name="folder"> The folder to draw. </param>
    /// <param name="selected"> If the folder is selected. (This does NOT mean if it's expanded or not. </param>
    /// <remarks> Everything here is wrapped in a group. </remarks>
    protected virtual void DrawFolderName(CkFileSystem<T>.Folder folder, bool selected)
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
    protected virtual bool DrawLeafName(CkFileSystem<T>.Leaf leaf, in TStateStorage state, bool selected)
    {
        // Can add custom color application in any override.
        using var id = ImRaii.PushId((int)leaf.Identifier);
        return ImGui.Selectable("○" + leaf.Name.Replace("%", "%%"), selected, ImGuiSelectableFlags.None);
    }

    protected void DrawFolderButton()
    {
        const string newFolderName = "folderName";
        if (CkGui.IconButton(FAI.FolderPlus, inPopup: true))
            ImGui.OpenPopup(newFolderName);
        CkGui.AttachToolTip("Create a new, empty folder. Can contain '/' to create a directory structure.");

        // Does not need to be delayed since it is not in the iteration itself.
        CkFileSystem<T>.Folder? folder = null;
        if (ImGuiUtil.OpenNameField(newFolderName, ref _newName) && _newName.Length > 0)
            try
            {
                folder = CkFileSystem.FindOrCreateAllFolders(_newName);
                _newName = string.Empty;
            }
            catch { /* Consumed */ }

        if (folder != null)
            _filterDirty |= ExpandAncestors(folder);
    }

    // Select a specific leaf in the file system by its value.
    // If a corresponding leaf can be found, also expand its ancestors.
    public void SelectByValue(T value)
    {
        var leaf = CkFileSystem.Root.GetAllDescendants(ISortMode<T>.Lexicographical).OfType<CkFileSystem<T>.Leaf>()
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
