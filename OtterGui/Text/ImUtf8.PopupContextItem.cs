using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary>
    /// Begin a context popup and end it on leaving scope. <br/>
    /// A context popup associates with the last item and opens when that item is right-clicked.
    /// </summary>
    /// <param name="id"> The popup ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the popup's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
    /// <remarks> Popups are subject to the ID stack. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PopupContextItem PopupContextItem(ReadOnlySpan<byte> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => new(id, flags);

    /// <param name="id"> The popup ID as a UTF16 string. </param>
    /// <inheritdoc cref="PopupContextItem(ReadOnlySpan{byte},ImGuiPopupFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PopupContextItem PopupContextItem(ReadOnlySpan<char> id, ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => new(id.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="id"> The popup ID as a format string. </param>
    /// <inheritdoc cref="PopupContextItem(ReadOnlySpan{char},ImGuiPopupFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PopupContextItem PopupContextItem(ref Utf8StringHandler<LabelStringHandlerBuffer> id,
        ImGuiPopupFlags flags = ImGuiPopupFlags.None)
        => new(id.Span(), flags);
}
