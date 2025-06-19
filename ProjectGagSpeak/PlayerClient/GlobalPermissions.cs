using GagSpeak.Services.Mediator;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Util;

namespace GagSpeak.PlayerClient;

/// <summary>
///     A wrapper class for the <see cref="GlobalPerms"/> data.
///     This helps prevent circular dependancy when accessing this class.
/// </summary>
/// <remarks> Yes, I've tried moving this to a manager, the handlers included make it freak out. </remarks>
public sealed class GlobalPermissions : DisposableMediatorSubscriberBase
{
    private GlobalPerms? _currentGlobals = null;

    public GlobalPermissions(ILogger<GlobalPermissions> logger, GagspeakMediator mediator) 
        : base(logger, mediator)
    {
        Svc.ClientState.Logout += OnLogout;
    }

    public GlobalPerms? Current 
        => _currentGlobals;

    public EmoteState ForcedEmoteState => EmoteState.FromString(_currentGlobals?.ForcedEmoteState ?? string.Empty);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Logout -= OnLogout;
    }

    private void OnLogout(int type, int code)
    {
        Logger.LogInformation("Clearing Global Permissions on Logout.");
        _currentGlobals = null;
    }

    public void ApplyFullDataChange(GlobalPerms newGlobalPerms)
        => _currentGlobals = newGlobalPerms;
    
    public bool TryApplyChange(string permKey, object permValue)
    {
        // Attempt to change the property.
        if (!PropertyChanger.TrySetProperty(_currentGlobals, permKey, permValue, out var _))
        {
            Logger.LogError($"Failed to set GlobalPermission setting [{permKey}] to [{permValue}].");
            return false;
        }
        return true;
    }
}
