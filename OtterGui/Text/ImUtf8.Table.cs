using ImGuiNET;
using OtterGui.Text.HelperObjects;

#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Begin a table and end it on leaving scope. </summary>
    /// <param name="id"> The table ID as a UTF8 string. HAS to be null-terminated. </param>
    /// <param name="columns"> The number of columns in the table. </param>
    /// <param name="flags"> Additional flags to control the table's behaviour. </param>
    /// <param name="outerSize">
    /// The size the table is fixed to.
    /// <list type="bullet">
    ///     <item> If this is non-positive in X, right-align from the available region. (0 means full available width). </item>
    ///     <item> If this is positive in X, set a fixed width. </item>
    ///     <item> If both scroll-bars are disabled and this is negative in Y, right-align from the available region. (0 means full available width). </item>
    /// </list>
    /// The behaviour in Y is dependent on the existence of scroll-bars and other <paramref name="flags"/> (see <see href="https://github.com/ocornut/imgui/blob/master/imgui_tables.cpp" >imgui_tables.cpp</see>).
    /// </param>
    /// <param name="innerWidth"> The inner width in case the horizontal scroll-bar is enabled. If 0, fits into <paramref name="outerSize"/>.X, otherwise overrides the scrolling width. Negative values make no sense. </param>
    /// <returns> A disposable object that evaluates to true if any part of the begun table is currently visible and should be checked before using table functionality. Use with using. </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EndObjects.Table Table(ReadOnlySpan<byte> id, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None,
        Vector2 outerSize = default, float innerWidth = default)
        => new(id, columns, flags, outerSize, innerWidth);

    /// <param name="id"> The table ID as a UTF16 string. </param>
    /// <inheritdoc cref="Table(ReadOnlySpan{byte},int,ImGuiTableFlags,Vector2,float)"/>
    /// <exception cref="ImUtf8FormatException" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EndObjects.Table Table(ReadOnlySpan<char> id, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None,
        Vector2 outerSize = default, float innerWidth = default)
        => new(id.Span<LabelStringHandlerBuffer>(), columns, flags, outerSize, innerWidth);

    /// <param name="id"> The table ID as a formatted string. </param>
    /// <inheritdoc cref="Table(ReadOnlySpan{char},int,ImGuiTableFlags,Vector2,float)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EndObjects.Table Table(ref Utf8StringHandler<LabelStringHandlerBuffer> id, int columns,
        ImGuiTableFlags flags = ImGuiTableFlags.None, Vector2 outerSize = default, float innerWidth = default)
        => new(id.Span(), columns, flags, outerSize, innerWidth);
}
