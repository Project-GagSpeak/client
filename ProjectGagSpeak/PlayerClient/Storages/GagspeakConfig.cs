using CkCommons.Audio;
using CkCommons.GarblerCore;
using GagSpeak.Services;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.PlayerClient;
public class GagspeakConfig
{
    public Version? LastRunVersion { get; set; } = null;
    public string LastUidLoggedIn { get; set; } = "";


    // used for detecting if in first install.
    public bool AcknowledgementUnderstood { get; set; } = false;
    public bool ButtonUsed { get; set; } = false;

    // DTR bar preferences
    public bool EnableDtrEntry { get; set; } = false;
    public bool ShowPrivacyRadar { get; set; } = true;
    public bool ShowActionNotifs { get; set; } = true;
    public bool ShowVibeStatus { get; set; } = true;

    // pair listing preferences
    public bool PreferThreeCharaAnonName { get; set; } = false;
    public bool PreferNicknamesOverNames { get; set; } = false;
    public bool ShowVisibleUsersSeparately { get; set; } = true;
    public bool ShowOfflineUsersSeparately { get; set; } = true;

    public bool OpenMainUiOnStartup { get; set; } = true;
    public bool ShowProfiles { get; set; } = true;
    public float ProfileDelay { get; set; } = 1.5f;
    public bool ShowContextMenus { get; set; } = true;
    public InptChannel PuppeteerChannelsBitfield { get; set; } = InptChannel.None;

    // logging (debug)
    public bool LiveGarblerZoneChangeWarn { get; set; } = true;
    public bool NotifyForServerConnections { get; set; } = true;
    public bool NotifyForOnlinePairs { get; set; } = true;
    public bool NotifyLimitToNickedPairs { get; set; } = false;

    public NotificationLocation InfoNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation WarningNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation ErrorNotification { get; set; } = NotificationLocation.Both;

    // GLOBAL SETTINGS for client user.
    public float FileIconScale { get; set; } = 1.0f; // File Icon Scale

    public string Safeword { get; set; } = "";
    public GarbleCoreLang Language { get; set; } = GarbleCoreLang.English; // MuffleCore
    public GarbleCoreDialect LanguageDialect { get; set; } = GarbleCoreDialect.US; // MuffleCore
    
    public bool CursedLootPanel { get; set; } = false; // CursedLootPanel
    public bool CursedItemsApplyTraits { get; set; } = false; // If Mimics can apply restriction traits to you.
    public bool RemoveRestrictionOnTimerExpire { get; set; } = false; // Auto-Remove Items when timer falloff occurs.

    // GLOBAL TOYBOX SETTINGS
    // public OutputType AudioOutputType { get; set; } = OutputType.DirectSound; // Best for FFXIV.
    public Guid DirectOutDevice { get; set; } = Guid.Empty;
    public string AsioDevice { get; set; } = "";
    public string WasapiDevice { get; set; } = "";

    // The name displayed when entering a vibe lobby and chatting in it. Should not be changed while in a room.
    public string NicknameInVibeRooms { get; set; } = "Anon. Kinkster";

    public bool IntifaceAutoConnect { get; set; } = false;                      // if we should auto-connect to intiface
    public string IntifaceConnectionSocket { get; set; } = "ws://localhost:12345"; // connection link from plugin to intiface

    // GLOBAL HARDCORE SETTINGS. (maybe make it its own file if it gets too rediculous but yeah.
    public string PiShockApiKey { get; set; } = ""; // PiShock Settings.
    public string PiShockUsername { get; set; } = ""; // PiShock Settings.
    public bool MoveToChambersInEstates { get; set; } = false; // Move to Chambers in Estates during ForcedStay

    public float OverlayMaxOpacity { get; set; } = 1.0f; // Blindfold Opacity
    public HypnoticEffect? HypnoEffectInfo { get; set; } = null;
    public string? Base64CustomImageData { get; set; } = null;
}

