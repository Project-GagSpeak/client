using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static ImRaii.IEndObject Child(string id)
        => new EndUnconditionally(() => ImGui.EndChild(), ImGui.BeginChild(id));

    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static IEndObjectContainer Child(string id, Vector2 size, WFlags flags = WFlags.None)
        => Child(id, size, 0, GetChildRounding(), ImDrawFlags.None, flags);

    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static IEndObjectContainer Child(string id, Vector2 size, uint bgCol, WFlags flags = WFlags.None)
        => Child(id, size, bgCol, GetChildRounding(), ImDrawFlags.None, flags);

    /// <summary> ImRaii.Child alternative with bgCol and rounding support. </summary>
    /// <remarks> The IEndObject returned is a EndObjectContainer, holding the inner content region size. </remarks>
    public static IEndObjectContainer Child(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size, bgCol, rounding, 0, dFlags, wFlags);



    /// <inheritdoc cref="FramedChild(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChild(string id, uint bgCol, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, 0, thickness, dFlags), ImGui.BeginChild(id), ImGui.GetContentRegionAvail());

    /// <inheritdoc cref="FramedChild(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChild(string id, Vector2 size, uint bgCol, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None, WFlags flags = WFlags.None)
        => FramedChild(id, size, bgCol, GetChildRounding(), thickness, dFlags, flags);

    /// <summary> ImRaii.Child alternative with bgCol and rounding support. (Supports frames) </summary>
    /// <remarks> The IEndObject returned is a EndObjectContainer, holding the inner content region size. </remarks>
    public static IEndObjectContainer FramedChild(string id, Vector2 size, uint bgCol, float rounding, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, rounding, thickness, dFlags), ImGui.BeginChild(id, size, false, wFlags), (wFlags & WFlags.AlwaysUseWindowPadding) != 0 ? size.WithoutWinPadding() : size);

    private static void FramedChildEndAction(uint bgCol, float rounding, float frameThickness, ImDrawFlags corners)
    {
        ImGui.EndChild();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        // Draw out the child BG.
        if (bgCol is not 0)
            ImGui.GetWindowDrawList().AddRectFilled(min, max, bgCol, rounding, corners);
        // Draw out the frame.
        if (frameThickness is not 0)
            ImGui.GetWindowDrawList().AddRect(min, max, bgCol, rounding, corners, frameThickness);
    }

    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, ImDrawFlags cFlags = ImDrawFlags.None, ImDrawFlags lFlags = ImDrawFlags.None)
        => LabelHeaderChild(size, label, labelWidth, ImGui.GetFrameHeight(), GetChildRounding(), cFlags, lFlags);

    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, ImDrawFlags cFlags = ImDrawFlags.None, ImDrawFlags lFlags = ImDrawFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, GetChildRounding(), cFlags, lFlags);

    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, ImDrawFlags cFlags = ImDrawFlags.None, ImDrawFlags lFlags = ImDrawFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, HeaderChildColors.Default, cFlags, lFlags);

    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, HeaderChildColors colors, ImDrawFlags cFlags = ImDrawFlags.None, ImDrawFlags lFlags = ImDrawFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, ImGui.GetStyle().WindowPadding.X/2, colors, cFlags, lFlags);

    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, float thickness, ImDrawFlags cFlags, ImDrawFlags lFlags)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, thickness, HeaderChildColors.Default, cFlags, lFlags);

    /// <summary> Creates a child object (no padding) with a nice colored background and label. </summary>
    /// <remarks> The label will not have a hitbox, and you will be able to draw overtop it. This is dont for cases that desire no padding. </remarks>
    public static IEndObjectContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, float thickness,
        HeaderChildColors colors, ImDrawFlags childFlags = ImDrawFlags.None, ImDrawFlags labelFlags = ImDrawFlags.RoundCornersBottomRight)
    {
        var labelH = ImGui.GetTextLineHeightWithSpacing();
        var textSize = ImGui.CalcTextSize(label);
        // Get inner height below header.
        var innerHeight = Math.Min(size.Y, ImGui.GetContentRegionAvail().Y - labelH);
        // Get full childHeight.
        // The pos to know absolute min.
        var pos = ImGui.GetCursorScreenPos();

        // Outer group.
        ImGui.BeginGroup();

        ImGui.SetCursorScreenPos(pos + new Vector2(0, labelH));
        // Draw out the child.
        return new EndObjectContainer(() =>
            {
                ImGui.EndChild();
                ImGui.EndGroup();
                var max = ImGui.GetItemRectMax();
                var wdl = ImGui.GetWindowDrawList();

                // Draw out the child BG.
                wdl.AddRectFilled(pos, max, colors.BodyColor, rounding, childFlags);

                // Now draw out the label header.
                var labelRectSize = new Vector2(labelWidth, labelH);
                wdl.AddRectFilled(pos, pos + labelRectSize + new Vector2(thickness), colors.SplitColor, rounding, labelFlags);
                wdl.AddRectFilled(pos, pos + labelRectSize, colors.HeaderColor, rounding, labelFlags);

                // add the text, centered to the height of the header, left aligned.
                var textStart = new Vector2(labelOffset, (labelH - textSize.Y) / 2);
                wdl.AddText(pos + textStart, ImGui.GetColorU32(ImGuiCol.Text), label);
            }, 
            ImGui.BeginChild(label, size, false, WFlags.AlwaysUseWindowPadding),
            size.WithoutWinPadding()
        );
    }

}
