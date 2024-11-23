using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a modal popup and end it on leaving scope. </summary>
    /// <param name="id"> The popup ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="open">
    /// Manually controls whether the popup is open or closed. <br/>
    /// If supplied, the popup will have a close button on the top right.
    /// </param>
    /// <param name="flags"> Additional flags to control the popup's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
    /// <remarks> Popups are subject to the ID stack. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ReadOnlySpan<byte> id, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id, ref open, flags);

    /// <param name="id"> The popup ID as a UTF16 string. </param>
    /// <inheritdoc cref="Modal(ReadOnlySpan{byte},ref bool, ImGuiWindowFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ReadOnlySpan<char> id, ref bool open, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span<LabelStringHandlerBuffer>(), ref open, flags);

    /// <param name="id"> The popup ID as a formatted string. </param>
    /// <inheritdoc cref="Modal(ReadOnlySpan{char},ref bool, ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ref Utf8StringHandler<LabelStringHandlerBuffer> id, ref bool open,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span(), ref open, flags);


    /// <inheritdoc cref="Modal(ReadOnlySpan{byte},ref bool, ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ReadOnlySpan<byte> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id, flags);

    /// <inheritdoc cref="Modal(ReadOnlySpan{char},ref bool, ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ReadOnlySpan<char> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span<LabelStringHandlerBuffer>(), flags);

    /// <inheritdoc cref="Modal(ref Utf8StringHandler{LabelStringHandlerBuffer},ref bool, ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Modal Modal(ref Utf8StringHandler<LabelStringHandlerBuffer> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span(), flags);
}
