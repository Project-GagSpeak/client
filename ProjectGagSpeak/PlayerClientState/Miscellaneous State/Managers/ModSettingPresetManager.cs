using Dalamud.Plugin;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.Interop.Ipc;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using Penumbra.Api.IpcSubscribers;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Responsible for tracking the custom settings we have configured for a mod. </summary>
public class ModSettingPresetManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly IpcCallerPenumbra _penumbra;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public ModSettingPresetManager(ILogger<ModSettingPresetManager> logger, GagspeakMediator mediator, 
        IpcCallerPenumbra penumbra, FavoritesManager favorites, ConfigFileProvider fileNames, 
        HybridSaveService saver, IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _penumbra = penumbra;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
        Load();

        _penumbra.OnModMoved = ModMoved.Subscriber(pi, OnModInfoChanged);
        Mediator.Subscribe<PenumbraInitializedMessage>(this, (msg) => OnPenumbraInitialized());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _penumbra.OnModMoved?.Dispose();
    }

    /// <summary> The collection of the client's current mods. Useful for the ModCombo. </summary>
    public List<Mod> _modList { get; private set; }

    /// <summary> The collection of the clients configured setting presets. </summary>
    /// <remarks> Format: (ModDirectory, (PresetName, Settings)) </remarks>
    public Dictionary<string, Dictionary<string, ModSettings>> _settingPresetStorage { get; private set; } = new();


    /// <summary> Fired when Penumbra is initialized. </summary>
    private void OnPenumbraInitialized()
    {
        Logger.LogInformation("Penumbra initialized. Loading mod list.");
        _modList = _penumbra.GetMods();
        // Validate the _settingPresetStorage with our current arrangement of mods.
        foreach (var directory in _settingPresetStorage.Keys.ToList())
        {
            // if the directory no longer exists in the mod list, we should remove it from the storage.
            if (!_modList.Any(m => m.DirectoryName == directory))
            {
                Logger.LogWarning($"Mod {directory} was not found in the mod list. Removing it from the settings.");
                _settingPresetStorage.Remove(directory);
                // we should also send out a mediator update that this mod is no longer valid and to reset to default.
                Mediator.Publish(new ModSettingPresetRemoved(directory));
            }
        }
    }

    /// <summary> Fired whenever a mod is moved. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        // First, see if the old path is null, this means a mod was added.
        if (oldPath is null)
        {
            Logger.LogInformation($"Mod added: {newPath}, updating mod list.");
            _modList = _penumbra.GetMods();
            // no need for cleanup, nothing was changed.
            return;
        }

        // If the new path was null, it means a mod was removed, so update the list.
        if (newPath is null)
        {
            Logger.LogInformation($"Mod removed: {oldPath}, updating mod list.");
            _modList.Remove(_modList.FirstOrDefault(m => m.DirectoryName == oldPath));
            return;
        }

        // if both paths are not null, it means a mod directory was changed.
        Logger.LogInformation($"Mod Directory Changed: {oldPath} -> {newPath}, updating mod list.");
        _modList = _penumbra.GetMods();
        if(_settingPresetStorage.ContainsKey(oldPath))
        {
            _settingPresetStorage[newPath] = _settingPresetStorage[oldPath];
            _settingPresetStorage.Remove(oldPath);
        }
    }

    /// <summary> Gets the count of custom setting presets for a particular mod. </summary>
    public int PresetCountForMod(string dir) 
        => _settingPresetStorage.TryGetValue(dir, out var presets) ? presets.Count : 0;

    /// <summary> Adds a new custom setting preset, or updates the existing. </summary>
    public void AddOrUpdateSettingPreset(string dir, string name, ModSettings settings)
    {
        if (!_settingPresetStorage.ContainsKey(dir))
            _settingPresetStorage[dir] = new Dictionary<string, ModSettings>();

        _settingPresetStorage[dir][name] = settings;
        Logger.LogInformation($"Added new preset '{name}' for mod path '{dir}'.");
    }

    /// <summary> Removes a custom setting preset. </summary>
    /// <remarks> If no other presets exist for this mod, the directory key is removed from the main dictionary. </remarks>
    public void RemoveSettingPreset(string dir, string name)
    {
        if (_settingPresetStorage.TryGetValue(dir, out var presets) && presets.Remove(name))
        {
            Logger.LogInformation($"Removed preset '{name}' from mod '{dir}'.");
            if (presets.Count == 0)
                _settingPresetStorage.Remove(dir);
        }
    }

    /// <summary> Renames a custom setting preset. </summary>
    public void RenameSettingPreset(string dir, string oldPresetName, string newPresetName)
    {
        if (_settingPresetStorage.TryGetValue(dir, out var presets) && presets.ContainsKey(oldPresetName))
        {
            presets[newPresetName] = presets[oldPresetName];
            presets.Remove(oldPresetName);
            Logger.LogInformation($"Renamed preset '{oldPresetName}' to '{newPresetName}' in mod '{dir}'.");
        }
    }

    /// <summary> Gets the custom settings for a particular preset. </summary>
    public ModSettings GetSettingPreset(string dir, string name)
    {
        return _settingPresetStorage.Keys.Contains(dir) && _settingPresetStorage[dir].TryGetValue(name, out var settings)
            ? settings : ModSettings.Empty;
    }

    /// <summary> Gets the current settings for the passed in mod or directory. </summary>
    public ModSettings GetCurrentModSettings(string dir)
    {
        if (!_modList.Any(m => m.DirectoryName == dir))
            return ModSettings.Empty;

        return _penumbra.GetSettingsForMod(_modList.First(m => m.DirectoryName == dir));
    }

    public ModSettings GetCurrentModSettings(Mod mod)
        => _penumbra.GetSettingsForMod(mod);

    /// <summary> Gets all settings of a particular mod by directory for editing. </summary>
    public ModSettingOptions GetAllOptionsForMod(string dir)
    {
        // grab the Mod item from the modlist with the matching directory.
        if (!_modList.Any(m => m.DirectoryName == dir))
            return ModSettingOptions.Empty;

        // obtain the mod from the list, since it exists.
        var mod = _modList.First(m => m.DirectoryName == dir);
        return _penumbra.GetAllOptionsForMod(mod);
    }

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CustomModSettings).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        var customPresets = new JObject(
            _settingPresetStorage.ToDictionary( // Maps mod directory keys to preset containers
                modDirectory => modDirectory.Key,
                modDirectory => new JObject(
                    modDirectory.Value.ToDictionary( // Maps preset names to settings
                        preset => preset.Key,
                        preset => JToken.FromObject(preset.Value.Settings))))); // Converts settings to JSON

        return new JObject
        {
            ["Version"] = ConfigVersion,
            ["CustomPresets"] = customPresets
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.CustomModSettings;
        _settingPresetStorage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No CustomModSettings file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject["CustomPresets"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject presetDict)
            return;

        // otherwise, parse it out and stuff YIPPEE
        foreach (var (modDir, presetContainer) in presetDict)
        {
            if (presetContainer is not JObject presets)
                continue;

            foreach (var (presetName, settings) in presets)
            {
                if (settings is not JObject settingsObj)
                    continue;

                var settingsDict = settingsObj.ToObject<Dictionary<string, List<string>>>() ?? [];
                var loadedSettings = new Dictionary<string, List<string>>(settingsDict.Count);
                foreach (var (key, value) in settingsDict)
                    loadedSettings.Add(key, value);
                var modSettings = new ModSettings(loadedSettings, 0, false, false, false);
                AddOrUpdateSettingPreset(modDir, presetName, modSettings);
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSavable

}
