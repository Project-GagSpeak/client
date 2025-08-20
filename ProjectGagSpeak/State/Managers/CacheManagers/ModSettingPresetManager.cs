using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.CustomCombos.Editor;
using GagSpeak.FileSystems;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using Lumina.Excel.Sheets;

namespace GagSpeak.State.Managers;

/// <summary> Responsible for tracking the custom settings we have configured for a mod. </summary>
public class ModPresetManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<ModSettingsPreset> _itemEditor = new();
    public ModPresetManager(ILogger<ModPresetManager> logger, GagspeakMediator mediator, 
        ConfigFileProvider fileNames, HybridSaveService saver) 
        : base(logger, mediator)
    {
        _fileNames = fileNames;
        _saver = saver;
        Load();

        // This Mod Combo needs to ping preset combo on selection.
        ModCombo = new ModCombo(Logger, () => [ ..ModData.OrderBy(m => m.Name).ThenBy(m => m.DirPath) ]);
        PresetCombo = new ModPresetCombo(Logger, this, () => [ 
            ..ModPresetStorage
                .ByDirectory(ModCombo.Current?.DirPath ?? string.Empty)?.ModPresets ?? new List<ModSettingsPreset>()
            ]);

        ModCombo.SelectionChanged += (s, a) => PresetCombo.SetDirty();
    }

    public ModCombo ModCombo { get; private set; }
    public ModPresetCombo PresetCombo { get; private set; }

    public ModPresetStorage ModPresetStorage { get; private set; } = new();
    public ModSettingsPreset? ItemInEditor => _itemEditor.ItemInEditor;

    /// <summary> Holds all essential information about each penumbra mod. </summary>
    /// <remarks> Contains Directory, Name, Priority, and ALL Available Options </remarks>
    public IReadOnlyList<ModInfo> ModData { get; private set; } = new List<ModInfo>();

    /// <summary> Adds a new custom setting preset, or updates the existing. </summary>
    public bool TryCreatePreset(ModPresetContainer? container, string newPresetName)
    {
        if (container is null)
            return false;

        if (!ModPresetStorage.Exists(m => m == container))
            return false;

        // Ensure uniqueness for the new name.
        var finalName = RegexEx.EnsureUniqueName(newPresetName, container.ModPresets, p => p.Label);
        var newPreset = new ModSettingsPreset(container)
        {
            Label = finalName,
            ModSettings = container.ModPresets[0].ModSettings
        };

        // Append the new preset to the list.
        Logger.LogDebug($"Adding preset '{finalName}' for mod '{container.ModName}'");
        container.ModPresets.Add(newPreset);
        _saver.Save(this);
        return true;
    }
    
    // do not rename mod containers, only their presets.
    public void RenamePreset(ModSettingsPreset preset, string newName)
    {
        var finalName = RegexEx.EnsureUniqueName(newName, preset.Container.ModPresets, p => p.Label);
        Logger.LogDebug($"Renaming preset '{preset.Label}' to '{finalName}' for mod '{preset.Container.ModName}'");
        preset.Label = finalName;
        _saver.Save(this);
    }

    /// <summary> Removes a custom setting preset. </summary>
    /// <remarks> If no other presets exist for this mod, the directory key is removed from the main dictionary. </remarks>
    public void RemovePreset(ModSettingsPreset preset)
    {
        // get the preset's container to properly remove it.
        var container = preset.Container;

        // try and remove the preset from the container.
        if (!container.ModPresets.Remove(preset))
            return;

        Logger.LogDebug($"Removed preset '{preset.Label}' for mod '{container.ModName}'");
        _saver.Save(this);
    }

    public void StartEditingCustomPreset(string modDirPath, string presetLabel)
    {
        // Do not edit if mods are invalid.
        if (!ModData.Any(m => m.DirPath == modDirPath))
            return;

        // Don't process if the mod directory is not setup in storage.
        if (ModPresetStorage.ByDirectory(modDirPath) is not { } container)
            return;

        // If the passed in preset can't be found in the list, reject it.
        if (container.ModPresets.FirstOrDefault(mp => mp.Label == presetLabel) is not { } preset)
            return;

        _itemEditor.StartEditing(ModPresetStorage, preset);
    }

    public void ExitEditingAndSave()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogDebug($"Saved changes to preset '{sourceItem.Label}' for mod '{sourceItem.Container.ModName}'");
            _saver.Save(this);
        }
    }

    public void ExitEditingAndDiscard()
        => _itemEditor.QuitEditing();

    public ModInfo? GetModInfo(string dirPath)
        => ModData.FirstOrDefault(m => m.DirPath == dirPath);

    /// <summary> Fired when Penumbra is initialized. </summary>
    public void PenumbraInitialized(IReadOnlyList<(ModInfo ModData, Dictionary<string, List<string>> CurrentOptions)> data)
    {
        // Set the mod data.
        ModData = data.Select(d => d.ModData).ToList();

        foreach (var (modInfo, curOptions) in data)
        {
            // if a container does not yet exist for a directory path, then create it.
            if (ModPresetStorage.ByDirectory(modInfo.DirPath) is not { } container)
            {
                Logger.LogTrace($"No Container in storage exists for: {modInfo.ToString()}");
                // Create a new container object for the mod.
                var newContainer = new ModPresetContainer(modInfo);
                ModPresetStorage.Add(newContainer);

                var preset = new ModSettingsPreset(newContainer) { Label = "Current", ModSettings = curOptions };
                newContainer.ModPresets.Add(preset);
            }
            else
            {
                // Keep not logged unless debugging. Its very spammy lol.
                // Logger.LogTrace($"Container already exists for: {modInfo.ToString()}, syncing with current!");
                container.SyncWithPenumbraInfo(modInfo, curOptions);
            }
        }

        var invalidMods = ModPresetStorage.Where(dir => !ModData.Any(m => m.DirPath == dir.DirectoryPath)).ToList();
        foreach (var mod in invalidMods)
            ModPresetStorage.Remove(mod);
        // Save changes.
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.ModPreset));
    }

    public void OnModDirChanged(string oldPath, ModInfo newInfo, Dictionary<string, List<string>> latestCurrentOptions)
    {
        // firstly, if the old path is not present, just return.
        if (ModPresetStorage.ByDirectory(oldPath) is not { } container)
            return;

        // if there is a change, resync the contents with the updated data.
        container.SyncWithPenumbraInfo(newInfo, latestCurrentOptions);
        _saver.Save(this);
        Mediator.Publish(new ConfigModPresetChanged(StorageChangeType.Modified, container));
    }

    public void OnModAdded(ModInfo info, Dictionary<string, List<string>> currentOptions)
    {
        // If a container does not yet exist for the mod, then create it.
        if (ModPresetStorage.ByDirectory(info.DirPath) is not { } container)
        {
            Logger.LogTrace($"No Container in storage exists for: {info.ToString()}");
            // Create a new container object for the mod.
            container = new ModPresetContainer(info);
            ModPresetStorage.Add(container);
            var preset = new ModSettingsPreset(container) { Label = "Current", ModSettings = currentOptions };
            container.ModPresets.Add(preset);
            _saver.Save(this);
            Mediator.Publish(new ConfigModPresetChanged(StorageChangeType.Created, container));
        }
        else
        {
            Logger.LogTrace($"Container already exists for: {info.ToString()}, syncing with current!");
            container.SyncWithPenumbraInfo(info, currentOptions);
            _saver.Save(this);
            Mediator.Publish(new ConfigModPresetChanged(StorageChangeType.Created, container));
        }
    }

    public void OnModRemoved(string dirPath)
    {
        // Remove all presets from the container, and then the container itself.
        if (ModPresetStorage.FirstOrDefault(x => x.DirectoryPath == dirPath) is { } container)
        {
            Logger.LogTrace($"Removing Mod Preset Container for {container.ModName} ({dirPath})");
            // Remove all presets from the container
            container.ModPresets.Clear();
            ModPresetStorage.Remove(container);
            // Optionally, save and notify
            _saver.Save(this);
            Mediator.Publish(new ConfigModPresetChanged(StorageChangeType.Deleted, container));
        }
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
        var customPresets = new JArray();
        foreach (var presetContainer in ModPresetStorage)
        {
            var presetArray = new JArray();
            foreach (var preset in presetContainer.ModPresets)
                presetArray.Add(preset.Serialize());

            var containerObject = new JObject
            {
                ["DirectoryPath"] = presetContainer.DirectoryPath,
                ["ModName"] = presetContainer.ModName,
                ["Priority"] = presetContainer.Priority,
                ["ModPresets"] = presetArray
            };

            // Add the preset array to the main customPresets JObject
            customPresets.Add(containerObject);
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
        ModPresetStorage.Clear();
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
        if (data is not JArray presetList)
            return;

        try
        {
            ModPresetStorage.Clear(); // Reset Storage before loading in data.
            foreach (var presetContainer in presetList)
            {
                var dirPath = presetContainer["DirectoryPath"]?.Value<string>() ?? string.Empty;
                var modName = presetContainer["ModName"]?.Value<string>() ?? string.Empty;
                var priority = presetContainer["Priority"]?.Value<int>() ?? 0;

                if (presetContainer["ModPresets"] is not JArray presetArray)
                    continue;

                // Add the container to the storage, so our presets can recognize it.
                var container = new ModPresetContainer(dirPath, modName, string.Empty, priority);
                ModPresetStorage.Add(container);
                // Append the existing presets for this mod there.
                foreach (var presetToken in presetArray)
                {
                    if (presetToken is not JObject presetObj)
                        continue;

                    var preset = ModSettingsPreset.FromToken(presetObj, this);
                    container.ModPresets.Add(preset);
                }

                // Logger.LogInformation($"Loaded {container.ModPresets.Count} ModSettingPresets for {container.ModName}");
            }
        }
        catch (Bagagwa ex)
        {
            Logger.LogError(ex, "Failed to load custom mod settings.");
            ModPresetStorage.Clear();
            return;
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSavable

}
