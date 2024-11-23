using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static unsafe partial class ImUtf8
{
    /// <summary> Draw a header button the width of the available content region. </summary>
    /// <param name="label"> The header label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags for a tree node. </param>
    /// <returns> Whether the header is open or closed. </returns>
    /// <remarks> This does not push any kind of ID to the ID stack or indent anything. </remarks>
    public static bool CollapsingHeader(ReadOnlySpan<byte> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => ImGuiNative.igCollapsingHeader_TreeNodeFlags(label.Start(), flags).Bool();

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    public static bool CollapsingHeader(ReadOnlySpan<char> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => CollapsingHeader(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{char}, ImGuiTreeNodeFlags)"/>
    public static bool CollapsingHeader(ref Utf8StringHandler<LabelStringHandlerBuffer> label,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => CollapsingHeader(label.Span(), flags);


    /// <summary> Draw a header button the width of the available content region that can be closed / hidden. </summary>
    /// <param name="visible"> Whether this header is visible. If it is, there is a right-aligned close button that toggles this bool. </param>
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    public static bool CollapsingHeader(ReadOnlySpan<byte> label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => ImGuiNative.igCollapsingHeader_BoolPtr(label.Start(), (byte*)Unsafe.AsPointer(ref visible), flags).Bool();

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{byte}, ref bool, ImGuiTreeNodeFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    public static bool CollapsingHeader(ReadOnlySpan<char> label, ref bool visible, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => CollapsingHeader(label.Span<LabelStringHandlerBuffer>(), ref visible, flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{char}, ref bool, ImGuiTreeNodeFlags)"/>
    public static bool CollapsingHeader(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref bool visible,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => CollapsingHeader(label.Span(), ref visible, flags);


    /// <summary> Draw a header button the width of the available content region and push its label as an ID, pop it on leaving scope. </summary>
    /// <returns> A disposable object indicating the open state of the header. Use with using. </returns>
    /// <remarks />
    /// <inheritdoc cref="CollapsingHeader(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    public static CollapsingHeader CollapsingHeaderId(ReadOnlySpan<byte> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label, flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeaderId(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    public static CollapsingHeader CollapsingHeaderId(ReadOnlySpan<char> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeaderId(ReadOnlySpan{char}, ImGuiTreeNodeFlags)"/>
    public static CollapsingHeader CollapsingHeaderId(ref Utf8StringHandler<LabelStringHandlerBuffer> label,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span(), flags);


    /// <summary> Draw a header button the width of the available content region that can be closed / hidden, and push its label as an ID, pop it on leaving scope. </summary>
    /// <param name="visible"> Whether this header is visible. If it is, there is a right-aligned close button that toggles this bool. </param>
    /// <inheritdoc cref="CollapsingHeaderId(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    public static CollapsingHeader CollapsingHeaderId(ReadOnlySpan<byte> label, ref bool visible,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label, ref visible, flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeaderId(ReadOnlySpan{byte}, ref bool, ImGuiTreeNodeFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    public static CollapsingHeader CollapsingHeaderId(ReadOnlySpan<char> label, ref bool visible,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), ref visible, flags);

    /// <param name="label"> The header label as a UTF16 string. </param>
    /// <inheritdoc cref="CollapsingHeaderId(ReadOnlySpan{char}, ref bool, ImGuiTreeNodeFlags)"/>
    public static CollapsingHeader CollapsingHeaderId(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ref bool visible,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span(), ref visible, flags);
}
