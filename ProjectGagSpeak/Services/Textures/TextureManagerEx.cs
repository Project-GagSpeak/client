using CkCommons.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.Services.Configs;
using GagspeakAPI.Util;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services.Textures;

/// <summary>
///     Friendly Reminder, all methods in this class must be called in the framework thread or they will fail.
/// </summary>
public static class TextureManagerEx
{
    public static IDalamudTextureWrap? GagImage(GagType gagType)
        => TextureManager.AssetImageOrDefault($"GagImages\\{gagType.GagName()}.png");

    public static IDalamudTextureWrap? PadlockImage(Padlocks padlock)
        => TextureManager.AssetImageOrDefault($"PadlockImages\\{padlock}.png");
    
    public static IDalamudTextureWrap GetProfilePicture(byte[] imageData)
        => Svc.Texture.CreateFromImageAsync(imageData).Result;
    
    public static IDalamudTextureWrap? GetMetadataPath(ImageDataType folder, string path)
        => Svc.Texture.GetFromFile(Path.Combine(ConfigFileProvider.ThumbnailDirectory, folder.ToString(), path)).GetWrapOrDefault();

    public static async Task<IDalamudTextureWrap?> RentMetadataPath(ImageDataType folder, string path)
        => await TextureManager.RentTextureAsync(Path.Combine(ConfigFileProvider.ThumbnailDirectory, folder.ToString(), path));

    public static bool TryRentAssetImage(string path, [NotNullWhen(true)] out IDalamudTextureWrap? fileTexture)
        => TextureManager.TryRentAssetDirectoryImage(path, out fileTexture);
}
