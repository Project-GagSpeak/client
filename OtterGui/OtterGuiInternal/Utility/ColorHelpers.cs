using ImGuiNET;

namespace OtterGuiInternal.Utility;

public static class ColorHelpers
{
    /// <summary> Obtain the correct color for a frame depending on mouse state. </summary>
    /// <param name="hovered"> Whether the object is hovered. </param>
    /// <param name="held"> Whether a mouse button is held down on the object. </param>
    /// <returns> The frame color. </returns>
    public static uint GetFrameBg(bool hovered, bool held)
        => ImGui.GetColorU32((hovered, held) switch
        {
            (true, true)  => ImGuiCol.FrameBgActive,
            (true, false) => ImGuiCol.FrameBgHovered,
            _             => ImGuiCol.FrameBg,
        });

    /// <summary> Obtain the correct color for a button depending on mouse state. </summary>
    /// <param name="hovered"> Whether the button is hovered. </param>
    /// <param name="held"> Whether a mouse button is held down on the button. </param>
    /// <returns> The frame color. </returns>
    public static uint GetButtonColor(bool hovered, bool held)
        => ImGui.GetColorU32((hovered, held) switch
        {
            (true, true)  => ImGuiCol.ButtonActive,
            (true, false) => ImGuiCol.ButtonHovered,
            _             => ImGuiCol.Button,
        });
}
