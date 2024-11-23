using ImGuiNET;

namespace OtterGuiInternal.Utility;

public static class SymbolHelpers
{
    /// <summary> Render a simple cross (X) in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the cross. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderCross(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var offset    = (int)size & 1;
        var thickness = Math.Max(size / 5, 1);
        var padding   = new Vector2(thickness / 3f);
        size     -= padding.X * 2 + offset;
        position += padding;
        var otherCorner = position + new Vector2(size);
        drawList.AddLine(position, otherCorner, color, thickness);
        position.X    += size;
        otherCorner.X -= size;
        drawList.AddLine(position, otherCorner, color, thickness);
    }

    /// <summary> Render a simple checkmark in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the checkmark. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderCheckmark(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var thickness = Math.Max(size / 5, 1);
        size -= thickness / 2;
        var padding = new Vector2(thickness / 4);
        position += padding;

        var third = size / 3;
        var bx    = position.X + third;
        var by    = position.Y + size - third / 2;
        drawList.PathLineTo(new Vector2(bx - third,        by - third));
        drawList.PathLineTo(new Vector2(bx,                by));
        drawList.PathLineTo(new Vector2(bx + third * 2.0f, by - third * 2.0f));
        drawList.PathStroke(color, 0, thickness);
    }

    /// <summary> Render a simple dot in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the dot. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderDot(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var padding = size / 7;
        var pos     = position + new Vector2(size / 2);
        size = size / 2 - padding;
        drawList.AddCircleFilled(pos, size, color);
    }

    /// <summary> Render a simple dash in a square. </summary>
    /// <param name="drawList"> The draw list to render in. </param>
    /// <param name="position"> The upper left corner of the square. </param>
    /// <param name="color"> The color of the dash. </param>
    /// <param name="size"> The size of the square. </param>
    public static void RenderDash(ImDrawListPtr drawList, Vector2 position, uint color, float size)
    {
        var offset    = (int)size & 1;
        var thickness = (int)Math.Max(size / 4, 1) | offset;
        var padding   = thickness / 2;
        position.X += padding;
        position.Y += size / 2;
        size       -= padding * 2;

        var otherCorner = position with { X = position.X + size };
        drawList.AddLine(position, otherCorner, color, thickness);
    }
}
