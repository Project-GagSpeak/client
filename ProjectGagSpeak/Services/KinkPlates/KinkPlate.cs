using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using Microsoft.IdentityModel.Tokens;

namespace GagSpeak.Services;

public class KinkPlate : DisposableMediatorSubscriberBase
{
    // KinkPlate Data for User.
    private string _base64ProfilePicture;
    private Lazy<byte[]> _imageData;
    private IDalamudTextureWrap? _storedProfileImage;

    public KinkPlate(ILogger<KinkPlate> logger, GagspeakMediator mediator,
        KinkPlateContent plateContent, string base64ProfilePicture) : base(logger, mediator)
    {
        // Set the KinkPlate Data
        Info = plateContent;
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

        Mediator.Subscribe<ClearKinkPlateDataMessage>(this, (msg) =>
        {
            if (msg.UserData == null || string.Equals(msg.UserData.UID, MainHub.UID, StringComparison.Ordinal))
            {
                _storedProfileImage?.Dispose();
                _storedProfileImage = null;
            }
        });
    }

    public KinkPlateContent Info;

    public bool TempDisabled => Info.Flagged;

    public string Base64ProfilePicture
    {
        get => _base64ProfilePicture;
        set
        {
            if (_base64ProfilePicture != value)
            {
                _base64ProfilePicture = value;
                Logger.LogDebug("Profile picture updated.", LoggerType.KinkPlates);
                if(!string.IsNullOrEmpty(_base64ProfilePicture))
                {
                    Logger.LogTrace("Refreshing profile image data!", LoggerType.KinkPlates);
                    _imageData = new Lazy<byte[]>(() => ConvertBase64ToByteArray(Base64ProfilePicture));
                    Logger.LogTrace("Refreshed profile image data!", LoggerType.KinkPlates);
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

    public IDalamudTextureWrap? GetProfileOrDefault()
    {
        // If the user does not have a profile set, return the default logo.
        if(string.IsNullOrEmpty(Base64ProfilePicture) || _imageData.Value.IsNullOrEmpty())
            return CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];

        // Otherwise, fetch the profile image for it.
        if(_storedProfileImage is not null)
            return _storedProfileImage;

        // load it
        try
        {
            Logger.LogTrace("Loading profile image data to wrap.");
            _storedProfileImage = TextureManagerEx.GetProfilePicture(_imageData.Value);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to load profile image data to wrap.");
        }
        return _storedProfileImage;
    }

    public KinkPlateBG GetBackground(PlateElement component)
        => component switch
        {
            PlateElement.Plate => Info.PlateBG,
            PlateElement.PlateLight => Info.PlateLightBG,
            PlateElement.Description => Info.DescriptionBG,
            PlateElement.GagSlot => Info.GagSlotBG,
            PlateElement.Padlock => Info.PadlockBG,
            PlateElement.BlockedSlots => Info.BlockedSlotsBG,
            _ => KinkPlateBG.Default
        };

    public void SetBackground(PlateElement component, KinkPlateBG bg)
    {
        switch (component)
        {
            case PlateElement.Plate:
                Info.PlateBG = bg;
                break;
            case PlateElement.PlateLight:
                Info.PlateLightBG = bg;
                break;
            case PlateElement.Description:
                Info.DescriptionBG = bg;
                break;
            case PlateElement.GagSlot:
                Info.GagSlotBG = bg;
                break;
            case PlateElement.Padlock:
                Info.PadlockBG = bg;
                break;
            case PlateElement.BlockedSlots:
                Info.BlockedSlotsBG = bg;
                break;
        }
    }

    public KinkPlateBorder GetBorder(PlateElement component)
        => component switch
        {
            PlateElement.Plate => Info.PlateBorder,
            PlateElement.PlateLight => Info.PlateLightBorder,
            PlateElement.Avatar => Info.AvatarBorder,
            PlateElement.Description => Info.DescriptionBorder,
            PlateElement.GagSlot => Info.GagSlotBorder,
            PlateElement.Padlock => Info.PadlockBorder,
            PlateElement.BlockedSlots => Info.BlockedSlotsBorder,
            PlateElement.BlockedSlot => Info.BlockedSlotBorder,
            _ => KinkPlateBorder.Default
        };

    public void SetBorder(PlateElement component, KinkPlateBorder border)
    {
        switch (component)
        {
            case PlateElement.Plate:
                Info.PlateBorder = border;
                break;
            case PlateElement.PlateLight:
                Info.PlateLightBorder = border;
                break;
            case PlateElement.Avatar:
                Info.AvatarBorder = border;
                break;
            case PlateElement.Description:
                Info.DescriptionBorder = border;
                break;
            case PlateElement.GagSlot:
                Info.GagSlotBorder = border;
                break;
            case PlateElement.Padlock:
                Info.PadlockBorder = border;
                break;
            case PlateElement.BlockedSlots:
                Info.BlockedSlotsBorder = border;
                break;
            case PlateElement.BlockedSlot:
                Info.BlockedSlotBorder = border;
                break;
        }
    }

    public KinkPlateOverlay GetOverlay(PlateElement component)
        => component switch
        {
            PlateElement.Avatar => Info.AvatarOverlay,
            PlateElement.Description => Info.DescriptionOverlay,
            PlateElement.GagSlot => Info.GagSlotOverlay,
            PlateElement.Padlock => Info.PadlockOverlay,
            PlateElement.BlockedSlots => Info.BlockedSlotsOverlay,
            PlateElement.BlockedSlot => Info.BlockedSlotOverlay,
            _ => KinkPlateOverlay.Default
        };

    public void SetOverlay(PlateElement component, KinkPlateOverlay overlay)
    {
        switch (component)
        {
            case PlateElement.Avatar:
                Info.AvatarOverlay = overlay;
                break;
            case PlateElement.Description:
                Info.DescriptionOverlay = overlay;
                break;
            case PlateElement.GagSlot:
                Info.GagSlotOverlay = overlay;
                break;
            case PlateElement.Padlock:
                Info.PadlockOverlay = overlay;
                break;
            case PlateElement.BlockedSlots:
                Info.BlockedSlotsOverlay = overlay;
                break;
            case PlateElement.BlockedSlot:
                Info.BlockedSlotOverlay = overlay;
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
