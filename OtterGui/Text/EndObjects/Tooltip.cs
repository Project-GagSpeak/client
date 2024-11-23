using ImGuiNET;

namespace OtterGui.Text.EndObjects;

public ref struct Tooltip
{
    public bool Disposed;

    public Tooltip()
        => Disposed = true;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    internal Tooltip(bool _)
        => ImGui.BeginTooltip();

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (Disposed)
            return;

        ImGui.EndTooltip();
        Disposed = true;
    }
}
