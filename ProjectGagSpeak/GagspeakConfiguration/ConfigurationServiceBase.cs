/*using GagSpeak.GagspeakConfiguration.Configurations;

namespace GagSpeak.GagspeakConfiguration;

// TODO: REMOVE THIS ENTIRELY.
public abstract class ConfigurationServiceBase<T> : IDisposable where T : IGagspeakConfiguration
{
    private readonly CancellationTokenSource _periodicCheckCts = new(); // cancellation token source for periodic checks
    protected bool _configIsDirty = false; // if the config is dirty
    protected DateTime _configLastWriteTime; // last write time
    private Lazy<T> _currentConfigInternal; // current config
    private string? _currentUid = null; // current user id
    protected ConfigurationServiceBase(string configurationDirectory)
    {
        ConfigurationDirectory = configurationDirectory;
        // Load the UID from persistent storage
        _currentUid = LoadUid();

        _ = Task.Run(CheckForDirtyConfigInternal, _periodicCheckCts.Token);

        _currentConfigInternal = LazyConfig();
    }

    public string ConfigurationDirectory { get; init; }
    public T Current => _currentConfigInternal.Value;
    protected abstract string ConfigurationName { get; }
    protected abstract bool PerCharacterConfigPath { get; }
    // path can either be universal or per character
    public string ConfigurationPath => PerCharacterConfigPath && !string.IsNullOrEmpty(_currentUid)
        ? Path.Combine(ConfigurationDirectory, _currentUid, ConfigurationName)
        : Path.Combine(ConfigurationDirectory, ConfigurationName);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    // Marks the configuration file as dirty to save on the next cycle.
    public void Save() => _configIsDirty = true;

    protected virtual void Dispose(bool disposing)
    {
        _periodicCheckCts.Cancel();
        _periodicCheckCts.Dispose();
        if (_configIsDirty) SaveDirtyConfig();
    }

    protected virtual JObject MigrateConfig(JObject oldConfigJson) { return oldConfigJson; }
    protected virtual T DeserializeConfig(JObject configJson) => JsonConvert.DeserializeObject<T>(configJson.ToString())!;

    // Direct Load.
    protected virtual T LoadConfig()
    {
        // if this config should be using a per-player file save, but the uid is null, return and do not load.
        if (PerCharacterConfigPath && string.IsNullOrEmpty(_currentUid))
        {
            //StaticLogger.Logger.LogWarning($"UID is null for {ConfigurationName} configuration. Not loading.");
            // return early so we do not save this config to the files
            return (T)Activator.CreateInstance(typeof(T))!;
        }

        EnsureDirectoryExists();

        T? config;
        if (!File.Exists(ConfigurationPath))
        {
            config = (T)Activator.CreateInstance(typeof(T))!;
            Save();
        }
        else
        {
            try
            {
                string json = File.ReadAllText(ConfigurationPath);
                var configJson = JObject.Parse(json);
                // Perform migration (If needed), then deserialization
                configJson = MigrateConfig(configJson);
                config = DeserializeConfig(configJson);
                Save();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load {ConfigurationName} configuration. {ex.StackTrace}");
            }
            // if config was null, create a new instance of the config
            if (config == null)
            {
                config = (T)Activator.CreateInstance(typeof(T))!;
                Save();
            }
        }
        // set last write time to prime save.
        _configLastWriteTime = GetConfigLastWriteTime();
        return config;
    }

    protected virtual void SaveDirtyConfig()
    {
        _configIsDirty = false;

        // Check if the UID is null and if the configuration should use a per-player file save
        if (PerCharacterConfigPath && string.IsNullOrEmpty(_currentUid))
        {
            //StaticLogger.Logger.LogWarning($"UID is null for {ConfigurationName} configuration. Not saving.");
            return; // Exit early to prevent saving
        }

        var existingConfigs = (PerCharacterConfigPath && !string.IsNullOrEmpty(_currentUid)
                            ? Directory.EnumerateFiles(Path.Combine(ConfigurationDirectory, _currentUid), ConfigurationName + ".bak.*").Select(c => new FileInfo(c))
                            : Directory.EnumerateFiles(ConfigurationDirectory, ConfigurationName + ".bak.*").Select(c => new FileInfo(c)))
            .OrderByDescending(c => c.LastWriteTime).ToList();

*//*        if (PerCharacterConfigPath && !string.IsNullOrEmpty(_currentUid))
            configFolder = Path.Combine(ConfigurationDirectory, _currentUid);

        var existingConfigs = Directory.EnumerateFiles(configFolder, ConfigurationName + ".bak.*")
            .Select(c => new FileInfo(c))
            .OrderByDescending(c => c.LastWriteTime).ToList();*//*

        var lastItem = existingConfigs.FirstOrDefault();
        if (lastItem is not null && lastItem.LastWriteTime.AddHours(2) <= DateTime.Now)
        {
            // Delete all but the most recent 2 backups
            if (existingConfigs.Skip(1).Any())
                foreach (var config in existingConfigs.Skip(1).ToList())
                    config.Delete();
            // Attempt to create backup. Consume if failed.
            try
            {
                File.Copy(ConfigurationPath, ConfigurationPath + ".bak." + DateTime.Now.ToString("yyyyMMddHHmmss"), overwrite: true);
            }
            catch {  *//* Consume *//* }
        }

        var temp = ConfigurationPath + ".tmp";
        string json = "";
        try
        {
            json = SerializeConfig(Current);
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to serialize {ConfigurationName} configuration. {ex.StackTrace}");
        }
        //StaticLogger.Logger.LogInformation($"Saving {ConfigurationName} configuration to {ConfigurationPath}");
        File.WriteAllText(temp, json);
        File.Move(temp, ConfigurationPath, true);
        _configLastWriteTime = new FileInfo(ConfigurationPath).LastWriteTimeUtc;
    }

    protected virtual string SerializeConfig(T config)
        => JsonConvert.SerializeObject(config, Formatting.Indented);

    private async Task CheckForDirtyConfigInternal()
    {
        while (!_periodicCheckCts.IsCancellationRequested)
        {
            if (_configIsDirty)
                SaveDirtyConfig();

            await Task.Delay(TimeSpan.FromSeconds(1), _periodicCheckCts.Token).ConfigureAwait(false);
        }
    }

    protected DateTime GetConfigLastWriteTime() => new FileInfo(ConfigurationPath).LastWriteTimeUtc;

    private Lazy<T> LazyConfig()
    {
        _configLastWriteTime = GetConfigLastWriteTime();
        return new Lazy<T>(LoadConfig);
    }

    // New method to update the UID
    public void UpdateUid(string newUid)
    {
        //StaticLogger.Logger.LogInformation($"Updating UID to {newUid}");
        if (_currentUid != newUid)
        {
            _currentUid = newUid;
            SaveUid(newUid);
        }
        _currentConfigInternal = LazyConfig(); // Recalculate the configuration path
        Save();
    }

    // Method to save the UID to persistent storage
    private void SaveUid(string uid)
    {
        var uidFilePath = Path.Combine(ConfigurationDirectory, "config-testing.json");
        if (!File.Exists(uidFilePath))
            return;

        string json = File.ReadAllText(uidFilePath);
        var configJson = JObject.Parse(json);
        configJson["LastUidLoggedIn"] = uid;
        File.WriteAllText(uidFilePath, configJson.ToString());
    }

    // Method to load the UID from persistent storage
    private string? LoadUid()
    {
        var uidFilePath = Path.Combine(ConfigurationDirectory, "config-testing.json");
        // if the file does not exist, throw an exception
        if (!File.Exists(uidFilePath))
            return null;
        // read the contents of the file
        string json = File.ReadAllText(uidFilePath);
        var configJson = JObject.Parse(json);
        var uid = configJson["LastUidLoggedIn"]?.Value<string>() ?? string.Empty;
        return uid;
    }

    private void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(ConfigurationPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            StaticLogger.Logger.LogInformation($"Creating directory: {directory}");
            Directory.CreateDirectory(directory);
        }
    }
}
*/
