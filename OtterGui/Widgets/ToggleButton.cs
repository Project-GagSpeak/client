using ImGuiNET;
using OtterGuiInternal;
using OtterGuiInternal.Structs;
using OtterGuiInternal.Utility;

namespace OtterGui.Widgets;

public static class ToggleButton
{
    private const ImGuiButtonFlags AlignTextBaseLine = (ImGuiButtonFlags)(1 << 15);

    public static bool ButtonEx(ReadOnlySpan<char> label, Vector2 sizeArg, ImGuiButtonFlags flags, ImDrawFlags corners)
    {
        // Copied from ImGui proper.
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return false;

        var style = ImGui.GetStyle();
        var (visibleEnd, textSize, id) = StringHelpers.ComputeSizeAndId(label);
        var screenPos = window.Dc.CursorPos;
        // Try to vertically align buttons that are smaller/have no padding so that text baseline matches (bit hacky, since it shouldn't be a flag)
        if (flags.HasFlag(AlignTextBaseLine) && style.FramePadding.Y < window.Dc.CurrLineTextBaseOffset)
            screenPos.Y += window.Dc.CurrLineTextBaseOffset - style.FramePadding.Y;

        var size        = ImGuiInternal.CalcItemSize(sizeArg, textSize.X + style.FramePadding.X * 2, textSize.Y + style.FramePadding.Y * 2);
        var boundingBox = new ImRect(screenPos, screenPos + size);
        ImGuiInternal.ItemSize(boundingBox, style.FramePadding.Y);
        if (!ImGuiInternal.ItemAdd(boundingBox, id))
            return false;

        // Custom.
        var clicked = ImGuiInternal.ButtonBehavior(boundingBox, id, out var hovered, out var held, flags);
        var color   = ImGui.GetColorU32(held ? ImGuiCol.ButtonActive : hovered ? ImGuiCol.ButtonHovered : ImGuiCol.Button);
        ImGuiInternal.RenderNavHighlight(boundingBox, id, 0);
        ImGui.GetWindowDrawList().AddRectFilled(boundingBox.Min, boundingBox.Max, color, style.FrameRounding, corners);
        ImGuiInternal.RenderTextClipped(ImGui.GetWindowDrawList(), boundingBox.Min + style.FramePadding, boundingBox.Max - style.FramePadding,
            label[..visibleEnd], style.ButtonTextAlign, textSize, boundingBox, true);
        return clicked;
    }
}
