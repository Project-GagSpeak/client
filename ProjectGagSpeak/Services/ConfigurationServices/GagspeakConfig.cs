using GagSpeak.CkCommons.GarblerCore;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;

namespace GagSpeak.Services.Configs;

public class GagspeakConfigService : IHybridSavable
{
    private readonly HybridSaveService _saver;
    [JsonIgnore] public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    [JsonIgnore] public HybridSaveType SaveType => HybridSaveType.Json;
    public int ConfigVersion => 0;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.MainConfig).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["Config"] = JObject.FromObject(Config),
            ["LogLevel"] = LogLevel.ToString(),
            ["LoggerFilters"] = JArray.FromObject(LoggerFilters)
        }.ToString(Formatting.Indented);
    }

    public GagspeakConfigService(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.MainConfig;
        GagSpeak.StaticLog.Information("Loading in Config for file: " + file);
        string jsonText = "";
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
                GagSpeak.StaticLog.Warning("Config file not found Attempting to find old config.");
                var backupFile = file.Insert(file.Length - 5, "-testing");
                if (File.Exists(backupFile))
                {
                    jsonText = File.ReadAllText(backupFile);
                    jObject = JObject.Parse(jsonText);
                    jObject = ConfigMigrator.MigrateMainConfig(jObject, _saver.FileNames);
                    // remove the old file.
                    // File.Delete(backupFile);
                }
                else
                {
                    GagSpeak.StaticLog.Warning("No Config file found for: " + backupFile);
                    return;
                }
            }
            // Read the json from the file.
            var version = jObject["Version"]?.Value<int>() ?? 0;

            // Load instance configuration
            Config = jObject["Config"]?.ToObject<GagspeakConfig>() ?? new GagspeakConfig();

            // Load static fields safely
            if (Enum.TryParse(jObject["LogLevel"]?.Value<string>(), out LogLevel logLevel))
                LogLevel = logLevel;
            else
                LogLevel = LogLevel.Trace;  // Default fallback

            LoggerFilters = jObject["LoggerFilters"]?.ToObject<HashSet<LoggerType>>() ?? new HashSet<LoggerType>();
            GagSpeak.StaticLog.Information("Config loaded.");
            Save();
        }
        catch (Exception ex) { GagSpeak.StaticLog.Error("Failed to load config." + ex); }
    }

    public GagspeakConfig Config { get; private set; } = new GagspeakConfig();
    public static LogLevel LogLevel = LogLevel.Trace;
    public static HashSet<LoggerType> LoggerFilters = new HashSet<LoggerType>();

    // Hardcore RUNTIME ONLY VARIABLE STORAGE.
    [JsonIgnore] internal string LastSeenNodeName { get; set; } = string.Empty; // The Node Visible Name
    [JsonIgnore] internal string LastSeenNodeLabel { get; set; } = string.Empty; // The Label of the nodes Prompt
    [JsonIgnore] internal (int Index, string Text)[] LastSeenListEntries { get; set; } = []; // The nodes Options
    [JsonIgnore] internal string LastSeenListSelection { get; set; } = string.Empty; // Option we last selected
    [JsonIgnore] internal int LastSeenListIndex { get; set; } // Index in the list that was selected
    [JsonIgnore] internal TextEntryNode LastSelectedListNode { get; set; } = new();

    #region Update Monitoring And Hardcore
    public IEnumerable<ITextNode> GetAllNodes()
    {
        return new ITextNode[] { Config.ForcedStayPromptList }
            .Concat(GetAllNodes(Config.ForcedStayPromptList.Children));
    }

    public IEnumerable<ITextNode> GetAllNodes(IEnumerable<ITextNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node is TextFolderNode folder)
            {
                var children = GetAllNodes(folder.Children);
                foreach (var childNode in children)
                    yield return childNode;
            }
        }
    }

    public bool TryFindParent(ITextNode node, out TextFolderNode? parent)
    {
        foreach (var candidate in GetAllNodes())
        {
            if (candidate is TextFolderNode folder && folder.Children.Contains(node))
            {
                parent = folder;
                return true;
            }
        }

        parent = null;
        return false;
    }

    public void AddLastSeenNode()
    {
        var newNode = new TextEntryNode()
        {
            Enabled = false,
            FriendlyName = (string.IsNullOrEmpty(LastSeenNodeLabel) ? LastSeenNodeName : LastSeenNodeLabel) + "(Friendly Name)",
            TargetNodeName = LastSeenNodeName,
            TargetRestricted = true,
            TargetNodeLabel = LastSeenNodeLabel,
            SelectedOptionText = LastSeenListSelection,
        };
        Config.ForcedStayPromptList.Children.Add(newNode);
        Save();
    }

    public void CreateTextNode()
    {
        // create a new blank one
        var newNode = new TextEntryNode()
        {
            Enabled = false,
            FriendlyName = "Placeholder Friendly Name",
            TargetNodeName = "Name Of Node You Interact With",
            TargetRestricted = true,
            TargetNodeLabel = "Label given to interacted node's prompt menu",
            SelectedOptionText = "Option we select from the prompt.",
        };
        Config.ForcedStayPromptList.Children.Add(newNode);
        Save();
    }

    public void CreateChamberNode()
    {
        var newNode = new ChambersTextNode()
        {
            Enabled = false,
            FriendlyName = "New ChamberNode",
            TargetRestricted = true,
            TargetNodeName = "Name Of Node You Interact With",
            ChamberRoomSet = 0,
            ChamberListIdx = 0,
        };
        Config.ForcedStayPromptList.Children.Add(newNode);
        Save();
    }

    #endregion Update Monitoring And Hardcore
}

// The container for our main config.
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
    public int PuppeteerChannelsBitfield { get; set; } = 0;

    // logging (debug)
    public bool LiveGarblerZoneChangeWarn { get; set; } = true;
    public bool NotifyForServerConnections { get; set; } = true;
    public bool NotifyForOnlinePairs { get; set; } = true;
    public bool NotifyLimitToNickedPairs { get; set; } = false;

    public NotificationLocation InfoNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation WarningNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation ErrorNotification { get; set; } = NotificationLocation.Both;

    // GLOBAL SETTINGS for client user.
    public string Safeword { get; set; } = "";
    public GarbleCoreLang Language { get; set; } = GarbleCoreLang.English; // MuffleCore
    public GarbleCoreDialect LanguageDialect { get; set; } = GarbleCoreDialect.US; // MuffleCore
    public bool CursedLootPanel { get; set; } = false; // CursedLootPanel
    public bool CursedItemsApplyTraits { get; set; } = false; // If Mimics can apply restriction traits to you.
    public bool RemoveRestrictionOnTimerExpire { get; set; } = false; // Auto-Remove Items when timer falloff occurs.

    // GLOBAL VIBRATOR SETTINGS
    public VibratorEnums VibratorMode { get; set; } = VibratorEnums.Actual;       // if the user is using a simulated vibrator
    public VibeSimType VibeSimAudio { get; set; } = VibeSimType.Quiet;          // the audio settings for the simulated vibrator
    public bool IntifaceAutoConnect { get; set; } = false;                      // if we should auto-connect to intiface
    public string IntifaceConnectionSocket { get; set; } = "ws://localhost:12345"; // connection link from plugin to intiface

    // GLOBAL HARDCORE SETTINGS. (maybe make it its own file if it gets too rediculous but yeah.
    public string PiShockApiKey { get; set; } = ""; // PiShock Settings.
    public string PiShockUsername { get; set; } = ""; // PiShock Settings.
    public float BlindfoldMaxOpacity { get; set; } = 1.0f; // Blindfold Opacity
    [JsonConverter(typeof(ConcreteNodeConverter))]
    public TextFolderNode ForcedStayPromptList { get; set; } = new TextFolderNode { FriendlyName = "ForcedDeclineList" }; // ForcedToStay storage
    public bool MoveToChambersInEstates { get; set; } = false; // Move to Chambers in Estates during ForcedStay
}

