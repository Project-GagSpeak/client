using Dalamud.Plugin;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.CkCommons.Newtonsoft;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
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
        _penumbra.OnModAdded = ModAdded.Subscriber(pi, OnModAdded);
        _penumbra.OnModDeleted = ModDeleted.Subscriber(pi, OnModDeleted);
        Mediator.Subscribe<PenumbraInitializedMessage>(this, (msg) => OnPenumbraInitialized());

        // if penumbra api is connected, immediately run a OnPenumbraInitialized after our load.
        if (IpcCallerPenumbra.APIAvailable)
            OnPenumbraInitialized();

        // This Mod Combo needs to ping preset combo on selection.
        ModCombo = new ModCombo(logger, () => [ ..ModData.OrderBy(m => m.Name).ThenBy(m => m.DirPath) ]);
        PresetCombo = new ModPresetCombo(logger, this, () => [ 
            ..ModPresetStorage
                .ByDirectory(ModCombo.Current?.DirPath ?? string.Empty)?.ModPresets ?? new List<ModSettingsPreset>()
            ]);

        ModCombo.SelectionChanged += (s, a) => PresetCombo.SetDirty();
    }

    public ModCombo ModCombo { get; private set; }
    public ModPresetCombo PresetCombo { get; private set; }
    public ModSettingsPreset? ActiveEditorItem { get; private set; } = null;
    protected override void Dispose(bool disposing)
    {
        // unsubscribe from the penumbra events.
        base.Dispose(disposing);
        _penumbra.OnModMoved?.Dispose();
        _penumbra.OnModAdded?.Dispose();
        _penumbra.OnModDeleted?.Dispose();
    }

    /// <summary> Holds all essential information about each penumbra mod. </summary>
    /// <remarks> Contains Directory, Name, Priority, and ALL Available Options </remarks>
    public IReadOnlyList<ModInfo> ModData { get; private set; } = new List<ModInfo>();

    // This is the internal storage and doesn't need to be a dictionary so that items can still reference it.
    public ModPresetStorage ModPresetStorage { get; private set; } = new();

    /// <summary> Fired when Penumbra is initialized. </summary>
    private void OnPenumbraInitialized()
    {
        Logger.LogInformation("Penumbra initialized. Retrieving Mod Info.");
        IReadOnlyList<(ModInfo ModData, Dictionary<string, List<string>> CurrentOptions)> data = _penumbra.GetModInfo();

        // Set the mod data.
        ModData = data.Select(d => d.ModData).ToList();

        foreach (var (modInfo, curOptions) in data)
        {
            if (ModPresetStorage.ByDirectory(modInfo.DirPath) is not { } container)
            {
                // Create a new container object for the mod.
                var newContainer = new ModPresetContainer(modInfo.DirPath, modInfo.Name, modInfo.Priority);
                ModPresetStorage.Add(newContainer);

                // add the default preset item for the mod.
                var preset = new ModSettingsPreset(newContainer)
                {
                    Label = "Current",
                    ModSettings = curOptions
                };

                // Add the preset to the container.
                newContainer.ModPresets.Add(preset);
            }
            // If the list is valid but has no presets, append the default one.
            else if (container.ModPresets.Count <= 0)
            {
                // Create a new preset object for the mod.
                var preset = new ModSettingsPreset(container)
                {
                    Label = "Current",
                    ModSettings = curOptions
                };
                // Add the preset to the container.
                container.ModPresets.Add(preset);
            }
            else
            {
                container.ModPresets[0].ModSettings = curOptions;
            }
        }

        var invalidMods = ModPresetStorage
            .Where(dir => !ModData.Any(m => m.DirPath == dir.DirectoryPath))
            .ToList();

        foreach (var mod in invalidMods)
            Logger.LogWarning($"Removing invalid mod preset: {mod}");

        // Save changes.
        _saver.Save(this);
    }

    /// <summary> Fired whenever a MOD DIRECTORY (not mod name) is moved or renamed in penumbra. We should get a full recalculation if this occurs. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        // TODO: (Handle how this affects other dependent sources, (Should not be an issue for us but we will see).
    }

    private void OnModAdded(string addedDirectory)
    {
        // TODO: Get the mod name for the directory, and its data necessary to construct a ModInfo object.
    }

    private void OnModDeleted(string deletedDirectory)
    {
        // TODO: Handle logic that updates anything using this directory to be removed.
    }

    /// <summary> Adds a new custom setting preset, or updates the existing. </summary>
    public bool TryAddModPreset(ModPresetContainer container, string newPresetName)
    {
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
        Logger.LogInformation($"Adding preset '{finalName}' for mod '{container.ModName}'");
        container.ModPresets.Add(newPreset);
        _saver.Save(this);
        return true;
    }

    /// <summary> Removes a custom setting preset. </summary>
    /// <remarks> If no other presets exist for this mod, the directory key is removed from the main dictionary. </remarks>
    public void RemovePreset(ModSettingsPreset preset)
    {
        var container = preset.Container;
        if (container.ModPresets.Remove(preset))
        {
            Logger.LogInformation($"Removed preset '{preset.Label}' for mod '{container.ModName}'");
            _saver.Save(this);
        }
        else
        {
            Logger.LogWarning($"Failed to remove preset '{preset.Label}' for mod '{container.ModName}'");
        }
    }

    public void StartEditingCustomPreset(string dirPath, string name)
    {
        // If we are already editing, we should not be able to start another edit.
        if (ActiveEditorItem is not null)
            return;

        // Do not edit if mods are invalid.
        if (!ModData.Any(m => m.DirPath == dirPath))
            return;

        // Don't process if the mod directory is not setup in storage.
        if (ModPresetStorage.ByDirectory(dirPath) is not { } container)
            return;

        // If the passed in preset can't be found in the list, reject it.
        if (container.ModPresets.FirstOrDefault(mp => mp.Label == name) is not { } preset)
        {
            Logger.LogError($"Mod {dirPath} not found in preset storage.");
            return;
        }

        // Clone the object for editing.
        ActiveEditorItem = new ModSettingsPreset(preset);
    }

    public void ExitEditingAndSave()
    {
        if (ActiveEditorItem is not { } editedItem)
            return;

        // update the preset data.
        if (editedItem.Container.ModPresets.FirstOrDefault(preset => preset.Label == editedItem.Label) is { } match)
        {
            match.Label = editedItem.Label;
            match.ModSettings = editedItem.ModSettings;
            _saver.Save(this);
            Logger.LogInformation($"Updated preset '{match.Label}' for mod '{editedItem.Container.ModName}'");
        }

        // reset to null.
        ActiveEditorItem = null;
    }

    public bool ExitEditingAndDiscard()
    {
        ActiveEditorItem = null;
        return true;
    }

    public ModInfo? GetModInfo(string dirPath)
        => ModData.FirstOrDefault(m => m.DirPath == dirPath);

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
                var container = new ModPresetContainer(dirPath, modName, priority);
                ModPresetStorage.Add(container);
                // Append the existing presets for this mod there.
                foreach (var presetToken in presetArray)
                {
                    if (presetToken is not JObject presetObj)
                        continue;

                    var preset = ModSettingsPreset.FromJToken(presetObj, this);
                    container.ModPresets.Add(preset);
                }

                Logger.LogDebug($"Loaded {container.ModPresets.Count} ModSettingPresets for {container.ModName}");
            }
        }
        catch (Exception ex)
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
