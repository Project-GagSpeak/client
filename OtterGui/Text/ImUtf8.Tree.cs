using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a tree node and end it on leaving scope. </summary>
    /// <param name="label"> The node label as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="flags"> Additional flags to control the tree's behaviour. </param>
    /// <returns> A disposable object that evaluates to true if the begun tree node is currently expanded. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TreeNode TreeNode(ReadOnlySpan<byte> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label, flags);

    /// <param name="label"> The node label as a UTF16 string. </param>
    /// <inheritdoc cref="TreeNode(ReadOnlySpan{byte}, ImGuiTreeNodeFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TreeNode TreeNode(ReadOnlySpan<char> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span<LabelStringHandlerBuffer>(), flags);

    /// <param name="label"> The node label as a formatted string. </param>
    /// <inheritdoc cref="TreeNode(ReadOnlySpan{char}, ImGuiTreeNodeFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TreeNode TreeNode(ref Utf8StringHandler<LabelStringHandlerBuffer> label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
        => new(label.Span(), flags);
}
