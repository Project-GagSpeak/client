using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct ListBox
{
    public readonly bool Success;
    public          bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal ListBox(ReadOnlySpan<byte> label, Vector2 size)
        => Success = ImGuiNative.igBeginListBox(label.Start(), size).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(ListBox value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(ListBox i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(ListBox i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(ListBox i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(ListBox i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(ListBox i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndListBox();
        Disposed = true;
    }
}
