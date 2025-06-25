using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.CkCommons.Raii;
using ImGuiNET;
using OtterGui.Text;

namespace GagSpeak.CkCommons;

public class ImagePayload : RichPayload
{
    /// <summary> if his image should be drawn inline or not. </summary>
    private bool _isInline = false;

    /// <summary> A potential function that obtains our texture wrap for us, over an image path. </summary>
    private readonly Func<IDalamudTextureWrap?>? _imageFunc;

    /// <summary> If no func provided, this image path should map to a valid image in the asset folder. </summary>
    public string _path { get; }

    public ImagePayload(string imagePath)
        => _path = imagePath;
    public ImagePayload(Func<IDalamudTextureWrap?> wrapFunc)
        => _imageFunc = wrapFunc;

    /// <summary> Draws out the image to ImGui. </summary>
    public void Draw()
    {
        if (_isInline)
            ImGui.SameLine(0, 0);

        var img = _imageFunc is not null
            ? _imageFunc.Invoke()
            : Svc.Texture.GetFromFile(Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", _path)).GetWrapOrDefault();
        // draw based on texture validity.
        if (img is { } validTexture)
            ImGui.Image(validTexture.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight()));
        else
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight())); // Fallback to dummy if texture is invalid.
    }

    public override void UpdateCache(ImFontPtr font, float wrapWidth, ref float curLineWidth)
    {
        if (curLineWidth != 0f)
            _isInline = true;

        // assert the new curLineWidth
        var newLineWidth = curLineWidth + ImGui.GetTextLineHeight();
        curLineWidth = newLineWidth > wrapWidth ? 0 : newLineWidth;
    }
}
