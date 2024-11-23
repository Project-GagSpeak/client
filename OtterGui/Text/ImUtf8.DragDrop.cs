using ImGuiNET;
using OtterGui.Text.EndObjects;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Open a new Drag & Drop source on the last item and close it on leaving scope. </summary>
    /// <param name="flags"> Additional flags to control the drag & drop behaviour. </param>
    /// <returns> A disposable object that indicates whether the source is active. Use with using. </returns>
    /// <remarks>
    /// You can draw things inside the source scope for a tooltip,
    /// and you should set a labeled payload from the returned object on success via <see cref="DragDropSource.SetPayload(ReadOnlySpan{byte}, ImGuiCond)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DragDropSource DragDropSource(ImGuiDragDropFlags flags = ImGuiDragDropFlags.None)
        => new(flags);

    /// <summary> Open a new Drag & Drop target on the last item and close it on leaving scope. </summary>
    /// <returns> A disposable object that indicates whether the target is active. Use with using. </returns>
    /// <remarks> You can use the returned object to check for a specific payload dropping with <see cref="DragDropTarget.IsDropping(ReadOnlySpan{byte}, ImGuiDragDropFlags)"/>. </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DragDropTarget DragDropTarget()
        => new();
}
