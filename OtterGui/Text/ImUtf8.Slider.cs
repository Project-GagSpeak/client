using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a slider for numerical values. </summary>
    /// <typeparam name="T"> The type of the numeric value to change. </typeparam>
    /// <param name="label"> The slider label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The numeric input/output value. </param>
    /// <param name="format"> The printf format-string to display the number in as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="minValue"> The minimum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="maxValue"> The maximum value for the scalar. This is only applied on manual input when the AlwaysClamp flag is set. </param>
    /// <param name="flags"> Additional flags controlling the sliders behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<byte> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => ImGuiNative.igSliderScalar(label.Start(), Type<T>(), Unsafe.AsPointer(ref value), &minValue, &maxValue, format.Start(), flags)
            .Bool();

    /// <param name="label"> The slider label as a UTF16 string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<byte> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span<LabelStringHandlerBuffer>(), ref value, format, minValue, maxValue, flags);

    /// <param name="label"> The slider label as a UTF16 string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<byte> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span(), ref value, format, minValue, maxValue, flags);


    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<char> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label, ref value, format.Span<HintStringHandlerBuffer>(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<char> format, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span<HintStringHandlerBuffer>(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="Slider{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<char> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span(), ref value, format.Span<HintStringHandlerBuffer>(), minValue, maxValue, flags);


    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<byte> label, ref T value, ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label, ref value, format.Span(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<char> label, ref T value, ref Utf8StringHandler<LabelStringHandlerBuffer> format,
        T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span(), minValue, maxValue, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="Slider{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value,
        ref Utf8StringHandler<LabelStringHandlerBuffer> format, T minValue, T maxValue, ImGuiSliderFlags flags = ImGuiSliderFlags.None)
        where T : unmanaged, INumber<T>
        => Slider(label.Span(), ref value, format.Span(), minValue, maxValue, flags);


    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<byte> label, ref T value, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label, ref value, DefaultSliderFormat<T>(), minValue, maxValue, flags);

    /// <inheritdoc cref="Slider{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ReadOnlySpan<char> label, ref T value, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span<LabelStringHandlerBuffer>(), ref value, minValue, maxValue, flags);

    /// <inheritdoc cref="Slider{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiSliderFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Slider<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T minValue, T maxValue,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None) where T : unmanaged, INumber<T>
        => Slider(label.Span(), ref value, minValue, maxValue, flags);


    /// <summary> Obtain the correct ImGui data type for a number type. </summary>
    /// <exception cref="NotImplementedException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static ImGuiDataType Type<T>() where T : unmanaged, INumber<T>
    {
        if (typeof(T) == typeof(byte))
            return ImGuiDataType.U8;

        if (typeof(T) == typeof(ushort))
            return ImGuiDataType.U16;

        if (typeof(T) == typeof(uint))
            return ImGuiDataType.U32;

        if (typeof(T) == typeof(ulong))
            return ImGuiDataType.U64;

        if (typeof(T) == typeof(sbyte))
            return ImGuiDataType.S8;

        if (typeof(T) == typeof(short))
            return ImGuiDataType.S16;

        if (typeof(T) == typeof(int))
            return ImGuiDataType.S32;

        if (typeof(T) == typeof(long))
            return ImGuiDataType.S64;

        if (typeof(T) == typeof(float))
            return ImGuiDataType.Float;

        if (typeof(T) == typeof(double))
            return ImGuiDataType.Double;

        if (typeof(T) == typeof(nint))
            return sizeof(T) == 8 ? ImGuiDataType.S64 : ImGuiDataType.S32;

        if (typeof(T) == typeof(nuint))
            return sizeof(T) == 8 ? ImGuiDataType.U64 : ImGuiDataType.U32;

        throw new NotImplementedException();
    }

    /// <summary> Obtain the default slider format for a number type. </summary>
    private static ReadOnlySpan<byte> DefaultSliderFormat<T>()
    {
        if (typeof(T) == typeof(byte))
            return "%u"u8;

        if (typeof(T) == typeof(ushort))
            return "%u"u8;

        if (typeof(T) == typeof(uint))
            return "%u"u8;

        if (typeof(T) == typeof(ulong))
            return "%llu"u8;

        if (typeof(T) == typeof(sbyte))
            return "%d"u8;

        if (typeof(T) == typeof(short))
            return "%d"u8;

        if (typeof(T) == typeof(int))
            return "%d"u8;

        if (typeof(T) == typeof(long))
            return "%lld"u8;

        if (typeof(T) == typeof(float))
            return "%f"u8;

        if (typeof(T) == typeof(double))
            return "%f"u8;

        if (typeof(T) == typeof(nint))
            return "0x%llx"u8;

        if (typeof(T) == typeof(nuint))
            return "0x%llx"u8;

        return ""u8;
    }
}
