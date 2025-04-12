using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CkCommons.Helpers;

public static partial class CkComponents
{
    public static float DefaultHeaderRounding => ImGui.GetStyle().FrameRounding * 2f;
    public static float HeaderHeight => ImGui.GetFrameHeight();

    public static ImRaii.IEndObject CenterHeaderChild(string id, string text, Vector2 size)
        => new UnconditionalCenterHeader(id, text, size, DefaultHeaderRounding, WFlags.None);

    public static ImRaii.IEndObject CenterHeaderChild(string id, string text, Vector2 size, WFlags flags)
        => new UnconditionalCenterHeader(id, text, size, DefaultHeaderRounding, flags);

    public static ImRaii.IEndObject CenterHeaderChild(string id, string text, Vector2 size, float bend)
    => new UnconditionalCenterHeader(id, text, size, bend, WFlags.None);

    public static ImRaii.IEndObject CenterHeaderChild(string id, string text, Vector2 size, float bend, WFlags flags)
        => new UnconditionalCenterHeader(id, text, size, bend, flags);

    public static ImRaii.IEndObject ButtonHeaderChild(string id, string text, Vector2 size, float bend, FAI icon, Action onClick)
        => new UnconditionalCenterHeader(id, text, size, bend, icon, onClick, string.Empty, WFlags.None);

    public static ImRaii.IEndObject ButtonHeaderChild(string id, string text, Vector2 size, float bend, WFlags flags, FAI icon, Action onClick)
        => new UnconditionalCenterHeader(id, text, size, bend, icon, onClick, string.Empty, flags);

    public static ImRaii.IEndObject ButtonHeaderChild(string id, string text, Vector2 size, float bend, FAI icon, string tt, Action onClick)
    => new UnconditionalCenterHeader(id, text, size, bend, icon, onClick, tt, WFlags.None);

    public static ImRaii.IEndObject ButtonHeaderChild(string id, string text, Vector2 size, float bend, WFlags flags, FAI icon, string tt, Action onClick)
        => new UnconditionalCenterHeader(id, text, size, bend, icon, onClick, tt, flags);

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
            ImGui.SetCursorScreenPos(pos + new Vector2(0, HeaderHeight));
            // get the height of the inner-size based on the flags attributes.
            var height = (flags & WFlags.AlwaysUseWindowPadding) != 0
                ? size.Y + ImGui.GetStyle().WindowPadding.Y * 2 : size.Y;

            var innerSize = new Vector2(size.X, Math.Min(height, ImGui.GetContentRegionAvail().Y));
            this.Success = ImGui.BeginChild(id, innerSize, false, flags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                FillChildBg(bend); // Draw background AFTER the child ends
                ImGui.EndGroup();
            };
        }

        public UnconditionalCenterHeader(string id, string txt, Vector2 size, float bend, FAI icon, Action onClick, string tooltip, WFlags flags)
        {
            ImGui.BeginGroup();

            var pos = ImGui.GetCursorScreenPos();
            CenteredHeaderButton(pos, txt, size.X, bend, icon, tooltip, onClick);
            
            ImGui.SetCursorScreenPos(pos + new Vector2(0, HeaderHeight));
            // get the height of the inner-size based on the flags attributes.
            var height = (flags & WFlags.AlwaysUseWindowPadding) != 0
                ? size.Y + ImGui.GetStyle().WindowPadding.Y * 2 : size.Y;

            var innerSize = new Vector2(size.X, Math.Min(height, ImGui.GetContentRegionAvail().Y));
            this.Success = ImGui.BeginChild(id, innerSize, false, flags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                FillChildBg(bend); // Draw background AFTER the child ends
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

    public static void FillChildBg(float rounding)
        => ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), rounding, ImDrawFlags.RoundCornersBottom);

    /// <summary> Places a header with text centered in the middle. </summary>
    /// <remarks> This will always draw above the last created item. Can be a child or group. </remarks>
    public static void CenteredHeader(Vector2 startPos, string text, float widthSpan, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = startPos;
        var max = startPos + new Vector2(widthSpan, HeaderHeight);
        var linePos = min + new Vector2(0, HeaderHeight - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);
        var textStart = new Vector2((widthSpan - ImGui.CalcTextSize(text).X) / 2, 0);
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);
    }

    public static void CenteredHeaderButton(Vector2 startPos, string text, float widthSpan, float rounding, FAI icon, Action onClick)
        => CenteredHeaderButton(startPos, text, widthSpan, rounding, icon, string.Empty, onClick);

    public static void CenteredHeaderButton(Vector2 startPos, string text, float widthSpan, float rounding, FAI icon, string tt, Action onClick)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = startPos;
        var max = startPos + new Vector2(widthSpan, HeaderHeight);
        var linePos = min + new Vector2(0, HeaderHeight - 2);

        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersTop);
        wdl.AddLine(linePos, linePos with { X = max.X }, CkColor.ElementSplit.Uint(), 2);

        // Text & Icon Alignment
        var textWidth = ImGui.CalcTextSize(text).X;
        var iconSize = CkGui.IconSize(icon);
        var textIconWidth = textWidth + ImGui.GetStyle().ItemInnerSpacing.X + iconSize.X;
        var centerStartX = min.X + (widthSpan - textIconWidth) / 2;
        var hoverRectStart = min with { X = centerStartX };
        var hoverSize = new Vector2(textIconWidth, HeaderHeight);
        
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
}
