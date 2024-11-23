using System.Text.Unicode;
using ImGuiNET;

namespace OtterGui.Text.HelperObjects;

[InterpolatedStringHandler]
public unsafe ref struct HoverUtf8StringHandler
{
    private Utf8.TryWriteInterpolatedStringHandler _handler;
    public readonly bool IsHovered;

    internal byte* Begin
        => TextStringHandlerBuffer.Buffer;

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public bool GetEnd(out byte* end)
    {
        if (IsHovered && Utf8.TryWrite([], ref _handler, out var bytes))
        {
            end = TextStringHandlerBuffer.Buffer + bytes;
            if (TextStringHandlerBuffer.Size > bytes)
                TextStringHandlerBuffer.Buffer[bytes] = 0;
            return true;
        }

        end = TextStringHandlerBuffer.Buffer;
        TextStringHandlerBuffer.Buffer[0] = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HoverUtf8StringHandler(int literalLength, int formattedCount, ImGuiHoveredFlags hoverFlags, out bool shouldAppend)
    {
        if (!ImGui.IsItemHovered(hoverFlags))
        {
            IsHovered = shouldAppend = false;
        }
        else
        {
            IsHovered = true;
            _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount,
                new Span<byte>(TextStringHandlerBuffer.Buffer, TextStringHandlerBuffer.Size),
                out shouldAppend);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HoverUtf8StringHandler(int literalLength, int formattedCount, out bool shouldAppend)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            IsHovered = shouldAppend = false;
        }
        else
        {
            IsHovered = true;
            _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, TextStringHandlerBuffer.Span,
                out shouldAppend);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HoverUtf8StringHandler(int literalLength, int formattedCount, ImGuiHoveredFlags hoverFlags, IFormatProvider? provider,
        out bool shouldAppend)
    {
        if (!ImGui.IsItemHovered(hoverFlags))
            IsHovered = shouldAppend = false;
        else
            _handler = new Utf8.TryWriteInterpolatedStringHandler(literalLength, formattedCount, TextStringHandlerBuffer.Span, provider,
                out shouldAppend);
    }

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
