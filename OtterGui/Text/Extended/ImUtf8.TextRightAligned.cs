using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

public static partial class ImUtf8
{
    /// <summary> Draw text aligned to the right of the current content region. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="offset"> Optional additional offset from the right of the available region. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextRightAligned(ReadOnlySpan<byte> text, float offset = 0)
    {
        var size      = CalcTextSize(text);
        var available = ImGui.GetContentRegionMax().X;
        ImGui.SetCursorPosX(available - size.X - offset);
        Text(text);
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextRightAligned(ReadOnlySpan{byte}, float)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextRightAligned(ReadOnlySpan<char> text, float offset = 0)
        => TextRightAligned(text.Span<TextStringHandlerBuffer>());

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="TextRightAligned(ReadOnlySpan{char}, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextRightAligned(ref Utf8StringHandler<TextStringHandlerBuffer> text, float offset = 0)
        => TextRightAligned(text.Span());
}
