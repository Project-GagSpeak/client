using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

public static partial class ImUtf8
{
    /// <summary> Draw automatically wrapped text. </summary>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="wrapPos"> Optional position for wrapping. 0 uses available content region. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextWrapped(ReadOnlySpan<byte> text, float wrapPos = 0)
    {
        ImGui.PushTextWrapPos(wrapPos);
        try
        {
            Text(text);
        }
        finally
        {
            ImGui.PopTextWrapPos();
        }
    }

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="TextWrapped(ReadOnlySpan{byte}, float)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextWrapped(ReadOnlySpan<char> text, float wrapPos = 0)
        => TextWrapped(text.Span<TextStringHandlerBuffer>(), wrapPos);

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="TextWrapped(ReadOnlySpan{char}, float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TextWrapped(ref Utf8StringHandler<TextStringHandlerBuffer> text, float wrapPos = 0)
        => TextWrapped(text.Span(), wrapPos);
}
