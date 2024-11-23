using Dalamud.Memory;
using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    #region In ByteSpan, Out ByteSpan

    /// <summary> Draw a text input. </summary>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="buffer"> The buffer preloaded with the input data, which should have enough space to edit inside the buffer. </param>
    /// <param name="result"> When true is returned, an owned, null-terminated span of UTF8 bytes. The span's length does not include the null-terminator. Otherwise, an empty span.  </param>
    /// <param name="hint"> An optional hint to display in the input box as long as the input is empty as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out TerminatedByteString result,
        ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (InputText(label, buffer, hint, flags))
        {
            result = buffer.ReadNullTerminated();
            return true;
        }

        result = TerminatedByteString.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out TerminatedByteString result,
        ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out TerminatedByteString result,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint, flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out TerminatedByteString result, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out TerminatedByteString result, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out TerminatedByteString result,
        ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out TerminatedByteString result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, buffer, out result, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out TerminatedByteString result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer},Span{byte}, out TerminatedByteString, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out TerminatedByteString result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint.Span(), flags);

    #endregion

    #region In ByteSpan, Out string

    /// <param name="result"> When true is returned, the current value transcoded to an UTF16 string. Otherwise, an empty string.  </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out string result, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        if (InputText(label, buffer, hint, flags))
        {
            result = MemoryHelper.ReadStringNullTerminated((nint)buffer.Start());
            return true;
        }

        result = string.Empty;
        return false;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out string result, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out string result,
        ReadOnlySpan<byte> hint = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint, flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out string result, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out string result, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer},Span{byte}, out string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out string result,
        ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, out string result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, buffer, out result, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char},Span{byte}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, Span<byte> buffer, out string result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), buffer, out result, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer}, Span{byte}, out string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Span<byte> buffer, out string result,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), buffer, out result, hint.Span(), flags);

    #endregion

    #region In string, Out string

    /// <param name="text"> The input/output value as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte},Span{byte}, out TerminatedByteString, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8SizeException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, ref string text, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        text.AsSpan().CopyInto<TextStringHandlerBuffer>(out _);
        var ret = InputText(label, TextStringHandlerBuffer.Span, hint, flags);
        if (ImGui.IsItemEdited())
            text = MemoryHelper.ReadStringNullTerminated((nint)TextStringHandlerBuffer.Buffer);
        return ret;
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, ref string text, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), ref text, hint, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text, ReadOnlySpan<byte> hint = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), ref text, hint, flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, ref string text, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, ref text, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, ref string text, ReadOnlySpan<char> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), ref text, hint.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a UTF16 string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref string, ReadOnlySpan{byte}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text,
        ReadOnlySpan<char> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), ref text, hint.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{byte}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<byte> label, ref string text, ref Utf8StringHandler<HintStringHandlerBuffer> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label, ref text, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ReadOnlySpan{char}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ReadOnlySpan<char> label, ref string text, ref Utf8StringHandler<HintStringHandlerBuffer> hint,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span<LabelStringHandlerBuffer>(), ref text, hint.Span(), flags);

    /// <param name="hint"> A hint to display in the input box as long as the input is empty as a formatted string. </param>
    /// <inheritdoc cref="InputText(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref string, ReadOnlySpan{char}, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputText(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref string text,
        ref Utf8StringHandler<HintStringHandlerBuffer> hint, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        => InputText(label.Span(), ref text, hint.Span(), flags);

    #endregion

    /// <summary> Handle the hint. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool InputText(ReadOnlySpan<byte> label, Span<byte> buffer, ReadOnlySpan<byte> hint, ImGuiInputTextFlags flags)
        => hint.Length > 0
            ? ImGuiNative.igInputTextWithHint(label.Start(), hint.Start(), buffer.Start(), (uint)buffer.Length, flags, null!, null).Bool()
            : ImGuiNative.igInputText(label.Start(), buffer.Start(), (uint)buffer.Length, flags, null!, null).Bool();
}
