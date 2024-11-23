using OtterGuiInternal.Structs;

namespace OtterGuiInternal.Wrappers;

public readonly unsafe struct ImGuiWindowPtr
{
    public ImGuiWindowPtr(ImGuiWindow* pointer)
        => Pointer = pointer;

    public readonly ImGuiWindow* Pointer = null;

    public nint Address
        => (nint)Pointer;

    public bool Valid
        => Pointer != null;

    public static implicit operator ImGuiWindowPtr(ImGuiWindow* pointer)
        => new(pointer);


    public bool SkipItems
        => Pointer->SkipItems;

    public ImGuiWindowTempPtr Dc
        => &Pointer->Dc;
}
