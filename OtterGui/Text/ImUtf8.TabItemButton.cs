using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a tab-button in the current tab bar. </summary>
    /// <param name="label"> The button label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the tab button's behaviour. </param>
    /// <returns> True if the button has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TabItemButton(ReadOnlySpan<byte> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => ImGuiNative.igTabItemButton(label.Start(), flags).Bool();

    /// <param name="label"> The button label as a UTF16 string. </param>
    /// <inheritdoc cref="TabItemButton(ReadOnlySpan{byte},ImGuiTabItemFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TabItemButton(ReadOnlySpan<char> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => TabItemButton(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="label"> The button label as a formatted string. </param>
    /// <inheritdoc cref="TabItemButton(ReadOnlySpan{char},ImGuiTabItemFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TabItemButton(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiTabItemFlags flags = ImGuiTabItemFlags.None)
        => TabItemButton(label.Span(), flags);
}
