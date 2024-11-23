using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public ref struct DragDropSource
{
    public readonly bool Success;
    public          bool Disposed;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe bool SetPayload(ReadOnlySpan<byte> label, ImGuiCond condition = ImGuiCond.None)
        => ImGuiNative.igSetDragDropPayload(label.Start(), null, 0, condition).Bool();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetPayload(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiCond condition = ImGuiCond.None)
        => SetPayload(label.Span(), condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SetPayload(ReadOnlySpan<char> label, ImGuiCond condition = ImGuiCond.None)
        => SetPayload(label.Span<LabelStringHandlerBuffer>(), condition);


    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal DragDropSource(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => Success = ImGuiNative.igBeginDragDropSource(flags).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(DragDropSource value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(DragDropSource i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(DragDropSource i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(DragDropSource i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(DragDropSource i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(DragDropSource i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndDragDropSource();
        Disposed = true;
    }
}
