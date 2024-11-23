using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

public static partial class ImUtf8
{
    /// <summary> Draw the given text centered in the current content region. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    public static void TextCentered(ReadOnlySpan<byte> text)
    {
        var size      = CalcTextSize(text);
        var available = ImGui.GetContentRegionMax().X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (available - size.X) / 2);
        Text(text);
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextCentered(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextCentered(ReadOnlySpan<char> text)
        => TextCentered(text.Span<TextStringHandlerBuffer>());

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextCentered(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextCentered(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => TextCentered(text.Span());
}
