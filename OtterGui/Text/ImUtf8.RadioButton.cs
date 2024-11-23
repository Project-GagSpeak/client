using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a radio button. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="active"> Whether the button is enabled. </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton(ReadOnlySpan<byte> label, bool active)
        => ImGuiNative.igRadioButton_Bool(label.Start(), active.Byte()).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="RadioButton(ReadOnlySpan{byte},bool)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton(ReadOnlySpan<char> label, bool active)
        => RadioButton(label.Span<LabelStringHandlerBuffer>(), active);

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="RadioButton(ReadOnlySpan{char},bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton(ref Utf8StringHandler<LabelStringHandlerBuffer> label, bool active)
        => RadioButton(label.Span(), active);


    /// <summary> Draw a radio button based on a value. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="value"> The current value for a group of radio buttons. </param>
    /// <param name="flag"> The value this button is representative of. </param>
    /// <returns> True if the button has been clicked in this frame, in which case <paramref name="value"/> is set to <paramref name="flag"/>. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton<T>(ReadOnlySpan<byte> label, ref T value, T flag) where T : IEquatable<T>
    {
        if (!RadioButton(label, value.Equals(flag)))
            return false;

        value = flag;
        return true;
    }

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="RadioButton{T}(ReadOnlySpan{byte}, ref T, T)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton<T>(ReadOnlySpan<char> label, ref T value, T flag) where T : IEquatable<T>
        => RadioButton(label.Span<LabelStringHandlerBuffer>(), ref value, flag);

    /// <param name="label"> The button label as a formatted string. </param>
    /// <inheritdoc cref="RadioButton{T}(ReadOnlySpan{char}, ref T, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RadioButton<T>(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref T value, T flag) where T : IEquatable<T>
        => RadioButton(label.Span(), ref value, flag);
}
