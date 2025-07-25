using CkCommons.HybridSaver;
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

            Svc.Logger.Information("Config loaded.");
            Save();
        }
        catch (Bagagwa ex) { Svc.Logger.Error("Failed to load config." + ex); }
    }

    public GagspeakConfig Current { get; private set; } = new();
    public static LogLevel LogLevel = LogLevel.Trace;
    public static LoggerType LoggerFilters = LoggerType.Recommended;
}
