using CkCommons;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Interop.Helpers;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;
using System.Runtime.InteropServices;

namespace GagSpeak.Services.Controller;

public sealed class MovementController : DisposableMediatorSubscriberBase
{
    private const int CONTROL_WALKING_OFFSET_NORMAL = 30259; // Applies when not automoving
    private const int CONTROL_WALKING_OFFSET_AUTOMOVE = 29976; // Applies when automoving

    private readonly record struct MoveState(bool MustWalk, bool WasWalking);

    private readonly PlayerControlCache _cache;
    private readonly HcTaskManager _hcTasks;
    private readonly MovementDetours _detours;

    // Fields useful for forced-follow behavior.
    private static Stopwatch _timeoutTracker = new Stopwatch();
    private Vector3 _lastPos = Vector3.Zero;
    private MoveState _moveState;
    private bool _freezePlayer = false;
    public MovementController(ILogger<MovementController> logger, GagspeakMediator mediator,
        PlayerControlCache cache, HcTaskManager hcTasks, MovementDetours detours)
        : base(logger, mediator)
    {
        _cache = cache;
        _hcTasks = hcTasks;
        _detours = detours;
        _timeoutTracker.Stop();
        Mediator.Subscribe<TerritoryChanged>(this, _ => EnsureConfinement());
        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
        Svc.Condition.ConditionChange += OnConditionChange;
    }

    public new void Dispose()
    {
        Svc.Condition.ConditionChange -= OnConditionChange;
        base.Dispose();
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag == ConditionFlag.Mounted)
            UpdateHardcoreStatus();
    }

    // reference for Auto-Unlock timer.
    public static TimeSpan TimeIdleDuringFollow => _timeoutTracker.Elapsed;

    private void UpdateHardcoreStatus()
    {
        // if our states to have an unfollow hook are met, but it isnt active, activate it.
        if (_cache.PreventUnfollowing && !_detours.NoUnfollowingActive)
        {
            _detours.NoUnfollowingActive = true;
            Logger.LogInformation("Activating Unfollow prevention due to hardcore status.", LoggerType.HardcoreMovement);
        }
        //if our states to have an unfollow hook are not met and it is active, disable it.
        else if (!_cache.PreventUnfollowing && _detours.NoUnfollowingActive)
        {
            _detours.NoUnfollowingActive = false;
            Logger.LogInformation("Deactivating Unfollow prevention due to change in hardcore status.", LoggerType.HardcoreMovement);
        }

        // if we were not set to require walking, but should be walking, enforce it.
        if (_cache.BlockRunning && !_moveState.MustWalk)
        {
            _moveState = new MoveState(true, IsWalking());
            Logger.LogInformation("Enforcing walking due to hardcore status.", LoggerType.HardcoreMovement);
        }
        // if there is no need to ban running, but we are forced to walk, revert it, along with the state.
        else if (!_cache.BlockRunning && _moveState.MustWalk)
        {
            Logger.LogInformation("Releasing walking restriction due to change in hardcore status.", LoggerType.HardcoreMovement);
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
            Logger.LogInformation("Freezing player due to hardcore status.", LoggerType.HardcoreMovement);
            _freezePlayer = true;
        }
        // If the player should not be immobilized, but the local value does match, update it!
        else if (!_cache.FreezePlayer && _freezePlayer)
        {
            Logger.LogInformation("Unfreezing player due to change in hardcore status.", LoggerType.HardcoreMovement);
            _freezePlayer = false;
            _detours.DisableFullMovementLock();
        }

        var shouldBlock = _cache.BlockMovementKeys || _cache.InLifestreamTask;
        var areBlocksActive = _detours.NoAutoMoveActive && _detours.NoMouseMovementActive;
        if (shouldBlock && !areBlocksActive)
        {
            Logger.LogInformation("Activating movement key blocking due to hardcore status.", LoggerType.HardcoreMovement);
            _detours.NoAutoMoveActive = true;
            _detours.NoMouseMovementActive = true;
        }
        else if (!shouldBlock && areBlocksActive)
        {
            Logger.LogInformation("Deactivating movement key blocking due to change in hardcore status.", LoggerType.HardcoreMovement);
            _detours.NoAutoMoveActive = false;
            _detours.NoMouseMovementActive = false;
        }
    }

    private async void EnsureConfinement()
    {
        // Occurs whenever we change zones.
        if (ClientData.Hardcore is not { } hcState)
        {
            Logger.LogInformation("Not ensuring confinement due to null hardcore state.", LoggerType.HardcoreMovement);
            return;
        }
        if (!hcState.IsEnabled(HcAttribute.Confinement))
        {
            Logger.LogInformation("Not ensuring confinement due to confinement hardcore status being disabled.", LoggerType.HardcoreMovement);
            return;
        }

        // Wait for us to load in.
        await GagspeakEx.WaitForPlayerLoading().ConfigureAwait(false);

        // By reaching this point, we have addressed we're in confinement,
        // meaning we are already on the way to our destination.
        // If we can identify the nearest node from this point, we almost
        // garentee it is our destination.
        Logger.LogInformation("Ensuring confinement by checking if we can approach nearest housing node.", LoggerType.HardcoreMovement);
        if (HcApproachNearestHousing.TargetNearestHousingNode())
        {
            // We could, so we should enqueue the task to re-enter.
            Logger.LogInformation("Enqueuing approach nearest housing task due to confinement hardcore status.", LoggerType.HardcoreMovement);
            var addr = AddressBookEntry.FromHardcoreStatus(hcState);
            var roomNum = addr is not null && addr.PropertyType is PropertyType.Apartment ? addr.Apartment : int.MaxValue;
            _hcTasks.EnqueueOperation(HcApproachNearestHousing.GetTaskCollection(_hcTasks, roomNum));
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
    private unsafe void ForceWalking()
    {
        Marshal.WriteByte((nint)Control.Instance(), CONTROL_WALKING_OFFSET_NORMAL, 0x1);
        Marshal.WriteByte((nint)Control.Instance(), CONTROL_WALKING_OFFSET_AUTOMOVE, 0x1);
    }
    private unsafe void ForceRunning()
    {
        Marshal.WriteByte((nint)Control.Instance(), CONTROL_WALKING_OFFSET_NORMAL, 0x0);
        Marshal.WriteByte((nint)Control.Instance(), CONTROL_WALKING_OFFSET_AUTOMOVE, 0x0);
    }
}
