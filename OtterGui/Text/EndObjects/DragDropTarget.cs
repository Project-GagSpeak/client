using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

[method: MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
public unsafe ref struct DragDropTarget()
{
    public readonly bool Success = ImGuiNative.igBeginDragDropTarget().Bool();
    public          bool Disposed;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDropping(ReadOnlySpan<byte> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => Success && CheckPayload(label, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDropping(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => Success && CheckPayload(ref label, flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsDropping(ReadOnlySpan<char> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => Success && CheckPayload(label, flags);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckPayload(ReadOnlySpan<byte> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => ImGuiNative.igAcceptDragDropPayload(label.Start(), flags) != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckPayload(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => CheckPayload(label.Span(), flags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckPayload(ReadOnlySpan<char> label, ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => CheckPayload(label.Span<LabelStringHandlerBuffer>(), flags);


    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(DragDropTarget value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(DragDropTarget i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(DragDropTarget i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(DragDropTarget i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(DragDropTarget i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(DragDropTarget i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndDragDropTarget();
        Disposed = true;
    }
}
