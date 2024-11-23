using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a drag slider, i.e. a button you can hold and drag to the side to change the value. </summary>
    /// <typeparam name="T"> The type of the numeric value to change. </typeparam>
    /// <param name="label"> The slider label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The numeric input/output value. </param>
    /// <param name="format"> The printf format-string to display the number in as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="min"> The minimum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="max"> The maximum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="speed"> The dragging speed. </param>
    /// <param name="flags"> Additional flags controlling the sliders behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    /// <remarks> Ctrl-clicking into a drag slider allows manual keyboard input for the number. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<byte> format, T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => ImGuiNative.igDragScalar(label.Start(), Type<T>(), Unsafe.AsPointer(ref value), speed, &min, &max, format.Start(), flags).Bool();

    /// <param name="label"> The slider label as a UTF16 string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{byte},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<byte> format, T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format, min, max, speed, flags);

    /// <param name="label"> The slider label as a formatted string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{char},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<byte> format,
        T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span(), ref value, format, min, max, speed, flags);


    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{byte},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<byte> label, ref T value, T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label, ref value, DefaultSliderFormat<T>(), min, max, speed, flags);

    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{char},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<char> label, ref T value, T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span<LabelStringHandlerBuffer>(), ref value, min, max, speed, flags);

    /// <inheritdoc cref="DragScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T min, T max, float speed = 1,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span(), ref value, min, max, speed, flags);


    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{byte},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<char> format,
        T min, T max, float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label, ref value, format.Span<HintStringHandlerBuffer>(), min, max, speed, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{char},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<char> format, T min,
        T max,
        float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span(), ref value, format.Span<HintStringHandlerBuffer>(), min, max, speed, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="DragScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<char> label, ref T value, ref ReadOnlySpan<char> format,
        T min, T max, float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span<HintStringHandlerBuffer>(), min, max, speed, flags);


    /// <param name="format"> The printf format-string to display the number in as a format string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{byte},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<byte> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format, T min, T max,
        float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label, ref value, format.Span(), min, max, speed, flags);

    /// <param name="format"> The printf format-string to display the number in as a format string. </param>
    /// <inheritdoc cref="DragScalar{T}(ReadOnlySpan{char},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ReadOnlySpan<char> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format, T min, T max,
        float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => DragScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span(), min, max, speed, flags);

    /// <param name="format"> The printf format-string to display the number in as a format string. </param>
    /// <inheritdoc cref="DragScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T,ReadOnlySpan{byte},T,T,float,ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool DragScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T min, T max,
        ref Utf8StringHandler<HintStringHandlerBuffer> format, float speed = 1, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>
        => DragScalar(label.Span(), ref value, format.Span(), min, max, speed, flags);
}
