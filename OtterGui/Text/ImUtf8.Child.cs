using ImGuiNET;
using OtterGui.Text.EndObjects;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a child and end it on leaving scope. </summary>
    /// <param name="id"> The ID of the child as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="size"> The desired size of the child. </param>
    /// <param name="border"> Whether the child should be framed by a border. </param>
    /// <param name="flags"> Additional flags for the child. </param>
    /// <returns> A disposable object that evaluates to true if any part of the begun child is currently visible. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Child Child(ReadOnlySpan<byte> id, Vector2 size, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id, size, border, flags);

    /// <param name="id"> The ID of the child as a UTF16 string. </param>
    /// <inheritdoc cref="Child(ReadOnlySpan{byte},Vector2,bool,ImGuiWindowFlags)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Child Child(ReadOnlySpan<char> id, Vector2 size, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span<LabelStringHandlerBuffer>(), size, border, flags);

    /// <param name="id"> The ID of the child as a format string. </param>
    /// <inheritdoc cref="Child(ReadOnlySpan{char},Vector2,bool,ImGuiWindowFlags)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Child Child(ref Utf8StringHandler<LabelStringHandlerBuffer> id, Vector2 size, bool border = false,
        ImGuiWindowFlags flags = ImGuiWindowFlags.None)
        => new(id.Span(), size, border, flags);
}
