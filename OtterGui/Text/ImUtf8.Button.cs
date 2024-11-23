using System.Text.Unicode;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw the given button. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="size">
    /// The desired size for the button. If (0, 0), it will fit to the label.<br/>
    /// You can pass <paramref name="size"/>.x = float.MinValue to span the available width.
    /// </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(ReadOnlySpan<byte> label, Vector2 size = default)
        => ImGuiNative.igButton(label.Start(), size).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="Button(ReadOnlySpan{byte}, Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(ReadOnlySpan<char> label, Vector2 size = default)
        => Button(label.Span<LabelStringHandlerBuffer>(), size);

    /// <param name="label"> The button label as a format string. </param>
    /// <inheritdoc cref="Button(ReadOnlySpan{char}, Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Button(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Vector2 size = default)
        => Button(label.Span(), size);
}
