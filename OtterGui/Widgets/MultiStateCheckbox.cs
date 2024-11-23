using ImGuiNET;
using OtterGuiInternal;
using OtterGuiInternal.Structs;
using OtterGuiInternal.Utility;

namespace OtterGui.Widgets;

/// <summary>
/// Draw a checkbox that toggles forward or backward between different states.
/// </summary>
public abstract class MultiStateCheckbox<T>
{
    /// <summary> Render the symbol corresponding to <paramref name="value"/> starting at <paramref name="position"/> and with <paramref name="size"/> as box size. </summary>
    protected abstract void RenderSymbol(T value, Vector2 position, float size);

    protected abstract T NextValue(T value);
    protected abstract T PreviousValue(T value);

    public unsafe bool Draw(ReadOnlySpan<char> label, T currentValue, out T newValue)
    {
        newValue = currentValue;
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return false;

        var (visibleEnd, labelSize, id) = StringHelpers.ComputeSizeAndId(label);
        // Calculate the bounding box of the checkbox including the label.
        var squareSize  = ImGui.GetFrameHeight();
        var style       = ImGui.GetStyle();
        var screenPos   = window.Dc.CursorPos;
        var itemSize    = new Vector2(squareSize + (labelSize.X > 0 ? style.ItemInnerSpacing.X + labelSize.X : 0), squareSize);
        var boundingBox = new ImRect(screenPos, screenPos + itemSize);

        // Add the item to internals. Skip it if it is clipped.
        ImGuiInternal.ItemSize(itemSize, style.FramePadding.Y);
        if (!ImGuiInternal.ItemAdd(boundingBox, id))
            return false;

        // Handle user interaction.
        var returnValue = ImGuiInternal.ButtonBehavior(boundingBox, id, out var hovered, out var held);
        var rightClick  = ImGui.IsItemClicked(ImGuiMouseButton.Right);
        if (rightClick)
            newValue = PreviousValue(currentValue);
        else if (returnValue)
            newValue = NextValue(currentValue);

        // Draw the checkbox.
        var checkBoundingBox = new ImRect(screenPos, screenPos + new Vector2(squareSize));
        ImGuiInternal.RenderNavHighlight(boundingBox, id);
        ImGuiInternal.RenderFrame(checkBoundingBox.Min, checkBoundingBox.Max, ColorHelpers.GetFrameBg(hovered, held), true,
            style.FrameRounding);

        // Draw the desired symbol into the checkbox.
        var paddingSize = Math.Max(1, (int)(squareSize / 6));
        RenderSymbol(currentValue, screenPos + new Vector2(paddingSize), squareSize - paddingSize * 2);

        // Add the label if there is one visible.
        if (labelSize.X > 0)
        {
            var labelPos = new Vector2(checkBoundingBox.MaxX + style.ItemInnerSpacing.X, checkBoundingBox.MinY + style.FramePadding.Y);
            StringHelpers.AddText(ImGui.GetWindowDrawList(), labelPos, ImGui.GetColorU32(ImGuiCol.Text), label[..visibleEnd], false);
        }

        return returnValue || rightClick;
    }
}
