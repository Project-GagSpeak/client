using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a tab bar and end it on leaving scope. </summary>
    /// <param name="label"> The tab bar label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the tab bar's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if any part of the begun tab bar is currently visible. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabBar TabBar(ReadOnlySpan<byte> label, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        => new(label, flags);

    /// <param name="label"> The tab bar label as a UTF16 string. </param>
    /// <inheritdoc cref="TabBar(ReadOnlySpan{byte},ImGuiTabBarFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabBar TabBar(ReadOnlySpan<char> label, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="label"> The tab bar label as a formatted string. </param>
    /// <inheritdoc cref="TabBar(ReadOnlySpan{char},ImGuiTabBarFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TabBar TabBar(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
        => new(label.Span(), flags);
}
