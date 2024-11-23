using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw the given checkbox. </summary>
    /// <param name="label"> The checkbox label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The input and output value of the checkbox. </param>
    /// <returns> True if the checkbox has been clicked in this frame, in which case <paramref name="value"/> will be flipped. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ReadOnlySpan<byte> label, ref bool value)
        => ImGuiNative.igCheckbox(label.Start(), (byte*)Unsafe.AsPointer(ref value)).Bool();

    /// <param name="label"> The checkbox label as a UTF16 string. </param>
    /// <inheritdoc cref="Checkbox(ReadOnlySpan{byte}, ref bool)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ReadOnlySpan<char> label, ref bool value)
        => Checkbox(label.Span<LabelStringHandlerBuffer>(), ref value);

    /// <param name="label"> The checkbox label as a format string. </param>
    /// <inheritdoc cref="Checkbox(ReadOnlySpan{char},ref bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref bool value)
        => Checkbox(label.Span(), ref value);


    /// <summary> Draw the given tri-state checkbox. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The bit-flag based input and output value. </param>
    /// <param name="flags"> The bit-flags this checkbox is representing. </param>
    /// <returns> True if the checkbox has been clicked in this frame. </returns>
    /// <remarks>
    ///     This checkbox will display empty if <paramref name="value"/> and <paramref name="flags"/> have no bits in common,
    ///     a square if some but not all <paramref name="flags"/> are set in <paramref name="value"/>,
    ///     and a checkmark if all are set. <br/>
    ///     If the current display is empty or square, clicking it results in setting all <paramref name="flags"/>,
    ///     if it is a checkmark, clicking unsets all <paramref name="flags"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ReadOnlySpan<byte> label, ref uint value, uint flags)
        => ImGuiNative.igCheckboxFlags_UintPtr(label.Start(), (uint*)Unsafe.AsPointer(ref value), flags).Bool();

    /// <param name="label"> The checkbox label as a UTF16 string. </param>
    /// <inheritdoc cref="Checkbox(ReadOnlySpan{byte},ref uint, uint)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ReadOnlySpan<char> label, ref uint value, uint flags)
        => Checkbox(label.Span<LabelStringHandlerBuffer>(), ref value, flags);

    /// <param name="label"> The checkbox label as a format string. </param>
    /// <inheritdoc cref="Checkbox(ReadOnlySpan{char},ref uint, uint)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref uint value, uint flags)
        => Checkbox(label.Span(), ref value, flags);


    /// <inheritdoc cref="Checkbox(ReadOnlySpan{byte},ref uint, uint)"/>
    /// <typeparam name="T"> A basic enumeration type with backing type using at most 4 bytes. </typeparam>
    /// <exception cref="ArgumentException"> If sizeof(T) > 4. </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox<T>(ReadOnlySpan<byte> label, ref T value, T flags) where T : unmanaged, Enum
    {
        var (val, f) = ConvertEnum(value, flags);
        if (!Checkbox(label, ref val, f))
            return false;

        value = *(T*)&val;
        return true;
    }

    /// <param name="label"> The checkbox label as a UTF16 string. </param>
    /// <inheritdoc cref="Checkbox{T}(ReadOnlySpan{byte},ref T, T)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox<T>(ReadOnlySpan<char> label, ref T value, T flags) where T : unmanaged, Enum
        => Checkbox(label.Span<LabelStringHandlerBuffer>(), ref value, flags);

    /// <param name="label"> The checkbox label as a format string. </param>
    /// <inheritdoc cref="Checkbox{T}(ReadOnlySpan{char},ref T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Checkbox<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T flags) where T : unmanaged, Enum
        => Checkbox(label.Span(), ref value, flags);


    /// <summary> Convert two enum-based values to uint. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static (uint, uint) ConvertEnum<T>(T value, T flags) where T : unmanaged, Enum
        => sizeof(T) switch
        {
            1 => (*(byte*)Unsafe.AsPointer(ref value), *(byte*)&flags),
            2 => (*(ushort*)Unsafe.AsPointer(ref value), *(ushort*)&flags),
            4 => (*(uint*)Unsafe.AsPointer(ref value), *(uint*)&flags),
            _ => throw new ArgumentException($"Enum type {typeof(T)} has size {sizeof(T)} > 4, which is not supported for flag checkboxes."),
        };
}
