using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a tab item and end it on leaving scope. </summary>
    /// <param name="label"> The tab item label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="open"> Whether the tab item is currently open. If this is provided, the tab item will render a close button that controls this value. </param>
    /// <param name="flags"> Additional flags to control the tab item's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the tab item is currently opened. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ReadOnlySpan<byte> label, ref bool open, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label, ref open, flags);

    /// <param name="label"> The tab item label as a UTF16 string. </param>
    /// <inheritdoc cref="TabItem(ReadOnlySpan{byte},ref bool,ImGuiTabItemFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ReadOnlySpan<char> label, ref bool open, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), ref open, flags);

    /// <param name="label"> The tab item label as a formatted string. </param>
    /// <inheritdoc cref="TabItem(ReadOnlySpan{char},ref bool,ImGuiTabItemFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref bool open,
        ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label.Span(), ref open, flags);


    /// <inheritdoc cref="TabItem(ReadOnlySpan{byte},ref bool,ImGuiTabItemFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ReadOnlySpan<byte> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label, flags);

    /// <inheritdoc cref="TabItem(ReadOnlySpan{char},ref bool,ImGuiTabItemFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ReadOnlySpan<char> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <inheritdoc cref="TabItem(ref Utf8StringHandler{LabelStringHandlerBuffer},ref bool,ImGuiTabItemFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabItem TabItem(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => new(label.Span(), flags);
}
