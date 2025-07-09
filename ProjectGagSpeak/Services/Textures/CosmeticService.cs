using CkCommons.RichText;
using CkCommons.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Services.Textures;

/// <summary>
///     Friendly Reminder, all methods in this class must be called in the framework thread or they will fail.
/// </summary>
public class CosmeticService : IHostedService, IDisposable
{
    private readonly ILogger<CosmeticService> _logger;
    public CosmeticService(ILogger<CosmeticService> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        CoreTextures = TextureManager.CreateEnumTextureCache(CosmeticLabels.NecessaryImages);
        EmoteTextures = TextureManager.CreateEnumTextureCache(CosmeticLabels.ChatEmoteTextures);
        IntifaceTextures = TextureManager.CreateEnumTextureCache(CosmeticLabels.IntifaceImages);
        CkRichText.DefineEmoteResolver(TryResolveEmote);

        LoadAllCosmetics();
    }
    
    public static EnumTextureCache<CoreTexture>         CoreTextures;
    public static EnumTextureCache<CoreEmoteTexture>    EmoteTextures;
    public static EnumTextureCache<CoreIntifaceTexture> IntifaceTextures;
    private static ConcurrentDictionary<string, IDalamudTextureWrap> InternalCosmeticCache = [];

    public void Dispose()
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Disposing.");
        foreach (var texture in InternalCosmeticCache.Values)
            texture?.Dispose();

        InternalCosmeticCache.Clear();
    }

    private IDalamudTextureWrap? TryResolveEmote(string name)
        => Enum.TryParse<CoreEmoteTexture>(name, out var key) ? EmoteTextures.Cache.GetValueOrDefault(key) : null;

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
            if (TextureManager.TryRentAssetDirectoryImage(path, out var texture))
                InternalCosmeticCache[key] = texture;
        }
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Fetched all Cosmetic Images!", LoggerType.Textures);
    }

    /// <summary> Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists. </summary>
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

    /// <summary> Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists. </summary>
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

    /// <summary> Grabs the texture from GagSpeak Cosmetic Cache Service, if it exists. </summary>
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
        var tooltipString = string.Empty;

        switch (userData.Tier)
        {
            case CkSupporterTier.ServerBooster:
                supporterWrap = CoreTextures.Cache[CoreTexture.TierBoosterIcon];
                tooltipString = userData.AliasOrUID + " is supporting the discord with a server Boost!";
                break;

            case CkSupporterTier.IllustriousSupporter:
                supporterWrap = CoreTextures.Cache[CoreTexture.Tier1Icon];
                tooltipString = userData.AliasOrUID + " is supporting CK as an Illustrious Supporter";
                break;

            case CkSupporterTier.EsteemedPatron:
                supporterWrap = CoreTextures.Cache[CoreTexture.Tier2Icon];
                tooltipString = userData.AliasOrUID + " is supporting CK as an Esteemed Patron";
                break;

            case CkSupporterTier.DistinguishedConnoisseur:
                supporterWrap = CoreTextures.Cache[CoreTexture.Tier3Icon];
                tooltipString = userData.AliasOrUID + " is supporting CK as a Distinguished Connoisseur";
                break;

            case CkSupporterTier.KinkporiumMistress:
                supporterWrap = CoreTextures.Cache[CoreTexture.Tier4Icon];
                tooltipString = userData.AliasOrUID + " is the Shop Mistress of CK, and the Dev of GagSpeak.";
                break;

            default:
                tooltipString = userData.AliasOrUID + " has an unknown supporter tier.";
                break;
        }

        return (supporterWrap, tooltipString);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Started.");
        try
        {
            // Create the directories if they do not yet exist.
            var transferTargets = new (string SourceFile, string SubDir, string TargetFile)[]
            {
                ("BACKUP_BF_Light.png",    ImageDataType.Blindfolds.ToString(), "Blindfold Light.png"),
                ("BACKUP_BF_Sensual.png",  ImageDataType.Blindfolds.ToString(), "Blindfold Sensual.png"),
                ("BACKUP_Hypno_Spiral.png", ImageDataType.Hypnosis.ToString(),  "Hypno Spiral.png")
            };

            // Ensure our directories exist.
            foreach (var (_, subDir, _) in transferTargets)
            {
                var targetDir = Path.Combine(ConfigFileProvider.ThumbnailDirectory, subDir);
                Directory.CreateDirectory(targetDir);
            }

            // Properly move the asset images into the correct folders.
            foreach (var (sourceFile, subDirectory, targetFile) in transferTargets)
            {
                var sourcePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Assets", "RequiredImages", sourceFile);
                var destDir = Path.Combine(ConfigFileProvider.ThumbnailDirectory, subDirectory);
                var destPath = Path.Combine(destDir, targetFile);

                // Migrate the file if it does not exist in the target directory (renaming it in the process)
                if (File.Exists(sourcePath) && !File.Exists(destPath))
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                    _logger.LogInformation($"Copied {sourceFile} to {destPath}");
                }
            }
            _logger.LogInformation("Default files migrated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to Migrate default files: {ex.Message}");
        }


        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("GagSpeak Profile Cosmetic Cache Stopped.");
        return Task.CompletedTask;
    }

}
