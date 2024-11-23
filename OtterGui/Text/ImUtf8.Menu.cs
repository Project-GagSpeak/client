using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a menu and end it on leaving scope. </summary>
    /// <param name="label"> The menu label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="enabled"> Whether the menu is enabled or not. </param>
    /// <returns> A disposable object that evaluates to true if the begun menu is currently open. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Menu Menu(ReadOnlySpan<byte> label, bool enabled = true)
        => new(label, enabled);

    /// <param name="label"> The menu label as a UTF16 string. </param>
    /// <inheritdoc cref="Menu(ReadOnlySpan{byte},bool)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Menu Menu(ReadOnlySpan<char> label, bool enabled = true)
        => new(label.Span<LabelStringHandlerBuffer>(), enabled);

    /// <param name="label"> The menu label as a formatted string. </param>
    /// <inheritdoc cref="Menu(ReadOnlySpan{char},bool)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Menu Menu(ref Utf8StringHandler<LabelStringHandlerBuffer> label, bool enabled = true)
        => new(label.Span(), enabled);
}
