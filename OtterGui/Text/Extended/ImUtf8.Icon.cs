using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

#pragma warning disable CS1573

public static partial class ImUtf8
{
    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{byte}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon)
    {
        using var _ = ImRaii.PushFont(UiBuilder.IconFont);
        Text(icon.Bytes().Span);
    }

    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{byte}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, uint textColor)
    {
        using var _ = ImRaii.PushFont(UiBuilder.IconFont);
        Text(icon.Bytes().Span, textColor);
    }

    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{byte}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ReadOnlySpan<byte> tooltip)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span);
        }

        HoverTooltip(tooltip);
    }

    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{char}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ReadOnlySpan<char> tooltip)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span);
        }

        HoverTooltip(tooltip);
    }

    /// <inheritdoc cref="Icon(FontAwesomeIcon, ref Utf8StringHandler{TextStringHandlerBuffer}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ref Utf8StringHandler<TextStringHandlerBuffer> tooltip)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span);
        }

        HoverTooltip(tooltip.Span());
    }


    /// <summary> Draw a text icon. </summary>
    /// <param name="icon"> The icon to draw. </param>
    /// <param name="tooltip"> A tooltip to show when hovering the icon as a UTF8 string. Does not have to be null-terminated. Nothing will be displayed for an empty span. </param>
    /// <param name="textColor"> The text color of the icon. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ReadOnlySpan<byte> tooltip, uint textColor)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span, textColor);
        }

        HoverTooltip(tooltip);
    }

    /// <param name="tooltip"> A tooltip to show when hovering the icon as a UTF16 string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{byte}, uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ReadOnlySpan<char> tooltip, uint textColor)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span, textColor);
        }

        HoverTooltip(tooltip);
    }

    /// <param name="tooltip"> A tooltip to show when hovering the icon as a formatted string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="Icon(FontAwesomeIcon, ReadOnlySpan{char}, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Icon(FontAwesomeIcon icon, ref Utf8StringHandler<TextStringHandlerBuffer> tooltip, uint textColor)
    {
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            Text(icon.Bytes().Span, textColor);
        }

        HoverTooltip(tooltip.Span());
    }
}
