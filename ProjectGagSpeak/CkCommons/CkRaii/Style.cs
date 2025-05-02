using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;

public static partial class CkRaii
{
    public enum HeaderFlags : byte
    {
        /// <summary> Aligns the header text to the center. </summary>
        /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
        AlignCenter = 0x00,

        /// <summary> Aligns the header text to the left. </summary>
        /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
        AlignLeft = 0x01,

        /// <summary> Aligns the header text to the right. </summary>
        /// <remarks> Only one AlignFlag will be accepted in any operation. </remarks>
        AlignRight = 0x02,

        /// <summary> The passed in size includes the header height, and should have it subtracted before making the body. </summary>
        /// <remarks> useful for cases where your height is ImGui.GetContentRegionAvail().Y </remarks>
        SizeIncludesHeader = 0x04,

        /// <summary> Means any container should append WindowPadding.Y * 2 to the size parameter. </summary>
        /// <remarks> Useful for when you want to pass in an internal height you know of. </remarks>
        AddPaddingToHeight = 0x08,


        ContentRegionHeaderLeft = SizeIncludesHeader | AlignLeft,
        ContentRegionHeaderCentered = SizeIncludesHeader | AlignCenter,
        ContentRegionHeaderRight = SizeIncludesHeader | AlignRight,
    }

    public static Vector2 HeaderTextOffset(float headerWidth, float headerHeight, float textWidth, HeaderFlags align)
        => align switch
        {
            HeaderFlags.AlignLeft => new Vector2(ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            HeaderFlags.AlignRight => new Vector2(headerWidth - textWidth - ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            _ => new Vector2((headerWidth - textWidth) / 2, (headerHeight - ImGui.GetTextLineHeight()) / 2), // Center is default.
        };

    // Flag Style Helpers
    public static WFlags WithPadding(this WFlags flags) => flags |= WFlags.AlwaysUseWindowPadding;


    // Size Helpers
    public static float RemoveWinPadX(this float s) => s - ImGui.GetStyle().WindowPadding.X * 2;
    public static float RemoveWinPadY(this float s) => s - ImGui.GetStyle().WindowPadding.Y * 2;
    public static float AddWinPadX(this float s) => s + ImGui.GetStyle().WindowPadding.X * 2;
    public static float AddWinPadY(this float s) => s + ImGui.GetStyle().WindowPadding.Y * 2;
    public static Vector2 WithoutWinPadding(this Vector2 s) => s - ImGui.GetStyle().WindowPadding * 2;
    public static Vector2 WithWinPadding(this Vector2 s) => s + ImGui.GetStyle().WindowPadding * 2;


    // Measurement Helpers
    public static float GetHeaderHeight() => ImGui.GetFrameHeight();

    public static float GetChildRounding() => ImGui.GetStyle().FrameRounding * 1.25f;
    public static float GetChildRoundingLarge() => ImGui.GetStyle().FrameRounding * 1.75f;
    public static float GetHeaderRounding() => ImGui.GetStyle().FrameRounding * 2f;

    public struct HeaderChildColors(uint headerColor, uint splitColor, uint bodyColor)
    {
        public uint HeaderColor { get; } = headerColor;
        public uint SplitColor { get; } = splitColor;
        public uint BodyColor { get; } = bodyColor;

        public static HeaderChildColors Default => new HeaderChildColors(CkColor.ElementHeader.Uint(), CkColor.ElementSplit.Uint(), CkColor.ElementBG.Uint());
    }

    // used by ImGui.Child and ImGui.Group
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

    /// <summary> An IEndObject that serves for ImRaii.EndUnconditionally, exclusively for containers. </summary>
    /// <remarks> This should only be used for unconditionally ended ImGui.Group objects. </remarks>
    private struct EndObjectContainer(Action endAction, bool success, Vector2 innerRegion) : IEndObjectContainer
    {
        private Action EndAction { get; } = endAction;
        public Vector2 InnerRegion { get; } = innerRegion;
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

    public interface IEndObjectContainer : ImRaii.IEndObject
    {
        /// <summary> The inner region of the container. </summary>
        Vector2 InnerRegion { get; }
    }
}
