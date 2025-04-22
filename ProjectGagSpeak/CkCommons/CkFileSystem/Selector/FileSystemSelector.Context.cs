using ImGuiNET;
using OtterGui;
using OtterGui.Raii;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage>
{
    /// <summary> Add a right-click context menu item to folder context menus at the given priority. </summary>
    /// <param name="action"> The Folder Right-Clicked on. </param>
    /// <param name="priority"> The priority of the context menu action. </param>
    /// <remarks> Context menu items are sorted from top to bottom on priority, then subscription order. </remarks>
    public void SubscribeRightClickFolder(Action<CkFileSystem<T>.Folder> action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsFolder, action, priority);

    /// <summary> Add a right-click context menu item to leaf context menus at the given priority. </summary>
    /// <param name="action"> The Leaf Right-Clicked on. </param>
    /// <param name="priority"> The priority of the context menu action. </param>
    /// <remarks> Context menu items are sorted from top to bottom on priority, then subscription order. </remarks>
    public void SubscribeRightClickLeaf(Action<CkFileSystem<T>.Leaf> action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsLeaf, action, priority);

    /// <summary> Add a right-click context menu item to the main context menu at the given priority. </summary>
    /// <param name="action"> The action to be performed. </param>
    /// <param name="priority"> The priority of the context menu action. </param>
    /// <remarks> Context menu items are sorted from top to bottom on priority, then subscription order. </remarks>
    public void SubscribeRightClickMain(Action action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsMain, action, priority);

    /// <summary> Remove a right-click context menu item from the folder context menu by reference equality. </summary>
    /// <param name="action"> The folder that is to be unsubscribed from. </param>
    public void UnsubscribeRightClickFolder(Action<CkFileSystem<T>.Folder> action)
        => RemovePrioritizedDelegate(_rightClickOptionsFolder, action);

    /// <summary> Remove a right-click context menu item from the leaf context menu by reference equality. </summary>
    /// <param name="action"> The leaf that is to be unsubscribed from. </param>
    public void UnsubscribeRightClickLeaf(Action<CkFileSystem<T>.Leaf> action)
        => RemovePrioritizedDelegate(_rightClickOptionsLeaf, action);

    /// <summary> Remove a right-click context menu item from the main context menu by reference equality. </summary>
    /// <param name="action"> The action that is to be unsubscribed from. </param>
    public void UnsubscribeRightClickMain(Action action)
        => RemovePrioritizedDelegate(_rightClickOptionsMain, action);

    /// <summary> Draw all context menu items for folders. </summary>
    /// <param name="folder"> The folder that was right-clicked on. </param>
    private void RightClickContext(CkFileSystem<T>.Folder folder)
    {
        using var _ = ImRaii.Popup(folder.Identifier.ToString());
        if (!_)
            return;

        foreach (var action in _rightClickOptionsFolder)
            action.Item1.Invoke(folder);
    }

    /// <summary> Draw all context menu items for leaves. </summary>
    /// <param name="leaf"> The leaf that was right-clicked on. </param>
    private void RightClickContext(CkFileSystem<T>.Leaf leaf)
    {
        using var _ = ImRaii.Popup(leaf.Identifier.ToString());
        if (!_)
            return;

        foreach (var action in _rightClickOptionsLeaf)
            action.Item1.Invoke(leaf);
    }

    /// <summary> Draw all context menu items for the main context. </summary>
    private void RightClickMainContext()
    {
        foreach (var action in _rightClickOptionsMain)
            action.Item1.Invoke();
    }


    // Lists are sorted on priority, then subscription order.
    private readonly List<(Action<CkFileSystem<T>.Folder>, int)> _rightClickOptionsFolder = new(4);
    private readonly List<(Action<CkFileSystem<T>.Leaf>, int)>   _rightClickOptionsLeaf   = new(1);
    private readonly List<(Action, int)>                         _rightClickOptionsMain   = new(4);

    private void InitDefaultContext()
    {
        SubscribeRightClickFolder(ExpandAllDescendants,   100);
        SubscribeRightClickFolder(CollapseAllDescendants, 100);
        SubscribeRightClickFolder(DissolveFolder,         999);
        SubscribeRightClickFolder(RenameFolder,           1000);
        SubscribeRightClickLeaf(RenameLeaf, 1000);
        SubscribeRightClickMain(ExpandAll,   1);
        SubscribeRightClickMain(CollapseAll, 1);
    }

    /// <summary> Default entries for the folder context menu. </summary>
    /// <param name="folder"> The folder that was right-clicked on. Protected so they can be removed by inheritors. </param>
    protected void DissolveFolder(CkFileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Dissolve Folder"))
            _fsActions.Enqueue(() => CkFileSystem.Merge(folder, folder.Parent));
        ImGuiUtil.HoverTooltip("Remove this folder and move all its children to its parent-folder, if possible.");
    }

    /// <summary> Expand all descendants of the folder by making them part of the statestruct list. </summary>
    /// <param name="folder"> The folder to expand. </param>
    protected void ExpandAllDescendants(CkFileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Expand All Descendants"))
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => ToggleDescendants(folder, idx, true));
        }

        ImGuiUtil.HoverTooltip("Successively expand all folders that descend from this folder, including itself.");
    }

    /// <summary> Collapse all descendants of the folder by removing them from the statestruct list. </summary>
    /// <param name="folder"> The folder to collapse. </param>
    protected void CollapseAllDescendants(CkFileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Collapse All Descendants"))
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => ToggleDescendants(folder, idx, false));
        }

        ImGuiUtil.HoverTooltip("Successively collapse all folders that descend from this folder, including itself.");
    }

    /// <summary> Renames the label given for the folder. </summary>
    /// <param name="folder"> The folder to rename. </param>
    protected void RenameFolder(CkFileSystem<T>.Folder folder)
    {
        var currentPath = folder.FullName();
        if (ImGui.InputText("##Rename", ref currentPath, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            _fsActions.Enqueue(() =>
            {
                CkFileSystem.RenameAndMove(folder, currentPath);
                _filterDirty |= ExpandAncestors(folder);
            });

        ImGuiUtil.HoverTooltip("Enter a full path here to move or rename the folder. Creates all required parent directories, if possible.");
    }

    protected void SetQuickMove(CkFileSystem<T>.Folder folder, int which, string current, Action<string> onSelect)
    {
        if (ImGui.MenuItem($"Set as Quick Move Folder #{which + 1}"))
            onSelect(folder.FullName());
        ImGuiUtil.HoverTooltip($"Set this folder as a quick move location{(current.Length > 0 ? $"instead of {current}." : ".")}");
    }

    protected void ClearQuickMove(int which, string current, Action onSelect)
    {
        if (current.Length == 0)
            return;

        if (ImGui.MenuItem($"Clear Quick Move Folder #{which + 1}"))
            onSelect();
        ImGuiUtil.HoverTooltip($"Clear the current quick move assignment of {current}.");
    }

    protected void QuickMove(CkFileSystem<T>.Leaf leaf, params string[] folders)
    {
        var currentName = leaf.Name;
        var currentPath = leaf.FullName();
        foreach (var (folder, idx) in folders.WithIndex().Where(s => s.Value.Length > 0))
        {
            using var id         = ImRaii.PushId(idx);
            var       targetPath = $"{folder}/{currentName}";
            if (CkFileSystem.Equal(targetPath, currentPath))
                continue;

            if (ImGui.MenuItem($"Move to {folder}"))
                _fsActions.Enqueue(() =>
                {
                    foreach(var path in _selectedPaths.OfType<CkFileSystem<T>.Leaf>())
                        CkFileSystem.RenameAndMove(path, $"{folder}/{path.Name}");
                    CkFileSystem.RenameAndMove(leaf, targetPath);
                    _filterDirty |= ExpandAncestors(leaf);
                });
        }

        ImGuiUtil.HoverTooltip("Move the selected objects to a previously set-up quick move location, if possible.");
    }

    protected void RenameLeaf(CkFileSystem<T>.Leaf leaf)
    {
        var currentPath = leaf.FullName();
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        ImGui.TextUnformatted("Rename Search Path or Move:");
        if (ImGui.InputText("##Rename", ref currentPath, 256, ImGuiInputTextFlags.EnterReturnsTrue))
        {
            _fsActions.Enqueue(() =>
            {
                CkFileSystem.RenameAndMove(leaf, currentPath);
                _filterDirty |= ExpandAncestors(leaf);
            });
            ImGui.CloseCurrentPopup();
        }

        ImGuiUtil.HoverTooltip("Enter a full path here to move or rename the search path of the leaf. " +
            "Creates all required parent directories, if possible.\n\nDoes NOT rename the actual data!");
    }

    protected void ExpandAll()
    {
        if (ImGui.Selectable("Expand All Directories"))
            _fsActions.Enqueue(() => ToggleDescendants(CkFileSystem.Root, -1, true));
    }

    protected void CollapseAll()
    {
        if (ImGui.Selectable("Collapse All Directories"))
            _fsActions.Enqueue(() =>
            {
                ToggleDescendants(CkFileSystem.Root, -1, false);
                AddDescendants(CkFileSystem.Root, -1);
            });
    }
}
