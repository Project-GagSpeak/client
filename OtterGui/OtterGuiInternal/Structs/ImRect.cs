namespace OtterGuiInternal.Structs;


/// <summary> A struct to represent rectangles, e.g. for bounding boxes. </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ImRect(float MinX, float MinY, float MaxX, float MaxY)
{
    public Vector2 Min
        => new(MinX, MinY);

    public Vector2 Max
        => new(MaxX, MaxY);

    public ImRect(Vector2 min, Vector2 max)
        : this(min.X, min.Y, max.X, max.Y)
    { }
}