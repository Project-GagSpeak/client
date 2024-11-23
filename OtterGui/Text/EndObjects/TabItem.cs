using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct TabItem
{
    public readonly bool Success;
    public          bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal TabItem(ReadOnlySpan<byte> label, ref bool open, ImGuiTabItemFlags flags)
        => Success = ImGuiNative.igBeginTabItem(label.Start(), (byte*)Unsafe.AsPointer(ref open), flags).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal TabItem(ReadOnlySpan<byte> label, ImGuiTabItemFlags flags)
        => Success = ImGuiNative.igBeginTabItem(label.Start(), null, flags).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(TabItem value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(TabItem i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(TabItem i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(TabItem i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(TabItem i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(TabItem i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndTabItem();
        Disposed = true;
    }
}
