using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin.Services;

namespace OtterGui.Classes;

public class TextureCache(IDataManager dataManager, ITextureProvider textureProvider)
{
    public readonly IDataManager     DataManager     = dataManager;
    public readonly ITextureProvider TextureProvider = textureProvider;

    public IDalamudTextureWrap? LoadIcon(uint iconId)
    {
        var icon = TextureProvider.GetFromGameIcon(new GameIconLookup(iconId));
        if (!icon.TryGetWrap(out var wrap, out _))
            return null;

        return wrap;
    }

    public IDalamudTextureWrap? LoadFile(string file)
    {
        var texture = Path.IsPathRooted(file)
            ? TextureProvider.GetFromFile(file)
            : TextureProvider.GetFromGame(file);

        if (!texture.TryGetWrap(out var wrap, out _))
            return null;

        return wrap;
    }

    public bool TryLoadIcon(uint iconId, [NotNullWhen(true)] out IDalamudTextureWrap? wrap)
    {
        wrap = LoadIcon(iconId);
        return wrap != null;
    }

    public bool TryLoadFile(string file, [NotNullWhen(true)] out IDalamudTextureWrap? wrap)
    {
        wrap = LoadFile(file);
        return wrap != null;
    }

    public bool FileExists(string file)
        => Path.IsPathRooted(file) ? File.Exists(file) : DataManager.FileExists(file);

    public bool IconExists(uint iconId)
        => DataManager.FileExists(TextureProvider.GetIconPath(new GameIconLookup(iconId)))
         || DataManager.FileExists(TextureProvider.GetIconPath(new GameIconLookup(iconId, false, false)));
}
