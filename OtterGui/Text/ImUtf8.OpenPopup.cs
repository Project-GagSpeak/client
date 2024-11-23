using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Ensure a popup is open. </summary>
    /// <param name="id"> The popup ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control for which popups to check. </param>
    /// <remarks> Popups are subject to the ID stack. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenPopup(ReadOnlySpan<byte> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => ImGuiNative.igOpenPopup_Str(id.Start(), flags);

    /// <param name="id"> The popup ID as a UTF16 string. </param>
    /// <inheritdoc cref="OpenPopup(ReadOnlySpan{byte},ImGuiPopupFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenPopup(ref Utf8StringHandler<LabelStringHandlerBuffer> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => OpenPopup(id.Span(), flags);

    /// <param name="id"> The popup ID as a formatted string. </param>
    /// <inheritdoc cref="OpenPopup(ReadOnlySpan{char},ImGuiPopupFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void OpenPopup(ReadOnlySpan<char> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => OpenPopup(id.Span<LabelStringHandlerBuffer>(), flags);
}
