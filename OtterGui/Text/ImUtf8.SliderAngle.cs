using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a slider specifically for angles in degrees radians. </summary>
    /// <param name="label"> The slider label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="degreeRadians"> The angle in degrees radians. </param>
    /// <param name="format"> The printf format-string to display the degrees in as a UTF8 string. HAS to be null-terminated. Default is '%.0f deg' </param>
    /// <param name="minDegrees"> The minimum value for the degrees. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="maxDegrees"> The maximum value for the degrees. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="flags"> Additional flags controlling the sliders behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<byte> label, ref float degreeRadians, ReadOnlySpan<byte> format, float minDegrees = -360f,
        float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => ImGuiNative.igSliderAngle(label.Start(), (float*)Unsafe.AsPointer(ref degreeRadians), minDegrees, maxDegrees, format.Start(), flags)
            .Bool();

    /// <param name="label"> The slider label as a UTF16 string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{byte},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<char> label, ref float degreeRadians, ReadOnlySpan<byte> format, float minDegrees = -360f,
        float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span<LabelStringHandlerBuffer>(), ref degreeRadians, format, minDegrees, maxDegrees, flags);

    /// <param name="label"> The slider label as a formatted string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{char},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref float degreeRadians, ReadOnlySpan<byte> format,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span(), ref degreeRadians, format, minDegrees, maxDegrees, flags);


    /// <param name="format"> The printf format-string to display the degrees in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{byte},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<byte> label, ref float degreeRadians, ReadOnlySpan<char> format,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label, ref degreeRadians, format.Span<HintStringHandlerBuffer>(), minDegrees, maxDegrees, flags);

    /// <param name="format"> The printf format-string to display the degrees in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{char},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<char> label, ref float degreeRadians, ReadOnlySpan<char> format,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span<LabelStringHandlerBuffer>(), ref degreeRadians, format.Span<HintStringHandlerBuffer>(), minDegrees,
            maxDegrees,                                        flags);

    /// <param name="format"> The printf format-string to display the degrees in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderAngle(ref Utf8StringHandler{LabelStringHandlerBuffer},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref float degreeRadians,
        ReadOnlySpan<char> format, float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span(), ref degreeRadians, format.Span<HintStringHandlerBuffer>(), minDegrees, maxDegrees, flags);


    /// <param name="format"> The printf format-string to display the degrees in as a formatted string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{byte},ref float, ReadOnlySpan{char}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<byte> label, ref float degreeRadians, ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label, ref degreeRadians, format.Span(), minDegrees, maxDegrees, flags);

    /// <param name="format"> The printf format-string to display the degrees in as a formatted string. </param>
    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{char},ref float, ReadOnlySpan{char}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<char> label, ref float degreeRadians, ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span<LabelStringHandlerBuffer>(), ref degreeRadians, format.Span(), minDegrees, maxDegrees, flags);

    /// <param name="format"> The printf format-string to display the degrees in as a formatted string. </param>
    /// <inheritdoc cref="SliderAngle(ref Utf8StringHandler{LabelStringHandlerBuffer},ref float, ReadOnlySpan{char}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref float degreeRadians,
        ref Utf8StringHandler<LabelStringHandlerBuffer> format, float minDegrees = -360f, float maxDegrees = 360f,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span(), ref degreeRadians, format.Span(), minDegrees, maxDegrees, flags);


    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{byte},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<byte> label, ref float degreeRadians, float minDegrees = -360f, float maxDegrees = 360f,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label, ref degreeRadians, "%.0f deg"u8, minDegrees, maxDegrees, flags);

    /// <inheritdoc cref="SliderAngle(ReadOnlySpan{char},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ReadOnlySpan<char> label, ref float degreeRadians,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span<LabelStringHandlerBuffer>(), ref degreeRadians, minDegrees, maxDegrees, flags);

    /// <inheritdoc cref="SliderAngle(ref Utf8StringHandler{LabelStringHandlerBuffer},ref float, ReadOnlySpan{byte}, float, float, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderAngle(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref float degreeRadians,
        float minDegrees = -360f, float maxDegrees = 360f, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        => SliderAngle(label.Span(), ref degreeRadians, minDegrees, maxDegrees, flags);
}
