using Dalamud.Interface.Utility;
using ImGuiNET;

namespace GagSpeak.CkCommons;
public static partial class CkStyle
{
    // Flag Style Helpers
    public static WFlags WithPadding(this WFlags flags) => flags |= WFlags.AlwaysUseWindowPadding;

    public static float GetFrameRowsHeight(uint rows)
        => rows == 0 ? ImGui.GetFrameHeight()
        : ImGui.GetFrameHeightWithSpacing() * (rows - 1) + ImGui.GetFrameHeight();


    // Size Helpers
    public static float RemoveWinPadX(this float s) => s - ImGui.GetStyle().WindowPadding.X * 2;
    public static float RemoveWinPadY(this float s) => s - ImGui.GetStyle().WindowPadding.Y * 2;
    public static float AddWinPadX(this float s) => s + ImGui.GetStyle().WindowPadding.X * 2;
    public static float AddWinPadY(this float s) => s + ImGui.GetStyle().WindowPadding.Y * 2;
    public static Vector2 WithoutWinPadding(this Vector2 s) => s - ImGui.GetStyle().WindowPadding * 2;
    public static Vector2 WithWinPadding(this Vector2 s) => s + ImGui.GetStyle().WindowPadding * 2;


    // Measurement Helpers
    public static float ThreeRowHeight() => ImGui.GetFrameHeightWithSpacing() * 2 + ImGui.GetFrameHeight();
    public static float TwoRowHeight() => ImGui.GetFrameHeightWithSpacing() + ImGui.GetFrameHeight();

    public static float HeaderHeight() => ImGui.GetFrameHeight() + 2 * ImGuiHelpers.GlobalScale;
    public static float ChildRounding() => ImGui.GetStyle().FrameRounding * 1.25f;
    public static float ChildRoundingLarge() => ImGui.GetStyle().FrameRounding * 1.75f;
    public static float HeaderRounding() => ImGui.GetStyle().FrameRounding * 2f;
    public static float FrameThickness() => ImGui.GetStyle().WindowPadding.X / 2;
}
