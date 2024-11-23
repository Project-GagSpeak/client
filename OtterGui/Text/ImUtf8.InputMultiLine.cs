using Dalamud.Memory;
using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    #region In ByteSpan, Out ByteSpan

    /// <summary> Draw a multi-line text input without automatic text-wrapping. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="buffer"> The buffer preloaded with the input data, which should have enough space to edit inside the buffer. </param>
    /// <param name="result"> When true is returned, an owned, null-terminated span of UTF8 bytes. The span's length does not include the null-terminator. Otherwise, an empty span.  </param>
    /// <param name="size"> The size of the input field. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ReadOnlySpan<byte> label, Span<byte> buffer, out TerminatedByteString result, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (InputMultiLine(label, buffer, size, flags))
        {
            result = buffer.ReadNullTerminated();
            return true;
        }

        result = TerminatedByteString.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ReadOnlySpan<char> label, Span<byte> buffer, out TerminatedByteString result, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span<LabelStringHandlerBuffer>(), buffer, out result, size, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{char},Span{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out TerminatedByteString result,
        Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span(), buffer, out result, size, flags);

    #endregion

    #region In ByteSpan, Out string

    /// <param name="result"> When true is returned, a UTF16 string of the updated value. Otherwise, an empty string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ReadOnlySpan<byte> label, Span<byte> buffer, out string result, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (InputMultiLine(label, buffer, size, flags))
        {
            result = string.Empty;
            return false;
        }

        result = MemoryHelper.ReadStringNullTerminated((nint)buffer.Start());
        return true;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{byte},Span{byte}, out string, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ReadOnlySpan<char> label, Span<byte> buffer, out string result, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span<LabelStringHandlerBuffer>(), buffer, out result, size, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{char},Span{byte}, out string, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out string result,
        Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span(), buffer, out result, size, flags);

    #endregion

    #region In string, Out string

    /// <summary> Draw a multi-line text input without automatic text-wrapping. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="text"> The input/output as a UTF16 string. </param>
    /// <param name="size"> The size of the input field. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    public static bool InputMultiLine(ReadOnlySpan<byte> label, ref string text, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        text.AsSpan().CopyInto<TextStringHandlerBuffer>(out _);
        if (!InputMultiLine(label, TextStringHandlerBuffer.Span, size, flags))
            return false;

        text = MemoryHelper.ReadStringNullTerminated((nint)TextStringHandlerBuffer.Buffer);
        return true;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{byte},ref string, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ReadOnlySpan<char> label, ref string text, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span<LabelStringHandlerBuffer>(), ref text, size, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputMultiLine(ReadOnlySpan{char},ref string, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLine(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLine(label.Span(), ref text, size, flags);

    #endregion

    /// <summary> Simple helper to wrap the multiline input. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputMultiLine(ReadOnlySpan<byte> label, Span<byte> buffer, Vector2 size, ImGuiInputTextFlags flags)
        => ImGuiNative.igInputTextMultiline(label.Start(), buffer.Start(), (uint)buffer.Length, size, flags, null!, null).Bool();
}
