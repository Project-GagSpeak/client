using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.CkCommons.Gui;
using ImGuiNET;
using OtterGui.Text;
using System.Runtime.InteropServices;

namespace GagSpeak.CkCommons.Widgets;

/// <summary> Helper for all functions related to drawing the header section of respective UI's </summary>
/// <remarks> Contains functions for icon row display, filters, and more. </remarks>
public class CkHeader
{
    /// <summary> Stores the position of a draw region, and its size. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct DrawRegion(float PosX, float PosY, float SizeX, float SizeY)
    {
        public Vector2 Pos  => new(PosX, PosY);
        public Vector2 Size => new(SizeX, SizeY);
        public Vector2 Max  => Pos + Size;
        public DrawRegion(Vector2 pos, Vector2 size)
            : this(pos.X, pos.Y, size.X, size.Y)
        { }
    }

    /// <summary> A struct to contain the upper and lower PosSize regions for a CkHeader drawn window. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct DrawRegions(DrawRegion Top, DrawRegion Bottom);

    /// <summary> A struct to contain the 4 corner PosSize regions for a CkHeader drawn window. </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct QuadDrawRegions(DrawRegion TopLeft, DrawRegion TopRight, DrawRegion BotLeft, DrawRegion BotRight);

    /// <summary> Helper function that draws a flat header title past the window padding to the window edge. </summary>
    public static QuadDrawRegions Flat(uint color, Vector2 innerSize, float leftWidth, float splitWidth)
    {
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXOffset = Math.Abs(winClipX - winPadding.X);
        var paddedSize = innerSize + winPadding * 2;

        var expandedMin = minPos - new Vector2(winClipX, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winClipX, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        // Draw the base header, and the left region positions.
        wdl.AddRectFilled(expandedMin, expandedMin + paddedSize, color, 0, ImDrawFlags.None);
        var topLeftPos = minPos + new Vector2(outerXOffset, winPadding.Y);
        var botLeftPos = topLeftPos + new Vector2(0, paddedSize.Y);
        var botRegionH = maxPos.Y - botLeftPos.Y - winPadding.Y;

        // define the midpoint positions, and also our right positions after we know the divider.
        var splitPos = botLeftPos + new Vector2(leftWidth + winPadding.X, 0);
        var topRightPos = new Vector2(splitPos.X + splitWidth + winPadding.X, topLeftPos.Y);
        var botRightPos = topRightPos with { Y = botLeftPos.Y };

        wdl.AddRectFilled(splitPos, new Vector2(splitPos.X + splitWidth, maxPos.Y - winPadding.Y), CkColor.FancyHeader.Uint());
        wdl.PopClipRect();

        // we need to return the content region struct, so create our end result content regions below.
        var topLeft = new DrawRegion(topLeftPos, new Vector2(leftWidth, innerSize.Y));
        var botLeft = new DrawRegion(botLeftPos, new Vector2(leftWidth, botRegionH));
        var topRight = new DrawRegion(topRightPos, new Vector2(maxPos.X - outerXOffset - topRightPos.X, innerSize.Y));
        var botRight = new DrawRegion(botRightPos, new Vector2(maxPos.X - outerXOffset - botRightPos.X, botRegionH));
        return new(topLeft, topRight, botLeft, botRight);
    }

    /// <summary> Draws a flat-header beyond window padding with inverted rounded curves at the bottom. </summary>
    /// <remarks> This will ALWAYS span the width of the content region entirely. </remarks>
    public static DrawRegions FlatWithBends(uint color, float height, float curveRadius)
    {
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXOffset = Math.Abs(winClipX - winPadding.X);
        
        var expandedMin = minPos - new Vector2(winClipX, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winClipX, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        // Get necessary positions.
        var paddedHeight = height + winPadding.Y * 2;
        var midpoint = (maxPos.X - minPos.X) / 2;
        var topLeftContentPos = minPos + new Vector2(outerXOffset, winPadding.Y);
        var botLeftContentPos = topLeftContentPos + new Vector2(0, paddedHeight);
        var topRightPos = expandedMax with { Y = expandedMin.Y };
        var circleLeftCenter = expandedMin + new Vector2(curveRadius, paddedHeight + curveRadius);
        var circleRightCenter = topRightPos + new Vector2(-curveRadius, paddedHeight + curveRadius);

        // Draw the left convex shape.
        wdl.PathClear();
        wdl.PathLineTo(expandedMin);
        wdl.PathArcTo(circleLeftCenter, curveRadius, float.Pi, 3 * float.Pi / 2);
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, paddedHeight));
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, 0));
        wdl.PathFillConvex(color);

        // Draw the right convex shape.
        wdl.PathClear();
        wdl.PathLineTo(topRightPos);
        wdl.PathArcTo(circleRightCenter, curveRadius, 2 * float.Pi, 3 * float.Pi / 2);
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, paddedHeight));
        wdl.PathLineTo(expandedMin + new Vector2(midpoint, 0));
        wdl.PathFillConvex(color);

        wdl.PopClipRect();

        // prepare exports.
        var topContent = new DrawRegion(topLeftContentPos, new Vector2(maxPos.X - minPos.X, height));
        var botContent = new DrawRegion(botLeftContentPos, (maxPos - new Vector2(outerXOffset, winPadding.Y)) - botLeftContentPos);
        return new DrawRegions(topContent, botContent);
    }


    /// <summary> A helper function that draws out the fancy curved header (not to be used for restraint sets) </summary>
    public static QuadDrawRegions FancyCurve(uint col, float searchHeight, float splitWidth, float iconBarWidth, bool showSplit = true)
    {
        // Grab the window padding that is currently set.
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var outerXOffset = Math.Abs(winClipX - winPadding.X);

        var leftSizeInner = new Vector2((maxPos.X - minPos.X) - iconBarWidth - splitWidth, searchHeight);

        var paddedLeftSize = leftSizeInner + winPadding * 2;
        var curveRadius = splitWidth / 2;
        var clippedOffset = new Vector2(outerXOffset, winPadding.Y);

        var expandedMin = minPos - new Vector2(winPadding.X / 2, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winPadding.X / 2, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        wdl.PathClear();
        // top right
        wdl.PathLineTo(expandedMax with { Y = expandedMin.Y });
        // top left.
        wdl.PathLineTo(expandedMin);
        // bottom left.
        var pointTwoPos = expandedMin + new Vector2(0, paddedLeftSize.Y);
        wdl.PathLineTo(pointTwoPos);

        var topLeftContentPos = minPos + clippedOffset;
        var botLeftContentPos = topLeftContentPos + new Vector2(0, paddedLeftSize.Y);
        var botContentH = maxPos.Y - winPadding.Y - botLeftContentPos.Y;

        //var pointThreePos = pointTwoPos + new Vector2(leftSize.X - splitWidth / 2, 0);
        var circleOneCenter = expandedMin + paddedLeftSize + new Vector2(-curveRadius, curveRadius);
        var circleTwoCenter = circleOneCenter + new Vector2(splitWidth, 0);

        // define the midpoint positions, and also our right positions after we know the divider.
        var splitPos = botLeftContentPos + new Vector2(leftSizeInner.X + winPadding.X, 0);
        var topRightContentPos = new Vector2(splitPos.X + curveRadius + winPadding.X, topLeftContentPos.Y);
        var botRightContentPos = topRightContentPos + new Vector2(0, leftSizeInner.Y + splitWidth + winPadding.Y + curveRadius);

        // left center curve.
        wdl.PathArcTo(circleOneCenter, curveRadius, -float.Pi / 2, 0, 16);
        wdl.PathArcTo(circleTwoCenter, curveRadius, float.Pi, float.Pi / 2, 16);

        // bottom right curve.
        var circleThreeCenter = new Vector2(expandedMax.X - splitWidth, pointTwoPos.Y + splitWidth*2);
        wdl.PathArcTo(circleThreeCenter, splitWidth, -float.Pi / 2, 0);
        wdl.PathLineTo(expandedMax with { Y = expandedMin.Y });
        wdl.PathFillConvex(col);

        // if we are not editing, draw the splitter.
        if (showSplit)
        {
            // clear the path.
            wdl.PathClear();
            var circleFourCenter = circleTwoCenter + new Vector2(0, curveRadius);
            var originPoint = new Vector2(circleOneCenter.X + curveRadius, expandedMax.Y - winPadding.Y);
            // bottom left
            wdl.PathLineTo(originPoint);
            wdl.PathArcTo(circleFourCenter, curveRadius, float.Pi, float.Pi / 2);
            // bottom right
            wdl.PathLineTo(originPoint + new Vector2(curveRadius, 0));
            wdl.PathFillConvex(col);
        }

        wdl.PopClipRect();

        // we need to return the content region struct, so create our end result content regions below.
        var topLeft = new DrawRegion(topLeftContentPos, leftSizeInner);
        var botLeft = new DrawRegion(botLeftContentPos, new Vector2(leftSizeInner.X, botContentH));

        // For when in editing mode to make things appear more aligned.
        var rightShift = showSplit ? Vector2.Zero : new Vector2(splitWidth / 2, 0);

        var topRightSize = new Vector2(maxPos.X - outerXOffset - topRightContentPos.X, leftSizeInner.Y + splitWidth);
        var topRight = new DrawRegion(topRightContentPos - rightShift, topRightSize + rightShift);

        var botRightSize = maxPos - clippedOffset - botRightContentPos;
        var botRight = new DrawRegion(botRightContentPos - rightShift, botRightSize + rightShift);

        return new(topLeft, topRight, botLeft, botRight);
    }

    /// <summary> A helper function that draws out the fancy curved header (not to be used for restraint sets) </summary>
    public static QuadDrawRegions FancyCurveFlipped(uint col, float iconBarWidth, float splitWidth, float searchHeight, bool showSplit = true)
    {
        // Grab the window padding that is currently set.
        var wdl = ImGui.GetWindowDrawList();
        var winPadding = ImGui.GetStyle().WindowPadding;
        var winClipX = ImGui.GetWindowContentRegionMin().X / 2;
        var minPos = wdl.GetClipRectMin();
        var maxPos = wdl.GetClipRectMax();
        var size = maxPos - minPos;
        var outerXOffset = Math.Abs(winClipX - winPadding.X);

        var iconBarSizeInner = new Vector2(iconBarWidth, searchHeight + splitWidth);
        var searchSizeInner = new Vector2((maxPos.X - minPos.X) - iconBarWidth - splitWidth, searchHeight);
        var paddedSearchSize = searchSizeInner + winPadding * 2;

        var curveRadius = splitWidth / 2;
        var clippedOffset = new Vector2(outerXOffset, winPadding.Y);

        var expandedMin = minPos - new Vector2(winPadding.X / 2, 0); // Extend the min boundary to include the padding
        var expandedMax = maxPos + new Vector2(winPadding.X / 2, 0); // Extend the max boundary to include the padding
        wdl.PushClipRect(expandedMin, expandedMax, false);

        var topRightContentPos = minPos + new Vector2(size.X -clippedOffset.X - searchSizeInner.X, clippedOffset.Y);
        var botRightContentPos = topRightContentPos + new Vector2(0, paddedSearchSize.Y);
        var topLeftContentPos = minPos + clippedOffset;
        var botLeftContentPos = topLeftContentPos + new Vector2(0, searchSizeInner.Y + splitWidth + winPadding.Y + curveRadius);

        var drawRegionTR = new DrawRegion(topRightContentPos, searchSizeInner);
        var drawRegionBR = new DrawRegion(botRightContentPos, maxPos - clippedOffset - botRightContentPos);
        var drawRegionTL = new DrawRegion(topLeftContentPos, iconBarSizeInner);
        var drawRegionBL = new DrawRegion(botLeftContentPos, new Vector2(iconBarSizeInner.X, maxPos.Y - clippedOffset.Y - botLeftContentPos.Y));

        wdl.PathClear();

        // top left
        wdl.PathLineTo(expandedMin);

        // top right.
        var topRightPos = expandedMax with { Y = expandedMin.Y };
        wdl.PathLineTo(topRightPos);

        // bot right.
        var rightSideMaxPos = topRightPos + new Vector2(0, paddedSearchSize.Y);
        wdl.PathLineTo(rightSideMaxPos);

        // the center curves.
        var circleOneCenter = rightSideMaxPos + new Vector2(-(paddedSearchSize.X - curveRadius), curveRadius);
        var circleTwoCenter = circleOneCenter - new Vector2(splitWidth, 0);
        wdl.PathArcTo(circleOneCenter, curveRadius, 3 * float.Pi / 2, float.Pi, 16);
        wdl.PathArcTo(circleTwoCenter, curveRadius, 0, float.Pi / 2, 16);


        var circleThreeCenter = expandedMin + new Vector2(splitWidth, paddedSearchSize.Y + splitWidth * 2);
        wdl.PathArcTo(circleThreeCenter, splitWidth, 3 * float.Pi / 2, float.Pi);
        wdl.PathLineTo(expandedMin);
        wdl.PathFillConvex(col);

        // if we are not editing, draw the splitter.
        if (showSplit)
        {
            // clear the path.
            wdl.PathClear();
            var circleFourCenter = circleTwoCenter + new Vector2(0, curveRadius);
            var originPoint = new Vector2(circleFourCenter.X + curveRadius, expandedMax.Y - winPadding.Y);
            // bottom left
            wdl.PathLineTo(originPoint);
            wdl.PathArcTo(circleFourCenter, curveRadius, 0, float.Pi / 2, 16);
            // bottom right
            wdl.PathLineTo(originPoint - new Vector2(curveRadius, 0));
            wdl.PathFillConvex(col);
        }

        wdl.PopClipRect();

        // we need to return the content region struct, so create our end result content regions below.
        return new(drawRegionTL, drawRegionTR, drawRegionBL, drawRegionBR);
    }
}
