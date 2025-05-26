using Dalamud.Interface.Utility;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;

// Credit to OtterGui for the original implementation.
namespace GagSpeak.CustomCombos;
public static class CkComboExtensions
{
    public static void SetSearchBgColor<T>(this CkFilterComboBase<T> combo, uint color)
    {
        combo.SetSearchBgColor(color);
    }
}

public abstract class CkFilterComboBase<T>
{
    private readonly HashSet<uint> _popupState = [];

    public readonly IReadOnlyList<T> Items;

    private LowerString _filter = LowerString.Empty;
    private string[] _filterParts = [];

    protected readonly ILogger Log;
    protected float? InnerWidth;
    protected bool SearchByParts;
    protected int? NewSelection;
    private int _lastSelection = -1;
    private bool _filterDirty = true;
    private bool _setScroll;
    private bool _closePopup;

    /// <summary> The stored filtered indexes available for display so we can avoid iterating the Items. </summary>
    private readonly List<int> _available;

    public LowerString Filter 
        => _filter;

    protected CkFilterComboBase(IReadOnlyList<T> items, ILogger log)
    {
        Items = items;
        Log = log;
        _available = [];
    }

    /// <summary> Cleans up the storage for the combo item list. </summary>
    /// <param name="label"> The label of the combo to clear the storage of.. </param>
    private void ClearStorage(string label)
    {
        Log.LogTrace($"Cleaning up Filter Combo Cache for {label}");
        _filter = LowerString.Empty;
        _filterParts = [];
        _lastSelection = -1;
        Cleanup();

        _filterDirty = true;
        _available.Clear();
        _available.TrimExcess();
    }

    /// <summary> Determines if the item is visible based on the filter. </summary>
    protected virtual bool IsVisible(int globalIndex, LowerString filter)
    {
        if (!SearchByParts)
            return filter.IsContained(ToString(Items[globalIndex]));

        if (_filterParts.Length == 0)
            return true;

        var name = ToString(Items[globalIndex]).ToLowerInvariant();
        return _filterParts.All(name.Contains);
    }

    /// <summary> The method used to get the string value of the indexed Type T object. </summary>
    protected virtual string ToString(T obj)
        => obj?.ToString() ?? string.Empty;

    /// <summary> Determines the width of the filter input field. </summary>
    /// <remarks> This in turn determines the width of the item selectables. </remarks>
    protected virtual float GetFilterWidth()
    {
        return InnerWidth.HasValue
            ? InnerWidth.Value - 2 * ImGui.GetStyle().FramePadding.X
            : ImGui.GetWindowWidth() - 2 * ImGui.GetStyle().FramePadding.X;
    }

    /// <summary> Called upon a storage clear. Should be overridden by Filter Combo Cache to ensure its caches are cleared. </summary>
    protected virtual void Cleanup() { }

    // Maybe remove later, unsure what purpose this serves.
    protected virtual void PostCombo(float previewWidth) { }

    /// <summary> Called by the filter combo base Draw() call. Handles updates and changed items. </summary>
    private void DrawCombo(string label, string preview, string tooltip, int currentSelected, float previewWidth, float itemHeight,
        ImGuiComboFlags flags, uint? customSearchBg = null)
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
            // Appends the popup to display the window of items when opened.
            _popupState.Add(id);
            // Updates the filter to have the correct _available indexes.
            UpdateFilter();

            // Width of the popup window and text input field.
            var width = GetFilterWidth();

            // Draws the filter and updates the scroll to the selected items.
            DrawFilter(currentSelected, width, customSearchBg);

            // Draws the remaining list of items.
            // If any items are selected, they are stored in `NewSelection`.
            // `NewSelection` is cleared at the end of the parent DrawFunction.
            DrawList(width, itemHeight);
            // If we should close the popup (after selection), do so.
            ClosePopup(id, label);
        }
        else if (_popupState.Remove(id))
        {
            // Clear the storage if the popup state can be removed. (We closed it)
            ClearStorage(label);
        }
    }

    /// <summary> Updates the current selection to be used in the DrawList function. </summary>
    /// <remarks> Do not ever override this unless you know what you're doing. If you do, you must call the base. </remarks>
    protected virtual int UpdateCurrentSelected(int currentSelected)
    {
        _lastSelection = currentSelected;
        return currentSelected;
    }

    /// <summary> Updates the last selection with the currently selected item. This is then updated to the proper index in _available[] </summary>
    /// <remarks> Additionally scrolls the list to the last selected item, if any, and displays the filter. </remarks>
    protected virtual void DrawFilter(int currentSelected, float width, uint? searchBg)
    {
        if(searchBg.HasValue)
            ImGui.PushStyleColor(ImGuiCol.FrameBg, searchBg.Value);

        _setScroll = false;
        // Scroll to current selected when opened if any, and set keyboard focus to the filter field.
        if (ImGui.IsWindowAppearing())
        {
            currentSelected = UpdateCurrentSelected(currentSelected);
            _lastSelection = _available.IndexOf(currentSelected);
            _setScroll = true;
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

        if (searchBg.HasValue)
            ImGui.PopStyleColor();
    }

    /// <summary> Draws the list of items. </summary>
    /// <remarks> If any are selected, the respective DrawSelectedInternal will set the NewSelection to a value. </remarks>
    protected virtual void DrawList(float width, float itemHeight)
    {
        // A child for the items, so that the filter remains visible.
        // Height is based on default combo height minus the filter input.
        var height = ImGui.GetTextLineHeightWithSpacing() * 12 - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y;
        using var _ = ImRaii.Child("ChildL", new Vector2(width, height));
        using var indent = ImRaii.PushIndent(ImGuiHelpers.GlobalScale);
        if (_setScroll)
            ImGui.SetScrollFromPosY(_lastSelection * itemHeight - ImGui.GetScrollY());

        // Draw all available objects with their name.
        OtterGui.ImGuiClip.ClippedDraw(_available, DrawSelectableInternal, itemHeight);
    }

    protected virtual bool DrawSelectable(int globalIdx, bool selected)
    {
        var obj = Items[globalIdx];
        var name = ToString(obj);
        return ImGui.Selectable(name, selected);
    }

    private void DrawSelectableInternal(int globalIdx, int localIdx)
    {
        using var id = ImRaii.PushId(globalIdx);
        if (DrawSelectable(globalIdx, _lastSelection == localIdx))
        {
            NewSelection = globalIdx;
            _closePopup = true;
        }
    }

    /// <summary> To execute any additional logic upon the popup closing prior to clearing the storage. Override this. </summary>
    protected virtual void OnClosePopup() { }

    /// <summary> The action to take upon a popup closing. </summary>
    private void ClosePopup(uint id, string label)
    {
        if (!_closePopup)
            return;

        // Close the popup and reset state.
        Log.LogTrace("Closing popup for {Label}.", label);
        ImGui.CloseCurrentPopup();
        _popupState.Remove(id);
        OnClosePopup();
        ClearStorage(label);
        // Reset the close popup state.
        _closePopup = false;
    }

    /// <summary> MAIN DRAW CALL LOGIC FUNC. Should ALWAYS be called by the Filter Combo Cache unless instructed otherwise. </summary>
    /// <returns> True if anything was selected, false otherwise. </returns>
    /// <remarks> This will return the index of the `ref` currentSelection, meaning Filter Combo Cache handles the selected item. </remarks>
    public virtual bool Draw(string label, string preview, string tooltip, ref int currentSelection, float previewWidth, float itemHeight,
        ImGuiComboFlags flags = ImGuiComboFlags.None, uint? customSearchBg = null)
    {
        DrawCombo(label, preview, tooltip, currentSelection, previewWidth, itemHeight, flags, customSearchBg);
        if (NewSelection is null)
            return false;

        currentSelection = NewSelection.Value;
        NewSelection = null;
        return true;
    }


    /// <summary> Be stateful and update the filter whenever it gets dirty. </summary>
    private void UpdateFilter()
    {
        if (!_filterDirty)
            return;

        Log.LogDebug("Updating Filter Combo Cache");
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
}
