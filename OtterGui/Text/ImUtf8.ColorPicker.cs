using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a color button opening a color edit popup for a given color. </summary>
    /// <param name="label"> The label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="color"> The input / output color value without Alpha channel. </param>
    /// <param name="flags"> Additional flags controlling the button display and editing popup. </param>
    /// <returns> True if the color has been changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<byte> label, ref Vector3 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ImGuiNative.igColorPicker3(label.Start(), (Vector3*)Unsafe.AsPointer(ref color), flags).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref Vector3, ImGuiColorEditFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<char> label, ref Vector3 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <param name="label"> The button label as a formatted string. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{char}, ref Vector3, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Vector3 color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span(), ref color, flags);


    /// <param name="color"> The input / output color value with an Alpha channel. </param>
    /// <param name="referenceColor"> An optional original color that can be displayed as a base value. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref Vector3, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<byte> label, ref Vector4 color, Vector4 referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ImGuiNative.igColorPicker4(label.Start(), (Vector4*)Unsafe.AsPointer(ref color), flags, (float*)&referenceColor).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<char> label, ref Vector4 color, Vector4 referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span<LabelStringHandlerBuffer>(), ref color, referenceColor, flags);

    /// <param name="label"> The button label as a formatted string. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{char}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Vector4 color, Vector4 referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span(), ref color, referenceColor, flags);


    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<byte> label, ref Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ImGuiNative.igColorPicker4(label.Start(), (Vector4*)Unsafe.AsPointer(ref color), flags, null).Bool();

    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{char}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<char> label, ref Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <inheritdoc cref="ColorPicker(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Vector4 color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span(), ref color, flags);


    /// <param name="color"> The input / output color value with an Alpha channel as RGBA32. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<byte> label, ref uint color, uint referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        var value = ImGui.ColorConvertU32ToFloat4(color);
        if (!ColorPicker(label, ref value, ImGui.ColorConvertU32ToFloat4(referenceColor), flags))
            return false;

        color = ImGui.ColorConvertFloat4ToU32(value);
        return true;
    }

    /// <param name="color"> The input / output color value with an Alpha channel as RGBA32. </param>
    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{char}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<char> label, ref uint color, uint referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span<LabelStringHandlerBuffer>(), ref color, referenceColor, flags);

    /// <param name="color"> The input / output color value with an Alpha channel as RGBA32. </param>
    /// <inheritdoc cref="ColorPicker(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref Vector4, Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref uint color, uint referenceColor,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span(), ref color, referenceColor, flags);


    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{byte}, ref uint, uint, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<byte> label, ref uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        var value = ImGui.ColorConvertU32ToFloat4(color);
        if (!ColorPicker(label, ref value, flags))
            return false;

        color = ImGui.ColorConvertFloat4ToU32(value);
        return true;
    }

    /// <inheritdoc cref="ColorPicker(ReadOnlySpan{char}, ref uint, uint, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ReadOnlySpan<char> label, ref uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <inheritdoc cref="ColorPicker(ref Utf8StringHandler{LabelStringHandlerBuffer}, ref uint, uint, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorPicker(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref uint color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorPicker(label.Span(), ref color, flags);
}
