using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;

public static partial class CkRaii
{
    /// <inheritdoc cref="DragDropHeader(string, float, HeaderChildColors, float, WFlags)"/>"
    public static ImRaii.IEndObject DragDropHeader(string text, float height, WFlags flags = WFlags.None)
        => DragDropHeader(text, height, HeaderChildColors.Default, GetHeaderRounding(), WFlags.None);

    /// <inheritdoc cref="DragDropHeader(string, float, HeaderChildColors, float, WFlags)"/>"
    public static ImRaii.IEndObject DragDropHeader(string text, float height, HeaderChildColors colors, WFlags flags = WFlags.None)
        => DragDropHeader(text, height, colors, GetHeaderRounding(), flags);


    /// <inheritdoc cref="DragDropHeader(string, float, HeaderChildColors, float, WFlags)"/>"
    public static ImRaii.IEndObject DragDropHeader(string text, float height, float rounding, WFlags flags = WFlags.None)
        => DragDropHeader(text, height, HeaderChildColors.Default, rounding, WFlags.None);

    /// <summary> A Defined header to the left, of a content body to the right. </summary>
    /// <remarks> The left body is drag-droppable. </remarks>
    public static ImRaii.IEndObject DragDropHeader(string text, float height, HeaderChildColors colors, float rounding, WFlags flags)
    {
        // Begin the group combining the two elements.
        ImGui.BeginGroup();
        var leftWidth = ImGui.CalcTextSize(text).X + ImGui.GetStyle().FramePadding.X * 2;

        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var max = min + new Vector2(leftWidth, height);
        var linePos = min + new Vector2(leftWidth, 0);

        // Draw the child background with the element header color.
        wdl.AddRectFilled(min, max, colors.HeaderColor, rounding, ImDrawFlags.RoundCornersLeft);
        // Draw the divider line down the middle.
        wdl.AddLine(linePos, linePos with { Y = max.Y }, colors.SplitColor, 2);
        var textStart = new Vector2(ImGui.GetStyle().FramePadding.X, (height - ImGui.GetTextLineHeight()) / 2);
        // add the text in the header box.
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);

        // Add the shift
        ImGui.SetCursorScreenPos(min + new Vector2(leftWidth, 0));
        return new EndUnconditionally(() => DDHCEndAction(colors.BodyColor, rounding), ImGui.BeginChild("DDHC_" + text, new Vector2(ImGui.GetContentRegionAvail().X, height), false, flags));

    }

    // Drag-Drop Header Child End Action.
    private static void DDHCEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.RoundCornersRight);
        ImGui.EndGroup();
    }
}
