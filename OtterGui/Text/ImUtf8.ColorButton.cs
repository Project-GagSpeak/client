using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw the given color button. </summary>
    /// <param name="description"> The tooltip description and ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="color"> The color the button should have. </param>
    /// <param name="size"> The desired size for the button. If (0, 0), it will be a square of frame-height. </param>
    /// <param name="flags"> Additional flags for the color information displayed on hover. </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    /// <remarks> The color button is a rectangle of the given size and color. On hover, it will display color information and the given description. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ReadOnlySpan<byte> description, Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None,
        Vector2 size = default)
        => ImGuiNative.igColorButton(description.Start(), color, flags, size).Bool();

    /// <param name="description"> The tooltip description and ID as a UTF16 string. </param>
    /// <inheritdoc cref="ColorButton(ReadOnlySpan{byte}, Vector4, ImGuiColorEditFlags, Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ReadOnlySpan<char> description, Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None,
        Vector2 size = default)
        => ColorButton(description.Span<LabelStringHandlerBuffer>(), color, flags, size);

    /// <param name="description"> The tooltip description and ID as a format string. </param>
    /// <inheritdoc cref="ColorButton(ReadOnlySpan{char}, Vector4, ImGuiColorEditFlags, Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ref Utf8StringHandler<LabelStringHandlerBuffer> description, Vector4 color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector2 size = default)
        => ColorButton(description.Span(), color, flags, size);


    /// <param name="color"> The color the button should have as RGBA32. </param>
    /// <inheritdoc cref="ColorButton(ReadOnlySpan{byte}, Vector4, ImGuiColorEditFlags, Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ReadOnlySpan<byte> description, uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None,
        Vector2 size = default)
        => ImGuiNative.igColorButton(description.Start(), ImGui.ColorConvertU32ToFloat4(color), flags, size).Bool();

    /// <param name="description"> The tooltip description and ID as a UTF16 string. </param>
    /// <inheritdoc cref="ColorButton(ReadOnlySpan{byte}, uint, ImGuiColorEditFlags, Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ReadOnlySpan<char> description, uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None,
        Vector2 size = default)
        => ColorButton(description.Span<LabelStringHandlerBuffer>(), color, flags, size);

    /// <param name="description"> The tooltip description and ID as a format string. </param>
    /// <inheritdoc cref="ColorButton(ReadOnlySpan{char}, uint, ImGuiColorEditFlags, Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorButton(ref Utf8StringHandler<LabelStringHandlerBuffer> description, uint color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None, Vector2 size = default)
        => ColorButton(description.Span(), color, flags, size);
}
