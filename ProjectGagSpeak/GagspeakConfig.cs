using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.UI;

namespace GagSpeak;

public class GagspeakConfigService : IHybridSavable
{
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    private readonly HybridSaveService _saver;
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public string GetFileName(ConfigFileProvider files, out bool upa) => (upa = false, files.MainConfig).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize() => JsonConvert.SerializeObject(Config, Formatting.Indented);
    public GagspeakConfigService(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
    public void Load()
    {
        var file = _saver.FileNames.MainConfig;
        if (!File.Exists(file))
            return;

        try
        {
            var load = JsonConvert.DeserializeObject<GagspeakConfig>(File.ReadAllText(file));
            if (load is null)
                throw new Exception("Failed to load Config.");

            Config = load;

            // Help do log checks.
            if (Config.LoggerFilters.Count is 0 || Config.LoggerFilters.Contains(LoggerType.SpatialAudioLogger))
                Config.LoggerFilters = LoggerFilter.GetAllRecommendedFilters();
        }
        catch (Exception e)
        {
            StaticLogger.Logger.LogCritical(e, "Failed to load Config.");
        }
    }

    public GagspeakConfig Config { get; private set; } = new GagspeakConfig();

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
    public bool AccountCreated { get; set; } = false;

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

    public LogLevel LogLevel { get; set; } = LogLevel.Trace;
    public HashSet<LoggerType> LoggerFilters { get; set; } = new HashSet<LoggerType>();
    public NotificationLocation InfoNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation WarningNotification { get; set; } = NotificationLocation.Both;
    public NotificationLocation ErrorNotification { get; set; } = NotificationLocation.Both;

    // GLOBAL SETTINGS for client user.
    public string Safeword { get; set; } = "";
    public string Language { get; set; } = "English"; // MuffleCore
    public string LanguageDialect { get; set; } = "IPA_US"; // MuffleCore
    public bool CursedLootPanel { get; set; } = false; // CursedLootPanel
    public bool RemoveRestrictionOnTimerExpire { get; set; } = false; // Auto-Remove Items when timer falloff occurs.

    // GLOBAL VIBRATOR SETTINGS
    public VibratorEnums VibratorMode { get; set; } = VibratorEnums.Actual;       // if the user is using a simulated vibrator
    public VibeSimType VibeSimAudio { get; set; } = VibeSimType.Quiet;          // the audio settings for the simulated vibrator
    public bool IntifaceAutoConnect { get; set; } = false;                      // if we should auto-connect to intiface
    public string IntifaceConnectionSocket { get; set; } = "ws://localhost:12345"; // connection link from plugin to intiface

    // GLOBAL HARDCORE SETTINGS. (maybe make it its own file if it gets too rediculous but yeah.
    public string PiShockApiKey { get; set; } = ""; // PiShock Settings.
    public string PiShockUsername { get; set; } = ""; // PiShock Settings.
    public BlindfoldType BlindfoldStyle { get; set; } = BlindfoldType.Sensual; // Blindfold Format
    public bool ForceLockFirstPerson { get; set; } = false; // Force First-Person state while blindfolded.
    public float BlindfoldOpacity { get; set; } = 1.0f; // Blindfold Opacity
    [JsonConverter(typeof(ConcreteNodeConverter))]
    public TextFolderNode ForcedStayPromptList { get; set; } = new TextFolderNode { FriendlyName = "ForcedDeclineList" }; // ForcedToStay storage
    public bool MoveToChambersInEstates { get; set; } = false; // Move to Chambers in Estates during ForcedStay
}

public static class GagspeakConfigEx
{
    public static bool HasValidSetup(this GagspeakConfig configuration)
    {
        return configuration.AcknowledgementUnderstood && configuration.AccountCreated;
    }
}

