using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a progress bar. </summary>
    /// <param name="fraction"> The current percentage of progress. If less than 0, displays indeterminate progress bar animation instead. </param>
    /// <param name="size"> The desired size for the progress bar. If the size is less than 0, aligns the progress bar to the end. If it is 0, automatic size is used. </param>
    /// <param name="format"> The printf format-string to display the number in as a UTF8 string. HAS to be null-terminated. </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, Vector2 size, ReadOnlySpan<byte> format)
        => ImGuiNative.igProgressBar(fraction, size, format.Start());

    /// <param name="format"> The printf format-string to display the number in as a UTF16 string. </param>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{byte})"/>>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, Vector2 size, ReadOnlySpan<char> format)
        => ProgressBar(fraction, size, format.Span<LabelStringHandlerBuffer>());

    /// <param name="format"> The printf format-string to display the number in as a formatted string. </param>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{char})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, Vector2 size, ref Utf8StringHandler<LabelStringHandlerBuffer> format)
        => ProgressBar(fraction, size, format.Span());

    /// <summary> Draw a progress bar displaying the fraction as an integral percentage. </summary>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{byte})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, Vector2 size)
        => ImGuiNative.igProgressBar(fraction, size, null);

    /// <summary> Draw a progress bar of standard height over the available content width. </summary>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{byte})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, ReadOnlySpan<byte> format)
        => ImGuiNative.igProgressBar(fraction, new Vector2(-float.MinValue, 0), format.Start());

    /// <summary> Draw a progress bar of standard height over the available content width. </summary>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{char})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, ReadOnlySpan<char> format)
        => ProgressBar(fraction, format.Span<LabelStringHandlerBuffer>());

    /// <summary> Draw a progress bar of standard height over the available content width. </summary>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ref Utf8StringHandler{LabelStringHandlerBuffer})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction, ref Utf8StringHandler<LabelStringHandlerBuffer> format)
        => ProgressBar(fraction, format.Span());


    /// <summary> Draw a progress bar displaying the fraction as an integral percentage and of standard height over the available content width. </summary>
    /// <inheritdoc cref="ProgressBar(float,Vector2,ReadOnlySpan{byte})"/>>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProgressBar(float fraction)
        => ImGuiNative.igProgressBar(fraction, new Vector2(-float.MinValue, 0), null);
}
