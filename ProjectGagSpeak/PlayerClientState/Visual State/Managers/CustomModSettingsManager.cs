using Dalamud.Plugin;
using GagSpeak.Interop.Ipc;
using Penumbra.Api.IpcSubscribers;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Responsible for tracking the custom settings we have configured for a mod. </summary>
public class CustomModSettingsManager
{
    private readonly ILogger<CustomModSettingsManager> _logger;
    private readonly IpcCallerPenumbra _penumbra;

    public CustomModSettingsManager(ILogger<CustomModSettingsManager> logger, 
        IpcCallerPenumbra penumbra, IDalamudPluginInterface pi)
    {
        _logger = logger;
        _penumbra = penumbra;

        _penumbra.OnModMoved = ModMoved.Subscriber(pi, OnModInfoChanged);
    }

    /// <summary> Fired whenever a mod is moved. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        _logger.LogDebug($"A Mod moved from {oldPath} to {newPath}. Updating Storage.");
    }
}


