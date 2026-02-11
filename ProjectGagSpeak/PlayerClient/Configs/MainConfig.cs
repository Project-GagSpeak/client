using CkCommons;
using CkCommons.HybridSaver;
using FFXIVClientStructs.FFXIV.Common.Lua;
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
            ["LoggerFilters"] = JToken.FromObject(LoggerFilters),
            ["ServerPaused"] = ServerPaused
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
            Current       = jObject["Config"]?.ToObject<GagspeakConfig>() ?? new GagspeakConfig();
            LogLevel      = Enum.TryParse(jObject["LogLevel"]?.Value<string>(), out LogLevel logLevel) ? logLevel : LogLevel.Debug;
            LoggerFilters = GetLoggerFilters(jObject["LoggerFilters"]);
            ServerPaused  = jObject["ServerPaused"]?.Value<bool>() ?? false;

            Svc.Logger.Information("Config loaded.");
            Save();
        }
        catch (Bagagwa ex) { Svc.Logger.Error("Failed to load config." + ex); }
    }

    public GagspeakConfig Current { get; private set; } = new();

    /// <summary>
    ///     Updates the paused state of the server. <para />
    ///     When set to a value, the config is automatically saved.
    /// </summary>
    public bool ServerPaused { get; set; } = false;

    // For Themes and color customization
    public Dictionary<GsCol, uint> GsColors { get; private set; } = [];
    public Dictionary<CkCol, uint> CkColors { get; private set; } = [];

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
}
