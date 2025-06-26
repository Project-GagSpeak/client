using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;
using GagSpeak.Gui;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, DFlags dFlag = DFlags.None)
        => LabelChildText(size, hSize, text, ImGui.GetStyle().WindowPadding.X, dFlag);

    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, float offset, DFlags dFlag = DFlags.None)
        => LabelChildText(size, hSize, text, offset,  CkStyle.ChildRounding(), dFlag);

    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, float offset, float rounding, DFlags dFlag = DFlags.None)
        => LabelChildText(size, hSize, text, offset, rounding, ImGui.GetStyle().WindowPadding.X / 2, dFlag);

    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, float offset, float rounding, ColorsLC col, DFlags dFlag = DFlags.None)
        => LabelChildText(size, hSize, text, offset, rounding, ImGui.GetStyle().WindowPadding.X / 2, col, dFlag, WFlags.None);

    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, float offset, float rounding, float fade, DFlags dFlag = DFlags.None)
        => LabelChildText(size, hSize, text, offset, rounding, fade, ColorsLC.Default, dFlag, WFlags.None);

    /// <summary> Constructs a Label Child object with a text based header. </summary>
    /// <param name="size"> The size of the child object. </param>
    /// <param name="hSize"> The size of the header. </param>
    /// <param name="text"> The text to display in the header. </param>
    /// <param name="offset"> The offset of the text from the left side of the header. </param>
    /// <param name="col"> The colors to use for the header. </param>
    /// <param name="rounding"> The rounding to use for the header. </param>
    /// <param name="fade"> How thick the outline around the header is. </param>
    /// <param name="dFlag"> Determines what corners are rounded on the child. </param>
    /// <param name="wFlags"> Any additional flags for the label child object. </param>
    /// <remarks> The IEOLabelContainer contains the size of the label region and the inner region. </remarks> 
    public static IEOLabelContainer LabelChildText(Vector2 size, Vector2 hSize, string text, float offset, float rounding, float fade, ColorsLC col, DFlags dFlag, WFlags wFlags)
    {
        // Calc the size of the label with fade.
        var labelThickness = hSize + new Vector2(fade);
        // from that, determine the dummy size, accounting for winPadding and itemSpacing.
        var dummySize = labelThickness - ImGui.GetStyle().ItemSpacing - ImGui.GetStyle().WindowPadding / 2;

        // Begin the child object.
        var success = ImGui.BeginChild($"##LabelChild-{text}", size, false, wFlags | WFlags.AlwaysUseWindowPadding);

        // Draw the dummy inside the child, so it spans the label size, ensuring we cannot draw in that space.
        ImGui.Dummy(dummySize);

        // Return the end object, closing the draw on the child.
        return new EndObjectLabelContainer(() =>
            {
                ImGui.EndChild();
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                var wdl = ImGui.GetWindowDrawList();

                // Draw out the child BG.
                wdl.AddRectFilled(min, max, col.BG, rounding, dFlag);

                // Draw out the label header.
                var labelThickness = hSize + new Vector2(fade);
                // make sure that if the dFlags include DFlags.RoundCornersTopLeft, to apply that flag.
                var labelDFlags = DFlags.RoundCornersBottomRight | ((dFlag & DFlags.RoundCornersTopLeft) != 0 ? DFlags.RoundCornersTopLeft : DFlags.None);
                wdl.AddRectFilled(min, min + labelThickness, col.Shadow, rounding, labelDFlags);
                wdl.AddRectFilled(min, min + hSize, col.Label, rounding, labelDFlags);
                // add the text, centered to the height of the header, left aligned.
                var textStart = new Vector2(offset, (hSize.Y - ImGui.GetTextLineHeight()) / 2);
                wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);
            },
            success,
            size.WithoutWinPadding(),
            hSize
        );
    }

    /// <inheritdoc cref="LabelChildAction(string, Vector2, Action, float, ColorsLC, Action?, Action?, string, DFlags)"/>"
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None)
        => LabelChildAction(id, size, label,  CkStyle.ChildRounding(), clicked, tt, dis, dFlag);

    /// <inheritdoc cref="LabelChildAction(string, Vector2, Action, float, ColorsLC, Action?, Action?, string, DFlags)"/>"
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, ColorsLC col, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None)
        => LabelChildAction(id, size, label,  CkStyle.ChildRounding(), col, clicked, tt, dis, dFlag);

    /// <inheritdoc cref="LabelChildAction(string, Vector2, Action, float, ColorsLC, Action?, Action?, string, DFlags)"/>"
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, float bend, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None)
        => LabelChildAction(id, size, label, bend, ImGui.GetStyle().WindowPadding.X/2, ColorsLC.Default, clicked, tt, dis, dFlag);

    /// <inheritdoc cref="LabelChildAction(string, Vector2, Action, float, ColorsLC, Action?, Action?, string, DFlags)"/>"
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, float bend, float fade, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None)
        => LabelChildAction(id, size, label, bend, fade, ColorsLC.Default, clicked, tt, dis, dFlag);

    /// <inheritdoc cref="LabelChildAction(string, Vector2, Action, float, ColorsLC, Action?, Action?, string, DFlags)"/>"
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, float rounding, ColorsLC col, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None)
        => LabelChildAction(id, size, label, rounding, ImGui.GetStyle().WindowPadding.X/2, col, clicked, tt, dis, dFlag);

    /// <summary> Interactable label header within a padded child. </summary>
    /// <remarks> Note that the dummy covering the header is part of the child. If you intend to make this scrollable, make another child inside. </remarks>
    public static IEOLabelContainer LabelChildAction(string id, Vector2 size, Action label, float bend, float fade, ColorsLC col, Action<ImGuiMouseButton> clicked, string tt = "", bool dis = false, DFlags dFlag = DFlags.None, WFlags wFlags = WFlags.None)
    {
        var tooltip = tt.IsNullOrWhitespace() ? "Double Click to Interact--SEP--Right-Click to Cancel" : tt;

        var wdl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        ImGui.BeginGroup();
        // Begin the child object.
        var success = ImGui.BeginChild($"##LabelChildActionOuter-{id}", size);

        // Handle drawing the label.
        using (ImRaii.Group()) 
            label.Invoke();
        var labelMin = ImGui.GetItemRectMin();
        var labelMax = ImGui.GetItemRectMax();
        var hovered = ImGui.IsMouseHoveringRect(labelMin, labelMax);

        // Handle Interaction.
        if (hovered)
        {
            CkGui.AttachToolTipRect(labelMin, labelMax, tooltip);
            if(ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))   clicked?.Invoke(ImGuiMouseButton.Left);
            if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Right)) clicked?.Invoke(ImGuiMouseButton.Right);
        }

        // Draw the padded Child (The inner contents we actually draw in).
        ImGui.SetCursorScreenPos(pos);
        success &= ImGui.BeginChild($"##LabelChildAction-{id}", size, false, wFlags | WFlags.AlwaysUseWindowPadding);

        // Draw the dummy inside the child, so it spans the label size, ensuring we cannot draw in that space.
        var labelThickness = (labelMax - labelMin) + new Vector2(fade);
        ImGui.Dummy(labelThickness - ImGui.GetStyle().ItemSpacing - ImGui.GetStyle().WindowPadding / 2);

        // Return the end object, closing the draw on the child.
        return new EndObjectLabelContainer(() =>
        {
            // End inner child.
            ImGui.EndChild();
            wdl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), col.BG, bend, dFlag);
            
            // make sure that if the dFlags include DFlags.RoundCornersTopLeft, to apply that flag.
            var labelDFlags = DFlags.RoundCornersBottomRight | ((dFlag & DFlags.RoundCornersTopLeft) != 0 ? DFlags.RoundCornersTopLeft : DFlags.None);
            wdl.AddRectFilled(labelMin, labelMin + labelThickness, col.Shadow, bend, labelDFlags);
            var labelCol = dis ? col.Label : hovered ? col.LabelHovered : col.Label;
            wdl.AddRectFilled(labelMin, labelMax, labelCol, bend, labelDFlags);

            // end outer child.
            ImGui.EndChild();
            ImGui.EndGroup();
        },
            success,
            size.WithoutWinPadding(),
            (labelMax - labelMin)
        );
    }


    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, DFlags cFlags = DFlags.None, DFlags lFlags = DFlags.None)
        => LabelHeaderChild(size, label, labelWidth, ImGui.GetFrameHeight(),  CkStyle.ChildRounding(), cFlags, lFlags);

    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, DFlags cFlags = DFlags.None, DFlags lFlags = DFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset,  CkStyle.ChildRounding(), cFlags, lFlags);

    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, DFlags cFlags = DFlags.None, DFlags lFlags = DFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, HeaderChildColors.Default, cFlags, lFlags);

    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, HeaderChildColors colors, DFlags cFlags = DFlags.None, DFlags lFlags = DFlags.None)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, ImGui.GetStyle().WindowPadding.X/2, colors, cFlags, lFlags);

    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, float thickness, DFlags cFlags, DFlags lFlags)
        => LabelHeaderChild(size, label, labelWidth, labelOffset, rounding, thickness, HeaderChildColors.Default, cFlags, lFlags);

    /// <summary> Creates a child object (no padding) with a nice colored background and label. </summary>
    /// <remarks> The label will not have a hitbox, and you will be able to draw overtop it. This is dont for cases that desire no padding. </remarks>
    public static IEOContainer LabelHeaderChild(Vector2 size, string label, float labelWidth, float labelOffset, float rounding, float thickness,
        HeaderChildColors colors, DFlags childFlags = DFlags.None, DFlags labelFlags = DFlags.RoundCornersBottomRight)
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
