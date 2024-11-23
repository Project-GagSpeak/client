namespace OtterGui.Text.HelperObjects;

public unsafe struct HintStringHandlerBuffer : IStringHandlerBuffer
{
    public static int Size
        => 128 * 1024;

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

    static HintStringHandlerBuffer()
        => Buffer = (byte*)Marshal.AllocHGlobal(Size);

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static bool Write(ReadOnlySpan<char> text, out byte* end)
        => IStringHandlerBuffer.Write<HintStringHandlerBuffer>(text, out end);
}
