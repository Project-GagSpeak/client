namespace OtterGui.Text.HelperObjects;

public interface IStringHandlerBuffer
{
    public abstract static        int        Size   { get; }
    public abstract static unsafe byte*      Buffer { get; }
    public abstract static        Span<byte> Span   { get; }
    public abstract static unsafe bool       Write(ReadOnlySpan<char> text, out byte* end);

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public static unsafe bool Write<T>(ReadOnlySpan<char> text, out byte* end) where T : IStringHandlerBuffer
    {
        if (Encoding.UTF8.TryGetBytes(text, T.Span, out var written))
        {
            if (written < T.Size)
                T.Buffer[written] = 0;
            end = T.Buffer + written;
            return true;
        }

        T.Buffer[0] = 0;
        end         = T.Buffer;
        return false;
    }
}
