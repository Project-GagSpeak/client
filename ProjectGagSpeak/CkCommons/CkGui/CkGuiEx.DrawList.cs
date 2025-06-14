using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;

namespace GagSpeak.CkCommons.Gui;

public enum CkGuiCircleBound : byte
{
    TopLeft = 0,
    TopRight = 1,
    BottomLeft = 2,
    BottomRight = 3
}


// ImGui Window Draw List Extentions.
public static class CkGuiEx
{
    /// <summary> A variant of ImGui's AddImageRounded that uses IDalamudTextureWraps for you. </summary>
    public static void AddDalamudImageRounded(this ImDrawListPtr wdl, IDalamudTextureWrap? wrap, Vector2 pos, Vector2 size, float rounding, string tt = "")
    {
        // Ensure the wrap is valid for drawing.
        if (wrap is { } valid)
            wdl.AddImageRounded(valid.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, 0xFFFFFFFF, rounding);
        CkGui.AttachToolTipRect(pos, pos + size, tt);
    }

    public static void AddDalamudImage(this ImDrawListPtr wdl, IDalamudTextureWrap? wrap, Vector2 pos, Vector2 size, string tt = "")
    {
        if (wrap is { } valid)
            wdl.AddImage(valid.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, 0xFFFFFFFF);
        CkGui.AttachToolTipRect(pos, pos + size, tt);
    }

    public static void AddDalamudImage(this ImDrawListPtr wdl, IDalamudTextureWrap? wrap, Vector2 pos, Vector2 size, uint tint, string tt = "")
    {
        if (wrap is { } valid)
            wdl.AddImage(valid.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, tint);
        CkGui.AttachToolTipRect(pos, pos + size, tt);
    }

    public static void AddDalamudImage(this ImDrawListPtr wdl, IDalamudTextureWrap? wrap, Vector2 pos, Vector2 size, Vector4 tint, string tt = "")
    {
        if (wrap is { } valid)
            wdl.AddImage(valid.ImGuiHandle, pos, pos + size, Vector2.Zero, Vector2.One, CkGui.Color(tint));
        CkGui.AttachToolTipRect(pos, pos + size, tt);
    }
}
