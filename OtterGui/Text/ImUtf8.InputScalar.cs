using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a text input for numerical values, with optional +/- buttons. </summary>
    /// <typeparam name="T"> The type of the numeric value to change. </typeparam>
    /// <param name="label"> The input label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The numeric input/output value. </param>
    /// <param name="format"> The printf format-string to display and parse the number in as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="step"> The step when clicking the +/- buttons. If this is 0, the buttons will not be displayed. </param>
    /// <param name="stepFast"> The step when holding the +/- buttons for a while. </param>
    /// <param name="flags"> Additional flags controlling the input behavior. </param>
    /// <returns> Whether the value changed in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<byte> format, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => ImGuiNative.igInputScalar(label.Start(), Type<T>(), Unsafe.AsPointer(ref value), step.Equals(default) ? null : &step, &stepFast,
            format.Start(), flags).Bool();

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<byte> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format, step, stepFast, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<byte> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span(), ref value, format, step, stepFast, flags);


    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<char> format, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label, ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<char> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, ReadOnlySpan<char> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span(), ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast, flags);


    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<byte> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format,
        T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label, ref value, format.Span(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<char> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value,
        ref Utf8StringHandler<HintStringHandlerBuffer> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span(), ref value, format.Span(), step, stepFast, flags);


    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<byte> label, ref T value, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label, ref value, DefaultInputFormat<T>(), step, stepFast, flags);

    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ReadOnlySpan<char> label, ref T value, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span<LabelStringHandlerBuffer>(), ref value, step, stepFast, flags);

    /// <inheritdoc cref="InputScalar{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalar<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalar(label.Span(), ref value, step, stepFast, flags);


    /// <summary> Return the default format string for a given type. </summary>
    private static ReadOnlySpan<byte> DefaultInputFormat<T>()
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
            return "%.6f"u8;

        if (typeof(T) == typeof(double))
            return "%.6f"u8;

        if (typeof(T) == typeof(nint))
            return "0x%llx"u8;

        if (typeof(T) == typeof(nuint))
            return "0x%llx"u8;

        return [];
    }
}
