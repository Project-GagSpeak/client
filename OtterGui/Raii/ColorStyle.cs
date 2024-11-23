using ImGuiNET;

namespace OtterGui.Raii;

public static partial class ImRaii
{
    public static ColorStyle PushFrameBorder(float thickness, uint color, bool condition = true)
    {
        var style = new ColorStyle();
        if (condition)
            style.Push(ImGuiCol.Border, color)
                .Push(ImGuiStyleVar.FrameBorderSize, thickness);

        return style;
    }

    public readonly struct ColorStyle() : IDisposable
    {
        private readonly Style _style = new();
        private readonly Color _color = new();

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public ColorStyle Push(ImGuiStyleVar idx, float value, bool condition = true)
        {
            _style.Push(idx, value, condition);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public ColorStyle Push(ImGuiStyleVar idx, Vector2 value, bool condition = true)
        {
            _style.Push(idx, value, condition);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public ColorStyle Push(ImGuiCol idx, uint color, bool condition = true)
        {
            _color.Push(idx, color, condition);
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _style.Dispose();
            _color.Dispose();
        }
    }
}
