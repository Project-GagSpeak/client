using CkCommons;
using CkCommons.GarblerCore;
using GagSpeak.DrawSystem;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Services;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;

namespace GagSpeak.PlayerClient;
public class MainConfigData : IAudioConfigData
{
    public Version? LastRunVersion { get; set; } = null;
    public string LastUidLoggedIn { get; set; } = "";

    public bool AcknowledgementUnderstood { get; set; } = false;
    public bool ButtonUsed { get; set; } = false;

    // Internal Memory
    public MainMenuTabs.SelectedTab MainUiTab { get; set; } = MainMenuTabs.SelectedTab.Whitelist;
    public SidePanelTabs.SelectedTab PairPanelTab { get; set; } = SidePanelTabs.SelectedTab.Interactions;

    // PLUGIN  UI -> MAIN UI //
    public bool OpenUiOnStartup { get; set; } = true;
    public bool VisibleFolder { get; set; } = true;
    public bool OfflineFolder { get; set; } = true;

    // PLUGIN UI -> WHITELIST //
    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<FolderSortFilter> WhitelistSortOrderAll { get; set; } = [.. SorterHelpers.DefaultSortOrderAll];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<FolderSortFilter> WhitelistSortOrderVisible { get; set; } = [.. SorterHelpers.DefaultSortOrderVisible];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<FolderSortFilter> WhitelistSortOrderOnline { get; set; } = [.. SorterHelpers.DefaultSortOrderOnline];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public List<FolderSortFilter> WhitelistSortOrderOffline { get; set; } = [.. SorterHelpers.DefaultSortOrderOffline];

    // PLUGIN UI -> USERS //
    public bool UseFocusTargetOnUsers { get; set; } = false;
    public bool UseNicksOverPlayerNames { get; set; } = false;

    // SERVICE -> PROFILE //
    // PLUGIN UI -> USERS //
    public bool ShowProfiles { get; set; } = true;
    public float ProfileDelay { get; set; } = 1.5f;
    public bool UseLegacyAnonName { get; set; } = false;

    // NATIVE UI -> NAMEPLATES //
    public bool PlateIncludeFriendHighlights { get; set; } = true;
    public bool PlateHighlightKinksters { get; set; } = false;
    public NativeUiColor KinksterHighlight { get; set; } = GsDefaults.NameplateColorKinkster;

    // NATIVE UI -> DTR //
    public bool DtrPrivacy { get; set; } = false;
    public NativeUiColor DtrPrivacyColor { get; set; } = GsDefaults.DtrColorPairs;
    public bool DtrActionNotifs { get; set; } = true;
    public NativeUiColor DtrActionNotifColor { get; set; } = GsDefaults.DtrColorDisconnected;
    public bool DtrVibeStatus { get; set; } = true;
    public NativeUiColor DtrVibeStatusColor { get; set; } = GsDefaults.DtrColorVisibleUsers;

    // NATIVE UI -> CONTEXT MENUS //
    public bool ShowContextMenus { get; set; } = true;

    // NOTIFICATIONS -> PLUGIN //
    public bool LiveGarblerZoneChangeWarn { get; set; } = true;
    public AlertLocation RequestAlertLocation { get; set; } = AlertLocation.Toast;
    public AlertLocation ConnectionAlertLocation { get; set; } = AlertLocation.Toast;
    public AlertLocation OnlineAlertLocation { get; set; } = AlertLocation.Toast;
    public AlertLocation InfoNotification { get; set; } = AlertLocation.Both;
    public AlertLocation WarningNotification { get; set; } = AlertLocation.Both;
    public AlertLocation ErrorNotification { get; set; } = AlertLocation.Both;

    public InptChannel PuppeteerChannelsBitfield { get; set; } = InptChannel.None;

    // GLOBAL SETTINGS for client user.
    public float FileIconScale { get; set; } = 1.0f; // File Icon Scale

    public string Safeword { get; set; } = "";
    public GarbleCoreLang Language { get; set; } = GarbleCoreLang.English; // MuffleCore
    public GarbleCoreDialect LanguageDialect { get; set; } = GarbleCoreDialect.US; // MuffleCore
    public bool GarbleWordsNotInDictionary { get; set; } = true; // toggle for fallback garbler.

    public bool CursedLootUI { get; set; } = false;                   // CursedLootUI
    public bool CursedItemsApplyTraits { get; set; } = false;         // If Mimics can apply restriction traits to you.
    public bool CursedItemsApplyOverlays { get; set; } = false;         // If Mimics can apply restriction overlays to you.
    public bool RemoveGagOnTimerExpire { get; set; } = false; // Auto-Remove Items when timer falloff occurs.
    public bool RemoveRestrictionOnTimerExpire { get; set; } = false; // Auto-Remove Restriction when timer falloff occurs.
    public bool RemoveRestraintOnTimerExpire { get; set; } = false; // Auto-Remove restraint when timer falloff occurs.

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
    public string PiShockApiKey { get; set; } = "";
    public string PiShockUsername { get; set; } = "";
    public int GlobalShockerId { get; set; } = 0;
    public Dictionary<string, int> PairShockerIds { get; set; } = new(); // Per-pair shocker device selection (UID → shocker ID).
    public bool MoveToChambersInEstates { get; set; } = false; // Move to Chambers in Estates during ForcedStay

    public float OverlayMaxOpacity { get; set; } = 1.0f; // Blindfold Opacity
    public HypnoticEffect? HypnoEffectInfo { get; set; } = null;
    public string? Base64CustomImageData { get; set; } = null;

    // NOTIFICATIONS -> REQUESTS //
    public AlertKind AlertKind { get; set; } = AlertKind.Bubble;
    public string AlertCustomPath { get; set; } = string.Empty;
    public float AlertVolume { get; set; } = 0.5f;
    public Sounds AlertSoundbyte { get; set; } = Sounds.Sound02;
    public bool AlertIsCustom { get; set; } = false;

    // NOTIFICATIONS -> ONLINE USERS //
    public OnlineFilter OnlineNotifyFilter { get; set; } = OnlineFilter.Sundesmos;
    public FilterPolicy OnlineNotifyPolicy { get; set; } = FilterPolicy.MatchAny;
}
