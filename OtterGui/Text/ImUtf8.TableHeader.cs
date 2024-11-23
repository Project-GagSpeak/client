using ImGuiNET;
using OtterGui.Text.HelperObjects;

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a header for a table cell. </summary>
    /// <param name="label"> The header label as a UTF8 string. HAS to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TableHeader(ReadOnlySpan<byte> label)
        => ImGuiNative.igTableHeader(label.Start());

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="TableHeader(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TableHeader(ReadOnlySpan<char> label)
        => TableHeader(label.Span<LabelStringHandlerBuffer>());

    /// <param name="label"> The header label as a formatted string. </param>
    /// <inheritdoc cref="TableHeader(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TableHeader(ref Utf8StringHandler<LabelStringHandlerBuffer> label)
        => TableHeader(label.Span());
}
