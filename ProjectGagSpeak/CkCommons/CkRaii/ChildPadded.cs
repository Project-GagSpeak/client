using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace GagSpeak.CkCommons.Raii;
public static partial class CkRaii
{
    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size)
        => new EndUnconditionally(() => PaddedChildEndAction(0, GetChildRounding()), ImGui.BeginChild(id, size.WithPadding(), false, WFlags.AlwaysUseWindowPadding));

    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size, uint bgCol)
        => new EndUnconditionally(() => PaddedChildEndAction(bgCol, GetChildRounding()), ImGui.BeginChild(id, size.WithPadding(), false, WFlags.AlwaysUseWindowPadding));

    public static ImRaii.IEndObject ChildPadded(string id, Vector2 size, uint bgCol, float rounding)
        => new EndUnconditionally(() => PaddedChildEndAction(bgCol, rounding), ImGui.BeginChild(id, size.WithPadding(), false, WFlags.AlwaysUseWindowPadding));

    private static void PaddedChildEndAction(uint bgCol, float rounding)
    {
        ImGui.EndChild();
        if(bgCol is not 0)
            ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), bgCol, rounding, ImDrawFlags.RoundCornersAll);
    }
}
