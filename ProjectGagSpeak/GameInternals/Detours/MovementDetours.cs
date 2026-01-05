using CkCommons;
using FFXIVClientStructs.FFXIV.Client.System.Input;
using GagSpeak.State.Caches;

namespace GagSpeak.GameInternals.Detours;

public partial class MovementDetours : IDisposable
{
    private readonly InputId[] movementInputs = [ InputId.MOVE_AND_STEER, InputId.MOVE_ANGLE_DESCENT, InputId.MOVE_ANGLE_RISING,
            InputId.MOVE_LEFT, InputId.MOVE_RIGHT, InputId.MOVE_DESCENT, InputId.MOVE_RETENTION, InputId.MOVE_STRIFE_L, InputId.MOVE_STRIFE_R, InputId.MOVE_FORE, InputId.MOVE_BACK ];

    private readonly ILogger<MovementDetours> _logger;
    private readonly PlayerControlCache _cache;
    public unsafe MovementDetours(ILogger<MovementDetours> logger, PlayerControlCache cache)
    {
        _logger = logger;
        _cache = cache;
        Svc.Hook.InitializeFromAttributes(this);
        IsInputIdPressedHook.Enable();
        IsInputIdDownHook.Enable();
        IsInputIdHeldHook.Enable();
        IsInputIdUnknownHook.Enable();
        _logger.LogInformation("MovementDetours initialized successfully.");
    }

    /// <summary> Returns if all Movement is currently disabled </summary>
    /// <remarks> Useful for knowing if other plugins are tempering with this pointer. </remarks>
    public bool ForceDisableMovementIsActive => ForceDisableMovement > 0;

    public bool NoAutoMoveActive
    {
        get => AutoMoveUpdateHook.IsEnabled;
        set
        {
            if (value) AutoMoveUpdateHook.Enable();
            else AutoMoveUpdateHook.Disable();
        }
    }

    public bool NoUnfollowingActive
    {
        get => UnfollowHook.IsEnabled;
        set
        {
            if (value) UnfollowHook.Enable();
            else UnfollowHook.Disable();
        }
    }

    public bool NoMouseMovementActive
    {
        get => MoveUpdateHook.IsEnabled;
        set
        {
            if (value) MoveUpdateHook.Enable();
            else MoveUpdateHook.Disable();
        }
    }

    public void Dispose()
    {
        _logger.LogInformation($"Disposing MovementDetours");
        DisableFullMovementLock();

        AutoMoveUpdateHook.SafeDispose();
        IsInputIdPressedHook.SafeDispose();
        IsInputIdDownHook.SafeDispose();
        IsInputIdHeldHook.SafeDispose();
        IsInputIdUnknownHook.SafeDispose();
        UnfollowHook.SafeDispose();
        MoveUpdateHook.SafeDispose();

        //UNK_sub_141719E40Hook.SafeDispose();
        // UNK_sub_14171A220Hook.SafeDispose();
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
}
