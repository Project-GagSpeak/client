using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a text input for numerical values, with optional +/- buttons, that does not update <paramref name="value"/> on every change. </summary>
    /// <returns> Whether the input field was deactivated in this frame and the value has been changed. </returns>
    /// <inheritdoc cref="InputScalar{T}(ReadOnlySpan{byte},ref T,ReadOnlySpan{byte},T,T,ImGuiInputTextFlags)"/>
    /// <remarks>
    /// <paramref name="value"/> is only changed after the input gets deactivated and when this returns true. <br/>
    /// If something else changes <paramref name="value"/> while this item is activated, this change will not be reflected in the input and have no effect.
    /// </remarks>
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<byte> format, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
    {
        flags &= ~ImGuiInputTextFlags.EnterReturnsTrue;
        var id = ImGuiNative.igGetID_StrStr(label.Start(out var end), end);
        if (!DataCache<int>.IsActive || id != DataCache<int>.LastId)
        {
            var tmp = value;
            if (InputScalar(label, ref tmp, step, stepFast, flags))
                DataCache<T>.Update(tmp, id);
            return false;
        }

        InputScalar(label, ref DataCache<T>.Value, step, stepFast, flags);
        return DataCache<T>.Return(ref value);
    }

    /// <param name="label"> The input label as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<byte> format, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref value, format, step, stepFast, flags);

    /// <param name="label"> The input label as a formatted string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value,
        ReadOnlySpan<byte> format, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span(), ref value, format, step, stepFast, flags);


    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<byte> label, ref T value, ReadOnlySpan<char> format, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label, ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<char> label, ref T value, ReadOnlySpan<char> format, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast,
            flags);

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value,
        ReadOnlySpan<char> format, T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
        where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span(), ref value, format.Span<HintStringHandlerBuffer>(), step, stepFast, flags);


    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<byte> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label, ref value, format.Span(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<char> label, ref T value, ref Utf8StringHandler<HintStringHandlerBuffer> format,
        T step = default, T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref value, format.Span(), step, stepFast, flags);

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{char}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value,
        ref Utf8StringHandler<HintStringHandlerBuffer> format, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span(), ref value, format.Span(), step, stepFast, flags);


    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{byte},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<byte> label, ref T value, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label, ref value, DefaultInputFormat<T>(), step, stepFast, flags);

    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ReadOnlySpan{char},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ReadOnlySpan<char> label, ref T value, T step = default, T stepFast = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span<LabelStringHandlerBuffer>(), ref value, step, stepFast, flags);

    /// <inheritdoc cref="InputScalarOnDeactivated{T}(ref Utf8StringHandler{LabelStringHandlerBuffer},ref T, ReadOnlySpan{byte}, T, T, ImGuiInputTextFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InputScalarOnDeactivated<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T step = default,
        T stepFast = default, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None) where T : unmanaged, INumber<T>
        => InputScalarOnDeactivated(label.Span(), ref value, step, stepFast, flags);
}
