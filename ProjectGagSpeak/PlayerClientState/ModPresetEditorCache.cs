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
