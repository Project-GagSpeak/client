using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;

public static partial class CkRaii
{
    public enum HeaderAlign : byte
    {
        AlignLeft = 0,
        AlignCenter = 1,
        AlignRight = 2,
    }

    public static Vector2 HeaderTextOffset(float headerWidth, float headerHeight, string text, HeaderAlign align)
        => align switch
        {
            HeaderAlign.AlignLeft => new Vector2(ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            HeaderAlign.AlignCenter => new Vector2((headerWidth - ImGui.CalcTextSize(text).X) / 2, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            HeaderAlign.AlignRight => new Vector2(headerWidth - ImGui.CalcTextSize(text).X - ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            _ => throw new ArgumentOutOfRangeException(nameof(align), align, null)
        };

    // Flag Style Helpers
    public static WFlags WithPadding(this WFlags flags) => flags | WFlags.AlwaysUseWindowPadding;


    // Size Helpers
    public static Vector2 WithPadding(this Vector2 s) => s + ImGui.GetStyle().WindowPadding * 2;


    // Measurement Helpers
    public static float GetSeparatorHeight() => 4 * ImGuiHelpers.GlobalScale;

    public static float GetHeaderHeight() => ImGui.GetFrameHeight();

    public static float GetHeaderRounding() => ImGui.GetStyle().FrameRounding * 2f;

    public static float GetChildRounding() => ImGui.GetStyle().FrameRounding * 1.25f;

    public static float GetChildRoundingLarge() => ImGui.GetStyle().FrameRounding * 1.75f;

    public struct HeaderChildColors(uint headerColor, uint splitColor, uint bodyColor)
    {
        public uint HeaderColor { get; } = headerColor;
        public uint SplitColor { get; } = splitColor;
        public uint BodyColor { get; } = bodyColor;

        public static HeaderChildColors Default => new HeaderChildColors(CkColor.ElementHeader.Uint(), CkColor.ElementSplit.Uint(), CkColor.ElementBG.Uint());
    }

    private struct EndUnconditionally(Action endAction, bool success) : ImRaii.IEndObject
    {
        private Action EndAction { get; } = endAction;
        public bool Success { get; } = success;
        public bool Disposed { get; private set; } = false;

        public void Dispose()
        {
            if (Disposed)
                return;

            EndAction();
            Disposed = true;
        }
    }
}
