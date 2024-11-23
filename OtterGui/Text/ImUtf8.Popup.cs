using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Begin a popup and end it on leaving scope. </summary>
    /// <param name="id"> The popup ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the popup's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the begun popup is currently open. Use with using. </returns>
    /// <remarks> Popups are subject to the ID stack. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Popup Popup(ReadOnlySpan<byte> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id, flags);

    /// <param name="id"> The popup ID as a UTF16 string. </param>
    /// <inheritdoc cref="Popup(ReadOnlySpan{byte},ImGuiWindowFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Popup Popup(ReadOnlySpan<char> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="id"> The popup ID as a formatted string. </param>
    /// <inheritdoc cref="Popup(ReadOnlySpan{char}, ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Popup Popup(ref Utf8StringHandler<LabelStringHandlerBuffer> id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span(), flags);
}
