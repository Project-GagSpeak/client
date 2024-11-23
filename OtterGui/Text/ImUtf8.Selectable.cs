using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a selectable, i.e. text that highlights on being hovered or selected. </summary>
    /// <param name="label"> The selectable label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="isSelected"> Whether the selectable is currently selected. </param>
    /// <param name="flags"> Additional flags that control the selectable's behaviour. </param>
    /// <param name="size"> The desired size of the selectable. </param>
    /// <returns> True if the selectable has been clicked in this frame. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Selectable(ReadOnlySpan<byte> label, bool isSelected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
        Vector2 size = default)
        => ImGuiNative.igSelectable_Bool(label.Start(), isSelected.Byte(), flags, size).Bool();

    /// <param name="label"> The selectable label as a UTF16 string. </param>
    /// <inheritdoc cref="Selectable(ReadOnlySpan{byte},bool,ImGuiSelectableFlags,Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Selectable(ReadOnlySpan<char> label, bool isSelected = false, ImGuiSelectableFlags flags = ImGuiSelectableFlags.None,
        Vector2 size = default)
        => Selectable(label.Span<LabelStringHandlerBuffer>(), isSelected, flags, size);

    /// <param name="label"> The selectable label as a formatted string. </param>
    /// <inheritdoc cref="Selectable(ReadOnlySpan{char},bool,ImGuiSelectableFlags,Vector2)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Selectable(ref Utf8StringHandler<TextStringHandlerBuffer> label, bool isSelected = false,
        ImGuiSelectableFlags flags = ImGuiSelectableFlags.None, Vector2 size = default)
        => Selectable(label.Span(), isSelected, flags, size);
}
