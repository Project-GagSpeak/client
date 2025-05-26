using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="ChildPaddedH(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPaddedH(string id, float width, float height, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedH(id, width, height, 0,  CkStyle.ChildRounding(), rFlags, wFlags);

    /// <inheritdoc cref="ChildPaddedH(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPaddedH(string id, float width, float height, uint bgCol, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedH(id, width, height, bgCol,  CkStyle.ChildRounding(), rFlags, wFlags);

    /// <summary>
    ///     ImRaii.Child variant, accepting <paramref name="width"/> as the InnerContentRegion().X dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="width"/> will have its winPaddingX appended before the child is made. </remarks>
    public static IEOContainer ChildPaddedH(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => Child(id, new Vector2(width.AddWinPadX(), height), bgCol, rounding, rFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="ChildPaddedW(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPaddedW(string id, float width, float height, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedW(id, width, height, 0,  CkStyle.ChildRounding(), rFlags, wFlags);

    /// <inheritdoc cref="ChildPaddedW(string, float, float, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPaddedW(string id, float width, float height, uint bgCol, ImDrawFlags rFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => ChildPaddedW(id, width, height, bgCol,  CkStyle.ChildRounding(), rFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="height"/> as the InnerContentRegion().Y dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="height"/> will have its winPaddingY appended before the child is made. </remarks>
    public static IEOContainer ChildPaddedW(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => Child(id, new Vector2(width, height.AddWinPadY()), bgCol, rounding, dFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="ChildPadded(string, Vector2, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPadded(string id, Vector2 size, ImDrawFlags dFlags = ImDrawFlags.None)
        => ChildPadded(id, size, 0,  CkStyle.ChildRounding(), dFlags);

    /// <inheritdoc cref="ChildPadded(string, Vector2, uint, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer ChildPadded(string id, Vector2 size, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None)
        => ChildPadded(id, size, bgCol,  CkStyle.ChildRounding(), dFlags);

    /// <summary> 
    ///     ImRaii.Child variant that accepts InnerContentRegion dimensions, handling padding internally. 
    /// </summary>
    /// <remarks> In this method, <paramref name="size"/> is expected to be the innerContentRegion() </remarks>
    public static IEOContainer ChildPadded(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => Child(id, size.WithWinPadding(), bgCol, rounding, dFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="FramedChildPaddedH(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedH(id, width, height, bgCol,  CkStyle.ChildRounding(), dFlags, wFlags);

    /// <inheritdoc cref="FramedChildPaddedH(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedH(id, width, height, bgCol, rounding, 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="width"/> as the InnerContentRegion().X dimension, and internally handles padding.
    /// </summary>
    /// <remarks> The passed in <paramref name="width"/> will have its winPaddingX appended before the child is made. </remarks>
    public static IEOContainer FramedChildPaddedH(string id, float width, float height, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, new Vector2(width.AddWinPadX(), height), bgCol, rounding, thickness, dFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="FramedChildPaddedW(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedW(id, width, height, bgCol,  CkStyle.ChildRounding(), 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <inheritdoc cref="FramedChildPaddedW(string, float, float, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChildPaddedW(id, width, height, bgCol, rounding, 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary> 
    ///     ImRaii.Child variant, accepting <paramref name="height"/> as the InnerContentRegion().Y dimension, and internally handles padding. 
    /// </summary>
    /// <remarks> The passed in <paramref name="height"/> will have its winPaddingY appended before the child is made. </remarks>
    public static IEOContainer FramedChildPaddedW(string id, float width, float height, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, new Vector2(width, height.AddWinPadY()), bgCol, rounding, thickness, dFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="FramedChildPaddedWH(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedWH(string id, Vector2 size, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size, bgCol,  CkStyle.ChildRounding(), 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags.WithPadding());

    /// <inheritdoc cref="FramedChildPaddedWH(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChildPaddedWH(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size, bgCol, rounding, 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags.WithPadding());

    /// <summary> 
    ///     ImRaii.Child variant, accepting the size as the ContentRegion(), and internally handles padding. 
    /// </summary>
    public static IEOContainer FramedChildPaddedWH(string id, Vector2 size, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size, bgCol, rounding, thickness, dFlags, wFlags.WithPadding());

    ////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////

    /// <inheritdoc cref="FrameChildPadded(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FrameChildPadded(string id, Vector2 size, uint bgCol, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, size, bgCol,  CkStyle.ChildRounding(), dFlags, wFlags);

    /// <inheritdoc cref="FrameChildPadded(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FrameChildPadded(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FrameChildPadded(id, size, bgCol,  CkStyle.ChildRounding(), 2 * ImGuiHelpers.GlobalScale, dFlags, wFlags);

    /// <summary>
    ///     ImRaii.Child variant that accepts InnerContentRegion dimensions, handling padding internally.
    /// </summary>
    /// <remarks> In this method, <paramref name="size"/> is expected to be the InnerContentRegion(), that will be padded. </remarks>
    public static IEOContainer FrameChildPadded(string id, Vector2 size, uint bgCol, float rounding, float thickness, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size.WithWinPadding(), bgCol, rounding, thickness, dFlags, wFlags.WithPadding());
}
