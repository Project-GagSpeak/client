using System.Text.Unicode;

namespace OtterGui.Text.HelperObjects;

[InterpolatedStringHandler]
public unsafe ref struct Utf8StringHandler<T> where T : IStringHandlerBuffer
{
    private Utf8.TryWriteInterpolatedStringHandler _handler;

    internal byte* Begin
        => T.Buffer;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public bool GetEnd(out byte* end)
    {
        if (Utf8.TryWrite([], ref _handler, out var bytes))
        {
            end = T.Buffer + bytes;
            if (T.Size > bytes)
                T.Buffer[bytes] = 0;
            return true;
        }

        end = T.Buffer;
        T.Buffer[0] = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public bool GetSpan(out ReadOnlySpan<byte> span)
    {
        if (Utf8.TryWrite([], ref _handler, out var bytes))
        {
            span = new ReadOnlySpan<byte>(T.Buffer, bytes);
            if (T.Size > bytes)
                T.Buffer[bytes] = 0;
            return true;
        }

        span = [0];
        return false;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8StringHandler(int literalLength, int formattedCount, out bool shouldAppend)
        => _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new Span<byte>(T.Buffer, T.Size),
            out shouldAppend);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8StringHandler(int literalLength, int formattedCount, IFormatProvider? provider, out bool shouldAppend)
        => _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, new Span<byte>(T.Buffer, T.Size), provider,
            out shouldAppend);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendLiteral(string value)
        => _handler.AppendLiteral(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<TValue>(TValue value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<TValue>(TValue value, string? format)
        => _handler.AppendFormatted(value, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<TValue>(TValue value, int alignment)
        => _handler.AppendFormatted(value, alignment);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted<TValue>(TValue value, int alignment, string? format)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<char> value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<char> value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value)
        => _handler.AppendFormatted(utf8Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(scoped ReadOnlySpan<byte> utf8Value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(utf8Value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(string? value)
        => _handler.AppendFormatted(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(string? value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AppendFormatted(object? value, int alignment = 0, string? format = null)
        => _handler.AppendFormatted(value, alignment, format);
}
