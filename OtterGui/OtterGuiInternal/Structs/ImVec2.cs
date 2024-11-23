namespace OtterGuiInternal.Structs;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ImVec2(float X, float Y)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static implicit operator Vector2(ImVec2 v)
        => new(v.X, v.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static implicit operator ImVec2(Vector2 v)
        => new(v.X, v.Y);
}
