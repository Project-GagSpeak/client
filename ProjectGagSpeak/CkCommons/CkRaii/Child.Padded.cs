using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="ChildPaddedH(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPaddedH(string id, float width, float height, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedH(id, width, height, 0, GetChildRounding(), rFlags, wFlags);

    /// <inheritdoc cref="ChildPaddedH(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPaddedH(string id, float width, float height, uint bgCol, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedH(id, width, height, bgCol, GetChildRounding(), rFlags, wFlags);

    /// <summary>
    ///     ImRaii.Child variant, accepting <paramref name="width"/> as the InnerContentRegion().X dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="width"/> will have its winPaddingX appended before the child is made. </remarks>
    public static IEndObjectContainer ChildPaddedH(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPadded(id, new Vector2(width.AddWinPadX(), height), bgCol, rounding, rFlags, wFlags);


    /// <inheritdoc cref="ChildPaddedW(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPaddedW(string id, float width, float height, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedW(id, width, height, 0, GetChildRounding(), rFlags, wFlags);

    /// <inheritdoc cref="ChildPaddedW(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPaddedW(string id, float width, float height, uint bgCol, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedW(id, width, height, bgCol, GetChildRounding(), rFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="height"/> as the InnerContentRegion().Y dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="height"/> will have its winPaddingY appended before the child is made. </remarks>
    public static IEndObjectContainer ChildPaddedW(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPadded(id, new Vector2(width, height.AddWinPadY()), bgCol, rounding, dFlags, wFlags);


    /// <inheritdoc cref="ChildPadded(string, Vector2, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPadded(string id, Vector2 size, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPadded(id, size, 0, GetChildRounding(), dFlags, wFlags);

    /// <inheritdoc cref="ChildPadded(string, Vector2, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer ChildPadded(string id, Vector2 size, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPadded(id, size, bgCol, GetChildRounding(), dFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant that accepts InnerContentRegion dimensions, handling padding internally. 
    /// </summary>
    /// <remarks> In this method, <paramref name="size"/> is expected to be the innerContentRegion() </remarks>
    public static IEndObjectContainer ChildPadded(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, rounding, 0, dFlags), ImGui.BeginChild(id, size, false, wFlags.WithPadding()), size.WithoutWinPadding());




    /// <inheritdoc cref="FramedChildPaddedH(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedH(id, width, height, bgCol, GetChildRounding(), dFlags, wFlags);

    /// <inheritdoc cref="FramedChildPaddedH(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedH(id, width, height, bgCol, rounding, 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="width"/> as the InnerContentRegion().X dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="width"/> will have its winPaddingX appended before the child is made. </remarks>
    public static IEndObjectContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, new Vector2(width.AddWinPadX(), height), bgCol, rounding, thickness, dFlags, wFlags);


    /// <inheritdoc cref="FramedChildPaddedW(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedW(id, width, height, bgCol, GetChildRounding(), 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <inheritdoc cref="FramedChildPaddedW(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedW(id, width, height, bgCol, rounding, 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="height"/> as the InnerContentRegion().Y dimension, and internally handles padding. 
    /// </summary>
    /// <remarks> The passed in <paramref name="height"/> will have its winPaddingY appended before the child is made. </remarks>
    public static IEndObjectContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, new Vector2(width, height.AddWinPadY()), bgCol, rounding, thickness, dFlags, wFlags);



    /// <inheritdoc cref="FrameChildPadded(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FrameChildPadded(string id, Vector2 size, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, size, bgCol, GetChildRounding(), dFlags, wFlags);

    /// <inheritdoc cref="FrameChildPadded(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEndObjectContainer FrameChildPadded(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, size, bgCol, GetChildRounding(), 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary>
    ///     ImRaii.Child variant that accepts InnerContentRegion dimensions, handling padding internally.
    /// </summary>
    /// <remarks> In this method, <paramref name="size"/> is expected to be the outerContentRegion(), that will be padded. </remarks>
    public static IEndObjectContainer FrameChildPadded(string id, Vector2 size, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, rounding, thickness, dFlags), ImGui.BeginChild(id, size, false, wFlags.WithPadding()), size.WithoutWinPadding());
}
