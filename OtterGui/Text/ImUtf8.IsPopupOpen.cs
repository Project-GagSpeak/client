using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Check whether a popup is currently open. </summary>
    /// <param name="id"> The popup ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control for which popups to check. </param>
    /// <returns> True if the popup is open. </returns>
    /// <remarks> Popups are subject to the ID stack. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPopupOpen(ReadOnlySpan<byte> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => ImGuiNative.igIsPopupOpen_Str(id.Start(), flags).Bool();

    /// <param name="id"> The popup ID as a UTF16 string. </param>
    /// <inheritdoc cref="IsPopupOpen(ReadOnlySpan{byte},ImGuiPopupFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPopupOpen(ref Utf8StringHandler<LabelStringHandlerBuffer> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => IsPopupOpen(id.Span(), flags);

    /// <param name="id"> The popup ID as a formatted string. </param>
    /// <inheritdoc cref="IsPopupOpen(ReadOnlySpan{char},ImGuiPopupFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPopupOpen(ReadOnlySpan<char> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => IsPopupOpen(id.Span<LabelStringHandlerBuffer>(), flags);
}
