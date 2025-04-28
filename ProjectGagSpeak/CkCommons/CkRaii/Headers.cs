using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.CkCommons.Gui;
using GagSpeak.Services;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, HeaderChildColors.Default, GetHeaderRounding(), WFlags.AlwaysUseWindowPadding, alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, WFlags flags, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, HeaderChildColors.Default, GetHeaderRounding(), flags.WithPadding(), alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, HeaderChildColors colors, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, colors, GetHeaderRounding(), WFlags.AlwaysUseWindowPadding, alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, HeaderChildColors colors, WFlags flags, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, colors, GetHeaderRounding(), flags.WithPadding(), alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, float rounding, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, HeaderChildColors.Default, rounding, WFlags.AlwaysUseWindowPadding, alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, float rounding, WFlags flags, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, HeaderChildColors.Default, rounding, flags.WithPadding(), alignment);


    /// <inheritdoc cref="HeaderChild(string, Vector2, HeaderChildColors, float, WFlags, HeaderAlign)"/>"
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, HeaderChildColors colors, float rounding, HeaderAlign alignment = HeaderAlign.AlignCenter)
        => HeaderChild(text, size, colors, rounding, WFlags.AlwaysUseWindowPadding, alignment);

    /// <summary> Creates a Head with the labeled text, and a child beneath it. </summary>
    /// <remarks> You can provide the size of the child after padding is applied. </remarks>
    public static ImRaii.IEndObject HeaderChild(string text, Vector2 size, HeaderChildColors colors, float rounding, WFlags flags, HeaderAlign alignment)
    {
        ImGui.BeginGroup();

        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var widthSpan = size.WithPadding().X;
        var max = min + new Vector2(widthSpan, GetHeaderHeight());
        var linePos = min + new Vector2(0, max.Y - 2);

        // Draw the header.
        wdl.AddRectFilled(min, max, colors.HeaderColor, rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, colors.SplitColor, 2);
        var textStart = HeaderTextOffset(widthSpan, GetHeaderHeight(), text, alignment);
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);

        // Draw the child.
        ImGui.SetCursorScreenPos(min + new Vector2(0, GetHeaderHeight()));
        return new EndUnconditionally(
            () => HeaderChildEndAction(colors.BodyColor, rounding),
            ImGui.BeginChild("CHC_" + text, size.WithPadding(), false, flags.WithPadding())
        );
    }


    /// <inheritdoc cref="ButtonHeaderChild(string, Vector2, Action, HeaderChildColors, float, HeaderAlign, string)"/>
    public static ImRaii.IEndObject ButtonHeaderChild(string text, Vector2 size, Action act, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
        => ButtonHeaderChild(text, size, act, HeaderChildColors.Default, GetHeaderRounding(), align, tt);


    /// <inheritdoc cref="ButtonHeaderChild(string, Vector2, Action, HeaderChildColors, float, HeaderAlign, string)"/>
    public static ImRaii.IEndObject ButtonHeaderChild(string text, Vector2 size, Action act, float rounding, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
        => ButtonHeaderChild(text, size, act, HeaderChildColors.Default, rounding, align, tt);

    /// <inheritdoc cref="ButtonHeaderChild(string, Vector2, Action, HeaderChildColors, float, HeaderAlign, string)"/>
    public static ImRaii.IEndObject ButtonHeaderChild(string text, Vector2 size, Action act, HeaderChildColors colors, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
        => ButtonHeaderChild(text, size, act, colors, GetHeaderRounding(), align, tt);

    /// <summary> Interactable Button Header that has a child body. </summary>
    /// <remarks> WindowPadding is always applied. Size passed in should be the size of the inner child space after padding. </remarks>
    public static ImRaii.IEndObject ButtonHeaderChild(string text, Vector2 size, Action act, HeaderChildColors colors, float rounding, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var widthSpan = size.WithPadding().X;
        var max = min + new Vector2(widthSpan, GetHeaderHeight());
        var linePos = min + new Vector2(0, max.Y - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);

        // Text & Icon Alignment
        var textWidth = ImGui.CalcTextSize(text).X;
        var centerStartX = min.X + (widthSpan - textWidth) / 2;
        var hoverRectStart = min with { X = centerStartX };
        var hoverSize = new Vector2((widthSpan - textWidth) / 2, GetHeaderHeight());

        // Text & Icon Drawing.
        var isHovered = ImGui.IsMouseHoveringRect(hoverRectStart, hoverRectStart + hoverSize);
        var col = isHovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGui.GetColorU32(ImGuiCol.Text);
        ImGui.GetWindowDrawList().AddText(hoverRectStart, col, text);

        // Action Handling.
        if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            act();

        // tooltip handling.
        if (isHovered && !string.IsNullOrEmpty(tt))
            CkGui.AttachToolTip(tt);

        return new EndUnconditionally(() => HeaderChildEndAction(colors.BodyColor, rounding), ImGui.BeginChild("CHC_" + text, size.WithPadding(), false, WFlags.AlwaysUseWindowPadding));
    }

    public static ImRaii.IEndObject IconButtonHeaderChild(string text, FAI icon, Vector2 size, Action act, HeaderAlign alignment = HeaderAlign.AlignCenter, string tt = "")
        => IconButtonHeaderChild(text, icon, size, act, HeaderChildColors.Default, GetHeaderRounding(), alignment, tt);

    public static ImRaii.IEndObject IconButtonHeaderChild(string text, FAI icon, Vector2 size, Action act, float rounding, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
        => IconButtonHeaderChild(text, icon, size, act, HeaderChildColors.Default, rounding, align, tt);

    public static ImRaii.IEndObject IconButtonHeaderChild(string text, FAI icon, Vector2 size, Action act, HeaderChildColors colors, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
        => IconButtonHeaderChild(text, icon, size, act, colors, GetHeaderRounding(), align, tt);

    /// <summary> Interactable Button Header that has a child body. </summary>
    /// <remarks> WindowPadding is always applied. Size passed in should be the size of the inner child space after padding. </remarks>
    public static ImRaii.IEndObject IconButtonHeaderChild(string text, FAI icon, Vector2 size, Action act, HeaderChildColors colors, float rounding, HeaderAlign align = HeaderAlign.AlignCenter, string tt = "")
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = ImGui.GetCursorScreenPos();
        var widthSpan = size.WithPadding().X;
        var max = min + new Vector2(widthSpan, GetHeaderHeight());
        var linePos = min + new Vector2(0, max.Y - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);

        // Text & Icon Alignment
        var textWidth = ImGui.CalcTextSize(text).X;
        var textIconWidth = textWidth + ImGui.GetStyle().ItemInnerSpacing.X + CkGui.IconSize(icon).X;
        var centerStartX = min.X + (widthSpan - textIconWidth) / 2;
        var hoverRectStart = min + new Vector2((widthSpan - textIconWidth) / 2, (ImGui.GetFrameHeight() - ImGui.GetTextLineHeight()) / 2);
        var hoverSize = new Vector2(textIconWidth, GetHeaderHeight());

        // Text & Icon Drawing.
        var isHovered = ImGui.IsMouseHoveringRect(hoverRectStart, hoverRectStart + hoverSize);
        var col = isHovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGui.GetColorU32(ImGuiCol.Text);
        ImGui.GetWindowDrawList().AddText(hoverRectStart, col, text);

        var centerPos = hoverRectStart + new Vector2(textWidth + ImGui.GetStyle().ItemInnerSpacing.X, 0);
        using (UiFontService.IconFont.Push()) ImGui.GetWindowDrawList().AddText(centerPos, col, icon.ToIconString());

        // Action Handling.
        if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            act();

        // tooltip handling.
        if (isHovered && !string.IsNullOrEmpty(tt))
            CkGui.AttachToolTip(tt);

        return new EndUnconditionally(() => HeaderChildEndAction(colors.BodyColor, rounding), ImGui.BeginChild("CHC_" + text, size.WithPadding(), false, WFlags.AlwaysUseWindowPadding));
    }

    /// <summary> Places a header with text centered in the middle. </summary>
    public static void CenteredHeader(Vector2 startPos, string text, float widthSpan, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = startPos;
        var max = startPos + new Vector2(widthSpan, GetHeaderHeight());
        var linePos = min + new Vector2(0, max.Y - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);
        var textStart = new Vector2((widthSpan - ImGui.CalcTextSize(text).X) / 2, 0);
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);
    }

    public static void CenteredHeaderButton(Vector2 startPos, string text, float widthSpan, float rounding, FAI icon, string tt, Action onClick)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = startPos;
        var max = startPos + new Vector2(widthSpan, GetHeaderHeight());
        var linePos = min + new Vector2(0, max.Y - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);

        // Text & Icon Alignment
        var textWidth = ImGui.CalcTextSize(text).X;
        var iconSize = CkGui.IconSize(icon);
        var textIconWidth = textWidth + ImGui.GetStyle().ItemInnerSpacing.X + iconSize.X;
        var centerStartX = min.X + (widthSpan - textIconWidth) / 2;
        var hoverRectStart = min with { X = centerStartX };
        var hoverSize = new Vector2(textIconWidth, GetHeaderHeight());

        // Text & Icon Drawing.
        var isHovered = ImGui.IsMouseHoveringRect(hoverRectStart, hoverRectStart + new Vector2(textIconWidth, ImGui.GetFrameHeight()));
        var col = isHovered ? ImGui.GetColorU32(ImGuiCol.ButtonHovered) : ImGui.GetColorU32(ImGuiCol.Text);
        ImGui.GetWindowDrawList().AddText(hoverRectStart, col, text);

        using var font = UiFontService.IconFont.Push();
        var centerPos = hoverRectStart + new Vector2(textWidth + ImGui.GetStyle().ItemInnerSpacing.X, 0);
        ImGui.GetWindowDrawList().AddText(centerPos, col, icon.ToIconString());

        // Action Handling.
        if (isHovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            onClick();

        // tooltip handling.
        if (isHovered && !string.IsNullOrEmpty(tt))
            CkGui.AttachToolTip(tt);
    }

    private static void HeaderChildEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.RoundCornersBottom);
        ImGui.EndGroup();
    }

    private struct UnconditionalCenterHeader : ImRaii.IEndObject
    {
        private Action EndAction { get; }
        public bool Success { get; }
        public bool Disposed { get; private set; }

        public UnconditionalCenterHeader(string id, string txt, Vector2 size, float bend, WFlags flags)
        {
            ImGui.BeginGroup();

            var pos = ImGui.GetCursorScreenPos();
            CenteredHeader(pos, txt, size.X, bend);
            ImGui.SetCursorScreenPos(pos + new Vector2(0, GetHeaderHeight()));
            // get the height of the inner-size based on the flags attributes.
            var height = (flags & WFlags.AlwaysUseWindowPadding) != 0
                ? size.Y + ImGui.GetStyle().WindowPadding.Y * 2 : size.Y;

            var innerSize = new Vector2(size.X, Math.Min(height, ImGui.GetContentRegionAvail().Y));
            this.Success = ImGui.BeginChild(id, innerSize, false, flags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), GetChildRounding(), ImDrawFlags.RoundCornersBottom);
                ImGui.EndGroup();
            };
        }

        public UnconditionalCenterHeader(string id, string txt, Vector2 size, float bend, FAI icon, Action onClick, string tooltip, WFlags flags)
        {
            ImGui.BeginGroup();

            var pos = ImGui.GetCursorScreenPos();
            CenteredHeaderButton(pos, txt, size.X, bend, icon, tooltip, onClick);
            
            ImGui.SetCursorScreenPos(pos + new Vector2(0, GetHeaderHeight()));
            // get the height of the inner-size based on the flags attributes.
            var height = (flags & WFlags.AlwaysUseWindowPadding) != 0
                ? size.Y + ImGui.GetStyle().WindowPadding.Y * 2 : size.Y;

            var innerSize = new Vector2(size.X, Math.Min(height, ImGui.GetContentRegionAvail().Y));
            this.Success = ImGui.BeginChild(id, innerSize, false, flags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), GetChildRounding(), ImDrawFlags.RoundCornersBottom);
                ImGui.EndGroup();
            };
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;

            this.EndAction();
            this.Disposed = true;
        }
    }
}
