using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.CkCommons.ImageHandling;
using GagSpeak.Services.Textures;
using ImGuiNET;
using System.Windows.Forms;

namespace GagSpeak.UI.Components;
public class ClipboardToThumbnail
{
    private readonly ILogger<ClipboardToThumbnail> _logger;
    private readonly CosmeticService _cosmetics;

    private IDalamudTextureWrap? _pastedWrap;
    public ClipboardToThumbnail(ILogger<ClipboardToThumbnail> logger, CosmeticService cosmetics)
    {
        _logger = logger;
        _cosmetics = cosmetics;
    }

    public void DrawClipboardImage(Vector2 size)
    {
        if (CkGui.IconTextButton(FAI.FileAlt, "Paste From Clipboard."))
        {
            if (Clipboard.ContainsImage())
            {
                var byteArray = ImageDataHandling.GetClipboardImageBytes();
                _pastedWrap = _cosmetics.GetImageFromBytes(byteArray);
            }
            else
            {
                _logger.LogError("Clipboard does not contain an image.");
            }
        }
        ImGui.SameLine();
        if (CkGui.IconTextButton(FAI.Trash, "Clear Pasted Image."))
        {
            _pastedWrap = null;
        }

        // Display the image here if we can.
        if (_pastedWrap is { } wrap)
        {
            // get the aspect ratio of the image, and scale it to the drawn content region so its ratio remains.
            var aspect = wrap.Width / (float)wrap.Height;
            var width = ImGui.GetContentRegionAvail().X;
            var height = width / aspect;
            ImGui.Image(wrap.ImGuiHandle, new Vector2(width, height));
        }
    }
}
