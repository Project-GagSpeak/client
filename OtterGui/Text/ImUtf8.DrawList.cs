using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw text into a specific position in a draw list with a given font, color and wrapping.  </summary>
    /// <param name="drawList"> The draw list. </param>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="font"> The font to use. </param>
    /// <param name="fontSize"> The font size to use. </param>
    /// <param name="pos"> The position to start drawing the text at. </param>
    /// <param name="color"> The color of the text. Uses the current default text color if this is 0. </param>
    /// <param name="wrapWidth"> The wrap width for the text. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ReadOnlySpan<byte> text, ImFontPtr font, float fontSize, Vector2 pos,
        uint color = 0, float wrapWidth = 0)
        => ImGuiNative.ImDrawList_AddText_FontPtr(drawList.NativePtr, font.NativePtr, fontSize, pos,
            color == 0 ? ImGui.GetColorU32(ImGuiCol.Text) : color, text.Start(out var end), end, wrapWidth,
            null);

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="AddText(ImDrawListPtr,ReadOnlySpan{byte},ImFontPtr,float,Vector2,uint,float)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ReadOnlySpan<char> text, ImFontPtr font, float fontSize, Vector2 pos,
        uint color = 0, float wrapWidth = 0)
        => AddText(drawList, text.Span<TextStringHandlerBuffer>(), font, fontSize, pos, color, wrapWidth);

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="AddText(ImDrawListPtr,ReadOnlySpan{char},ImFontPtr,float,Vector2,uint,float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ref Utf8StringHandler<TextStringHandlerBuffer> text, ImFontPtr font, float fontSize,
        Vector2 pos, uint color = 0, float wrapWidth = 0)
        => AddText(drawList, text.Span(), font, fontSize, pos, color, wrapWidth);


    /// <summary> Draw text into a specific position in a draw list in a given color.  </summary>
    /// <param name="drawList"> The draw list. </param>
    /// <param name="text"> The given text as a UTF8 string. Does not have to be null-terminated. </param>
    /// <param name="pos"> The position to start drawing the text at. </param>
    /// <param name="color"> The color of the text. Uses the current default text color if this is 0. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ReadOnlySpan<byte> text, Vector2 pos, uint color = 0)
        => ImGuiNative.ImDrawList_AddText_Vec2(drawList.NativePtr, pos, color == 0 ? ImGui.GetColorU32(ImGuiCol.Text) : color, text.Start(out var end), end);

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="AddText(ImDrawListPtr,ReadOnlySpan{byte},Vector2,uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ReadOnlySpan<char> text, Vector2 pos, uint color = 0)
        => AddText(drawList, text.Span<TextStringHandlerBuffer>(), pos, color);

    /// <param name="text"> The given text as a formatted string. </param>
    /// <inheritdoc cref="AddText(ImDrawListPtr,ReadOnlySpan{char},Vector2,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddText(this ImDrawListPtr drawList, ref Utf8StringHandler<TextStringHandlerBuffer> text, Vector2 pos, uint color)
        => AddText(drawList, text.Span(), pos, color);
}
