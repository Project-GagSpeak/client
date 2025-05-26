using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static Vector2 HeaderTextOffset(float headerWidth, float headerHeight, float textWidth, HeaderFlags align)
        => align switch
        {
            HeaderFlags.AlignLeft => new Vector2(ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            HeaderFlags.AlignRight => new Vector2(headerWidth - textWidth - ImGui.GetStyle().FramePadding.X, (headerHeight - ImGui.GetTextLineHeight()) / 2),
            _ => new Vector2((headerWidth - textWidth) / 2, (headerHeight - ImGui.GetTextLineHeight()) / 2), // Center is default.
        };

    public struct ColorsLC(uint label, uint shadow, uint background, uint labelHovered = 0)
    {
        public uint Label { get; } = label;
        public uint LabelHovered { get; } = labelHovered;
        public uint Shadow { get; } = shadow;
        public uint BG { get; } = background;
        public static ColorsLC Default => new ColorsLC(CkColor.VibrantPink.Uint(), CkColor.ElementSplit.Uint(), CkColor.FancyHeader.Uint(), CkColor.VibrantPinkHovered.Uint());
    }

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
    private struct EndObjectContainer(Action endAction, bool success, Vector2 innerRegion) : IEOContainer
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

    /// <summary> An IEndObject extention of EndObjectContainer, for advanced container objects built from CkRaii. </summary>
    /// <remarks> This should only be used for unconditionally ended ImGui.Group and ImGui.Child objects. </remarks>
    private struct EndObjectLabelContainer(Action endAction, bool success, Vector2 inner, Vector2 label) : IEOLabelContainer
    {
        private Action EndAction { get; } = endAction;
        public Vector2 InnerRegion { get; } = inner;
        public Vector2 LabelRegion { get; } = label;
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

    public interface IEOLabelContainer : IEOContainer
    {
        /// <summary> The label region of the container. </summary>
        Vector2 LabelRegion { get; }
    }

    public interface IEOContainer : ImRaii.IEndObject
    {
        /// <summary> The inner region of the container. </summary>
        Vector2 InnerRegion { get; }
    }
}
