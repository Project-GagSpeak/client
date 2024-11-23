using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw the given invisible button, i.e. draw a region that reacts to mouse clicks. </summary>
    /// <param name="id"> The button ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="size"> The desired size for the button. This should not be 0 in either dimension. </param>
    /// <param name="flags"> Additional flags to control the buttons behaviour. </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvisibleButton(ReadOnlySpan<byte> id, Vector2 size, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
        => ImGuiNative.igInvisibleButton(id.Start(), size, flags).Bool();

    /// <param name="id"> The button ID as a UTF16 string. </param>
    /// <inheritdoc cref="Button(ReadOnlySpan{byte}, Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvisibleButton(ReadOnlySpan<char> id, Vector2 size, ImGuiButtonFlags flags = ImGuiButtonFlags.None)
        => InvisibleButton(id.Span<LabelStringHandlerBuffer>(), size, flags);

    /// <param name="id"> The button ID as a formatted string. </param>
    /// <inheritdoc cref="Button(ReadOnlySpan{char}, Vector2)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InvisibleButton(ref Utf8StringHandler<LabelStringHandlerBuffer> id, Vector2 size,
        ImGuiButtonFlags flags = ImGuiButtonFlags.None)
        => InvisibleButton(id.Span(), size, flags);
}
