using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text.EndObjects;

public unsafe ref struct CollapsingHeader
{
    public readonly bool Success;
    public          bool Disposed;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal CollapsingHeader(ReadOnlySpan<byte> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        Success = ImGuiNative.igCollapsingHeader_TreeNodeFlags(label.Start(), flags).Bool();
        ImGuiNative.igPushID_StrStr(label.Start(out var end), end);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal CollapsingHeader(ReadOnlySpan<byte> label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        Success = ImGuiNative.igCollapsingHeader_BoolPtr(label.Start(), (byte*) Unsafe.AsPointer(ref visible), flags).Bool();
        ImGuiNative.igPushID_StrStr(label.Start(out var end), end);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(CollapsingHeader value)
        => value.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator true(CollapsingHeader i)
        => i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator false(CollapsingHeader i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator !(CollapsingHeader i)
        => !i.Success;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator &(CollapsingHeader i, bool value)
        => i.Success && value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool operator |(CollapsingHeader i, bool value)
        => i.Success || value;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        ImGui.PopID();
        Disposed = true;
    }
}
