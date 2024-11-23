using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw an editing panel for a given color. </summary>
    /// <param name="label"> The label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="color"> The input / output color value without Alpha channel. </param>
    /// <param name="flags"> Additional flags controlling the editing panel. </param>
    /// <returns> True if the color has been changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<byte> label, ref Vector3 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ImGuiNative.igColorEdit3(label.Start(), (Vector3*)Unsafe.AsPointer(ref color), flags).Bool();

    /// <param name="label"> The panel label as a UTF16 string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{byte}, ref Vector3, ImGuiColorEditFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<char> label, ref Vector3 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <param name="label"> The panel label as a formatted string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{char}, ref Vector3, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Vector3 color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span(), ref color, flags);


    /// <param name="color"> The input / output color value with an Alpha channel. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{byte}, ref Vector3, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<byte> label, ref Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ImGuiNative.igColorEdit4(label.Start(), (Vector4*)Unsafe.AsPointer(ref color), flags).Bool();

    /// <param name="label"> The panel label as a UTF16 string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{byte}, ref Vector4, ImGuiColorEditFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<char> label, ref Vector4 color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <param name="label"> The panel label as a formatted string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{char}, ref Vector4, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Vector4 color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span(), ref color, flags);


    /// <param name="color"> The input / output color value with an Alpha channel as RGBA32. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{byte}, ref Vector3, ImGuiColorEditFlags)"/>
    /// <remarks> This does not allow for HDR color values. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<byte> label, ref uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
    {
        var value = ImGui.ColorConvertU32ToFloat4(color);
        if (!ImGuiNative.igColorEdit4(label.Start(), (Vector4*)Unsafe.AsPointer(ref value), flags).Bool())
            return false;

        color = ImGui.ColorConvertFloat4ToU32(value);
        return true;
    }

    /// <param name="label"> The panel label as a UTF16 string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{byte}, ref uint, ImGuiColorEditFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ReadOnlySpan<char> label, ref uint color, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span<LabelStringHandlerBuffer>(), ref color, flags);

    /// <param name="label"> The panel label as a formatted string. </param>
    /// <inheritdoc cref="ColorEdit(ReadOnlySpan{char}, ref uint, ImGuiColorEditFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ColorEdit(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref uint color,
        ImGuiColorEditFlags flags = ImGuiColorEditFlags.None)
        => ColorEdit(label.Span(), ref color, flags);
}
