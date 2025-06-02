using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage>
{
    /// <summary> Gets set by defining the label itself. </summary>
    public readonly string MoveLabel = string.Empty;

    /// <summary> The cache of the currently moved object paths. </summary>
    private readonly Dictionary<string, CkFileSystem<T>.IPath>          _movedPathsDragDropCache = new();
    private          List<KeyValuePair<string, CkFileSystem<T>.IPath>>? _movedPathsDragDrop;

    /// <summary> The Source path for the drag-drop operation. </summary>
    private void DragDropSource(CkFileSystem<T>.IPath path)
    {
        // might regret that flag but we will see.
        using var _ = ImRaii.DragDropSource(ImGuiDragDropFlags.SourceAllowNullID);
        if (!_)
            return;

        ImGui.SetDragDropPayload(MoveLabel, nint.Zero, 0);
        _movedPathsDragDrop = MoveList(path);
        ImGui.TextUnformatted(_movedPathsDragDropCache.Count == 1
            ? $"Moving {_movedPathsDragDropCache.Keys.First()}..."
            : $"Moving ...\n\t - {string.Join("\n\t - ", _movedPathsDragDrop.Select(kvp => kvp.Key))}");
    }

    /// <summary> The target path that the drag-drop operation is currently over. </summary>
    /// <param name="path"> The path being handled. </param>
    private void DragDropTarget(CkFileSystem<T>.IPath path)
    {
        using var _ = ImRaii.DragDropTarget();
        if (!_)
            return;

        if (!ImGuiUtil.IsDropping(MoveLabel) || _movedPathsDragDrop == null)
            return;

        var paths = _movedPathsDragDrop;
        _movedPathsDragDrop = null;
        _fsActions.Enqueue(() =>
        {
            foreach (var (_, movedPath) in paths)
                CkFileSystem.Move(movedPath, path as CkFileSystem<T>.Folder ?? path.Parent);
        });
    }

    private List<KeyValuePair<string, CkFileSystem<T>.IPath>> MoveList(CkFileSystem<T>.IPath path)
    {
        _movedPathsDragDropCache.Clear();
        if (!AllowMultipleSelection)
        {
            _movedPathsDragDropCache.Add(path.FullName(), path);
            return _movedPathsDragDropCache.ToList();
        }

        _movedPathsDragDropCache.EnsureCapacity(_selectedPaths.Count + 1);
        foreach (var p in _selectedPaths.Append(path))
            _movedPathsDragDropCache.TryAdd(p.FullName(), p);

        var list = new List<KeyValuePair<string, CkFileSystem<T>.IPath>>(_movedPathsDragDropCache.Count);
        foreach (var kvp in _movedPathsDragDropCache)
        {
            var skip = false;

            var parent = DirectoryNameWithSlash(kvp.Key);
            while (parent.Length > 0)
            {
                if (_movedPathsDragDropCache.ContainsKey(parent))
                {
                    skip = true;
                    break;
                }

                parent = DirectoryNameWithSlash(parent);
            }

            if (!skip)
                list.Add(kvp);
        }

        return list;
    }

    /// <summary> Get the directory name with a trailing slash. </summary>
    private static string DirectoryNameWithSlash(string path)
    {
        var idx = path.LastIndexOf('/');
        return idx == -1 ? string.Empty : path[..idx];
    }
}
