using ImGuiNET;
using OtterGuiInternal;
using OtterGuiInternal.Enums;
using OtterGuiInternal.Structs;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    public static bool Spinner(ReadOnlySpan<byte> label, float radius, int thickness, uint color)
    {
        var window = ImGuiInternal.GetCurrentWindow();
        if (window.SkipItems)
            return false;

        var style       = ImGui.GetStyle();
        var id          = (ImGuiId)GetId(label);
        var pos         = window.Dc.CursorPos;
        var size        = new Vector2(radius * 2, (radius + style.FramePadding.Y) * 2);
        var boundingBox = new ImRect(pos, pos + size);
        ImGuiInternal.ItemSize(boundingBox, style.FramePadding.Y);
        if (!ImGuiInternal.ItemAdd(boundingBox, id))
            return false;

        // Render
        var drawList = ImGui.GetWindowDrawList();
        drawList.PathClear();
        const float numSegments = 30;
        var         time        = (float)ImGui.GetTime();
        var         start       = MathF.Abs(MathF.Sin(time * 1.8f) * (numSegments - 5));

        const float max    = MathF.PI * 2 * (numSegments - 3) / numSegments;
        var         min    = MathF.PI * 2 * start / numSegments;
        var         diff   = max - min;
        var         center = new Vector2(pos.X + radius, pos.Y + radius + style.FramePadding.Y);
        for (var i = 0; i < numSegments; ++i)
        {
            var segment = min + (float)i / numSegments * diff + time * 8;
            drawList.PathLineTo(new Vector2(center.X + MathF.Cos(segment) * radius, center.Y + MathF.Sin(segment) * radius));
        }


        drawList.PathStroke(color, ImDrawFlags.None, thickness);
        return true;
    }
}
