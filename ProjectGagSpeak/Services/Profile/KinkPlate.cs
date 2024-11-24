using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.Services;

/// <summary>
/// Determines the configuration data stored within a GagSpeak KinkPlate.
/// </summary>
public class KinkPlate : DisposableMediatorSubscriberBase
{
    private readonly PlayerCharacterData _playerData;
    private readonly CosmeticService _cosmetics;

    // KinkPlate Data for User.
    private string _base64ProfilePicture;
    private Lazy<byte[]> _imageData;
    private IDalamudTextureWrap? _storedProfileImage;

    public KinkPlate(ILogger<KinkPlate> logger, GagspeakMediator mediator,
        PlayerCharacterData playerData, CosmeticService cosmeticService, 
        KinkPlateContent plateContent, string base64ProfilePicture) : base(logger, mediator)
    {
        _playerData = playerData;
        _cosmetics = cosmeticService;

        // Set the KinkPlate Data
        KinkPlateInfo = plateContent;
        Base64ProfilePicture = base64ProfilePicture;
        // set the image data if the profilePicture is not empty.
        if (!string.IsNullOrEmpty(Base64ProfilePicture))
        {
            _imageData = new Lazy<byte[]>(() => Convert.FromBase64String(Base64ProfilePicture));
        }
        else
        {
            _imageData = new Lazy<byte[]>(() => Array.Empty<byte>());
        }

        Mediator.Subscribe<ClearProfileDataMessage>(this, (msg) =>
        {
            if (msg.UserData == null || string.Equals(msg.UserData.UID, MainHub.UID, StringComparison.Ordinal))
            {
                _storedProfileImage?.Dispose();
                _storedProfileImage = null;
            }
        });
    }

    public KinkPlateContent KinkPlateInfo;

    public bool TempDisabled => KinkPlateInfo.Disabled || KinkPlateInfo.Flagged;

    public string Base64ProfilePicture
    {
        get => _base64ProfilePicture;
        set
        {
            if (_base64ProfilePicture != value)
            {
                _base64ProfilePicture = value;
                Logger.LogDebug("Profile picture updated.", LoggerType.KinkPlateMonitor);
                if(!string.IsNullOrEmpty(_base64ProfilePicture))
                {
                    Logger.LogTrace("Refreshing profile image data!", LoggerType.KinkPlateMonitor);
                    _imageData = new Lazy<byte[]>(() => ConvertBase64ToByteArray(Base64ProfilePicture));
                    Logger.LogTrace("Refreshed profile image data!", LoggerType.KinkPlateMonitor);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Logger.LogInformation("Disposing profile image data!");
            _storedProfileImage?.Dispose();
            _storedProfileImage = null;
        }
        base.Dispose(disposing);
    }

    public IDalamudTextureWrap? GetCurrentProfileOrDefault()
    {
        // If the user does not have a profile set, return the default logo.
        if(string.IsNullOrEmpty(Base64ProfilePicture) || _imageData.Value.IsNullOrEmpty())
            return _cosmetics.CorePluginTextures[CorePluginTexture.Logo256bg];

        // Otherwise, fetch the profile image for it.
        if(_storedProfileImage is not null)
            return _storedProfileImage;

        // load it
        try
        {
            Logger.LogTrace("Loading profile image data to wrap.");
            _storedProfileImage = _cosmetics.GetProfilePicture(_imageData.Value);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load profile image data to wrap.");
        }
        return _storedProfileImage;
    }

    public ProfileStyleBG GetBackground(ProfileComponent component)
        => component switch
        {
            ProfileComponent.Plate => KinkPlateInfo.PlateBackground,
            ProfileComponent.PlateLight => KinkPlateInfo.PlateLightBackground,
            ProfileComponent.Description => KinkPlateInfo.DescriptionBackground,
            ProfileComponent.GagSlot => KinkPlateInfo.GagSlotBackground,
            ProfileComponent.Padlock => KinkPlateInfo.PadlockBackground,
            ProfileComponent.BlockedSlots => KinkPlateInfo.BlockedSlotsBackground,
            _ => ProfileStyleBG.Default
        };

    public void SetBackground(ProfileComponent component, ProfileStyleBG bg)
    {
        switch (component)
        {
            case ProfileComponent.Plate:
                KinkPlateInfo.PlateBackground = bg;
                break;
            case ProfileComponent.PlateLight:
                KinkPlateInfo.PlateLightBackground = bg;
                break;
            case ProfileComponent.Description:
                KinkPlateInfo.DescriptionBackground = bg;
                break;
            case ProfileComponent.GagSlot:
                KinkPlateInfo.GagSlotBackground = bg;
                break;
            case ProfileComponent.Padlock:
                KinkPlateInfo.PadlockBackground = bg;
                break;
            case ProfileComponent.BlockedSlots:
                KinkPlateInfo.BlockedSlotsBackground = bg;
                break;
        }
    }

    public ProfileStyleBorder GetBorder(ProfileComponent component)
        => component switch
        {
            ProfileComponent.Plate => KinkPlateInfo.PlateBorder,
            ProfileComponent.PlateLight => KinkPlateInfo.PlateLightBorder,
            ProfileComponent.ProfilePicture => KinkPlateInfo.ProfilePictureBorder,
            ProfileComponent.Description => KinkPlateInfo.DescriptionBorder,
            ProfileComponent.GagSlot => KinkPlateInfo.GagSlotBorder,
            ProfileComponent.Padlock => KinkPlateInfo.PadlockBorder,
            ProfileComponent.BlockedSlots => KinkPlateInfo.BlockedSlotsBorder,
            ProfileComponent.BlockedSlot => KinkPlateInfo.BlockedSlotBorder,
            _ => ProfileStyleBorder.Default
        };

    public void SetBorder(ProfileComponent component, ProfileStyleBorder border)
    {
        switch (component)
        {
            case ProfileComponent.Plate:
                KinkPlateInfo.PlateBorder = border;
                break;
            case ProfileComponent.PlateLight:
                KinkPlateInfo.PlateLightBorder = border;
                break;
            case ProfileComponent.ProfilePicture:
                KinkPlateInfo.ProfilePictureBorder = border;
                break;
            case ProfileComponent.Description:
                KinkPlateInfo.DescriptionBorder = border;
                break;
            case ProfileComponent.GagSlot:
                KinkPlateInfo.GagSlotBorder = border;
                break;
            case ProfileComponent.Padlock:
                KinkPlateInfo.PadlockBorder = border;
                break;
            case ProfileComponent.BlockedSlots:
                KinkPlateInfo.BlockedSlotsBorder = border;
                break;
            case ProfileComponent.BlockedSlot:
                KinkPlateInfo.BlockedSlotBorder = border;
                break;
        }
    }

    public ProfileStyleOverlay GetOverlay(ProfileComponent component)
        => component switch
        {
            ProfileComponent.ProfilePicture => KinkPlateInfo.ProfilePictureOverlay,
            ProfileComponent.Description => KinkPlateInfo.DescriptionOverlay,
            ProfileComponent.GagSlot => KinkPlateInfo.GagSlotOverlay,
            ProfileComponent.Padlock => KinkPlateInfo.PadlockOverlay,
            ProfileComponent.BlockedSlots => KinkPlateInfo.BlockedSlotsOverlay,
            ProfileComponent.BlockedSlot => KinkPlateInfo.BlockedSlotOverlay,
            _ => ProfileStyleOverlay.Default
        };

    public void SetOverlay(ProfileComponent component, ProfileStyleOverlay overlay)
    {
        switch (component)
        {
            case ProfileComponent.ProfilePicture:
                KinkPlateInfo.ProfilePictureOverlay = overlay;
                break;
            case ProfileComponent.Description:
                KinkPlateInfo.DescriptionOverlay = overlay;
                break;
            case ProfileComponent.GagSlot:
                KinkPlateInfo.GagSlotOverlay = overlay;
                break;
            case ProfileComponent.Padlock:
                KinkPlateInfo.PadlockOverlay = overlay;
                break;
            case ProfileComponent.BlockedSlots:
                KinkPlateInfo.BlockedSlotsOverlay = overlay;
                break;
            case ProfileComponent.BlockedSlot:
                KinkPlateInfo.BlockedSlotOverlay = overlay;
                break;
        }
    }

    private byte[] ConvertBase64ToByteArray(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            return Array.Empty<byte>();
        }

        try
        {
            return Convert.FromBase64String(base64String);
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex, "Invalid Base64 string for profile picture.");
            return Array.Empty<byte>();
        }
    }
}
