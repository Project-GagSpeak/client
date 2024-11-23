using ImGuiNET;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> The current style. </summary>
    public static ImGuiStylePtr Style
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetStyle();
    }

    /// <summary> The regular spacing between items when using <see cref="ImGui.SameLine()"/> or new lines. </summary>
    public static Vector2 ItemSpacing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Style.ItemSpacing;
    }

    /// <summary> The spacing used between grouped objects like an input and a label. </summary>
    public static Vector2 ItemInnerSpacing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Style.ItemInnerSpacing;
    }

    /// <summary> The padding added to frames like buttons. </summary>
    public static Vector2 FramePadding
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Style.FramePadding;
    }

    /// <summary> The current IO data. </summary>
    public static ImGuiIOPtr IO
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetIO();
    }

    /// <summary> The global scale applied to almost anything. </summary>
    public static float GlobalScale
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IO.FontGlobalScale;
    }

    /// <summary> The height of a text line. </summary>
    public static float TextHeight
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetTextLineHeight();
    }

    /// <summary> The height of a text line with frame padding. </summary>
    public static float FrameHeight
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetFrameHeight();
    }

    /// <summary> The height of a text line with vertical spacing. </summary>
    public static float TextHeightSpacing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetTextLineHeightWithSpacing();
    }

    /// <summary> The height of a text line with frame padding and vertical spacing. </summary>
    public static float FrameHeightSpacing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ImGui.GetFrameHeightWithSpacing();
    }

    /// <summary> Stay in the same line with the inner spacing instead of the regular spacing. </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SameLineInner()
        => ImGui.SameLine(0, ItemInnerSpacing.X);
}
