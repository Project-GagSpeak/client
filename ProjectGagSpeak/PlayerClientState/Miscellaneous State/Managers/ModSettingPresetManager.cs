using Dalamud.Plugin;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using OtterGui;
using Penumbra.Api.IpcSubscribers;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerState.Visual;

public sealed class ModPresetEditorCache
{
    /// <summary> The Mod being edited. </summary>
    public readonly Mod CurrentMod;

    /// <summary> The name of the preset. </summary>
    public readonly string PresetName;

    /// <summary> All of the Mod's available Options. </summary>
    public readonly ModSettingOptions AllModOptions;

    /// <summary> The Selected Settings for the preset. Edits are made through modified settings var. </summary>
    public readonly ModSettings SelectedSettings;

    /// <summary> The settings adjusted during editing. </summary>
    public Dictionary<string, string[]> ModifiedSettings { get; private set; } = new();

    // Make the only constructor require everything
    public ModPresetEditorCache(Mod mod, string presetName, ModSettingOptions options, ModSettings settings)
    {
        CurrentMod = mod;
        PresetName = presetName;
        AllModOptions = options;
        SelectedSettings = settings;
        // set up the modified settings.
        ModifiedSettings = settings.Settings.ToDictionary(k => k.Key, v => v.Value.ToArray());
    }

    public string GroupSelectedOption(string key)
        => ModifiedSettings.GetValueOrDefault(key)?[0] ?? string.Empty;

    public string[] GroupSelectedOptions(string key)
        => ModifiedSettings.GetValueOrDefault(key) ?? new string[0];

    public void UpdateSetting(string key, string value)
        => ModifiedSettings[key] = new string[] { value };

    public void UpdateSetting(string key, string[] value)
        => ModifiedSettings[key] = value;
}


// MAINTAINERS NOTE: (And possibly future corby that will be pissed off to read this)
// - There is a lot of checking going on because it is difficult to know if the containers are in sync.
// - Idealy in the future, make it so that there is a container updater / syncer to prevent this.
// But for now, it will look messy...
//


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

        // if penumbra api is connected, immidiately run a onpenumbrainitialized after our load.
        if (IpcCallerPenumbra.APIAvailable)
            OnPenumbraInitialized();

        // This Mod Combo needs to ping preset combo on selection.
        ModCombo = new ModCombo(logger, () => [ .. _modOptionsAvailable.Keys.OrderBy(m => m.DirectoryName) ]);
        PresetCombo = new ModPresetCombo(logger, () => [
            // Dependant on ModCombo. Can look into how glamourer updates current design selection to fix this, but otherwise idk.
            .. _settingPresetStorage
                .TryGetValue(ModCombo.CurrentSelection.DirectoryName, out var presets)
                ? presets.Select(p => (p.Key, p.Value)).ToList()
                : new List<(string, ModSettings)>()
            ]);
    }

    // Should be moved over to the drawer. This is a mess currently.
    public ModCombo ModCombo { get; private set; }
    public ModPresetCombo PresetCombo { get; private set; }

    protected override void Dispose(bool disposing)
    {
        // unsubscribe from the penumbra events.
        base.Dispose(disposing);
        _penumbra.OnModMoved?.Dispose();
    }

    // Internal Cache for editing.
    public ModPresetEditorCache? CurrentEditCache { get; private set; } = null;

    /// <summary> The collection of the client's current mods. Useful for the ModCombo. </summary>
    public IReadOnlyList<Mod> _modList => _modOptionsAvailable.Keys.ToList();
    public Dictionary<Mod, ModSettingOptions> _modOptionsAvailable { get; private set; } = new();

    /// <summary> The collection of the clients configured setting presets. </summary>
    /// <remarks> Format: (ModDirectory, (PresetName, Settings)) </remarks>
    public Dictionary<string, Dictionary<string, ModSettings>> _settingPresetStorage { get; private set; } = new();


    /// <summary> Fired when Penumbra is initialized. </summary>
    private void OnPenumbraInitialized()
    {
        Logger.LogInformation("Penumbra initialized. Loading mod list.");
        _modOptionsAvailable = _penumbra.GetModsWithAllOptions();

        // Remove any invalid entries from the preset storage
        var invalidMods = _settingPresetStorage.Keys
            .Where(dir => !_modOptionsAvailable.Keys.Any(m => m.DirectoryName == dir))
            .ToList();

        foreach (var mod in invalidMods)
        {
            Logger.LogWarning($"Removing invalid mod preset: {mod}");
            _settingPresetStorage.Remove(mod);
            Mediator.Publish(new ModSettingPresetRemoved(mod));
        }
        _saver.Save(this);
    }

    /// <summary> Fired whenever a MOD DIRECTORY (not mod name) is moved or renamed in penumbra. We should get a full recalculation if this occurs. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        // First, see if the old path is null, this means a mod was added.
        if (oldPath is null)
        {
            Logger.LogInformation($"Mod added: {newPath}, updating mod list.");
            _modOptionsAvailable = _penumbra.GetModsWithAllOptions();
            return;
        }

        // If the new path was null, it means a mod was removed, so update the list.
        if (newPath is null)
        {
            Logger.LogInformation($"Mod removed: {oldPath}, updating mod list.");
            _modOptionsAvailable.Remove(_modList.FirstOrDefault(m => m.DirectoryName == oldPath));
            return;
        }

        // if both paths are not null, it means a mod directory was changed.
        Logger.LogInformation($"Mod renamed: {oldPath} → {newPath}");
        if (_settingPresetStorage.TryGetValue(oldPath, out var settings))
        {
            _settingPresetStorage[newPath] = settings;
            _settingPresetStorage.Remove(oldPath);
            _saver.Save(this);
        }
    }

    public ModSettings CurrentModSettings(Mod mod)
        => _penumbra.GetSettingsForMod(mod);

    public IReadOnlyDictionary<string, ModSettings> GetModPresets(string modDirectory)
        => _settingPresetStorage.TryGetValue(modDirectory, out var presets)
            ? presets : new Dictionary<string, ModSettings>();

    /// <summary> Adds a new custom setting preset, or updates the existing. </summary>
    public void AddModPreset(Mod modItem, string presetName)
    {
        // if the storage does not yet contain a directory path for our mod, create it.
        if (!_settingPresetStorage.ContainsKey(modItem.DirectoryName))
            _settingPresetStorage[modItem.DirectoryName] = new Dictionary<string, ModSettings>();

        // grab the current settings for the default.
        var settings = CurrentModSettings(modItem);
        // if we are trying to add a preset name that already exists, modify the name
        var finalName = RegexEx.EnsureUniqueName(presetName, _settingPresetStorage[modItem.DirectoryName].Keys, p => p);
        // add the new preset to the storage.
        _settingPresetStorage[modItem.DirectoryName][finalName] = settings;
        _saver.Save(this);
        Logger.LogInformation($"Adding preset '{finalName}' for mod '{modItem}'");
    }

    public void UpdateSettingPreset(Mod modItem, string presetName, ModSettings modSettings)
    {
        if (!_settingPresetStorage.ContainsKey(modItem.DirectoryName))
            _settingPresetStorage[modItem.DirectoryName] = new Dictionary<string, ModSettings>();

        _settingPresetStorage[modItem.DirectoryName][presetName] = modSettings;
        _saver.Save(this);
        Logger.LogInformation($"Updated preset '{presetName}' for mod '{modItem}'");
    }

    public void RenameModPreset(Mod modItem, string oldName, string newName)
    {
        if (!_settingPresetStorage.TryGetValue(modItem.DirectoryName, out var presets))
            return;

        if (!presets.TryGetValue(oldName, out var settings))
            return;

        presets.Remove(oldName);
        presets[newName] = settings;
        _saver.Save(this);
        Logger.LogInformation($"Renamed preset '{oldName}' to '{newName}' for mod '{modItem}'");
    }

    /// <summary> Removes a custom setting preset. </summary>
    /// <remarks> If no other presets exist for this mod, the directory key is removed from the main dictionary. </remarks>
    public void RemoveSettingPreset(string modDirectory, string presetName)
    {
        if (_settingPresetStorage.TryGetValue(modDirectory, out var presets) && presets.Remove(presetName))
        {
            Logger.LogInformation($"Removed preset '{presetName}' from mod '{modDirectory}'.");
            _saver.Save(this);
        }
    }

    /// <summary> Gets the custom settings for a particular preset. </summary>
    public ModSettings GetSettingPreset(string dir, string presetName)
        => _settingPresetStorage.TryGetValue(dir, out var presets) && presets.TryGetValue(presetName, out var settings)
            ? settings : ModSettings.Empty;

    public bool StartEditingCustomPreset(Mod chosenMod, string chosenModsPreset)
    {
        // If we are already editing, we should not be able to start another edit.
        if (CurrentEditCache != null)
        {
            Logger.LogError("Already editing a preset.");
            return false;
        }

        // Verify the integrity of the directory paramater.
        if (!_modOptionsAvailable.ContainsKey(chosenMod))
        {
            Logger.LogError($"Mod {chosenMod.DirectoryName} not found in available options.");
            return false;
        }

        if (!_settingPresetStorage.ContainsKey(chosenMod.DirectoryName))
        {
            Logger.LogError($"Mod {chosenMod.DirectoryName} not found in preset storage.");
            return false;
        }

        if (!_settingPresetStorage[chosenMod.DirectoryName].ContainsKey(chosenModsPreset))
        {
            Logger.LogError($"Preset {chosenModsPreset} not found in preset storage for mod {chosenMod.DirectoryName}.");
            return false;
        }

        // By now we are 100% certain all values exist, so get them.
        CurrentEditCache = new ModPresetEditorCache(
            chosenMod,
            chosenModsPreset,
            _modOptionsAvailable[chosenMod],
            _settingPresetStorage[chosenMod.DirectoryName][chosenModsPreset]
            );

        return true;
    }

    public bool ExitEditingAndSave()
    {
        if (CurrentEditCache is null)
        {
            Logger.LogError("No current edit cache to save.");
            return false;
        }

        UpdateSettingPreset(
            CurrentEditCache.CurrentMod,
            CurrentEditCache.PresetName,
            CurrentEditCache.SelectedSettings with { Settings = CurrentEditCache.ModifiedSettings.ToDictionary(k => k.Key, v => v.Value.ToList()) }
            );
        CurrentEditCache = null;
        return true;
    }

    public bool ExitEditingAndDiscard()
    {
        CurrentEditCache = null;
        return true;
    }


    // Primarily for previewing.
    public bool TryGetModOptions(Mod mod, string presetName, out ModSettingOptions options, out ModSettings chosen)
    {
        options = ModSettingOptions.Empty;
        chosen = new ModSettings();
        if (!_modOptionsAvailable.TryGetValue(mod, out options))
            return false;

        if (_settingPresetStorage.TryGetValue(mod.DirectoryName, out var p) && p.TryGetValue(presetName, out var preset))
            chosen = preset;

        return true;
    }

    /// <summary> Used by ModCombos for associations that do not yet exist. </summary>
    /// <remarks> Unsure at the moment how much this is really needed. </remarks>
    public ModAssociation GenerateModAssociation(string modDir)
    {
        var mod = _modOptionsAvailable.Keys.FirstOrDefault(m => m.DirectoryName == modDir);
        var ret = new ModAssociation { ModInfo = mod };

        ret.CustomSettings = _settingPresetStorage.TryGetValue(modDir, out var presets)
            ? presets.Keys.FirstOrDefault() ?? string.Empty
            : string.Empty;

        return ret;
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
        // Create a JObject to hold all the preset data
        JObject customPresets = new JObject();

        // Loop through each mod directory in _settingPresetStorage
        foreach (var modDirectory in _settingPresetStorage)
        {
            // Create a JObject for each mod directory's presets
            JObject presetContainer = new JObject();

            // Loop through each preset within the mod directory
            foreach (var preset in modDirectory.Value)
            {
                // Serialize the ModSettings for each preset as a JObject
                JObject presetSettings = JObject.FromObject(preset.Value.Settings);

                // Add the preset settings to the preset container
                presetContainer[preset.Key] = presetSettings;
            }

            // Add the preset container to the main customPresets JObject
            customPresets[modDirectory.Key] = presetContainer;
        }

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

                if (!_settingPresetStorage.TryGetValue(modDir, out var storagePresets))
                {
                    storagePresets = new Dictionary<string, ModSettings>();
                    _settingPresetStorage[modDir] = storagePresets;
                }
                storagePresets[presetName] = modSettings;
                Logger.LogInformation($"Added/Updated preset '{presetName}' for mod '{modDir}'");
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
