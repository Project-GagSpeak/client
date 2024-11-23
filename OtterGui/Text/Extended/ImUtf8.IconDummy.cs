using ImGuiNET;

namespace OtterGui.Text;

public static partial class ImUtf8
{
    /// <summary> Draw a transparent dummy the size of an icon button. </summary>
    public static void IconDummy()
        => ImGui.Dummy(new Vector2(FrameHeight));
}
