using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    #region In ByteSpan, Out ByteSpan

    /// <summary> Draw a multi-line text input without automatic text-wrapping. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="input"> The input as a readonly UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="result"> When true is returned, an owned, null-terminated span of UTF8 bytes. The span's length does not include the null-terminator. Otherwise, an empty span.  </param>
    /// <param name="size"> The size of the input field. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the input field was deactivated in this frame and the value has been changed. </returns>
    /// <remarks>
    /// <paramref name="result"/> is only returned after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="input"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<byte> input, out TerminatedByteString result,
        Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;
        var id = ImGuiNative.igGetID_StrStr(label.Start(out var end), end);
        if (InputStringHandlerBuffer.IsActive && id == InputStringHandlerBuffer.LastId)
        {
            ImGuiNative.igInputTextMultiline(label.Start(), InputStringHandlerBuffer.Buffer, (uint)InputStringHandlerBuffer.Size, size, flags,
                null!, null);
            return InputStringHandlerBuffer.Return(input, out result);
        }

        input.CopyInto<TextStringHandlerBuffer>();
        if (InputMultiLine(label, TextStringHandlerBuffer.Span, size, flags))
            InputStringHandlerBuffer.Update(TextStringHandlerBuffer.Span, id);
        result = TerminatedByteString.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{byte},ReadOnlySpan{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, size, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{char},ReadOnlySpan{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span(), input, out result, size, flags);

    #endregion

    #region In ReadOnlyChar, Out string

    /// <param name="input"> The input as a UTF16 string. </param>
    /// <param name="result"> When true is returned, a UTF16 string of the changed input. Otherwise, an empty string.  </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{byte},ReadOnlySpan{byte}, out TerminatedByteString, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8SizeException" />
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<char> input, out string result,
        Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;
        var id = ImGuiNative.igGetID_StrStr(label.Start(out var end), end);
        if (InputStringHandlerBuffer.IsActive && id == InputStringHandlerBuffer.LastId)
        {
            ImGuiNative.igInputTextMultiline(label.Start(), InputStringHandlerBuffer.Buffer, (uint)InputStringHandlerBuffer.Size, size, flags,
                null!, null);
            return InputStringHandlerBuffer.Return(input, out result);
        }

        input.CopyInto<TextStringHandlerBuffer>(out _);
        if (InputMultiLine(label, TextStringHandlerBuffer.Span, size, flags))
            InputStringHandlerBuffer.Update(TextStringHandlerBuffer.Span, id);
        result = string.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{byte},ReadOnlySpan{char}, out string, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<char> input,
        out string result, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span(), input, out result, size, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{char},ReadOnlySpan{char}, out string, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<char> input,
        out string result, Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, size, flags);

    #endregion

    #region In string, Out string

    /// <summary> Draw a multi-line text input without automatic text-wrapping. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="text"> The input/output as a UTF16 string. Only gets changed when this is deactivated and returns true. </param>
    /// <param name="size"> The size of the input field. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the input field was deactivated in this frame and the value has been changed. </returns>
    /// <remarks>
    /// <paramref name="text"/> is only updated after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="text"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<byte> label, ref string text, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (!InputMultiLineOnDeactivated(label, text.AsSpan(), out var tmp, size, flags))
            return false;

        text = tmp;
        return true;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{byte}, ref string, Vector2, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text,
        Vector2 size = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span(), ref text, size, flags);

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputMultiLineOnDeactivated(ReadOnlySpan{char}, ref string, Vector2, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputMultiLineOnDeactivated(ReadOnlySpan<char> label, ref string text, Vector2 size = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputMultiLineOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref text, size, flags);

    #endregion
}
