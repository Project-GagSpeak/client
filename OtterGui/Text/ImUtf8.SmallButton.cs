using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a small button in regular text height. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SmallButton(ReadOnlySpan<byte> label)
        => ImGuiNative.igSmallButton(label.Start()).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="SmallButton(ReadOnlySpan{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SmallButton(ReadOnlySpan<char> label)
        => SmallButton(label.Span<LabelStringHandlerBuffer>());

    /// <param name="label"> The button label as a formatted string. </param>
    /// <inheritdoc cref="SmallButton(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SmallButton(ref Utf8StringHandler<TextStringHandlerBuffer> label)
        => SmallButton(label.Span());
}
