namespace OtterGui.Text.HelperObjects;

public unsafe struct TextStringHandlerBuffer : IStringHandlerBuffer
{
    public static int Size
        => 4 * 1024 * 1024;

    public static byte* Buffer { get; }

    public static Span<byte> Span
        => new(Buffer, Size);

    private static readonly Cleaner Cleanup = new();

    private class Cleaner
    {
        ~Cleaner()
        {
            Marshal.FreeHGlobal((nint)Buffer);
        }
    }

    static TextStringHandlerBuffer()
        => Buffer = (byte*)Marshal.AllocHGlobal(Size);

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Write(ReadOnlySpan<char> text, out byte* end)
        => IStringHandlerBuffer.Write<TextStringHandlerBuffer>(text, out end);
}
