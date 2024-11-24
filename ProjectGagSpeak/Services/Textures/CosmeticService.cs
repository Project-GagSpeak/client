using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.Achievements;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace GagSpeak.Services.Textures;

// Friendly Reminded, this is a scoped service, and IDalamudTextureWraps will only return values on the framework thread.
// Attempting to use or access this class to obtain information outside the framework draw update thread will result in a null return.
public class CosmeticService : IHostedService, IDisposable
{
    private readonly ILogger<CosmeticService> _logger;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly ITextureProvider _textures;
    private readonly IDalamudPluginInterface _pi;
    public CosmeticService(ILogger<CosmeticService> logger, GagspeakMediator mediator,
        OnFrameworkService frameworkUtils, IDalamudPluginInterface pi, ITextureProvider tp)
    {
        _logger = logger;
        _frameworkUtils = frameworkUtils;
        _textures = tp;
        _pi = pi;
    }

    private Dictionary<string, IDalamudTextureWrap> InternalCosmeticCache = [];
    public Dictionary<CorePluginTexture, IDalamudTextureWrap> CorePluginTextures = [];

    // MUST ensure ALL images are disposed of or else we will leak a very large amount of memory.
    public void Dispose()
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Disposing.");
        foreach (var texture in CorePluginTextures.Values)
            texture?.Dispose();
        foreach (var texture in InternalCosmeticCache.Values)
            texture?.Dispose();

        // clear the dictionary, erasing all disposed textures.
        CorePluginTextures.Clear();
        InternalCosmeticCache.Clear();
    }

    public void LoadAllCoreTextures()
    {
        foreach (var label in CosmeticLabels.NecessaryImages)
        {
            var key = label.Key;
            var path = label.Value;
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("Cosmetic Key: " + key + " Texture Path is Empty.");
                return;
            }

            _logger.LogDebug("Renting image to store in Cache: " + key, LoggerType.Textures);
            if(TryRentImageFromFile(path, out var texture))
                CorePluginTextures[key] = texture;
        }
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Fetched all NecessaryImages!", LoggerType.Cosmetics);
    }

    public void LoadAllCosmetics()
    {
        // load in all the images to the dictionary by iterating through all public const strings stored in the cosmetic labels
        // and appending them as new texture wraps that should be stored into the cache.
        foreach (var label in CosmeticLabels.CosmeticTextures)
        {
            var key = label.Key;
            var path = label.Value;
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogError("Cosmetic Key: " + key + " Texture Path is Empty.");
                return;
            }

            _logger.LogDebug("Renting image to store in Cache: " + key, LoggerType.Textures);
            if (TryRentImageFromFile(path, out var texture))
                InternalCosmeticCache[key] = texture;
        }
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Fetched all Cosmetic Images!", LoggerType.Cosmetics);

    }

    /// <summary>
    /// Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists.
    /// </summary>
    /// <returns>True if the texture is valid, false otherwise. If returning false, the wrap WILL BE NULL. </returns>
    public bool TryGetBackground(ProfileComponent section, ProfileStyleBG style, out IDalamudTextureWrap value)
    {
        // See if the item exists in our GagSpeak Cache Service.
        if(InternalCosmeticCache.TryGetValue(section.ToString() + "_Background_" + style.ToString(), out var texture))
        {
            value = texture;
            return true;
        }
        // not valid, so return false.
        value = null!;
        return false;
    }

    /// <summary>
    /// Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists.
    /// </summary>
    /// <returns>True if the texture is valid, false otherwise. If returning false, the wrap WILL BE NULL. </returns>
    public bool TryGetBorder(ProfileComponent section, ProfileStyleBorder style, out IDalamudTextureWrap value)
    {
        if(InternalCosmeticCache.TryGetValue(section.ToString() + "_Border_" + style.ToString(), out var texture))
        {
            value = texture;
            return true;
        }
        value = null!;
        return false;
    }

    /// <summary>
    /// Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists.
    /// </summary>
    /// <returns>True if the texture is valid, false otherwise. If returning false, the wrap WILL BE NULL. </returns>
    public bool TryGetOverlay(ProfileComponent section, ProfileStyleOverlay style, out IDalamudTextureWrap value)
    {
        if(InternalCosmeticCache.TryGetValue(section.ToString() + "_Overlay_" + style.ToString(), out var texture))
        {
            value = texture;
            return true;
        }
        value = null!;
        return false;
    }

    public (IDalamudTextureWrap? SupporterWrap, string Tooltip) GetSupporterInfo(UserData userData)
    {
        IDalamudTextureWrap? supporterWrap = null;
        string tooltipString = string.Empty;

        switch (userData.SupporterTier)
        {
            case CkSupporterTier.ServerBooster:
                supporterWrap = CorePluginTextures[CorePluginTexture.SupporterBooster];
                tooltipString = userData.AliasOrUID + " is supporting the discord with a server Boost!";
                break;

            case CkSupporterTier.IllustriousSupporter:
                supporterWrap = CorePluginTextures[CorePluginTexture.SupporterTier1];
                tooltipString = userData.AliasOrUID + " is supporting CK as an Illustrious Supporter";
                break;

            case CkSupporterTier.EsteemedPatron:
                supporterWrap = CorePluginTextures[CorePluginTexture.SupporterTier2];
                tooltipString = userData.AliasOrUID + " is supporting CK as an Esteemed Patron";
                break;

            case CkSupporterTier.DistinguishedConnoisseur:
                supporterWrap = CorePluginTextures[CorePluginTexture.SupporterTier3];
                tooltipString = userData.AliasOrUID + " is supporting CK as a Distinguished Connoisseur";
                break;

            case CkSupporterTier.KinkporiumMistress:
                supporterWrap = CorePluginTextures[CorePluginTexture.SupporterTier4];
                tooltipString = userData.AliasOrUID + " is the Shop Mistress of CK, and the Dev of GagSpeak.";
                break;

            default:
                tooltipString = userData.AliasOrUID + " has an unknown supporter tier.";
                break;
        }

        return (supporterWrap, tooltipString);
    }


    public IDalamudTextureWrap GetImageFromDirectoryFile(string path)
        => _textures.GetFromFile(Path.Combine(_pi.AssemblyLocation.DirectoryName!, "Assets", path)).GetWrapOrEmpty();
    public IDalamudTextureWrap GetProfilePicture(byte[] imageData)
        => _textures.CreateFromImageAsync(imageData).Result;

    private bool TryRentImageFromFile(string path, out IDalamudTextureWrap fileTexture)
    {
        try
        {
            var image = _textures.GetFromFile(Path.Combine(_pi.AssemblyLocation.DirectoryName!, "Assets", path)).RentAsync().Result;
            fileTexture = image;
            return true;
        }
        catch (Exception)
        {
            // TODO: Remove surpression once we have defined proper images.
            //_logger.LogWarning($"Failed to load texture from path: {path}");
            fileTexture = null!;
            return false;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Started.");
        LoadAllCoreTextures();
        LoadAllCosmetics();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Stopped.");
        return Task.CompletedTask;
    }

}
