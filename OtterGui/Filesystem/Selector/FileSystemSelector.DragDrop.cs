using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    public readonly string MoveLabel = string.Empty; // Gets set by setting the label itself.

    // The currently moved object.
    private readonly Dictionary<string, FileSystem<T>.IPath>          _movedPathsDragDropCache = new();
    private          List<KeyValuePair<string, FileSystem<T>.IPath>>? _movedPathsDragDrop;

    private void DragDropSource(FileSystem<T>.IPath path)
    {
        using var _ = ImRaii.DragDropSource();
        if (!_)
            return;

        ImGui.SetDragDropPayload(MoveLabel, nint.Zero, 0);
        _movedPathsDragDrop = MoveList(path);
        ImGui.TextUnformatted(_movedPathsDragDropCache.Count == 1
            ? $"Moving {_movedPathsDragDropCache.Keys.First()}..."
            : $"Moving ...\n\t - {string.Join("\n\t - ", _movedPathsDragDrop.Select(kvp => kvp.Key))}");
    }

    private void DragDropTarget(FileSystem<T>.IPath path)
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
                FileSystem.Move(movedPath, path as FileSystem<T>.Folder ?? path.Parent);
        });
    }

    private List<KeyValuePair<string, FileSystem<T>.IPath>> MoveList(FileSystem<T>.IPath path)
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

        var list = new List<KeyValuePair<string, FileSystem<T>.IPath>>(_movedPathsDragDropCache.Count);
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

    private static string DirectoryNameWithSlash(string path)
    {
        var idx = path.LastIndexOf('/');
        return idx == -1 ? string.Empty : path[..idx];
    }
}
