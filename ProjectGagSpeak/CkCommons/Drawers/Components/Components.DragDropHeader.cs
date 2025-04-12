using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Services;
using GagSpeak.UI;
using ImGuiNET;
using OtterGui;

namespace GagSpeak.CkCommons.Helpers;

public static partial class CkComponents
{
    public static float SideHeaderRounding => ImGui.GetStyle().FrameRounding * 2f;
    public static float SideHeaderHeight => ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y + ImGui.GetStyle().WindowPadding.Y * 2;

    public static ImRaii.IEndObject SideHeaderChild(string id, string text, float leftWidth)
        => new DragDropHeaderChild(id, text, leftWidth, SideHeaderHeight, SideHeaderRounding, WFlags.None);

    public static ImRaii.IEndObject SideHeaderChild(string id, string text, float leftWidth, float height)
        => new DragDropHeaderChild(id, text, leftWidth, SideHeaderHeight, SideHeaderRounding, WFlags.None);

    public static ImRaii.IEndObject SideHeaderChild(string id, string text, float leftWidth, float height, float rounding)
        => new DragDropHeaderChild(id, text, leftWidth, SideHeaderHeight, rounding, WFlags.None);

    public static ImRaii.IEndObject SideHeaderChild(string id, string text, float leftWidth, float height, float rounding, WFlags flags)
        => new DragDropHeaderChild(id, text, leftWidth, SideHeaderHeight, rounding, flags);

    private struct DragDropHeaderChild : ImRaii.IEndObject
    {
        private Action EndAction { get; }
        public bool Success { get; }
        public bool Disposed { get; private set; }

        public DragDropHeaderChild(string id, string txt, float leftWidth, float height, float bend, WFlags flags)
        {
            // Begin the group combining the two elements.
            ImGui.BeginGroup();
            // Draw out the left child, then shift the cursor pos for the right child.
            var pos = ImGui.GetCursorScreenPos();
            DrawSideHeader(pos, txt, leftWidth, height, bend);
            ImGui.SetCursorScreenPos(pos + new Vector2(leftWidth, 0));
            // then the child object.
            this.Success = ImGui.BeginChild(id, new Vector2(ImGui.GetContentRegionAvail().X, height), false, flags);
            this.Disposed = false;

            EndAction = () =>
            {
                ImGui.EndChild();
                // Fill the BG of it.
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), CkColor.ElementBG.Uint(), SideHeaderRounding, ImDrawFlags.RoundCornersRight);
                // end the encapsulation group.
                ImGui.EndGroup();
            };
        }

        public void Dispose()
        {
            if (this.Disposed)
                return;

            this.EndAction();
            this.Disposed = true;
        }
    }

    /// <summary> Places a header left-aligned beside a child window. </summary>
    private static void DrawSideHeader(Vector2 startPos, string text, float width, float height, float rounding)
    {
        var wdl = ImGui.GetWindowDrawList();
        var min = startPos;
        var max = startPos + new Vector2(width, height);
        var linePos = min + new Vector2(width, 0);

        // Draw the child background with the element header color.
        wdl.AddRectFilled(min, max, CkColor.ElementHeader.Uint(), rounding, ImDrawFlags.RoundCornersLeft);
        // Draw the line off to the left.
        wdl.AddLine(linePos, linePos with { Y = max.Y }, CkColor.ElementSplit.Uint(), 2);
        var textStart = new Vector2((width - ImGui.CalcTextSize(text).X) / 2, (height - ImGui.GetTextLineHeight())/2);
        wdl.AddText(min + textStart, ImGui.GetColorU32(ImGuiCol.Text), text);
    }
}
