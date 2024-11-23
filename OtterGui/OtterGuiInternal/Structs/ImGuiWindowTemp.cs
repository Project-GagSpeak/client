// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace OtterGuiInternal.Structs;

[StructLayout(LayoutKind.Sequential)]
public struct ImGuiWindowTemp
{
    public Vector2 CursorPos;
    public Vector2 CursorPosPrevLine;
    public Vector2 CursorStartPos;
    public Vector2 CursorMaxPos;
    public Vector2 IdealMaxPos;
    public Vector2 CurrLineSize;
    public Vector2 PrevLineSize;
    public float CurrLineTextBaseOffset;
}
