using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Calculate the required size to display the given text. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="hideTextAfterDashes"> Whether everything after the first ## is to be included or not. </param>
    /// <param name="wrapWidth"> The text wrap width to use for wrapping. 0 uses the current wrapping position, if any. </param>
    /// <returns> The required size to display the text. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 CalcTextSize(ReadOnlySpan<byte> text, bool hideTextAfterDashes = true, float wrapWidth = 0)
    {
        var ret = Vector2.Zero;
        ImGuiNative.igCalcTextSize(&ret, text.Start(out var end), end, hideTextAfterDashes.Byte(), wrapWidth);
        return ret;
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="CalcTextSize(ReadOnlySpan{byte}, bool, float)" />
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 CalcTextSize(ReadOnlySpan<char> text, bool hideTextAfterDashes = true, float wrapWidth = 0)
        => CalcTextSize(text.Span<TextStringHandlerBuffer>(), hideTextAfterDashes, wrapWidth);

    /// <param name="text"> The given text as a format string. </param>
    /// <inheritdoc cref="CalcTextSize(ReadOnlySpan{char}, bool, float)" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 CalcTextSize(ref Utf8StringHandler<TextStringHandlerBuffer> text, bool hideTextAfterDashes = true,
        float wrapWidth = 0)
        => CalcTextSize(text.Span(), hideTextAfterDashes, wrapWidth);

    /// <summary> Calculate the required size to display the given text and return the cloned transcoded text. </summary>
    /// <param name="formatted"> The transcoded text as null-terminated UTF8. </param>
    /// <inheritdoc cref="CalcTextSize(ReadOnlySpan{char}, bool, float)" />
    /// <remarks> Only use this if the transcoding is expected to be more expensive than the allocation of a clone. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 CalcTextSize(ReadOnlySpan<char> text, out TerminatedByteString formatted, bool hideTextAfterDashes = true, float wrapWidth = 0)
    {
        formatted = text.Span<TextStringHandlerBuffer>().CloneNullTerminated();
        return CalcTextSize(formatted, hideTextAfterDashes, wrapWidth);
    }

    /// <summary> Calculate the required size to display the given text and return the cloned formatted text. </summary>
    /// <param name="formatted"> The formatted text as null-terminated UTF8. </param>
    /// <inheritdoc cref="CalcTextSize(ref Utf8StringHandler{TextStringHandlerBuffer}, bool, float)" />
    /// <remarks> Only use this if the formatting is expected to be more expensive than the allocation of a clone. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 CalcTextSize(ref Utf8StringHandler<TextStringHandlerBuffer> text, out TerminatedByteString formatted,
        bool hideTextAfterDashes = true, float wrapWidth = 0)
    {
        formatted = text.Span().CloneNullTerminated();
        return CalcTextSize(formatted, hideTextAfterDashes, wrapWidth);
    }
}
