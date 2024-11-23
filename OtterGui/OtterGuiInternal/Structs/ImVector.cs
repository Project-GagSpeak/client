namespace OtterGuiInternal.Structs;

[StructLayout(LayoutKind.Sequential)]
public readonly unsafe record struct ImVector<T>(int Size, int Capacity, nint Data);