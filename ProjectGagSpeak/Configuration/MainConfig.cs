using CkCommons;
using CkCommons.HybridSaver;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Services.Configs;
using NAudio.Wave;

namespace GagSpeak.PlayerClient;

public class MainConfig : IHybridSavable, IAudioConfig<MainConfigData>, IDisposable
{
    private readonly ILogger<MainConfig> _logger;
    private readonly HybridSaveService _saver;

    // Cached items for custom alert configuration.
    private AudioFileReader? _audioFile;
    private WaveOutEvent? _audioEvent;

    [JsonIgnore] public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    [JsonIgnore] public HybridSaveType SaveType => HybridSaveType.Json;
    public int ConfigVersion => 1;
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

    public MainConfig(ILogger<MainConfig> logger, HybridSaveService saver)
    {
        _logger = logger;
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
