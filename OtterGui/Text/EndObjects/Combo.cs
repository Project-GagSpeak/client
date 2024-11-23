using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct Combo
{
    public readonly bool Success;
    public          bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal Combo(ReadOnlySpan<byte> label, ReadOnlySpan<byte> preview, ImGuiComboFlags flags)
        => Success = ImGuiNative.igBeginCombo(label.Start(), preview.Start(), flags).Bool();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Combo value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(Combo i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(Combo i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(Combo i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(Combo i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(Combo i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        if (Success)
            ImGui.EndCombo();

        Disposed = true;
    }
}
