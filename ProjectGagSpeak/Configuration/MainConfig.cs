using CkCommons;
using CkCommons.GarblerCore;
using CkCommons.HybridSaver;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.DrawSystem;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.MainWindow;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using NAudio.Wave;

namespace GagSpeak.PlayerClient;

public class MainConfigData : IAudioConfigData
{
    public Version? LastRunVersion { get; set; } = null;

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
    public OnlineFilter OnlineNotifyFilter { get; set; } = OnlineFilter.Favorited;
    public FilterPolicy OnlineNotifyPolicy { get; set; } = FilterPolicy.MatchAny;
}

public class MainConfig : IHybridSavable, IAudioConfig<MainConfigData>, IDisposable
{
    private readonly ILogger<MainConfig> _logger;
    private readonly ConnectionsConfig _connections;
    private readonly HybridSaveService _saver;

    // Cached items for custom alert configuration.
    private AudioFileReader? _audioFile;
    private WaveOutEvent? _audioEvent;

    [JsonIgnore] public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    [JsonIgnore] public HybridSaveType SaveType => HybridSaveType.Json;
    public int ConfigVersion => 2;
    public int LogFilterVersion => 1;
    public int MaxBackups => 4;
    public string ToFilePath(GsFiles files) => files.MainConfig;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Config"] = JObject.FromObject(Data),
            ["LogLevel"] = LogLevel.ToString(),
            ["LoggerFilters"] = JToken.FromObject(LoggerFilters),
            ["ServerPaused"] = ServerPaused
        }.ToString(Formatting.Indented);
    }

    public MainConfig(ILogger<MainConfig> logger, ConnectionsConfig connections, HybridSaveService saver)
    {
        _logger = logger;
        _connections = connections;
        _saver = saver;
        Load();
    }

    public void Dispose()
        => DisposeAudio();

    private void DisposeAudio()
    {
        _audioFile?.Dispose();
        _audioFile = null;
        _audioEvent?.Dispose();
        _audioEvent = null;
    }
    public void Save()
        => _saver.Save(this);

    public void Load()
    {
        var file = _saver.FileNames.MainConfig;
        Svc.Logger.Information("Loading in Config for file: " + file);
        var jsonText = "";
        JObject jObject = new();
        try
        {
            // if the main file does not exist, attempt to load the text from the backup.
            if (File.Exists(file))
            {
                jsonText = File.ReadAllText(file);
                jObject = JObject.Parse(jsonText);
            }
            else
            {
                Svc.Logger.Warning("Config file not found Attempting to find old config.");
                var backupFile = file.Insert(file.Length - 5, "-testing");
                if (File.Exists(backupFile))
                {
                    jsonText = File.ReadAllText(backupFile);
                    jObject = JObject.Parse(jsonText);
                    Svc.Logger.Warning("Old Config found, attempting to migrate.");
                    jObject = ConfigMigrator.MigrateMainConfig(jObject, _connections, _saver.FileNames);
                    // remove the old file.
                    // File.Delete(backupFile);
                }
                else
                {
                    Svc.Logger.Warning("No Config file found for: " + backupFile);
                    return;
                }
            }
            // Read the json from the file.
            var version = jObject["Version"]?.Value<int>() ?? 0;

            // Load instance configuration
            Data       = jObject["Config"]?.ToObject<MainConfigData>() ?? new MainConfigData();
            LogLevel      = Enum.TryParse(jObject["LogLevel"]?.Value<string>(), out LogLevel logLevel) ? logLevel : LogLevel.Debug;
            LoggerFilters = GetLoggerFilters(jObject["LoggerFilters"]);
            ServerPaused  = jObject["ServerPaused"]?.Value<bool>() ?? false;

            Svc.Logger.Information("Config loaded.");
            Save();
            UpdateAudio();
        }
        catch (Bagagwa ex)
        {
            _logger.LogError("Failed to load config." + ex);
        }
    }

    public MainConfigData Data { get; private set; } = new();
    public Dictionary<GsCol, uint> GsColors { get; private set; } = [];
    public Dictionary<CkCol, uint> CkColors { get; private set; } = [];

    /// <summary>
    ///   Updates the paused state of the server. <para />
    ///   When set to a value, the config is automatically saved.
    /// </summary>
    public bool ServerPaused { get; set; } = false;


    public static LogLevel LogLevel = LogLevel.Trace;
    public static LoggerType LoggerFilters = LoggerType.Recommended;

    public void SetPauseState(bool newValue)
    {
        ServerPaused = newValue;
        Save();
    }

    private LoggerType GetLoggerFilters(JToken? filtersToken)
    {
        if (filtersToken is JArray array)
        {
            var list = array.ToObject<List<LoggerType>>() ?? new List<LoggerType>();
            return list.Aggregate(LoggerType.None, (acc, val) => acc | val);
        }
        else
        {
            return filtersToken?.ToObject<LoggerType>() ?? LoggerType.Recommended;
        }
    }

    // Audio Helpers
    public bool IsAudioReady()
        => !Data.AlertIsCustom || (_audioFile != null && _audioEvent != null);

    public unsafe bool PlaySound()
    {
        if (!Data.AlertKind.HasAny(AlertKind.Audio))
            return false;

        if (Data.AlertIsCustom)
        {
            if (_audioFile is null || _audioEvent is null)
                return false;

            _audioEvent.Stop();
            _audioFile.Position = 0;
            _audioEvent.Play();
            return true;
        }

        UIGlobals.PlaySoundEffect((uint)Data.AlertSoundbyte);
        return true;
    }

    public void UpdateAudio()
    {
        // Dispose the audio if no longer valid.
        if (!(Data.AlertKind.HasAny(AlertKind.Audio) && Data.AlertIsCustom))
        {
            DisposeAudio();
            Save();
            return;
        }

        try
        {
            // If the audio file name is no longer the chosen sound path, dispose of it.
            if (_audioFile?.FileName != Data.AlertCustomPath)
                DisposeAudio();
            // Recreate the audio with the requested path and volume.
            _audioFile = new AudioFileReader(Data.AlertCustomPath) { Volume = Data.AlertVolume };
            _audioEvent = new WaveOutEvent();
            _audioEvent.Init(_audioFile);
        }
        catch (ArgumentException argEx)
        {
            _logger.LogDebug($"Path was not valid for alert sound: {argEx.Message}");
            DisposeAudio();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error setting up alert sound: {ex}");
            DisposeAudio();
        }
        finally
        {
            Save();
        }
    }
}
