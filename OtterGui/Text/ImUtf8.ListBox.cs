using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a list box and end it on leaving scope. </summary>
    /// <param name="label"> The combo label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="size"> The required size of the list box. You can pass <paramref name="size"/>.x = float.MinValue to span the available width. </param>
    /// <returns> A disposable object that evaluates to true if any part of the begun list box is currently visible. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ListBox ListBox(ReadOnlySpan<byte> label, Vector2 size)
        => new(label, size);

    /// <param name="label"> The combo label as a UTF16 string. </param>
    /// <inheritdoc cref="ListBox(ReadOnlySpan{byte},Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ListBox ListBox(ReadOnlySpan<char> label, Vector2 size)
        => new(label.Span<LabelStringHandlerBuffer>(), size);

    /// <param name="label"> The combo label as a formatted string. </param>
    /// <inheritdoc cref="ListBox(ReadOnlySpan{char},Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ListBox ListBox(ref Utf8StringHandler<LabelStringHandlerBuffer> label, Vector2 size)
        => new(label.Span(), size);
}
