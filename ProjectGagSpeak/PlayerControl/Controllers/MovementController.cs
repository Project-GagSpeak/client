using CkCommons;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagspeakAPI.Attributes;
using System.Runtime.InteropServices;

namespace GagSpeak.Services.Controller;

public sealed class MovementController : DisposableMediatorSubscriberBase
{
    private readonly PlayerControlCache _cache;
    private readonly MovementDetours _detours;

    // Fields useful for forced-follow behavior.
    private static Stopwatch _timeoutTracker = new Stopwatch();
    private Vector3 _lastPos = Vector3.Zero;
    private bool _runningBanned = false;
    private bool _freezePlayer = false;
    public MovementController(ILogger<KeystateController> logger, GagspeakMediator mediator,
        PlayerControlCache cache, MovementDetours detours)
        : base(logger, mediator)
    {
        _cache = cache;
        _detours = detours;
        _timeoutTracker.Stop();
        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreState());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    // reference for Auto-Unlock timer.
    public static TimeSpan TimeIdleDuringFollow => _timeoutTracker.Elapsed;

    private void UpdateHardcoreState()
    {
        // if our states to have an unfollow hook are met, but it isnt active, activate it.
        if (_cache.PreventUnfollowing && !_detours.UnfollowHookActive)
            MovementDetours.UnfollowHook.SafeEnable();
        // if our states to have an unfollow hook are not met and it is active, disable it.
        else if (!_cache.PreventUnfollowing && _detours.UnfollowHookActive)
            MovementDetours.UnfollowHook.SafeDisable();

        // if running should be banned, but it is not, update it.
        if (_cache.BlockRunning && !_runningBanned)
            _runningBanned = true;
        // if running should not be banned, but it is, update it.
        else if (!_cache.BlockRunning && _runningBanned)
            _runningBanned = false;

        // If the player should be immobilized, but the local value does not match, update it!
        // *(note that we don't update the pointer value because other plugins like cammy change this,
        // * so we must update it every frame)
        if (_cache.BlockAnyMovement && !_freezePlayer)
            _freezePlayer = true;
        // If the player should not be immobilized, but the local value does match, update it!
        else if (!_cache.BlockAnyMovement && _freezePlayer)
        {
            _freezePlayer = false;
            _detours.DisableFullMovementLock();
        }

        // if we should ban mouse movement, but it is not active, activate it.
        if (_cache.BlockMovementKeys && !_detours.MouseAutoMoveHookActive)
            MovementDetours.MoveUpdateHook.SafeEnable();
        // if we should not ban mouse movement, but it is active, disable it.
        else if (!_cache.BlockMovementKeys && _detours.MouseAutoMoveHookActive)
            MovementDetours.MoveUpdateHook.SafeDisable();
    }

    private unsafe void FrameworkUpdate()
    {
        // ForceFollow Specific Logic.
        if (_timeoutTracker.IsRunning && PlayerData.Object!.Position != _lastPos)
        {
            RestartTimeoutTracker();
            _lastPos = PlayerData.Object!.Position;
        }

        // Ensure full movement lock if we should.
        if (_freezePlayer && !_detours.ForceDisableMovementIsActive)
            _detours.EnableFullMovementLock();

        // Enforce walking if running is banned.
        if (_runningBanned && !IsWalking())
            ForceWalking();
    }

    public void RestartTimeoutTracker()
    {
        Logger.LogTrace("Restarting timeout tracker for forced-follow behavior.", LoggerType.HardcoreMovement);
        _timeoutTracker.Restart();
    }

    public void ResetTimeoutTracker()
    {
        _timeoutTracker.Reset();
        _lastPos = Vector3.Zero;
    }

    // Direct marshal byte manipulation for walking state
    // (because the control access wont read you the right values apparently?)
    private unsafe bool IsWalkingMarshal() => Marshal.ReadByte((nint)Control.Instance(), 30259) == 0x1;
    private unsafe bool IsWalking() => Control.Instance()->IsWalking;
    private unsafe void ForceWalking() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x1);
    private unsafe void ForceRunning() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x0);
}
