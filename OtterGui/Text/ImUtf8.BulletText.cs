using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw the given text. </summary>
    /// <param name="text"> The given text as a UTF8 string. HAS to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BulletText(ReadOnlySpan<byte> text)
        => ImGuiNative.igBulletText(text.Start());

    /// <param name="text"> The given text as a UTF16 string. </param>
    /// <inheritdoc cref="BulletText(ReadOnlySpan{byte})"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BulletText(ReadOnlySpan<char> text)
        => BulletText(text.Span<TextStringHandlerBuffer>());

    /// <param name="text"> The given text as a format string. </param>
    /// <inheritdoc cref="BulletText(ReadOnlySpan{char})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BulletText(ref Utf8StringHandler<TextStringHandlerBuffer> text)
        => BulletText(text.Span());
}
