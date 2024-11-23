using ImGuiNET;

namespace OtterGui.Text.EndObjects;

public ref struct Group
{
    public bool Disposed;

    public Group()
        => Disposed = true;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal Group(bool _)
        => ImGui.BeginGroup();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        ImGui.EndGroup();
        Disposed = true;
    }
}
