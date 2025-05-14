using Dalamud.Utility;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Visual;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerData.Storage;

public class ModPresetStorage : List<ModPresetContainer>
{
    public bool TryGetPresetContainer(string dirPath, [NotNullWhen(true)] out ModPresetContainer? container)
        => (container = this.FirstOrDefault(x => x.DirectoryPath == dirPath)) != null;

    public ModPresetContainer? ByDirectory(string dirPath)
        => this.FirstOrDefault(x => x.DirectoryPath == dirPath);

    public bool HasDirectory(string dirPath)
        => this.Any(mpc => mpc.DirectoryPath == dirPath);
}

public class ModPresetContainer
{
    public string DirectoryPath { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public int Priority { get; set; } = -1;
    public List<ModSettingsPreset> ModPresets { get; set; } = new();

    public ModPresetContainer() { }
    public ModPresetContainer(string directoryPath, string modName, int priority)
    {
        DirectoryPath = directoryPath;
        ModName = modName;
        Priority = priority;
    }
}


/// <summary> Interface for ModSettingPresets. Used to help define the structure for the stored presets in a mod container. </summary>
public interface IModSettingPreset
{
    /// <summary> This holds a reference back to the container to retrieve essential mod information. </summary>
    public ModPresetContainer               Container   { get; }

    /// <summary> The name of the Mod Preset. </summary>
    public string                           Label       { get; set; }

    /// <summary> The selected options for this mod. </summary>
    public Dictionary<string, List<string>> ModSettings { get; set; }

}

public sealed class ModSettingsPreset : IModSettingPreset, IComparable<ModSettingsPreset>
{
    public ModPresetContainer               Container   { get; }
    public string                           Label       { get; set; } = string.Empty;
    public Dictionary<string, List<string>> ModSettings { get; set; } = new();

    public ModSettingsPreset(ModPresetContainer container)
    {
        Container = container;
    }

    public ModSettingsPreset(ModSettingsPreset other)
    {
        Container = other.Container;
        Label = other.Label;
        ModSettings = new Dictionary<string, List<string>>(other.ModSettings);
    }

    public bool HasData => !string.IsNullOrEmpty(Container.DirectoryPath) && !string.IsNullOrEmpty(Label);

    public int CompareTo(ModSettingsPreset? other)
    {
        if (other == null)
            return 1;

        return string.Compare(Container.ModName, other.Container.ModName, StringComparison.Ordinal);
    }

    public string SelectedOption(string group)
        => ModSettings.TryGetValue(group, out var selected) ? selected[0] : string.Empty;

    public string[] SelectedOptions(string group)
        => ModSettings.TryGetValue(group, out var selected) ? selected.ToArray() : Array.Empty<string>();

    /// <summary> To be used by the ModSettingPresetManager for serialization. </summary>

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
    public static ModSettingsPreset FromRefToken(JToken? token, ModSettingPresetManager mp)
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
    public static ModSettingsPreset FromToken(JToken? token, ModSettingPresetManager mp)
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
