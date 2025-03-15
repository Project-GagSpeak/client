using ImGuiNET;
using OtterGui.Raii;
using OtterGuiInternal.Structs;

namespace GagSpeak.CkCommons.Gui;

public enum CkGuiCircleBound : byte
{
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}


// ImGui Window Draw List Extentions.
public static class CkGuiEx
{
    /// <summary> Extension method that draws a concave of a circle within a given region. </summary>
    /// <param name="drawlistPtr"> the ImGui.GetWindowDrawList() pointer </param>
    /// <param name="topLeftPos"> The position of the defined region the concave filled circle will be drawn in. </param>
    /// <param name="size"> How large the region square will be. </param>
    /// <param name="color"> What color it is drawn in. </param>
    /// <param name="segments"> the resolution of the arc given to the circle. </param>
    /// <remarks> Using multiple flags will deform the shape inproperly. </remarks>
    public static void AddConcaveCircle(this ImDrawListPtr drawlistPtr, Vector2 topLeftPos, float size, uint color, CkGuiCircleBound bindingFlag, int segments = 32)
    {
        ImVec2 topLeft = topLeftPos;
        ImVec2 topRight = topLeftPos + new Vector2(size, 0);
        ImVec2 bottomLeft = topLeftPos + new Vector2(0, size);
        ImVec2 bottomRight = topLeftPos + new Vector2(size);

        // currently for visual display, remove later.
        //drawlistPtr.AddRect(topLeftPos, topLeftPos + new Vector2(size), color);

        // Top Right = Center of Circle, whose arc is the "inner Bound"
        ImVec2 circleCenter = bindingFlag switch
        {
            CkGuiCircleBound.TopLeft => bottomRight,
            CkGuiCircleBound.TopRight => bottomLeft,
            CkGuiCircleBound.BottomLeft => topRight,
            CkGuiCircleBound.BottomRight => topLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(bindingFlag), bindingFlag, null)
        };

        // Start drawing the path
        drawlistPtr.PathClear();
        ImVec2 cornerStartPoint = bindingFlag switch
        {
            CkGuiCircleBound.TopLeft => topLeft,
            CkGuiCircleBound.TopRight => topRight,
            CkGuiCircleBound.BottomLeft => bottomLeft,
            CkGuiCircleBound.BottomRight => bottomRight,
            _ => throw new ArgumentOutOfRangeException(nameof(bindingFlag), bindingFlag, null)
        };
        drawlistPtr.PathLineTo(cornerStartPoint);

        // Arc Start Point
        ImVec2 arcStartPoint = bindingFlag switch
        {
            CkGuiCircleBound.TopLeft => bottomLeft,
            CkGuiCircleBound.TopRight => topLeft,
            CkGuiCircleBound.BottomLeft => bottomRight,
            CkGuiCircleBound.BottomRight => topRight,
            _ => throw new ArgumentOutOfRangeException(nameof(bindingFlag), bindingFlag, null)
        };
        drawlistPtr.PathLineTo(arcStartPoint);

        // Determine the arc's start and end angles
        float startAngle = bindingFlag switch
        {
            CkGuiCircleBound.TopRight => 3 * float.Pi / 2,  // 270° (CW from top)
            CkGuiCircleBound.TopLeft => float.Pi,           // 180° (CW from left)
            CkGuiCircleBound.BottomLeft => float.Pi / 2,    // 90°  (CW from bottom)
            CkGuiCircleBound.BottomRight => 0,              // 0°   (CW from right)
            _ => throw new ArgumentOutOfRangeException(nameof(bindingFlag), bindingFlag, null)
        };
        float endAngle = startAngle + float.Pi / 2; // Quarter-circle (90°)

        // Draw the arc using `PathArcTo`
        drawlistPtr._PathArcToN(circleCenter, size, startAngle, endAngle, segments);

        // Arc Start Point
        ImVec2 arcEndPoint = bindingFlag switch
        {
            CkGuiCircleBound.TopLeft => topRight,
            CkGuiCircleBound.TopRight => bottomRight,
            CkGuiCircleBound.BottomLeft => topLeft,
            CkGuiCircleBound.BottomRight => bottomLeft,
            _ => throw new ArgumentOutOfRangeException(nameof(bindingFlag), bindingFlag, null)
        };
        drawlistPtr.PathLineTo(arcEndPoint);

        // Close path by going back to start point.
        drawlistPtr.PathLineTo(cornerStartPoint);
        drawlistPtr.PathFillConvex(color);
    }
}
