using Dalamud.Utility;
using GagSpeak.Interop;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerClient;

public class ModPresetStorage : List<ModPresetContainer>, IEditableStorage<ModSettingsPreset>
{
    public bool TryGetPresetContainer(string dirPath, [NotNullWhen(true)] out ModPresetContainer? container)
        => (container = this.FirstOrDefault(x => x.DirectoryPath == dirPath)) != null;

    public ModPresetContainer? ByDirectory(string dirPath)
        => this.FirstOrDefault(x => x.DirectoryPath == dirPath);

    public bool HasDirectory(string dirPath)
        => this.Any(mpc => mpc.DirectoryPath == dirPath);

    public bool TryApplyChanges(ModSettingsPreset oldItem, ModSettingsPreset changedItem)
    {
        if (changedItem is null || changedItem is not ModSettingsPreset)
            return false;

        oldItem.ApplyChanges(changedItem);
        return true;
    }
}

public class ModPresetContainer
{
    public string DirectoryPath { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public string FileSystemPath { get; set; } = string.Empty;
    public int Priority { get; set; } = -1;
    public List<ModSettingsPreset> ModPresets { get; set; } = new();

    public ModPresetContainer() 
    { }

    public ModPresetContainer(string dirPath, string modName, string fileSystemPath, int priority)
    {
        DirectoryPath = dirPath;
        ModName = modName;
        FileSystemPath = fileSystemPath;
        Priority = priority;
    }

    public ModPresetContainer(ModInfo info)
    {
        DirectoryPath = info.DirPath;
        ModName = info.Name;
        FileSystemPath = info.FsPath;
        Priority = info.Priority;
    }

    public void SyncWithPenumbraInfo(ModInfo info, Dictionary<string, List<string>> currentOptions)
    {
        // update directory path and mod name and file system path.
        DirectoryPath = info.DirPath;
        ModName = info.Name;
        FileSystemPath = info.FsPath;
        Priority = info.Priority;
        // add the current options as a new preset if there are none, or update them if there is.
        if (ModPresets.Count <= 0)
        {
            // Create a new preset object for the mod.
            var preset = new ModSettingsPreset(this)
            {
                Label = "Current",
                ModSettings = currentOptions
            };
            // Add the preset to the container.
            ModPresets.Add(preset);
        }
        else
        {
            ModPresets[0].ModSettings = currentOptions;
        }
    }
}

public sealed class ModSettingsPreset : IEditableStorageItem<ModSettingsPreset>, IEquatable<ModSettingsPreset>, IComparable<ModSettingsPreset>
{
    public ModPresetContainer               Container   { get; private set; }
    public string                           Label       { get; set; } = string.Empty;
    public Dictionary<string, List<string>> ModSettings { get; set; } = new();

    public string CacheKey => $"{Container.DirectoryPath}-{Label}";

    public ModSettingsPreset(ModPresetContainer container)
        => Container = container;

    public ModSettingsPreset(ModSettingsPreset other)
        => ApplyChanges(other);

    public bool HasData 
        => !string.IsNullOrEmpty(Container.DirectoryPath) && !string.IsNullOrEmpty(Label);

    public ModSettingsPreset Clone(bool _) 
        => new ModSettingsPreset(this);

    // perform a deep copy of the settings from another preset.
    public void ApplyChanges(ModSettingsPreset other)
    {
        Container = other.Container;
        Label = other.Label;
        ModSettings = new Dictionary<string, List<string>>(other.ModSettings);
    }

    public int CompareTo(ModSettingsPreset? other)
    {
        if (other == null)
            return 1;

        return string.Compare(CacheKey, other.CacheKey, StringComparison.Ordinal);
    }

    public bool Equals(ModSettingsPreset? other)
    {
        if (other is null)
            return false;
        return (CacheKey == other.CacheKey);
    }

    public override int GetHashCode()
        => CacheKey.GetHashCode();

    public override string ToString()
        => HasData ? $"{Label} ({Container.ModName})" : "UNK";

    public string SelectedOption(string group)
        => ModSettings.TryGetValue(group, out var selected) ? selected[0] : string.Empty;

    public string[] SelectedOptions(string group)
        => ModSettings.TryGetValue(group, out var selected) ? selected.ToArray() : Array.Empty<string>();

    /// <summary> To be used by the ModPresetManager for serialization. </summary>

    public JObject Serialize()
    {
        return new JObject
        {
            ["DirectoryPath"] = Container.DirectoryPath,
            ["Label"] = Label,
            ["ModSettings"] = new JObject(ModSettings.Select(kvp => new JProperty(kvp.Key, new JArray(kvp.Value)))),
        };
    }

    public JObject SerializeReference()
    {
        return new JObject
        {
            ["DirectoryPath"] = Container.DirectoryPath,
            ["Label"] = Label,
        };
    }

    // For other managers outside modPresetStorage
    public static ModSettingsPreset FromRefToken(JToken? token, ModPresetManager mp)
    {
        if (token is not JObject jsonObject)
            throw new Exception("Invalid ModSettingsPreset data!");

        var dirPath = jsonObject["DirectoryPath"]?.Value<string>();
        var presetName = jsonObject["Label"]?.Value<string>();
        // if the directory path is an empty string, then we should return a default preset, otherwise, we should load it.
        if (dirPath.IsNullOrEmpty() || presetName.IsNullOrEmpty())
            return new ModSettingsPreset(new ModPresetContainer());
        else
        {
            // if the directory path is not in the mod preset storage, then we should throw an exception.
            var container = mp.ModPresetStorage.FirstOrDefault(x => x.DirectoryPath == dirPath)
                ?? throw new Exception($"ModSettingsPreset: No container found for directory path {dirPath}" +
                $"\nCurrent Containers are: {string.Join("\n", mp.ModPresetStorage.Select(x => x.DirectoryPath))}");

            var preset = container.ModPresets.FirstOrDefault(x => x.Label == presetName)
                ?? throw new Exception($"ModSettingsPreset: No preset found for directory path {dirPath} with name {presetName}" +
                $"\nCurrent Presets are: {string.Join("\n", container.ModPresets.Select(x => x.Label))}");

            return preset;
        }
    }

    // For mod preset storage
    public static ModSettingsPreset FromToken(JToken? token, ModPresetManager mp)
    {
        if (token is not JObject jsonObject)
            throw new Exception("Invalid ModSettingsPreset data!");

        var dirPath = jsonObject["DirectoryPath"]?.Value<string>();
        // if the directory path is an empty string, then we should return a default preset, otherwise, we should load it.
        if (dirPath.IsNullOrEmpty())
            return new ModSettingsPreset(new ModPresetContainer());
        else
        {
            // if the directory path is not in the mod preset storage, then we should throw an exception.
            var container = mp.ModPresetStorage.FirstOrDefault(x => x.DirectoryPath == dirPath)
                ?? throw new Exception($"ModSettingsPreset: No container found for directory path {dirPath}" +
                $"\nCurrent Containers are: {string.Join("\n", mp.ModPresetStorage.Select(x => x.DirectoryPath))}");

            return new ModSettingsPreset(container)
            {
                Label = jsonObject["Label"]?.Value<string>() ?? string.Empty,
                ModSettings = jsonObject["ModSettings"]?.ToObject<Dictionary<string, List<string>>>() ?? new Dictionary<string, List<string>>()
            };
        }
    }
}
