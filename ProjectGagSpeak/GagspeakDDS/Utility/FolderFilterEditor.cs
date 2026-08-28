using CkCommons.DrawSystem;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Extensions;
using OtterGui.Text;
using OtterGui.Text.EndObjects;

namespace GagSpeak.DrawSystem;

public class FolderFilterEditor<T> where T : class
{
    private static ReadOnlySpan<byte> FilterDragLabel => "##DragFilterOption"u8;
    private Func<bool>? _postDrawAction;

    private ISortMethod<DynamicLeaf<T>>? _lastAnchor = null;

    private ISortableFolder<T>? _dragDropFolder;
    private List<ISortMethod<DynamicLeaf<T>>>? _dragDropSteps;
    private readonly HashSet<ISortMethod<DynamicLeaf<T>>> _selectedSteps = [];

    public bool DrawPopup(string popupId, ISortableFolder<T> folder, float width)
    {
        ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X, 0));
        using var s = ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 1f)
            .Push(ImGuiStyleVar.PopupRounding, 5f)
            .Push(ImGuiStyleVar.WindowPadding, ImGuiHelpers.ScaledVector2(4f, 1f));
        using var c = ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.ParsedGold);

        using var popup = ImRaii.Popup(popupId, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar);
        if (!popup) return false;

        CkGui.InlineSpacingInner();
        CkGui.ColorTextFrameAligned("Filters:", ImGuiColors.ParsedGold);
        ImGui.Separator();

        return DrawFilterOptions(folder, width);
    }

    private bool DrawFilterOptions(ISortableFolder<T> folder, float width)
    {
        var leftSize = new Vector2(width - ImUtf8.FrameHeight, ImUtf8.TextHeight);
        // Active Options
        foreach (var (sortStep, stepIdx) in folder.DynamicSorter.Steps.WithIndex())
        {
            using var id = ImRaii.PushId(stepIdx);

            DrawFilterOption(folder, sortStep, leftSize, stepIdx);
            ImUtf8.SameLineInner();

            bool active = true;
            if (ImUtf8.Checkbox("##toggle", ref active))
                _postDrawAction = () => folder.RemoveFilter(stepIdx);
        }

        foreach (var step in folder.UnusedSteps)
        {
            using var id = ImRaii.PushId($"unused_{step.Name}");

            DrawStaleFilterOption(step, leftSize);
            ImUtf8.SameLineInner();
            // Checkbox, then filter option.
            bool inactive = false;
            if (ImUtf8.Checkbox("##toggle", ref inactive))
                _postDrawAction = () => folder.AddFilter(step);
        }

        ImGui.Spacing();

        if (_postDrawAction is not null)
        {
            var updated = _postDrawAction.Invoke();
            _postDrawAction = null;
            return updated;
        }

        return false;
    }

    private void DrawFilterOption(ISortableFolder<T> folder, ISortMethod<DynamicLeaf<T>> step, Vector2 size, int idx)
    {
        using var _ = ImRaii.Group();
        var posX = ImGui.GetCursorPosX();
        ImGui.AlignTextToFramePadding();

        var clicked = ImGui.Selectable("##" + step.Name, _selectedSteps.Contains(step), ImGuiSelectableFlags.DontClosePopups, size);

        Target(folder, idx);
        Source(folder, step);

        ImGui.SameLine(posX);
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(step.Icon);
        CkGui.TextFrameAlignedInline(step.Name, false);

        if (idx != int.MaxValue && clicked)
        {
            var io = ImGui.GetIO();
            // CTRL: toggle individual selection.
            if (io.KeyCtrl)
            {
                if (!_selectedSteps.Remove(step))
                    _selectedSteps.Add(step);
                // Update the last anchor point.
                _lastAnchor = step;
            }
            // Shift: range select from last selection.
            else if (io.KeyShift && _dragDropFolder == folder && _lastAnchor is not null)
            {
                var lastAnchorIdx = folder.DynamicSorter.IndexOf(_lastAnchor);
                var start = Math.Min(lastAnchorIdx, idx);
                var end = Math.Max(lastAnchorIdx, idx);
                // Select all inbetween.
                for (var i = start; i <= end; ++i)
                    _selectedSteps.Add(folder.DynamicSorter[i]);
            }
            // No modifier means it is a simple single select.
            else
            {
                _selectedSteps.Clear();
                _lastAnchor = null;
            }
        }
    }

    private void DrawStaleFilterOption(ISortMethod<DynamicLeaf<T>> step, Vector2 size)
    {
        using var _ = ImRaii.Group();
        using var dis = ImRaii.Disabled();

        var posX = ImGui.GetCursorPosX();
        ImGui.Dummy(size);
        ImGui.SameLine(posX);
        ImGui.AlignTextToFramePadding();
        CkGui.IconText(step.Icon);
        CkGui.TextFrameAlignedInline(step.Name, false);
    }

    private void Target(ISortableFolder<T> folder, int idx)
    {
        if (_dragDropFolder == null || _dragDropFolder.ID != folder.ID || _dragDropSteps == null || _dragDropSteps.Count == 0)
            return;

        using var target = ImUtf8.DragDropTarget();
        if (!target.IsDropping(FilterDragLabel))
            return;

        var fromIndices = _dragDropSteps
            .Select(s => folder.DynamicSorter.IndexOf(s))
            .Where(i => i >= 0)
            .ToArray();

        if (fromIndices.Length > 0)
            _postDrawAction = () => folder.MoveFilters(fromIndices, idx);

        ClearDragState();
    }

    private void Source(ISortableFolder<T> folder, ISortMethod<DynamicLeaf<T>> step)
    {
        using var source = ImUtf8.DragDropSource();
        if (!source) return;

        if (!DragDropSource.SetPayload(FilterDragLabel))
        {
            _dragDropFolder = folder;
            _dragDropSteps = _selectedSteps.Count > 1 ? [.. _selectedSteps] : [step];
        }

        if (_dragDropSteps is null)
            return;
        var names = string.Join(", ", _dragDropSteps.Select(s => s.Name));
        ImUtf8.Text($"Reordering step{(_dragDropSteps.Count > 1 ? "s" : "")} {names}..");
    }

    private void ClearDragState()
    {
        _dragDropFolder = null;
        _dragDropSteps = null;
        _selectedSteps.Clear();
    }
}


