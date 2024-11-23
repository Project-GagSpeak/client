namespace OtterGui.Text.HelperObjects;

public static class ImTextExtensions
{
    /// <summary> Clone the given byte span with an appended null-terminator. </summary>
    public static TerminatedByteString CloneNullTerminated(this ReadOnlySpan<byte> input)
    {
        var bytes = new byte[input.Length + 1];
        input.CopyTo(bytes);
        bytes[input.Length] = 0;
        return new TerminatedByteString(bytes);
    }

    /// <summary> Clone the given byte span with an appended null-terminator. </summary>
    public static TerminatedByteString CloneNullTerminated(this Span<byte> input)
    {
        var bytes = new byte[input.Length + 1];
        input.CopyTo(bytes);
        bytes[input.Length] = 0;
        return new TerminatedByteString(bytes);
    }

    /// <summary> Copy a given span with null-termination into a buffer or throw if too large. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void CopyInto<T>(this ReadOnlySpan<byte> data) where T : IStringHandlerBuffer
    {
        if (data.Length >= T.Size)
            throw new ImUtf8SizeException();

        data.CopyTo(T.Span);
        T.Buffer[data.Length] = 0;
    }

    /// <summary> Transcode a given span with null-termination into a buffer or throw if too large. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe void CopyInto<T>(this ReadOnlySpan<char> data, out int bytesWritten) where T : IStringHandlerBuffer
    {
        if (!Encoding.UTF8.TryGetBytes(data, T.Span, out bytesWritten) || bytesWritten == T.Size)
            throw new ImUtf8SizeException();

        T.Buffer[bytesWritten] = 0;
    }

    /// <summary> Read a null-terminated string from the buffer and clone it with null-terminator. </summary>
    internal static TerminatedByteString ReadNullTerminated(this Span<byte> buffer)
    {
        var nullTerminator = buffer.IndexOf((byte)0);
        if (nullTerminator == -1)
        {
            var result = new byte[buffer.Length + 1];
            buffer.CopyTo(result);
            result[buffer.Length] = 0;
            return new TerminatedByteString(result);
        }
        else
        {
            var result = new byte[nullTerminator + 1];
            buffer[..(nullTerminator + 1)].CopyTo(result);
            return new TerminatedByteString(result);
        }
    }

    /// <summary> Read a null-terminated string from the buffer and copy it into the given buffer. </summary>
    internal static unsafe Span<byte> CopyNullTerminated<T>(this Span<byte> buffer) where T : IStringHandlerBuffer
    {
        var nullTerminator = buffer.IndexOf((byte)0);
        if (nullTerminator == -1)
        {
            buffer.CopyTo(T.Span);
            T.Buffer[buffer.Length] = 0;
        }

        buffer[..(nullTerminator + 1)].CopyTo(T.Span);
        return T.Span[..(nullTerminator + 1)];
    }

    /// <summary> Get the starting pointer for a span. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* Start(this Span<byte> text)
    {
        fixed (byte* ptr = text)
        {
            return ptr;
        }
    }

    /// <summary> Get the starting and ending pointer for a span. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* Start(this Span<byte> text, out byte* end)
    {
        fixed (byte* ptr = text)
        {
            end = ptr + text.Length;
            return ptr;
        }
    }

    /// <summary> Get the starting pointer for a span. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* Start(this ReadOnlySpan<byte> text)
    {
        fixed (byte* ptr = text)
        {
            return ptr;
        }
    }

    /// <summary> Get the starting and ending pointer for a span. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* Start(this ReadOnlySpan<byte> text, out byte* end)
    {
        fixed (byte* ptr = text)
        {
            end = ptr + text.Length;
            return ptr;
        }
    }

    /// <summary> Return the formatted span of a text handler or throw if you can not. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static ReadOnlySpan<byte> Span<T>(this ref Utf8StringHandler<T> text) where T : IStringHandlerBuffer
        => text.GetSpan(out var span) ? span : throw new ImUtf8FormatException();

    /// <summary> Return the formatted span of a text handler or throw if you can not. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* End<T>(this ref Utf8StringHandler<T> text) where T : IStringHandlerBuffer
        => text.GetEnd(out var end) ? end : throw new ImUtf8FormatException();

    /// <summary> Return the transcoded span of the text or throw if you can not. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe byte* End<T>(this ReadOnlySpan<char> text) where T : IStringHandlerBuffer
        => T.Write(text, out var end) ? end : throw new ImUtf8FormatException();

    /// <summary> Return the transcoded span of the text or throw if you can not. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static unsafe ReadOnlySpan<byte> Span<T>(this ReadOnlySpan<char> text) where T : IStringHandlerBuffer
        => T.Write(text, out var end) ? new ReadOnlySpan<byte>(T.Buffer, (int)(end - T.Buffer)) : throw new ImUtf8FormatException();

    /// <summary> Return a bool as a byte value. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static byte Byte(this bool value)
        => value ? (byte)1 : (byte)0;

    /// <summary> Return a byte as a bool value. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    internal static bool Bool(this byte value)
        => value != 0;
}
