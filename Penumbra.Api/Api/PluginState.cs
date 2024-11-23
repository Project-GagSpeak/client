namespace Penumbra.Api.Api;

/// <summary> API methods pertaining to Penumbras own state. </summary>
public interface IPenumbraApiPluginState
{
    /// <returns> The full path of the current penumbra root directory. </returns>
    public string GetModDirectory();

    /// <returns> The entire current penumbra configuration as a json encoded string. </returns>
    public string GetConfiguration();

    /// <summary>
    /// Fired whenever a mod directory change is finished.
    /// </summary>
    /// <returns>The full path of the mod directory and whether Penumbra treats it as valid.</returns>
    public event Action<string, bool>? ModDirectoryChanged;

    /// <returns>True if Penumbra is enabled, false otherwise.</returns>
    public bool GetEnabledState();

    /// <summary>
    /// Fired whenever the enabled state of Penumbra changes.
    /// </summary>
    /// <returns>True if the new state is enabled, false if the new state is disabled</returns>
    public event Action<bool>? EnabledChange;
}
