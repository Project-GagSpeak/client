using Dalamud.Interface.Utility;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using ImGuiNET;
using OtterGui;
using OtterGui.Classes;
using OtterGui.Raii;
using OtterGui.Text;
using System.Numerics;

namespace GagSpeak.UI.Components.Combos;

/// <summary>
/// Customized GagSpeak Combo Base. Searchable, filterable, highly configurable.
/// This combo is designed to clear/remove the combo item on right click, and to set on selection.
/// </summary>
public abstract class GagspeakComboBase<T>
{
    protected readonly ILogger _logger;
    protected readonly UiSharedService _uiShared;

    private string _label;
    private readonly HashSet<uint> _popupState = [];

    protected GagspeakComboBase(ILogger log, UiSharedService uiShared, string label)
    {
        _logger = log;
        _uiShared = uiShared;
        _label = label;
    }

    /// <summary> Fetched items from the respective pairData. </summary>
    public IReadOnlyList<T> Items => ExtractItems();

    protected string _defaultPreviewText = "Select an item...";
    private bool _adjustScroll;
    private bool _closePopup;

    private LowerString _filter = LowerString.Empty;

    private IReadOnlyList<T> FilteredItems => (_filter.IsEmpty
        ? Items : Items.Where(item => ToItemString(item).ToLowerInvariant().Contains(_filter)).ToList());

    /// <summary>
    /// Virtual Method for dictating how items are fetched from the pairData. 
    /// </summary>
    /// <returns>The list of items to be used when displaying the open combo.</returns>
    protected abstract IReadOnlyList<T> ExtractItems();

    /// <summary>
    /// Gets the active item in the combo. Should always be valid. If an item can ever not be valid, not be a part of this.
    /// </summary>
    protected abstract T CurrentActiveItem();

    /// <summary>
    /// The action to perform on any item being selected.
    /// </summary>
    protected abstract void OnItemSelected(T selectedItem);

    /// <summary>
    /// what to do when we right click the combo.
    /// </summary>
    protected abstract void OnClearActiveItem();

    /// <summary>
    /// Helper method for obtaining the item name/string from the selected Item.
    /// </summary>
    /// <returns>The string identifying name of the passed in item.</returns>
    protected virtual string ToItemString(T item) => item?.ToString() ?? string.Empty;

    /// <summary>
    /// Obtain the combo's filter width here. This is however long it is defined to be.
    /// </summary>
    /// <returns>the width of the combo's filter bar</returns>
    protected virtual float GetFilterWidth() => ImGui.GetWindowWidth() - 2 * ImGui.GetStyle().FramePadding.X;

    /// <summary>
    /// Used to get the preview text that should be displayed on the combo while it is closed.
    /// </summary>
    /// <returns>The Combo Box preview text.</returns>
    protected virtual string GetPreviewText() => ToItemString(CurrentActiveItem());

    /// <summary>
    /// A virtual method for drawing out the tooltip that should be displayed when hovering over the combo.
    /// </summary>
    protected virtual void DrawTooltip() => ImGuiUtil.HoverTooltip("", ImGuiHoveredFlags.AllowWhenDisabled);

    /// <summary>
    /// The condition that if satisfied disables the combo.
    /// </summary>
    protected abstract bool DisableCondition();

    /// <summary>
    /// The internal method used for drawing the combos which is virtual.
    /// </summary>
    public virtual bool DrawCombo(string label, string tt, float width, float popupWidthScale, float itemH, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // obtain the ID for the combo that we are about to spawn.
        var id = ImGui.GetID(_label + label);
        // set its width to whatever the fuck we want it to be
        ImGui.SetNextItemWidth(width);
        // init the combo with ImRaii.
        using var disabled = ImRaii.Disabled(DisableCondition());
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using var combo = ImRaii.Combo(label, GetPreviewText(), flags | ImGuiComboFlags.HeightLarge);

        // display the tooltip for the combo with visible.
        using (ImRaii.Enabled())
        {
            DrawTooltip();
            // Handle right click clearing.
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                OnClearActiveItem();
        }

        // if the combo is opened up.
        if (combo)
        {
            // Add the new popup below this combo to our popup state monitor.
            _popupState.Add(id);

            // obtain the width that we desire the filter to be so we can draw it appropriately.
            var filterWidth = GetFilterWidth();
            DrawFilter(filterWidth); // This also corrects keyboard navigation.

            // Draw the list via custom clip draw.
            var result = DrawList(filterWidth, itemH);

            // Close the popup if we should be.
            ClosePopup(id);
            return result;
        }
        // If the combo is not open, remove its popup state.
        _popupState.Remove(id);
        return false;
    }

    /// <summary>
    /// Method that should draw the filter for the combo boxy.
    /// </summary>
    /// <param name="width">the width it should be drawn</param>
    protected virtual void DrawFilter(float width)
    {
        _adjustScroll = false;
        // If the popup is opening, set the last selection to the currently selected object, if any,
        // scroll to it, and set keyboard focus to the filter field.
        if (ImGui.IsWindowAppearing())
        {
            _adjustScroll = true;
            ImGui.SetKeyboardFocusHere();
        }

        // Draw the text input.
        ImGui.SetNextItemWidth(width);
        LowerString.InputWithHint("##filter", "Filter...", ref _filter);
    }

    /// <summary>
    /// Draws all selectable elements of the list.
    /// </summary>
    /// <param name="width">the width of the child inside of the pop up.</param>
    /// <param name="itemHeight">the height each item will be in the clipped list.</param>
    /// <returns>if any item in the list was selected.</returns>
    protected virtual bool DrawList(float width, float itemHeight)
    {
        // A child for the items, so that the filter remains visible.
        // Height is based on default combo height minus the filter input.
        var height = ImGui.GetTextLineHeightWithSpacing() * 12 - ImGui.GetFrameHeight() - ImGui.GetStyle().WindowPadding.Y;
        using var _ = ImRaii.Child("ChildL", new Vector2(width, height));
        using var indent = ImRaii.PushIndent(ImGuiHelpers.GlobalScale);

        // Adjust the scroll if we should.
        if (_adjustScroll)
        {
            // get the index of the current selection in the filtered item list, then set the scroll accordingly.
            var selectedIdx = FilteredItems.ToList().IndexOf(CurrentActiveItem());
            ImGui.SetScrollFromPosY(selectedIdx * itemHeight - ImGui.GetScrollY());
        }

        // Draw all available objects with their name.
        return ClippedDrawBool(DrawSelectableInternal, itemHeight);
    }

    /// <summary>
    /// Draws a selectable item in the list.
    /// If the item was selected, the CurrentSelection will be updated and the popup requested to close.
    /// </summary>
    /// <param name="item">the item to draw for.</param>
    /// <param name="filteredIdx">the filtered index in the list that it is drawing.</param>
    /// <returns>if the item was selected or not.</returns>
    private bool DrawSelectableInternal(T item, int filteredIdx)
    {
        using var id = ImRaii.PushId(filteredIdx);
        try
        {
            bool isSelected = EqualityComparer<T>.Default.Equals(CurrentActiveItem(), item);
            if (DrawSelectable(item, isSelected))
            {
                OnItemSelected(item);
                _closePopup = true;
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error drawing selectable item.");
        }
        return false;
    }

    /// <summary>
    /// The internal virtual draw method for the selectable.
    /// Result logic of what to do if true should not happen here. only the UI display logic.
    /// </summary>
    /// <param name="item">the item to draw the selectable for.</param>
    /// <param name="selected">if it is currently selected.</param>
    /// <returns>if the item got selected or not.</returns>
    protected virtual bool DrawSelectable(T item, bool selected)
    {
        var name = ToItemString(item);
        var res = ImGui.Selectable(name, selected);
        return res;
    }

    /// <summary>
    /// Method to close the popup.
    /// </summary>
    /// <param name="id">the id of the combo popup to close when the combo closes.</param>
    protected void ClosePopup(uint id)
    {
        if (!_closePopup)
            return;

        // Close the popup and reset state.
        ImGui.CloseCurrentPopup();
        _popupState.Remove(id);
        _filter = LowerString.Empty;
        _closePopup = false;
    }


    /// <summary>
    /// Custom ImGuiClip function from OtterGui.ImGuiClip that returns a boolean action for clipped drawing.
    /// </summary>
    /// <param name="draw">the function used to dictate what is drawn into each clip of the list clipper.</param>
    /// <param name="lineHeight">the height that each clip listing should be.</param>
    /// <returns></returns>
    private bool ClippedDrawBool(Func<T, int, bool> draw, float lineHeight)
    {
        using var clipper = ImUtf8.ListClipper(FilteredItems.Count(), lineHeight);
        while (clipper.Step())
        {
            for (var actualRow = clipper.DisplayStart; actualRow < clipper.DisplayEnd; actualRow++)
            {
                if (actualRow >= FilteredItems.Count())
                    return false; // End gracefully if we are out of bounds.

                if (actualRow < 0)
                    continue; // Skip invalid indexes.

                // if the draw functions action returns true then we should halt drawing and return true.
                // This prevents the need to store additional variables such as NewSelection and LastSelection.
                if (draw(FilteredItems[actualRow], actualRow))
                    return true;
            }
        }
        return false; // return false if we have not selected anything.
    }
}
