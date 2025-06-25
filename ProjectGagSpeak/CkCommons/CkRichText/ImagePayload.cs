using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.CkCommons.Raii;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons;

public class ImagePayload : RichPayload
{
    private Func<IDalamudTextureWrap?>? _wrapFunc;
    public string ImagePath { get; }
    public ImagePayload(string imagePath)
    {
        ImagePath = imagePath;
    }

    public ImagePayload(Func<IDalamudTextureWrap?> wrapFunc)
    {
        _wrapFunc = wrapFunc;
    }

    public override void Draw(CkRaii.RichColor _)
    {
        if (IsInline)
            ImGui.SameLine(0, 0);

        var imageSize = new Vector2(ImGui.GetTextLineHeight());
        var img = _wrapFunc is not null
            ? _wrapFunc.Invoke()
            : Svc.Texture.GetFromFile(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", ImagePath)).GetWrapOrDefault();
        // draw based on texture validity.
        if (img is { } validTexture)
            ImGui.Image(validTexture.ImGuiHandle, imageSize);
        else
            ImGui.Dummy(imageSize); // Fallback to dummy if texture is invalid.
    }

    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        if (curLineWidth != 0f)
            IsInline = true;
        // assert the new curLineWidth
        var newLineWidth = curLineWidth + ImGui.GetTextLineHeight();
        curLineWidth = newLineWidth > wrapWidth ? 0 : newLineWidth;
    }
}
