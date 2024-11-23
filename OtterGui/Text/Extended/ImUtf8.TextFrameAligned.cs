using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

public static partial class ImUtf8
{
    /// <summary> Draw text vertically aligned to the current frame padding. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextFrameAligned(ReadOnlySpan<byte> text)
    {
        ImGui.AlignTextToFramePadding();
        Text(text);
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextFrameAligned(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextFrameAligned(ReadOnlySpan<char> text)
        => TextFrameAligned(text.Span<TextStringHandlerBuffer>());

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="TextFrameAligned(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextFrameAligned(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => TextFrameAligned(text.Span());
}
