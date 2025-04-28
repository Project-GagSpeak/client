using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size));

    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol, WFlags flags)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size, false, flags));

    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol, float rounding)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size));

    public static ImRaii.IEndObject FramedChild(string id, Vector2 size, uint bgCol, float rounding, WFlags flags)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size, false, flags));

    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol)
    => new EndUnconditionally(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size.WithPadding()));

    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol, WFlags flags)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size.WithPadding(), false, flags.WithPadding()));

    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol, float rounding)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size.WithPadding()));

    public static ImRaii.IEndObject FramedChildPadded(string id, Vector2 size, uint bgCol, float rounding, WFlags flags)
        => new EndUnconditionally(() => FramedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size.WithPadding(), false, flags.WithPadding()));


    private static void FramedChildEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();
        ImGui.GetWindowDrawList().AddRectFilled(min, max, bgCol, rounding, ImDrawFlags.RoundCornersAll);
        ImGui.GetWindowDrawList().AddRect(min, max, bgCol, rounding, ImDrawFlags.None, 2);
    }
}
