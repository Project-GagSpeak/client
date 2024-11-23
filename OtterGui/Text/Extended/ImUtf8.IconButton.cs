using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

// ReSharper disable MethodOverloadWithOptionalParameter

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Draw an icon button of frame height and width if not specified otherwise. </summary>
    /// <param name="icon"> The icon to draw. </param>
    /// <param name="tooltip"> A tooltip to show when hovering the button as a UTF8 string. Does not have to be null-terminated. Nothing will be displayed for an empty span. </param>
    /// <param name="size"> The size of the button. </param>
    /// <param name="disabled"> Whether the button should be disabled. </param>
    /// <param name="textColor"> The text color inside the button. No color is pushed for 0. </param>
    /// <param name="buttonColor"> The background color inside the button. No color is pushed for 0. </param>
    /// <returns> Whether the button was clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ReadOnlySpan<byte> tooltip, Vector2 size = default, bool disabled = false,
        uint textColor = 0, uint buttonColor = 0)
    {
        if (size.X == 0)
            size.X = FrameHeight;
        if (size.Y == 0)
            size.Y = FrameHeight;

        bool ret;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, textColor, textColor != 0)
                .Push(ImGuiCol.Button, buttonColor, buttonColor != 0);
            using var _ = ImRaii.Disabled(disabled);
            ret = Button(icon.Bytes().Span, size);
        }

        HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, tooltip);
        return ret;
    }

    /// <param name="tooltip"> A tooltip to show when hovering the button as a UTF16 string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{byte},Vector2,bool,uint,uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ReadOnlySpan<char> tooltip, Vector2 size = default, bool disabled = false,
        uint textColor = 0, uint buttonColor = 0)
        => IconButton(icon, tooltip.Span<TextStringHandlerBuffer>(), size, disabled, textColor, buttonColor);

    /// <param name="tooltip"> A tooltip to show when hovering the button as a formatted string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{char},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ref Utf8StringHandler<TextStringHandlerBuffer> tooltip, Vector2 size = default,
        bool disabled = false, uint textColor = 0, uint buttonColor = 0)
        => IconButton(icon, tooltip.Span(), size, disabled, textColor, buttonColor);


    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{byte},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ReadOnlySpan<byte> tooltip, Vector2 size)
    {
        if (size.X == 0)
            size.X = FrameHeight;
        if (size.Y == 0)
            size.Y = FrameHeight;
        bool ret;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            ret = Button(icon.Bytes().Span, size);
        }

        HoverTooltip(ImGuiHoveredFlags.AllowWhenDisabled, tooltip);
        return ret;
    }

    /// <param name="tooltip"> A tooltip to show when hovering the button as a formatted string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{char},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ReadOnlySpan<char> tooltip, Vector2 size = default)
        => IconButton(icon, tooltip.Span<TextStringHandlerBuffer>(), size);

    /// <param name="tooltip"> A tooltip to show when hovering the button as a UTF16 string. Nothing will be displayed for an empty span. </param>
    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ref Utf8StringHandler{TextStringHandlerBuffer},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, ref Utf8StringHandler<TextStringHandlerBuffer> tooltip, Vector2 size = default)
        => IconButton(icon, tooltip.Span(), size);


    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{byte},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, Vector2 size = default)
    {
        if (size.X == 0)
            size.X = FrameHeight;
        if (size.Y == 0)
            size.Y = FrameHeight;
        using (ImRaii.PushFont(UiBuilder.IconFont))
        {
            return Button(icon.Bytes().Span, size);
        }
    }

    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{byte},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, bool disabled, Vector2 size = default)
    {
        using var dis = ImRaii.Disabled(disabled);
        return IconButton(icon, size);
    }

    /// <inheritdoc cref="IconButton(FontAwesomeIcon,ReadOnlySpan{byte},Vector2,bool,uint,uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IconButton(FontAwesomeIcon icon, bool disabled, Vector2 size, uint textColor = 0, uint buttonColor = 0)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Text, textColor, textColor != 0)
            .Push(ImGuiCol.Button, buttonColor, buttonColor != 0);
        return IconButton(icon, disabled, size);
    }
}

internal static class FontAwesomeExtensions
{
    internal unsafe struct IconBuffer
    {
        private ulong _buffer;

        public IconBuffer(FontAwesomeIcon icon)
        {
            var   iconChar = icon.ToIconChar();
            ulong tmp      = 0;
            var   bytes    = (byte)Encoding.UTF8.GetBytes(&iconChar, 1, (byte*)&tmp, 8);
            _buffer = tmp | ((ulong)bytes << 40);
        }

        public ReadOnlySpan<byte> Span
        {
            get
            {
                fixed (ulong* ptr = &_buffer)
                {
                    return new Span<byte>(ptr, (int)(_buffer >> 40));
                }
            }
        }
    }

    internal static IconBuffer Bytes(this FontAwesomeIcon icon)
        => new(icon);
}
