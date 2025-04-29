using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size, WFlags flags = WFlags.None)
        => new EndObjectContainer(() => PaddedChildEndAction(0, GetChildRounding()), ImGui.BeginChild(id, size, false, flags.WithPadding()), size.MinusWinPadding());

    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size, uint bgCol, WFlags flags = WFlags.None)
        => new EndObjectContainer(() => PaddedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size, false, flags.WithPadding()), size.MinusWinPadding());

    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size, uint bgCol, float rounding, WFlags flags = WFlags.None)
        => new EndObjectContainer(() => PaddedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size, false, flags.WithPadding()), size.MinusWinPadding());

    // Padded Child End Action
    private static void PaddedChildEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        if(bgCol is not 0)
        {
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.RoundCornersAll);
        }
    }

    /// <inheritdoc cref="FramedChild(string, Vector2, uint, WFlags)"/>"
    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol, WFlags flags = WFlags.None)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size, false, flags));

    /// <summary> Draws a framed child. </summary>
    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol, float rounding, WFlags flags = WFlags.None)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size, false, flags));

    /// <inheritdoc cref="FramedChildPadded(string, Vector2, uint, float, WFlags)"/>"
    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol, WFlags flags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size, false, flags.WithPadding()), size.MinusWinPadding());

    /// <summary> Draws a framed child with padding. </summary>
    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol, float rounding, WFlags flags = WFlags.None)
        => new EndObjectContainer(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size, false, flags.WithPadding()), size.MinusWinPadding());

    // Framed Child End Action
    private static void FramedChildEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        ImGui.GetWindowDrawList().AddRectFilled(min, max, bgCol, rounding, ImDrawFlags.RoundCornersAll);
        ImGui.GetWindowDrawList().AddRect(min, max, bgCol, rounding, ImDrawFlags.None, 2);
    }
}
