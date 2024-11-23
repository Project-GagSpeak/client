using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a combo field and end it on leaving scope. </summary>
    /// <param name="label"> The combo label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="preview"> The currently displayed string in the combo field as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the combo's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the begun combo popup is currently open. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<byte> label, ReadOnlySpan<byte> preview, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label, preview, flags);

    /// <param name="label"> The combo label as a UTF16 string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{byte},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<char> label, ReadOnlySpan<byte> preview, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), preview, flags);

    /// <param name="label"> The combo label as a formatted string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{char},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<byte> preview,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span(), preview, flags);


    /// <param name="preview"> The currently displayed string in the combo field as a UTF16 string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{byte},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<byte> label, ReadOnlySpan<char> preview, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label, preview.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="preview"> The currently displayed string in the combo field as a UTF16 string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{char},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<char> label, ReadOnlySpan<char> preview, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), preview.Span<HintStringHandlerBuffer>(), flags);

    /// <param name="preview"> The currently displayed string in the combo field as a UTF16 string. </param>
    /// <inheritdoc cref="Combo(ref Utf8StringHandler{LabelStringHandlerBuffer},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ReadOnlySpan<char> preview,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span(), preview.Span<HintStringHandlerBuffer>(), flags);


    /// <param name="preview"> The currently displayed string in the combo field as a formatted string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{byte},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<byte> label, ref Utf8StringHandler<HintStringHandlerBuffer> preview,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label, preview.Span(), flags);

    /// <param name="preview"> The currently displayed string in the combo field as a formatted string. </param>
    /// <inheritdoc cref="Combo(ReadOnlySpan{char},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ReadOnlySpan<char> label, ref Utf8StringHandler<HintStringHandlerBuffer> preview,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), preview.Span(), flags);

    /// <param name="preview"> The currently displayed string in the combo field as a formatted string. </param>
    /// <inheritdoc cref="Combo(ref Utf8StringHandler{LabelStringHandlerBuffer},ReadOnlySpan{byte},ImGuiComboFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Combo Combo(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref Utf8StringHandler<HintStringHandlerBuffer> preview,
        ImGuiComboFlags flags = ImGuiComboFlags.None)
        => new(label.Span(), preview.Span(), flags);
}
