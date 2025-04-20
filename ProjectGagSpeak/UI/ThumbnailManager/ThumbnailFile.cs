using Dalamud.Interface.Textures.TextureWraps;

namespace GagSpeak.UI;

public class ThumbnailFile : IDisposable
{
    public string FileName { get; init; }
    public IDalamudTextureWrap? Icon { get; init; }

    public ThumbnailFile(string fileName, IDalamudTextureWrap? icon)
    {
        FileName = fileName;
        Icon = icon;
    }

    public void Dispose()
    {
        Icon?.Dispose();
    }
}
