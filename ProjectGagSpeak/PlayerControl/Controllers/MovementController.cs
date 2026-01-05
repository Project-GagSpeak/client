using CkCommons;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using System.Runtime.InteropServices;

namespace GagSpeak.Services.Controller;

public sealed class MovementController : DisposableMediatorSubscriberBase
{
    private readonly record struct MoveState(bool MustWalk, bool WasWalking);

    private readonly PlayerControlCache _cache;
    private readonly MovementDetours _detours;

    // Fields useful for forced-follow behavior.
    private static Stopwatch _timeoutTracker = new Stopwatch();
    private Vector3 _lastPos = Vector3.Zero;
    private MoveState _moveState;
    private bool _freezePlayer = false;
    public MovementController(ILogger<KeystateController> logger, GagspeakMediator mediator,
        PlayerControlCache cache, MovementDetours detours)
        : base(logger, mediator)
    {
        _cache = cache;
        _detours = detours;
        _timeoutTracker.Stop();
        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    // reference for Auto-Unlock timer.
    public static TimeSpan TimeIdleDuringFollow => _timeoutTracker.Elapsed;

    private void UpdateHardcoreStatus()
    {
        // if our states to have an unfollow hook are met, but it isnt active, activate it.
        if (_cache.PreventUnfollowing && !_detours.NoUnfollowingActive)
            _detours.NoUnfollowingActive = true;
        //if our states to have an unfollow hook are not met and it is active, disable it.
        else if (!_cache.PreventUnfollowing && _detours.NoUnfollowingActive)
            _detours.NoUnfollowingActive = false;

        // if we were not set to require walking, but should be walking, enforce it.
        if (_cache.BlockRunning && !_moveState.MustWalk)
            _moveState = new MoveState(true, IsWalking());
        // if there is no need to ban running, but we are forced to walk, revert it, along with the state.
        else if (!_cache.BlockRunning && _moveState.MustWalk)
        {
            // restore the state only if we were running before.
            if (!_moveState.WasWalking) 
                ForceRunning();
            // reset movestate.
            _moveState = new MoveState(false, false);
        }

        // If the player should be immobilized, but the local value does not match, update it!
        // *(note that we don't update the pointer value because other plugins like cammy change this,
        // * so we must update it every frame)
        if (_cache.FreezePlayer && !_freezePlayer)
        {
            Svc.Logger.Warning("Freeze Player was true!");
            _freezePlayer = true;
        }
        // If the player should not be immobilized, but the local value does match, update it!
        else if (!_cache.FreezePlayer && _freezePlayer)
        {
            _freezePlayer = false;
            _detours.DisableFullMovementLock();
        }

        var shouldBlock = _cache.BlockMovementKeys || _cache.InLifestreamTask;
        var areBlocksActive = _detours.NoAutoMoveActive && _detours.NoMouseMovementActive;
        if (shouldBlock && !areBlocksActive)
        {
            _detours.NoAutoMoveActive = true;
            _detours.NoMouseMovementActive = true;
        }
        else if (!shouldBlock && areBlocksActive)
        {
            _detours.NoAutoMoveActive = false;
            _detours.NoMouseMovementActive = false;
        }
    }

    private unsafe void FrameworkUpdate()
    {
        // ForceFollow Specific Logic.
        if (_timeoutTracker.IsRunning && PlayerData.Position != _lastPos)
        {
            RestartTimeoutTracker();
            _lastPos = PlayerData.Position;
        }

        // we need to do the following because other plugins can share this pointer control (Cammy)
        if (_freezePlayer && !_detours.ForceDisableMovementIsActive)
            _detours.EnableFullMovementLock();

        var isWalking = IsWalking();

        // If we are following someone and too far away we should catch up to them.
        if (_timeoutTracker.IsRunning && PlayerData.DistanceTo(Svc.Targets.Target) > 8f)
        {
            if (isWalking && NodeThrottler.Throttle("MoveController.RunToggle", 500))
                ForceRunning();
            return;
        }

        // Enforce walking if running is banned.
        if (_moveState.MustWalk && !isWalking)
            ForceWalking();
    }

    public void RestartTimeoutTracker()
        => _timeoutTracker.Restart();

    public void ResetTimeoutTracker()
    {
        _timeoutTracker.Reset();
        _lastPos = Vector3.Zero;
    }

    // Direct marshal byte manipulation for walking state
    // (because the control access wont read you the right values apparently?)
    // private unsafe bool IsWalkingMarshal() => Marshal.ReadByte((nint)Control.Instance(), 30259) == 0x1;
    private unsafe bool IsWalking() => Control.Instance()->IsWalking;
    private unsafe void ForceWalking() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x1);
    private unsafe void ForceRunning() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x0);
}
