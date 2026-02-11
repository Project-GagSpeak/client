//using CkCommons.DrawSystem;
//using CkCommons.Gui;
//using Dalamud.Bindings.ImGui;
//using Dalamud.Interface.Colors;
//using Dalamud.Interface.Utility;
//using Dalamud.Interface.Utility.Raii;
//using OtterGui.Extensions;
//using OtterGui.Text;
//using OtterGui.Text.EndObjects;
//using Sundouleia.Pairs;
//using Sundouleia.PlayerClient;

//namespace Sundouleia.DrawSystem;

//public class FolderFilterEditor(GroupsManager manager)
//{
//    private static ReadOnlySpan<byte> FilterDragLabel => "##DragFilterOption"u8;
//    private Func<bool>? _postDrawAction;

//    // For Selecting
//    private ISortMethod<DynamicLeaf<Kinkster>>? _lastAnchor = null;

//    // For Dragging
//    private GroupFolder? _dragDropFolder;
//    private List<ISortMethod<DynamicLeaf<Kinkster>>>? _dragDropSteps;
//    private readonly HashSet<ISortMethod<DynamicLeaf<Kinkster>>> _selectedSteps = new();

//    /// <summary>
//    ///     Draws the popup display to the topright of the last drawn item. <para />
//    ///     The popup draws the filter options, that are drag-drop re-orderable.
//    ///     Additionally checkboxes can enable/disable filters. <para />
//    /// </summary>
//    /// <returns> True if <paramref name="group"/>'s Sorter in <see cref="FolderConfig"/> updated. </returns>
//    public bool DrawPopup(string popupId, GroupFolder folder, float width)
//    {
//        // Set next popup position, style, color, and display.
//        ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0));
//        using var s = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f)
//            .Push(ImGuiStyleVar.PopupRounding, 5f)
//            .Push(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(4f, 1f));
//        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedGold);
//        using var popup = ImRaii.Popup(popupId, WFlags.NoMove | WFlags.NoResize | WFlags.NoCollapse | WFlags.NoScrollbar);
//        if (!popup)
//            return false;

//        // Display the filter editor inside, after drawing the filter popup display.
//        CkGui.InlineSpacingInner();
//        CkGui.ColorTextFrameAligned("Filters:", ImGuiColors.ParsedGold);
//        ImGui.Separator();

//        // If this returns true it means that the sort filters changed and we should update the folder's sorter.
//        // Additionally we should refresh the folder, not the entire cache.
//        return DrawFilterOptions(folder, width);
//    }

//    /// <summary>
//    ///     Draws the filter options, that are drag-drop re-orderable.
//    ///     Additionally checkboxes can enable/disable filters. <para />
//    /// </summary>
//    /// <returns> True if <paramref name="group"/>'s Sorter in <see cref="FolderConfig"/> updated. </returns>
//    public bool DrawFilterOptions(GroupFolder group, float width)
//    {
//        // Need to draw out the included options first, then the unincluded options.
//        var selectableSize = new Vector2(width - ImUtf8.FrameHeight, ImUtf8.TextHeight);
//        foreach (var (sortStep, stepIdx) in group.FolderSorter.WithIndex())
//        {
//            using var id = ImRaii.PushId(stepIdx);

//            DrawFilterOption(group, sortStep, selectableSize, stepIdx);
//            ImUtf8.SameLineInner();
//            // Checkbox, then filter option.
//            bool active = true;
//            if (ImUtf8.Checkbox("##toggle", ref active))
//                _postDrawAction = () => manager.RemoveFilter(group.Name, stepIdx);
//        }

//        // For all remaining unused options, draw these too.
//        foreach (var step in group.UnusedSteps)
//        {
//            using var id = ImRaii.PushId($"unused_{step.Name}");

//            DrawStaleFilterOption(group, step, selectableSize);
//            ImUtf8.SameLineInner();
//            // Checkbox, then filter option.
//            bool inactive = false;
//            if (ImUtf8.Checkbox("##toggle", ref inactive))
//                _postDrawAction = () => manager.AddFilter(group.Name, step.ToFolderSortFilter());
//        }

//        ImGui.Spacing();

//        // Process any post draw actions.
//        bool updated = false;
//        // If the action is not null, execute it then clear it.
//        if (_postDrawAction is not null)
//        {
//            updated = _postDrawAction.Invoke();
//            _postDrawAction = null;
//        }

//        return updated;
//    }

//    private void DrawFilterOption(GroupFolder group, ISortMethod<DynamicLeaf<Kinkster>> step, Vector2 size, int idx)
//    {
//        using var _ = ImRaii.Group();
//        var posX = ImGui.GetCursorPosX();
//        ImGui.AlignTextToFramePadding();
//        var clicked = ImGui.Selectable("##" + step.Name, _selectedSteps.Contains(step), ImGuiSelectableFlags.DontClosePopups, size);
//        // Mark the selectable as a dragdrop target and source.
//        if (idx != int.MaxValue)
//        {
//            Target(group, idx);
//            Source(group, step);
//        }

//        ImGui.SameLine(posX);
//        ImGui.AlignTextToFramePadding();
//        CkGui.IconText(step.Icon);
//        CkGui.TextFrameAlignedInline(step.Name, false);

//        if (idx != int.MaxValue && clicked)
//        {
//            var io = ImGui.GetIO();
//            // CTRL: toggle individual selection.
//            if (io.KeyCtrl)
//            {
//                if (!_selectedSteps.Remove(step))
//                    _selectedSteps.Add(step);
//                // Update the last anchor point.
//                _lastAnchor = step;
//                // some combining display here?
//            }
//            // Shift: range select from last selection.
//            else if (io.KeyShift && _dragDropFolder == group && _lastAnchor is not null)
//            {
//                var lastAnchorIdx = group.FolderSorter.IndexOf(_lastAnchor);
//                var start = Math.Min(lastAnchorIdx, idx);
//                var end = Math.Max(lastAnchorIdx, idx);
//                // Select all inbetween.
//                for (var i = start; i <= end; ++i)
//                    _selectedSteps.Add(group.FolderSorter[i]);
//            }
//            // No modifier means it is a simple single select.
//            else
//            {
//                _selectedSteps.Clear();
//                _lastAnchor = null;
//            }
//        }
//    }

//    private void DrawStaleFilterOption(GroupFolder group, ISortMethod<DynamicLeaf<Kinkster>> step, Vector2 size)
//    {
//        using var _ = ImRaii.Group();
//        using var dis = ImRaii.Disabled();
        
//        var posX = ImGui.GetCursorPosX();
//        ImGui.Dummy(size);
//        ImGui.SameLine(posX);
//        ImGui.AlignTextToFramePadding();
//        CkGui.IconText(step.Icon);
//        CkGui.TextFrameAlignedInline(step.Name, false);
//    }

//    // If a payload was set on the target.
//    private void Target(GroupFolder folder, int idx)
//    {
//        // Should be impossible to drop into a different group, but check anyways.
//        if (_dragDropFolder != folder || _dragDropSteps is null || _dragDropSteps.Count is 0)
//            return;

//        using var target = ImUtf8.DragDropTarget();
//        if (!target.IsDropping(FilterDragLabel))
//            return;

//        // Determine steps to move
//        var stepsToMove = _dragDropSteps ?? [];
//        // Map them to their indices.
//        var fromIndices = stepsToMove
//            .Select(s => folder.FolderSorter.IndexOf(s))
//            .Where(i => i >= 0)
//            .ToArray();

//        // Clear and return if not moving anything.
//        if (fromIndices.Length is 0)
//        {
//            Clear();
//            return;
//        }

//        // Enqueue the move in a post-draw action.
//        _postDrawAction = () => manager.MoveFilters(folder.Name, fromIndices, idx);
//        Clear();

//        // Clear the dragdrop state.
//        void Clear()
//        {
//            _dragDropFolder = null;
//            _dragDropSteps = null;
//            _selectedSteps.Clear();
//        }
//    }

//    private void Source(GroupFolder group, ISortMethod<DynamicLeaf<Kinkster>> step)
//    {
//        using var source = ImUtf8.DragDropSource();
//        if (!source)
//            return;

//        if (!DragDropSource.SetPayload(FilterDragLabel))
//        {
//            _dragDropFolder = group;
//            if (_selectedSteps.Count > 1)
//                _dragDropSteps = _selectedSteps.ToList();
//            else
//                _dragDropSteps = [ step ];
//        }

//        if (_dragDropSteps is null)
//            return;

//        var names = _dragDropSteps != null ? string.Join(", ", _dragDropSteps.Select(s => s.Name)) : step.Name;
//        ImUtf8.Text($"Reordering step{(_dragDropSteps is not null && _dragDropSteps.Count > 1 ? "s" : "")} {names}..");
//    }
//}


