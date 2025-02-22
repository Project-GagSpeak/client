using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.CkCommons.FileSystem.Selector;

public partial class CkFileSystemSelector<T, TStateStorage>
{
    private int             _currentDepth;
    private int             _currentIndex;
    private int             _currentEnd;
    private DateTimeOffset  _lastButtonTime = DateTimeOffset.UtcNow;

    /// <summary> The main function that determines what is drawn for each item in the listclipper. </summary>
    /// <returns> The Width and Height of the drawn item. </returns>
    private (Vector2, Vector2) DrawStateStruct(StateStruct state)
    {
        return state.Path switch
        {
            CkFileSystem<T>.Folder f => DrawFolder(f),
            CkFileSystem<T>.Leaf l   => DrawLeaf(l, state.StateStorage),
            _                        => (Vector2.Zero, Vector2.Zero),
        };
    }

    /// <summary> Draw a leaf. Supports drag'n drop, right-click context menus, and selection. </summary>
    /// <param name="leaf"> The Leaf to draw. </param>
    /// <param name="state"> The StateStorage for this leaf's path, if any. </param>
    /// <returns> The minimum and maximum points of the drawn item. </returns>
    private (Vector2, Vector2) DrawLeaf(CkFileSystem<T>.Leaf leaf, in TStateStorage state)
    {
        DrawLeafName(leaf, state, leaf == SelectedLeaf || SelectedPaths.Contains(leaf));
        if(ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            Select(leaf, state, ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(leaf.Identifier.ToString());

        DragDropSource(leaf);
        DragDropTarget(leaf);
        RightClickContext(leaf);
        return (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
    }

    /// <summary> Used for clipping. </summary>
    /// <remarks> If we start with an object not on depth 0, we need to add its indentation and the folder-lines for it. </remarks>
    private void DrawPseudoFolders()
    {
        var first   = _state[_currentIndex]; // The first object drawn during this iteration
        var parents = first.Path.Parents();
        // Push IDs in order and indent.
        ImGui.Indent(ImGui.GetStyle().IndentSpacing * parents.Length);

        // Get start point for the lines (top of the selector).
        var lineStart = ImGui.GetCursorScreenPos();

        // For each pseudo-parent in reverse order draw its children as usual, starting from _currentIndex.
        for (_currentDepth = parents.Length; _currentDepth > 0; --_currentDepth)
        {
            DrawChildren(lineStart);
            lineStart.X -= ImGui.GetStyle().IndentSpacing;
            ImGui.Unindent();
        }
    }

    /// <summary> Used for clipping. </summary>
    /// <remarks> If we end not on depth 0, check whether to terminate the folder lines or continue them to the screen end. </remarks>
    /// <returns> The adjusted line end. </returns>
    private Vector2 AdjustedLineEnd(Vector2 lineEnd)
    {
        if (_currentIndex != _currentEnd)
            return lineEnd;

        var y = ImGui.GetWindowHeight() + ImGui.GetWindowPos().Y;
        if (y > lineEnd.Y + ImGui.GetTextLineHeight())
            return lineEnd;

        // Continue iterating from the current end.
        for (var idx = _currentEnd; idx < _state.Count; ++idx)
        {
            var state = _state[idx];

            // If we find an object at the same depth, the current folder continues
            // and the line has to go out of the screen.
            if (state.Depth == _currentDepth)
                return lineEnd with { Y = y };

            // If we find an object at a lower depth before reaching current depth,
            // the current folder stops and the line should stop at the last drawn child, too.
            if (state.Depth < _currentDepth)
                return lineEnd;
        }

        // All children are in subfolders of this one, but this folder has no further children on its own.
        return lineEnd;
    }

    /// <summary> Draw children of a folder or pseudo-folder with a given line start using the current index and end. </summary>
    /// <param name="lineStart"> The start of the folder line. </param>
    private void DrawChildren(Vector2 lineStart)
    {
        // Folder line stuff.
        var offsetX  = -ImGui.GetStyle().IndentSpacing + ImGui.GetTreeNodeToLabelSpacing() / 2;
        var drawList = ImGui.GetWindowDrawList();
        lineStart.X += offsetX;
        lineStart.Y -= 2 * ImGuiHelpers.GlobalScale;
        var lineEnd = lineStart;

        for (; _currentIndex < _currentEnd; ++_currentIndex)
        {
            // If we leave _currentDepth, its not a child of the current folder anymore.
            var state = _state[_currentIndex];
            if (state.Depth != _currentDepth)
                break;

            var lineSize = Math.Max(0, ImGui.GetStyle().IndentSpacing - 9 * ImGuiHelpers.GlobalScale);
            // Draw the child
            var (minRect, maxRect) = DrawStateStruct(state);
            if (minRect.X == 0)
                continue;

            // Draw the notch and increase the line length.
            var midPoint = (minRect.Y + maxRect.Y) / 2f - 1f;
            drawList.AddLine(lineStart with { Y = midPoint }, new Vector2(lineStart.X + lineSize, midPoint), FolderLineColor,
                ImGuiHelpers.GlobalScale);
            lineEnd.Y = midPoint;
        }

        // Finally, draw the folder line.
        drawList.AddLine(lineStart, AdjustedLineEnd(lineEnd), FolderLineColor, ImGuiHelpers.GlobalScale);
    }

    /// <summary> Draw a folder. Handles drag'n drop, right-click context menus, expanding/collapsing, and selection. </summary>
    /// <param name="folder"> The Folder to draw. </param>
    /// <remarks> If the folder is expanded, draw its children one tier deeper. </remarks>
    /// <returns> The minimum and maximum points of the drawn item. </returns>
    private (Vector2, Vector2) DrawFolder(CkFileSystem<T>.Folder folder)
    {
        var selected = SelectedPaths.Contains(folder);
        DrawFolderName(folder, selected);
        if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left))
        {
            // update the state, then add or remove the descendants.
            folder.UpdateState(!folder.State);
            AddOrRemoveDescendants(folder);
        }

        if (AllowMultipleSelection && ImGui.IsItemClicked(ImGuiMouseButton.Left) && ImGui.GetIO().KeyCtrl)
            Select(folder, default, true, false);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(folder.Identifier.ToString());
        DragDropSource(folder);
        DragDropTarget(folder);
        RightClickContext(folder);

        var rect = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

        // If the folder is expanded, draw its children one tier deeper.
        if (!folder.State)
            return rect;

        ++_currentDepth;
        ++_currentIndex;
        ImGui.Indent();
        DrawChildren(ImGui.GetCursorScreenPos());
        ImGui.Unindent();
        --_currentIndex;
        --_currentDepth;

        return rect;
    }

    /// <summary> Open a collapse/expand context menu when right-clicking the selector without a selected item. </summary>
    private void MainContext()
    {
        const string mainContext = "MainContext";
        if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
        {
            if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                ImGui.SetWindowFocus(Label);
            ImGui.OpenPopup(mainContext);
        }

        using var pop = ImRaii.Popup(mainContext);
        if (!pop)
            return;

        RightClickMainContext();
    }


    /// <summary> Draw the whole list. </summary>
    /// <param name="width"> The width of the list. </param>
    /// <returns> If the list was drawn. </returns>
    private bool DrawList(float width)
    {
        // Filter row is outside the child for scrolling.
        DrawFilterRow(width);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var _     = ImRaii.Child(Label, new Vector2(width, -ImGui.GetFrameHeight()), true);
        style.Pop();
        MainContext();
        if (!_)
            return false;

        ImGui.SetScrollX(0);
        style.Push(ImGuiStyleVar.IndentSpacing, 14f * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.ItemSpacing,  new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGuiHelpers.GlobalScale))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(ImGuiHelpers.GlobalScale,       ImGui.GetStyle().FramePadding.Y));
        //// Check if filters are dirty and recompute them before the draw iteration if necessary.
        ApplyFilters();
        if (_jumpToSelection != null)
        {
            var idx = _state.FindIndex(s => s.Path == _jumpToSelection);
            if (idx >= 0)
                ImGui.SetScrollFromPosY(ImGui.GetTextLineHeightWithSpacing() * idx - ImGui.GetScrollY());

            _jumpToSelection = null;
        }

        using (var clipper = ImUtf8.ListClipper(_state.Count, ImGui.GetTextLineHeightWithSpacing()))
        {
            while (clipper.Step())
            {
                _currentIndex = clipper.DisplayStart;
                _currentEnd   = Math.Min(_state.Count, clipper.DisplayEnd);
                if (_currentIndex >= _currentEnd)
                    continue;

                if (_state[_currentIndex].Depth != 0)
                    DrawPseudoFolders();
                _currentEnd = Math.Min(_state.Count, _currentEnd);
                for (; _currentIndex < _currentEnd; ++_currentIndex)
                    DrawStateStruct(_state[_currentIndex]);
            }
        }

        //// Handle all queued actions at the end of the iteration.
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        HandleActions();
        style.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        return true;
    }
}
