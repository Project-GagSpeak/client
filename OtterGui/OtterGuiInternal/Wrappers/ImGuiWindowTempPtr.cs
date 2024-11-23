using OtterGuiInternal.Structs;

namespace OtterGuiInternal.Wrappers;

public readonly unsafe struct ImGuiWindowTempPtr
{
    public ImGuiWindowTempPtr(ImGuiWindowTemp* pointer)
        => Pointer = pointer;

    public readonly ImGuiWindowTemp* Pointer = null;

    public nint Address
        => (nint)Pointer;

    public bool Valid
        => Pointer != null;

    public static implicit operator ImGuiWindowTempPtr(ImGuiWindowTemp* pointer)
        => new(pointer);

    public Vector2 CursorPos
        => Pointer->CursorPos;

    public float CurrLineTextBaseOffset
        => Pointer->CurrLineTextBaseOffset;
}
