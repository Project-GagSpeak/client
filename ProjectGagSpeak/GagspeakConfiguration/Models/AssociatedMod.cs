
using GagSpeak.Interop.Ipc;
using Newtonsoft.Json.Linq;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public record AssociatedMod
{
    /// <summary> The mod Name and Mod Directory </summary>
    public Mod Mod { get; set; } = new();

    /// <summary> The settings for the associated mod </summary>
    public ModSettings ModSettings { get; set; } = new();

    /// <summary> Whether the mod should be disabled when the set is inactive </summary>
    public bool DisableWhenInactive { get; set; } = false;

    /// <summary> Whether the mod should be redrawn after toggling </summary>
    public bool RedrawAfterToggle { get; set; } = false;

    /// <summary> If it should update display in drawtableUpdateDisplayData </summary>
    public JObject Serialize()
    {
        return new JObject
        {
            ["Mod"] = new JObject
            {
                ["Name"] = Mod.Name,
                ["DirectoryName"] = Mod.DirectoryName,
            },
            ["ModSettings"] = new JObject
            {
                ["Enabled"] = ModSettings.Enabled,
                ["Settings"] = JObject.FromObject(ModSettings.Settings), // Assuming Settings is a suitable type for FromObject
                ["Priority"] = ModSettings.Priority,
            },
            ["DisableWhenInactive"] = DisableWhenInactive,
            ["RedrawAfterToggle"] = RedrawAfterToggle,
        };
    }
}
