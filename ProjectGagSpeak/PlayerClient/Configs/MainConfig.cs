using CkCommons.GarblerCore;
using CkCommons.HybridSaver;
using GagSpeak.Game;
using GagSpeak.Services;
using GagSpeak.Services.Configs;

namespace GagSpeak.PlayerClient;

public class MainConfig : IHybridSavable
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
            ["Config"] = JObject.FromObject(Current),
            ["LogLevel"] = LogLevel.ToString(),
            ["LoggerFilters"] = JToken.FromObject(LoggerFilters)
        }.ToString(Formatting.Indented);
    }

    public MainConfig(HybridSaveService saver)
    {
        _saver = saver;
        Load();
    }

    public void Save() => _saver.Save(this);
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
                    jObject = ConfigMigrator.MigrateMainConfig(jObject, _saver.FileNames);
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
            Current = jObject["Config"]?.ToObject<GagspeakConfig>() ?? new GagspeakConfig();

            // Load static fields safely
            if (Enum.TryParse(jObject["LogLevel"]?.Value<string>(), out LogLevel logLevel))
                LogLevel = logLevel;
            else
                LogLevel = LogLevel.Trace;  // Default fallback

            // Handle outdated hashset format, and new format for log filters.
            var token = jObject["LoggerFilters"];
            if(token is JArray array)
            {
                var list = array.ToObject<List<LoggerType>>() ?? new List<LoggerType>();
                LoggerFilters = list.Aggregate(LoggerType.None, (acc, val) => acc | val);
            }
            else
            {
                LoggerFilters = token?.ToObject<LoggerType>() ?? LoggerType.Recommended;
            }

            // Load ForcedStayPromptList
            ForcedStayPromptList = jObject["ForcedStayPromptList"]?.ToObject<TextFolderNode>() 
                ?? new TextFolderNode { FriendlyName = "ForcedDeclineList" };

            Svc.Logger.Information("Config loaded.");
            Save();
        }
        catch (Exception ex) { Svc.Logger.Error("Failed to load config." + ex); }
    }

    public GagspeakConfig Current { get; private set; } = new();
    public static LogLevel LogLevel = LogLevel.Trace;
    public static LoggerType LoggerFilters = LoggerType.Recommended;
    [JsonConverter(typeof(ConcreteNodeConverter))]
    public static TextFolderNode ForcedStayPromptList = new TextFolderNode { FriendlyName = "ForcedDeclineList" }; // ForcedToStay storage


    // Hardcore RUNTIME ONLY VARIABLE STORAGE.
    [JsonIgnore] internal static string LastSeenNodeName { get; set; } = string.Empty; // The Node Visible Name
    [JsonIgnore] internal static string LastSeenNodeLabel { get; set; } = string.Empty; // The Label of the nodes Prompt
    [JsonIgnore] internal static (int Index, string Text)[] LastSeenListEntries { get; set; } = []; // The nodes Options
    [JsonIgnore] internal static string LastSeenListSelection { get; set; } = string.Empty; // Option we last selected
    [JsonIgnore] internal static int LastSeenListIndex { get; set; } // Index in the list that was selected
    [JsonIgnore] internal static TextEntryNode LastSelectedListNode { get; set; } = new();

    #region Hardcore & Helpers
    public static IEnumerable<ITextNode> GetAllNodes()
    {
        return new ITextNode[] { ForcedStayPromptList }
            .Concat(GetAllNodes(ForcedStayPromptList.Children));
    }

    public static IEnumerable<ITextNode> GetAllNodes(IEnumerable<ITextNode> nodes)
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
        ForcedStayPromptList.Children.Add(newNode);
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
        ForcedStayPromptList.Children.Add(newNode);
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
        ForcedStayPromptList.Children.Add(newNode);
        Save();
    }

    #endregion Hardcore & Helpers
}
