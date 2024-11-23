using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Log;
using OtterGui.Raii;
using OtterGuiInternal;

namespace OtterGui.Widgets;

[Flags]
public enum MouseWheelType : byte
{
    None       = 0,
    Unmodified = 1,
    Shift      = 2,
    Control    = 4,
    Alt        = 8,
}

public abstract class FilterComboBase<T>
{
    private readonly HashSet<uint> _popupState = [];


    public readonly IReadOnlyList<T> Items;

    private LowerString _filter      = LowerString.Empty;
    private string[]    _filterParts = [];

    protected readonly Logger         Log;
    protected          bool           SearchByParts;
    protected          int?           NewSelection;
    private            int            _lastSelection = -1;
    private            bool           _filterDirty   = true;
    private            bool           _setScroll;
    private            bool           _closePopup;
    protected          MouseWheelType AllowMouseWheel { get; set; }
    private readonly   bool           _keepStorage;

    private readonly List<int> _available;

    public LowerString Filter
        => _filter;

    protected FilterComboBase(IReadOnlyList<T> items, bool keepStorage, Logger log)
    {
        Items        = items;
        _keepStorage = keepStorage;
        Log          = log;
        _available   = _keepStorage ? new List<int>(Items.Count) : [];
    }

    private void ClearStorage(string label)
    {
        Log.Verbose("Cleaning up Filter Combo Cache for {Label}.", label);
        _filter        = LowerString.Empty;
        _filterParts   = [];
        _lastSelection = -1;
        Cleanup();

        if (_keepStorage)
            return;

        _filterDirty = true;
        _available.Clear();
        _available.TrimExcess();
    }

    protected virtual bool IsVisible(int globalIndex, LowerString filter)
    {
        if (!SearchByParts)
            return filter.IsContained(ToString(Items[globalIndex]));

        if (_filterParts.Length == 0)
            return true;

        var name = ToString(Items[globalIndex]).ToLowerInvariant();
        return _filterParts.All(name.Contains);
    }

    protected virtual string ToString(T obj)
        => obj?.ToString() ?? string.Empty;

    // Can be called to manually reset the filter,
    // if it is dependent on things other than the entered string.
    public void ResetFilter()
        => _filterDirty = true;

    protected virtual float GetFilterWidth()
        => ImGui.GetWindowWidth() - 2 * ImGui.GetStyle().FramePadding.X;

    protected virtual void Cleanup()
    { }

    protected virtual void PostCombo(float previewWidth)
    { }

    protected virtual void DrawCombo(string label, string preview, string tooltip, int currentSelected, float previewWidth, float itemHeight,
        ImGuiComboFlags flags)
    {
        var id = ImGui.GetID(label);
        ImGui.SetNextItemWidth(previewWidth);
        using var combo = ImRaii.Combo(label, preview, flags | ImGuiComboFlags.HeightLarge);
        PostCombo(previewWidth);
        using (var dis = ImRaii.Enabled())
        {
            ImGuiUtil.HoverTooltip(tooltip, ImGuiHoveredFlags.AllowWhenDisabled);
        }

        if (combo)
        {
            _popupState.Add(id);
            UpdateFilter();

            // Width of the popup window and text input field.
            var width = GetFilterWidth();

            DrawFilter(currentSelected, width);
            DrawKeyboardNavigation();
            DrawList(width, itemHeight);
            ClosePopup(id, label);
        }
        else if (_popupState.Remove(id))
        {
            ClearStorage(label);
        }
    }

    protected virtual int UpdateCurrentSelected(int currentSelected)
    {
        _lastSelection = currentSelected;
        return currentSelected;
    }

    protected virtual void DrawFilter(int currentSelected, float width)
    {
        _setScroll = false;
        // If the popup is opening, set the last selection to the currently selected object, if any,
        // scroll to it, and set keyboard focus to the filter field.
        if (ImGui.IsWindowAppearing())
        {
            currentSelected = UpdateCurrentSelected(currentSelected);
            _lastSelection  = _available.IndexOf(currentSelected);
            _setScroll      = true;
            ImGui.SetKeyboardFocusHere();
        }

        // Draw the text input.
        ImGui.SetNextItemWidth(width);
        if (LowerString.InputWithHint("##filter", "Filter...", ref _filter))
        {
            _filterDirty = true;
            if (SearchByParts)
                _filterParts = _filter.Lower.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }

    protected virtual void DrawList(float width, float itemHeight)
    {
        // A child for the items, so that the filter remains visible.
        // Height is based on default combo height minus the filter input.
        var       height = ImGui.GetTextLineHeightWithSpacing() * 12 - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y;
        using var _      = ImRaii.Child("ChildL", new Vector2(width, height));
        using var indent = ImRaii.PushIndent(ImGuiHelpers.GlobalScale);
        if (_setScroll)
            ImGui.SetScrollFromPosY(_lastSelection * itemHeight - ImGui.GetScrollY());

        // Draw all available objects with their name.
        ImGuiClip.ClippedDraw(_available, DrawSelectableInternal, itemHeight);
    }

    protected virtual bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj  = Items[globalIdx];
        var name = ToString(obj);
        return ImGui.Selectable(name, selected);
    }

    private void DrawSelectableInternal(int globalIdx, int localIdx)
    {
        using var id = ImRaii.PushId(globalIdx);
        if (DrawSelectable(globalIdx, _lastSelection == localIdx))
        {
            NewSelection = globalIdx;
            _closePopup  = true;
        }
    }

    // Does not handle Enter.
    protected void DrawKeyboardNavigation()
    {
        // Enable keyboard navigation for going up and down,
        // jumping if reaching the end. This also scrolls to the element.
        if (_available.Count > 0)
        {
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
                (_lastSelection, _setScroll) = ((_lastSelection + 1) % _available.Count, true);
            else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
                (_lastSelection, _setScroll) = ((_lastSelection - 1 + _available.Count) % _available.Count, true);
        }

        // Escape closes the popup without selection
        _closePopup = ImGui.IsKeyPressed(ImGuiKey.Escape);

        // Enter selects the current selection if any, or the first available item.
        if (ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            if (_lastSelection >= 0)
                NewSelection = _available[_lastSelection];
            else if (_available.Count > 0)
                NewSelection = _available[0];
            _closePopup = true;
        }
    }

    protected virtual void OnClosePopup()
    { }

    protected virtual void OnMouseWheel(string preview, ref int currentSelection, int steps)
    { }

    protected void ClosePopup(uint id, string label)
    {
        if (!_closePopup)
            return;

        // Close the popup and reset state.
        ImGui.CloseCurrentPopup();
        _popupState.Remove(id);
        OnClosePopup();
        ClearStorage(label);
    }

    // Basic Draw.
    public virtual bool Draw(string label, string preview, string tooltip, ref int currentSelection, float previewWidth, float itemHeight,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        DrawCombo(label, preview, tooltip, currentSelection, previewWidth, itemHeight, flags);
        if (CheckMouseWheel(AllowMouseWheel) && ImGui.IsItemHovered())
        {
            ImGuiInternal.ItemSetUsingMouseWheel();
            var mw = (int)ImGui.GetIO().MouseWheel;
            if (mw != 0)
                OnMouseWheel(preview, ref currentSelection, mw);
        }

        if (NewSelection == null)
            return false;

        currentSelection = NewSelection.Value;
        NewSelection     = null;
        return true;
    }


    // Be stateful and update the filter whenever it gets dirty.
    // This is when the string is changed or on manual calls.
    private void UpdateFilter()
    {
        if (!_filterDirty)
            return;

        _filterDirty = false;
        _available.EnsureCapacity(Items.Count);

        // Keep the selected key if possible.
        var lastSelection = _lastSelection == -1 ? -1 : _available[_lastSelection];
        _lastSelection = -1;

        _available.Clear();
        for (var idx = 0; idx < Items.Count; ++idx)
        {
            if (!IsVisible(idx, _filter))
                continue;

            if (lastSelection == idx)
                _lastSelection = _available.Count;
            _available.Add(idx);
        }
    }

    // Check Mousewheel and modifiers,
    private static bool CheckMouseWheel(MouseWheelType type)
        => type switch
        {
            MouseWheelType.None                           => false,
            MouseWheelType.Unmodified                     => true,
            MouseWheelType.Shift                          => ImGui.GetIO().KeyShift,
            MouseWheelType.Control                        => ImGui.GetIO().KeyCtrl,
            MouseWheelType.Alt                            => ImGui.GetIO().KeyAlt,
            MouseWheelType.Shift | MouseWheelType.Control => ImGui.GetIO().KeyShift && ImGui.GetIO().KeyCtrl,
            MouseWheelType.Shift | MouseWheelType.Alt     => ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt,
            MouseWheelType.Control | MouseWheelType.Alt   => ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyAlt,
            MouseWheelType.Shift | MouseWheelType.Control | MouseWheelType.Alt => ImGui.GetIO().KeyShift
             && ImGui.GetIO().KeyCtrl
             && ImGui.GetIO().KeyAlt,
            _ => true,
        };
}
