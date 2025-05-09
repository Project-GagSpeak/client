using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static ImRaii.IEndObject Child(string id)
        => new EndUnconditionally(() => ImGui.EndChild(), ImGui.BeginChild(id));

    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static IEOContainer Child(string id, Vector2 size, WFlags flags = WFlags.None)
        => Child(id, size, 0, GetChildRounding(), ImDrawFlags.None, flags);

    /// <inheritdoc cref="Child(string, Vector2, uint, float ImDrawFlags, WFlags)"/>"
    public static IEOContainer Child(string id, Vector2 size, uint bgCol, WFlags flags = WFlags.None)
        => Child(id, size, bgCol, GetChildRounding(), ImDrawFlags.None, flags);

    /// <summary> ImRaii.Child alternative with bgCol and rounding support. </summary>
    /// <remarks> The IEndObject returned is a EndObjectContainer, holding the inner content region size. </remarks>
    public static IEOContainer Child(string id, Vector2 size, uint bgCol, float rounding, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => FramedChild(id, size, bgCol, rounding, 0, dFlags, wFlags);



    /// <inheritdoc cref="FramedChild(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChild(string id, uint bgCol, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, 0, thickness, dFlags), ImGui.BeginChild(id), ImGui.GetContentRegionAvail());

    /// <inheritdoc cref="FramedChild(string, Vector2, uint, float, float, ImDrawFlags, WFlags)"/>/>
    public static IEOContainer FramedChild(string id, Vector2 size, uint bgCol, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None, WFlags flags = WFlags.None)
        => FramedChild(id, size, bgCol, GetChildRounding(), thickness, dFlags, flags);

    /// <summary> ImRaii.Child alternative with bgCol and rounding support. (Supports frames) </summary>
    /// <remarks> The IEndObject returned is a EndObjectContainer, holding the inner content region size. </remarks>
    public static IEOContainer FramedChild(string id, Vector2 size, uint bgCol, float rounding, float thickness = 0, ImDrawFlags dFlags = ImDrawFlags.None, WFlags wFlags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, rounding, thickness, dFlags), ImGui.BeginChild(id, size, false, wFlags), (wFlags & WFlags.AlwaysUseWindowPadding) != 0 ? size.WithoutWinPadding() : size);

    private static void FramedChildEndAction(uint bgCol, float rounding, float frameThickness, ImDrawFlags corners)
    {
        ImGui.EndChild();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        // Draw out the child BG.
        if (bgCol is not 0)
            ImGui.GetWindowDrawList().AddRectFilled(min, max, bgCol, rounding, corners);
        // Draw out the frame.
        if (frameThickness is not 0)
            ImGui.GetWindowDrawList().AddRect(min, max, bgCol, rounding, corners, frameThickness);
    }
}
