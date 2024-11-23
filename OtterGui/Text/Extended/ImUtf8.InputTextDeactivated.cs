using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    #region In ByteSpan, Out ByteSpan

    /// <summary> Draw a text input that does not return the <paramref name="result"/> on every change. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="input"> The input as a readonly UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="result"> When true is returned, an owned, null-terminated span of UTF8 bytes. The span's length does not include the null-terminator. Otherwise, an empty span.  </param>
    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the input field was deactivated in this frame and the value has been changed. </returns>
    /// <remarks>
    /// <paramref name="result"/> is only returned after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="input"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<byte> input, out TerminatedByteString result,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;
        var id = ImGuiNative.igGetID_StrStr(label.Start(out var end), end);
        if (InputStringHandlerBuffer.IsActive && id == InputStringHandlerBuffer.LastId)
        {
            ImGuiNative.igInputText(label.Start(), InputStringHandlerBuffer.Buffer, (uint)InputStringHandlerBuffer.Size, flags, null!, null);
            return InputStringHandlerBuffer.Return(input, out result);
        }

        input.CopyInto<TextStringHandlerBuffer>();
        if (InputText(label, TextStringHandlerBuffer.Span, hint, flags))
            InputStringHandlerBuffer.Update(TextStringHandlerBuffer.Span, id);

        result = TerminatedByteString.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint, flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, input, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, input, out result, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ReadOnlySpan{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<byte> input,
        out TerminatedByteString result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint.Span(), flags);

    #endregion

    #region In ReadOnlyChar, Out string

    /// <summary> Draw a text input that does not return the <paramref name="result"/> on every change. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="input"> The input as a readonly UTF16 string. </param>
    /// <param name="result"> When true is returned, a UTF16 string of the input.  </param>
    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    /// <remarks>
    /// <paramref name="result"/> is only returned after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="input"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    /// <exception cref="ImUtf8SizeException" />
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<char> input, out string result,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;
        var id = ImGuiNative.igGetID_StrStr(label.Start(out var end), end);
        if (InputStringHandlerBuffer.IsActive && id == InputStringHandlerBuffer.LastId)
        {
            ImGuiNative.igInputText(label.Start(), InputStringHandlerBuffer.Buffer, (uint)InputStringHandlerBuffer.Size, flags, null!, null);
            return InputStringHandlerBuffer.Return(input, out result);
        }

        input.CopyInto<TextStringHandlerBuffer>(out _);
        if (InputText(label, TextStringHandlerBuffer.Span, hint, flags))
            InputStringHandlerBuffer.Update(TextStringHandlerBuffer.Span, id);

        result = string.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{char}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<char> input, out string result,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{char}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<char> input,
        out string result, ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint, flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{char}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<char> input, out string result, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, input, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{char}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<char> input,
        out string result, ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ReadOnlySpan{char}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<char> input,
        out string result, ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ReadOnlySpan{char}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ReadOnlySpan<char> input,
        out string result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, input, out result, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ReadOnlySpan{char}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ReadOnlySpan<char> input,
        out string result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), input, out result, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ReadOnlySpan{char}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<char> input,
        out string result, ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), input, out result, hint.Span(), flags);

    #endregion

    #region In string, Out string

    /// <summary> Draw a text input that does not update the <paramref name="text"/> on every change. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="text"> The input/output as a UTF16 string. Only gets changed when this is deactivated and returns true. </param>
    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    /// <remarks>
    /// <paramref name="text"/> is only updated after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="text"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    /// <exception cref="ImUtf8SizeException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ref string text, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (!InputTextOnDeactivated(label, text.AsSpan(), out var tmp, hint, flags))
            return false;

        text = tmp;
        return true;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ref string text, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref text, hint, flags);

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), ref text, hint, flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ref string text, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, ref text, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ref string text, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref text, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), ref text, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{byte}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<byte> label, ref string text, ref Utf8StringHandler<HintStringHandlerBuffer> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label, ref text, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ReadOnlySpan{char}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ReadOnlySpan<char> label, ref string text, ref Utf8StringHandler<HintStringHandlerBuffer> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref text, hint.Span(), flags);

    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputTextOnDeactivated(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputTextOnDeactivated(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputTextOnDeactivated(label.Span(), ref text, hint.Span(), flags);

    #endregion
}
