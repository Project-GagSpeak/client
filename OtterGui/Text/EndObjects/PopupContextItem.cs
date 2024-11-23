using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct PopupContextItem
{
    public readonly bool Success;
    public          bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal PopupContextItem(ReadOnlySpan<byte> label, ImGuiPopupFlags flags)
        => Success = ImGuiNative.igBeginPopupContextItem(label.Start(), flags).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(PopupContextItem value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(PopupContextItem i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(PopupContextItem i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(PopupContextItem i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(PopupContextItem i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(PopupContextItem i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndPopup();
        Disposed = true;
    }
}
