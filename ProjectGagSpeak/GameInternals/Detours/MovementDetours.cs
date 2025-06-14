using Dalamud.Plugin.Services;

namespace GagSpeak.GameInternals.Detours;
public partial class MovementDetours : IDisposable
{
    private readonly ILogger<MovementDetours> _logger;
    private readonly IObjectTable _objectTable;
    public unsafe MovementDetours(ILogger<MovementDetours> logger,
        IGameInteropProvider interopProvider, IObjectTable objectTable)
    {
        _logger = logger;
        _objectTable = objectTable;
        interopProvider.InitializeFromAttributes(this);
        _logger.LogInformation("MovementDetours initialized successfully.");
    }

    /// <summary> Returns if all Movement is currently disabled </summary>
    /// <remarks> Useful for knowing if other plugins are tempering with this pointer. </remarks>
    public bool ForceDisableMovementIsActive => ForceDisableMovement > 0;
    public bool UnfollowHookActive => UnfollowHook?.IsEnabled ?? false;
    public bool MouseAutoMoveHookActive => MouseAutoMove2Hook?.IsEnabled ?? false;

    public void Dispose()
    {
        _logger.LogInformation($"Disposing MovementDetours");
        DisableFullMovementLock();
        UnfollowHook?.Disable();
        UnfollowHook?.Dispose();
        MouseAutoMove2Hook?.Disable();
        MouseAutoMove2Hook?.Dispose();
        UnfollowHook = null;
        MouseAutoMove2Hook = null;
    }

    public void EnableFullMovementLock()
    {
        // Dont do anything if another plugin has already activated this.
        if (ForceDisableMovementIsActive)
            return;

        _logger.LogTrace("Turning on ForceDisableMovement due to being in a locked state or immobile!", LoggerType.HardcoreMovement);
        ForceDisableMovement = 1;
    }

    public void DisableFullMovementLock()
    {
        // If another plugin had this to disable at the same time we called it, don't disable.
        if (!ForceDisableMovementIsActive)
            return;

        _logger.LogTrace("Turning off ForceDisableMovement as you are no longer in a locked state or immobile!", LoggerType.HardcoreMovement);
        ForceDisableMovement = 0;
    }

    public void EnableUnfollowHook()
    {
        if (UnfollowHookActive)
            return;
        UnfollowHook?.Enable();
    }

    public void DisableUnfollowHook()
    {
        if (!UnfollowHookActive)
            return;
        UnfollowHook?.Disable();
    }

    public void EnableMouseAutoMoveHook()
    {
        if (MouseAutoMoveHookActive)
            return;
        MouseAutoMove2Hook?.Enable();
    }

    public void DisableMouseAutoMoveHook()
    {
        if (!MouseAutoMoveHookActive)
            return;
        MouseAutoMove2Hook?.Disable();
    }
}
