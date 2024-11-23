using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a vertical slider for numerical values. </summary>
    /// <typeparam name="T"> The type of the numeric value to change. </typeparam>
    /// <param name="label"> The slider label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The numeric input/output value. </param>
    /// <param name="size"> The desired size for the slider. </param>
    /// <param name="format"> The printf format-string to display the number in as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="minValue"> The minimum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="maxValue"> The maximum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="flags"> Additional flags controlling the sliders behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<byte> label, ref T value, Vector2 size, ReadOnlySpan<byte> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => ImGuiNative.igVSliderScalar(label.Start(), size, Type<T>(), Unsafe.AsPointer(ref value), &minValue, &maxValue, format.Start(), flags)
            .Bool();

    /// <param name="label"> The slider label as a UTF16 string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{byte},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<char> label, ref T value, Vector2 size, ReadOnlySpan<byte> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span<LabelStringHandlerBuffer>(), ref value, size, format, minValue, maxValue, flags);

    /// <param name="label"> The slider label as a labeled string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{char},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, Vector2 size,
        ReadOnlySpan<byte> format, T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span(), ref value, size, format, minValue, maxValue, flags);


    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{byte},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<byte> label, ref T value, Vector2 size, ReadOnlySpan<char> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label, ref value, size, format.Span<HintStringHandlerBuffer>(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{char},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<char> label, ref T value, Vector2 size, ReadOnlySpan<char> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span<LabelStringHandlerBuffer>(), ref value, size, format.Span<HintStringHandlerBuffer>(), minValue, maxValue,
            flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, Vector2 size,
        ReadOnlySpan<char> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span(), ref value, size, format.Span<HintStringHandlerBuffer>(), minValue, maxValue, flags);


    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{byte},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<byte> label, ref T value, Vector2 size,
        ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label, ref value, size, format.Span(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{char},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<char> label, ref T value, Vector2 size,
        ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span<LabelStringHandlerBuffer>(), ref value, size, format.Span(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="SliderVertical{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, Vector2 size,
        ref Utf8StringHandler<LabelStringHandlerBuffer> format, T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>
        => SliderVertical(label.Span(), ref value, size, format.Span(), minValue, maxValue, flags);


    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{byte},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<byte> label, ref T value, Vector2 size, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label, ref value, size, DefaultSliderFormat<T>(), minValue, maxValue, flags);

    /// <inheritdoc cref="SliderVertical{T}(ReadOnlySpan{char},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ReadOnlySpan<char> label, ref T value, Vector2 size, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span<LabelStringHandlerBuffer>(), ref value, size, minValue, maxValue, flags);

    /// <inheritdoc cref="SliderVertical{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,Vector2, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SliderVertical<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, Vector2 size,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => SliderVertical(label.Span(), ref value, size, minValue, maxValue, flags);
}
