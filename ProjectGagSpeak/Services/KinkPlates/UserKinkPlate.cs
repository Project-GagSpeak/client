using CkCommons;
using Dalamud.Interface.Textures.TextureWraps;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Profiles;
using GagspeakAPI.User;

namespace GagSpeak.Services;

public class UserKinkPlate : DisposableMediatorSubscriberBase
{
    private readonly UserData _user;
    // private UserProfileV1 _data; // Future ref
    private KinkPlateContent _currData;

    private string _base64Icon;
    private string _base64Background;

    private Lazy<byte[]> _iconBytes;
    private Lazy<byte[]> _backgroundBytes;

    private IDalamudTextureWrap? _iconWrap;
    private IDalamudTextureWrap? _bgWrap;

    public UserKinkPlate(UserData user, ILogger<UserKinkPlate> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        _user = user;
        // Dont mark as initialized, just set empty data.
        _currData = new();
        IsInitialized = false;
        NsfwIcon = false;
        NsfwBg = false;
        NsfwDesc = false;
        Flagged = false;
    }

    public UserKinkPlate(UserData user, KinkPlateContent info, string base64Icon,
        ILogger<UserKinkPlate> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        _user = user;
        // Set all internal data.
        Flagged = info.Flagged;
        try
        {
            _currData = info;
            // _data = ProfilesJsonEx.ReadUserV1(contents.ContentJson);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Error Parsing V1 from JsonString: {ex}");
        }

        InitLazyIcon(base64Icon);
        // InitLazyBackground(images.BackgroundBase64 ?? string.Empty);
        IsInitialized = true;
    }

    public UserData Owner => _user;
    public KinkPlateContent Data => _currData; // Make UserProfileV1 later.
    public bool IsInitialized { get; private set; }
    public bool NsfwIcon { get; private set; }
    public bool NsfwBg { get; private set; }
    public bool NsfwDesc { get; private set; }
    public bool Flagged { get; private set; }
    public bool HasIconImage => !string.IsNullOrEmpty(_base64Icon);
    public bool HasBgImage => !string.IsNullOrEmpty(_base64Background);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _bgWrap?.Dispose();
            _bgWrap = null;
            _iconWrap?.Dispose();
            _iconWrap = null;
            // Temp disabling this so we have it next time we need it.
            //InitLazyIcon(string.Empty);
            //InitLazyBackground(string.Empty);
        }
        base.Dispose(disposing);
    }

    public byte[] GetIconBytesOrDefault()
    {
        var bytes = _iconBytes?.Value;
        return (bytes is not null && bytes.Length > 0) ? bytes : CosmeticService.GetDefaultIconBytes();
    }

    // Obtains the image bytes, or retrieves the bytes for the default image data.
    // WE DONT HAVE THIS AS A VALID ITEM YET!
    public byte[] GetBgBytesOrDefault()
    {
        var bytes = _backgroundBytes?.Value;
        return (bytes is not null && bytes.Length > 0) ? bytes : CosmeticService.GetDefaultBackgroundBytes();
    }

    // This is a work around temporarily until we get a better system for profiles during disconnects.
    public IDalamudTextureWrap GetIconWrapOrDefault()
    {
        if (string.IsNullOrEmpty(_base64Icon) || !MainHub.IsConnected)
            return CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];

        if (_iconWrap is not null)
            return _iconWrap;

        var bytes = _iconBytes.Value;
        if (bytes.Length == 0)
            return CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];

        Generic.Safe(() => _iconWrap = Svc.Texture.CreateFromImageAsync(bytes).Result);
        return _iconWrap ?? CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];
    }

    // NOTE: NO BG IS IMPLEMENTED YET!
    // This is a work around temporarily until we get a better system for profiles during disconnects.
    public IDalamudTextureWrap GetBgWrapOrDefault()
    {
        if (string.IsNullOrEmpty(_base64Background) || !MainHub.IsConnected)
            return CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];

        if (_bgWrap is not null)
            return _bgWrap;

        var bytes = _backgroundBytes.Value;
        if (bytes.Length == 0)
            return CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];

        Generic.Safe(() => _bgWrap = Svc.Texture.CreateFromImageAsync(bytes).Result);
        return _bgWrap ?? CosmeticService.CoreTextures.Cache[CoreTexture.Icon256Bg];
    }

    // public void ApplyDataFromHub(UserProfileInfo content, ProfileImages images)
    public void ApplyDataFromHub(KinkPlateContent content, string? base64Icon)
    {
        Flagged = content.Flagged;
        try
        {
            _currData = content;
            // _data = ProfilesJsonEx.ReadUserV1(content.ContentJson);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Summoned Bagagwa while Parsing JsonData: {ex}");
        }
        // Re-Init the image
        InitLazyIcon(base64Icon ?? string.Empty);
        // InitLazyBackground(images.BackgroundBase64 ?? string.Empty);
        IsInitialized = true;
    }

    internal void ApplyInfoFromHub(KinkPlateContent content)
    {
        Flagged = content.Flagged;
        try
        {
            _currData = content;
            // _data = ProfilesJsonEx.ReadUserV1(content.ContentJson);
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Summoned Bagagwa while Parsing JsonData: {ex}");
        }
        IsInitialized = true;
    }

    internal void ApplyIconFromHub(string iconBase64)
    {
        InitLazyIcon(iconBase64);
        IsInitialized = true;
    }

    internal void ApplyBackgroundFromHub(string bgBase64)
    {
        InitLazyBackground(bgBase64);
        IsInitialized = true;
    }

    private void InitLazyIcon(string iconBase64)
    {
        _base64Icon = iconBase64;
        _iconWrap?.Dispose();
        _iconWrap = null;
        // Then reinit the byte arrays, priming their calculations lazily.
        _iconBytes = new Lazy<byte[]>(() => DecodeBase64(iconBase64), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private void InitLazyBackground(string bgBase64)
    {
        _base64Background = bgBase64;
        _bgWrap?.Dispose();
        _bgWrap = null;
        // Then reinit the byte arrays, priming their calculations lazily.
        _backgroundBytes = new Lazy<byte[]>(() => DecodeBase64(bgBase64), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private byte[] DecodeBase64(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return [];
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException ex)
        {
            Logger.LogError(ex, "Invalid Base64 string for image.");
            return [];
        }
    }

    public KinkPlateBG GetBackground(PlateElement component)
        => component switch
        {
            PlateElement.Plate => Data.PlateBG,
            PlateElement.PlateLight => Data.PlateLightBG,
            PlateElement.Description => Data.DescriptionBG,
            PlateElement.GagSlot => Data.GagSlotBG,
            PlateElement.Padlock => Data.PadlockBG,
            PlateElement.BlockedSlots => Data.BlockedSlotsBG,
            _ => KinkPlateBG.Default
        };

    public void SetBackground(PlateElement component, KinkPlateBG bg)
    {
        switch (component)
        {
            case PlateElement.Plate:
                Data.PlateBG = bg;
                break;
            case PlateElement.PlateLight:
                Data.PlateLightBG = bg;
                break;
            case PlateElement.Description:
                Data.DescriptionBG = bg;
                break;
            case PlateElement.GagSlot:
                Data.GagSlotBG = bg;
                break;
            case PlateElement.Padlock:
                Data.PadlockBG = bg;
                break;
            case PlateElement.BlockedSlots:
                Data.BlockedSlotsBG = bg;
                break;
        }
    }

    public KinkPlateBorder GetBorder(PlateElement component)
        => component switch
        {
            PlateElement.Plate => Data.PlateBorder,
            PlateElement.PlateLight => Data.PlateLightBorder,
            PlateElement.Avatar => Data.AvatarBorder,
            PlateElement.Description => Data.DescriptionBorder,
            PlateElement.GagSlot => Data.GagSlotBorder,
            PlateElement.Padlock => Data.PadlockBorder,
            PlateElement.BlockedSlots => Data.BlockedSlotsBorder,
            PlateElement.BlockedSlot => Data.BlockedSlotBorder,
            _ => KinkPlateBorder.Default
        };

    public void SetBorder(PlateElement component, KinkPlateBorder border)
    {
        switch (component)
        {
            case PlateElement.Plate:
                Data.PlateBorder = border;
                break;
            case PlateElement.PlateLight:
                Data.PlateLightBorder = border;
                break;
            case PlateElement.Avatar:
                Data.AvatarBorder = border;
                break;
            case PlateElement.Description:
                Data.DescriptionBorder = border;
                break;
            case PlateElement.GagSlot:
                Data.GagSlotBorder = border;
                break;
            case PlateElement.Padlock:
                Data.PadlockBorder = border;
                break;
            case PlateElement.BlockedSlots:
                Data.BlockedSlotsBorder = border;
                break;
            case PlateElement.BlockedSlot:
                Data.BlockedSlotBorder = border;
                break;
        }
    }

    public KinkPlateOverlay GetOverlay(PlateElement component)
        => component switch
        {
            PlateElement.Avatar => Data.AvatarOverlay,
            PlateElement.Description => Data.DescriptionOverlay,
            PlateElement.GagSlot => Data.GagSlotOverlay,
            PlateElement.Padlock => Data.PadlockOverlay,
            PlateElement.BlockedSlots => Data.BlockedSlotsOverlay,
            PlateElement.BlockedSlot => Data.BlockedSlotOverlay,
            _ => KinkPlateOverlay.Default
        };

    public void SetOverlay(PlateElement component, KinkPlateOverlay overlay)
    {
        switch (component)
        {
            case PlateElement.Avatar:
                Data.AvatarOverlay = overlay;
                break;
            case PlateElement.Description:
                Data.DescriptionOverlay = overlay;
                break;
            case PlateElement.GagSlot:
                Data.GagSlotOverlay = overlay;
                break;
            case PlateElement.Padlock:
                Data.PadlockOverlay = overlay;
                break;
            case PlateElement.BlockedSlots:
                Data.BlockedSlotsOverlay = overlay;
                break;
            case PlateElement.BlockedSlot:
                Data.BlockedSlotOverlay = overlay;
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
