using GagSpeak.ChatMessages;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Hardcore.Movement;
using GagSpeak.UI;

namespace GagSpeak.GagspeakConfiguration.Configurations;

[Serializable]
public class GagspeakConfig : IGagspeakConfiguration
{
    // Internal data used for account checking and changelogs.
    public static int CurrentVersion => 0;
    public int Version { get; set; } = CurrentVersion;
    public Version? LastRunVersion { get; set; } = null;
    public string LastUidLoggedIn { get; set; } = "";

    // used for detecting if in first install.
    public bool AcknowledgementUnderstood { get; set; } = false;
    public bool ButtonUsed { get; set; } = false;
    public bool AccountCreated { get; set; } = false;

    // Nicks
    public bool OpenPopupOnAdd { get; set; } = true;

    // DTR bar preferences
    public bool EnableDtrEntry { get; set; } = false;
    public bool ShowPrivacyRadar { get; set; } = true;
    public bool ShowActionNotifs { get; set; } = true;
    public bool ShowVibeStatus { get; set; } = true;

    // pair listing preferences
    public bool PreferNicknamesOverNames { get; set; } = false;
    public bool ShowVisibleUsersSeparately { get; set; } = true;
    public bool ShowOfflineUsersSeparately { get; set; } = true;

    public bool ShowProfiles { get; set; } = true;
    public float ProfileDelay { get; set; } = 1.5f;
    public bool ShowContextMenus { get; set; } = true;
    public HashSet<ChatChannel.Channels> ChannelsGagSpeak { get; set; } = new HashSet<ChatChannel.Channels> { ChatChannel.Channels.Say };
    public HashSet<ChatChannel.Channels> ChannelsPuppeteer { get; set; } = new HashSet<ChatChannel.Channels> { ChatChannel.Channels.Say };

    // logging (debug)
    public bool LiveGarblerZoneChangeWarn { get; set; } = true;
    public bool NotifyForServerConnections { get; set; } = true;
    public bool NotifyForOnlinePairs { get; set; } = true;
    public bool NotifyLimitToNickedPairs { get; set; } = false;

    public LogLevel LogLevel { get; set; } = LogLevel.Trace;
    public HashSet<LoggerType> LoggerFilters { get; set; } = LoggerFilter.GetAllRecommendedFilters();
    public NotificationLocation InfoNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation WarningNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation ErrorNotification { get; set; } = NotificationLocation.Both;

    // GLOBAL SETTINGS for client user.
    public string Safeword { get; set; } = "";
    public string Language { get; set; } = "English"; // MuffleCore
    public string LanguageDialect { get; set; } = "IPA_US"; // MuffleCore
    public bool CursedDungeonLoot { get; set; } = false; // CursedDungeonLoot
    public bool RemoveGagUponLockExpiration { get; set; } = false; // Auto-Remove Gags
    public RevertStyle RevertStyle { get; set; } = RevertStyle.RevertToAutomation; // How to revert Character when reset
    public bool DisableSetUponUnlock { get; set; } = false; // Auto-Remove Restraint Sets

    // GLOBAL VIBRATOR SETTINGS
    public VibratorMode VibratorMode { get; set; } = VibratorMode.Actual;       // if the user is using a simulated vibrator
    public VibeSimType VibeSimAudio { get; set; } = VibeSimType.Quiet;          // the audio settings for the simulated vibrator
    public bool IntifaceAutoConnect { get; set; } = false;                      // if we should auto-connect to intiface
    public string IntifaceConnectionSocket { get; set; } = "ws://localhost:12345"; // connection link from plugin to intiface
    public bool VibeServerAutoConnect { get; set; } = false;                    // if we should auto-connect to the vibe server

    // GLOBAL HARDCORE SETTINGS. (maybe make it its own file if it gets too rediculous but yeah.
    public string PiShockApiKey { get; set; } = ""; // PiShock Settings.
    public string PiShockUsername { get; set; } = ""; // PiShock Settings.
    public bool UsingLegacyControls { get; set; } = GameConfig.UiControl.GetBool("MoveMode");
    public BlindfoldType BlindfoldStyle { get; set; } = BlindfoldType.Sensual; // Blindfold Format
    public bool ForceLockFirstPerson { get; set; } = false; // Force First-Person state while blindfolded.
    public TextFolderNode ForcedStayPromptList { get; private set; } = new TextFolderNode { FriendlyName = "ForcedDeclineList" }; // ForcedToStay storage
    public bool MoveToChambersInEstates { get; set; } = false; // Move to Chambers in Estates during ForcedStay
}

