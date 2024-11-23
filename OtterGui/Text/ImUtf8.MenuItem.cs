using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573
public static unsafe partial class ImUtf8
{
    /// <summary> Draw a context menu selectable. </summary>
    /// <param name="text"> The menu item text as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="shortcut"> The keyboard shortcut for this menu item as a UTF8 string. HAS to be null-terminated or empty. This is only displayed, not evaluated. </param>
    /// <param name="selected"> Whether the menu item is currently selected or not. </param>
    /// <param name="enabled"> Whether the menu item is enabled or not. </param>
    /// <returns> True when the menu item was activated this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<byte> text, ReadOnlySpan<byte> shortcut, bool selected = false, bool enabled = true)
        => ImGuiNative.igMenuItem_Bool(text.Start(), shortcut.Length == 0 || shortcut[0] == '\0' ? shortcut.Start() : null, selected.Byte(),
            enabled.Byte()).Bool();

    /// <param name="text"> The menu label as a UTF16 string. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{byte},ReadOnlySpan{byte},bool,bool)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<char> text, ReadOnlySpan<byte> shortcut, bool selected = false, bool enabled = true)
        => MenuItem(text.Span<LabelStringHandlerBuffer>(), shortcut, selected, enabled);

    /// <param name="text"> The menu label as a formatted string. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{char},ReadOnlySpan{byte},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ref Utf8StringHandler<LabelStringHandlerBuffer> text, ReadOnlySpan<byte> shortcut, bool selected = false,
        bool enabled = true)
        => MenuItem(text.Span(), shortcut, selected, enabled);


    /// <param name="shortcut"> The keyboard shortcut for this menu item as a UTF16 string. This is only displayed, not evaluated. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{byte},ReadOnlySpan{byte},bool,bool)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<byte> text, ReadOnlySpan<char> shortcut, bool selected = false, bool enabled = true)
        => MenuItem(text, shortcut.Span<HintStringHandlerBuffer>(), selected, enabled);

    /// <param name="shortcut"> The keyboard shortcut for this menu item as a UTF16 string. This is only displayed, not evaluated. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{char},ReadOnlySpan{byte},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<char> text, ReadOnlySpan<char> shortcut, bool selected = false, bool enabled = true)
        => MenuItem(text.Span<LabelStringHandlerBuffer>(), shortcut.Span<HintStringHandlerBuffer>(), selected, enabled);

    /// <param name="shortcut"> The keyboard shortcut for this menu item as a UTF16 string. This is only displayed, not evaluated. </param>
    /// <inheritdoc cref="MenuItem(ref Utf8StringHandler{LabelStringHandlerBuffer},ReadOnlySpan{byte},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ref Utf8StringHandler<LabelStringHandlerBuffer> text, ReadOnlySpan<char> shortcut, bool selected = false,
        bool enabled = true)
        => MenuItem(text.Span(), shortcut.Span<HintStringHandlerBuffer>(), selected, enabled);


    /// <param name="shortcut"> The keyboard shortcut for this menu item as a formatted string. This is only displayed, not evaluated. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{byte},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<byte> text, ref Utf8StringHandler<HintStringHandlerBuffer> shortcut, bool selected = false,
        bool enabled = true)
        => MenuItem(text, shortcut.Span(), selected, enabled);

    /// <param name="text"> The menu label as a UTF16 string. </param>
    /// <inheritdoc cref="MenuItem(ReadOnlySpan{char},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<char> text, ref Utf8StringHandler<HintStringHandlerBuffer> shortcut, bool selected = false,
        bool enabled = true)
        => MenuItem(text.Span<LabelStringHandlerBuffer>(), shortcut.Span(), selected, enabled);

    /// <param name="text"> The menu label as a formatted string. </param>
    /// <inheritdoc cref="MenuItem(ref Utf8StringHandler{LabelStringHandlerBuffer},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ref Utf8StringHandler<LabelStringHandlerBuffer> text, ref Utf8StringHandler<HintStringHandlerBuffer> shortcut,
        bool selected = false, bool enabled = true)
        => MenuItem(text.Span(), shortcut.Span(), selected, enabled);


    /// <inheritdoc cref="MenuItem(ReadOnlySpan{byte},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<byte> text, bool selected = false, bool enabled = true)
        => MenuItem(text, ""u8, selected, enabled);

    /// <inheritdoc cref="MenuItem(ReadOnlySpan{char},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ReadOnlySpan<char> text, bool selected = false, bool enabled = true)
        => MenuItem(text.Span<LabelStringHandlerBuffer>(), ""u8, selected, enabled);

    /// <inheritdoc cref="MenuItem(ref Utf8StringHandler{LabelStringHandlerBuffer},ReadOnlySpan{char},bool,bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool MenuItem(ref Utf8StringHandler<LabelStringHandlerBuffer> text, bool selected = false, bool enabled = true)
        => MenuItem(text.Span(), ""u8, selected, enabled);
}
