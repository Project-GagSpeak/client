using GagSpeak.Interop;
using GagSpeak.State.Caches;
using GagspeakAPI.Data.Struct;

namespace GagSpeak.State.Handlers;

public class CustomizePlusHandler
{
    private readonly ILogger<CustomizePlusHandler> _logger;
    private readonly CustomizePlusCache _cache;
    private readonly IpcCallerCustomize _ipc;

    public CustomizePlusHandler(ILogger<CustomizePlusHandler> logger,
        CustomizePlusCache cache, IpcCallerCustomize ipc)
    {
        _logger = logger;
        _cache = cache;
        _ipc = ipc;
    }

    private CustomizeProfile FinalProfile => _cache.FinalProfile;

    public void EnsureRestrictedProfile()
    {
        if (!IpcCallerCustomize.APIAvailable)
            return;

        if (FinalProfile.Equals(CustomizeProfile.Empty))
            return;

        // Grab the active profile
        var active = _ipc.CurrentActiveProfile();

        // Ensure the item is staying enabled.
        if (active.ProfileGuid != FinalProfile.ProfileGuid)
        {
            _logger.LogTrace($"C+ Profile [{FinalProfile.ProfileName}] found in Cache! Reapplying to enforce helplessness!", LoggerType.IpcCustomize);
            _ipc.SetProfileEnable(FinalProfile.ProfileGuid);
        }
    }

    public Task ApplyProfileCache()
    {
        if (FinalProfile.Equals(CustomizeProfile.Empty))
        {
            _logger.LogWarning("No C+ Profile found in Cache! Cannot apply profile cache.", LoggerType.IpcCustomize);
            if (_ipc.CurrentActiveProfile() is { } activeProfile && activeProfile.ProfileGuid != Guid.Empty)
                _ipc.SetProfileDisable(activeProfile.ProfileGuid);
            return Task.CompletedTask;
        }
        else
        {
            _logger.LogDebug($"Applying C+ Cache ([{FinalProfile.ProfileName} - Priority {FinalProfile.Priority}]", LoggerType.IpcGlamourer);
            EnsureRestrictedProfile();
            return Task.CompletedTask;
        }
    }
}
