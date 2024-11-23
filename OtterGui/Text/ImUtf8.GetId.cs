using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Calculate the current ID for an object with the given label. </summary>
    /// <param name="label"> The object's label as a UTF8 string. Does not have to be null-terminated. </param>
    /// <returns> The ImGui ID of the label in the current ID stack. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetId(ReadOnlySpan<byte> label)
        => ImGuiNative.igGetID_StrStr(label.Start(out var end), end);

    /// <param name="label"> The object's label as a UTF16 string. </param>
    /// <inheritdoc cref="GetId(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetId(ReadOnlySpan<char> label)
        => GetId(label.Span<LabelStringHandlerBuffer>());

    /// <param name="label"> The object's label as format string. </param>
    /// <inheritdoc cref="GetId(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetId(ref Utf8StringHandler<LabelStringHandlerBuffer> label)
        => GetId(label.Span());

    /// <summary> Calculate the current ID for an object with the given label and return the cloned transcoded text. </summary>
    /// <param name="utf8"> The transcoded text as null-terminated UTF8. </param>
    /// <inheritdoc cref="GetId(ReadOnlySpan{char})"/>
    /// <remarks> Only use this if the transcoding is expected to be more expensive than the allocation of a clone. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetId(ReadOnlySpan<char> label, out TerminatedByteString utf8)
    {
        utf8 = label.Span<LabelStringHandlerBuffer>().CloneNullTerminated();
        return GetId(utf8);
    }

    /// <summary> Calculate the current ID for an object with the given label and return the cloned formatted text. </summary>
    /// <param name="formatted"> The formatted text as null-terminated UTF8. </param>
    /// <inheritdoc cref="GetId(ref Utf8StringHandler{LabelStringHandlerBuffer})"/>
    /// <remarks> Only use this if the formatting is expected to be more expensive than the allocation of a clone. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint GetId(ref Utf8StringHandler<LabelStringHandlerBuffer> label, out TerminatedByteString formatted)
    {
        formatted = label.Span().CloneNullTerminated();
        return GetId(formatted);
    }
}
