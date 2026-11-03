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
    // Offset reference: FFXIVClientStructs.FFXIV.Client.Game.Control
    private const int CONTROL_WALKING_OFFSET_NORMAL = 0x7637; // Applies when not automoving
    private const int CONTROL_WALKING_OFFSET_AUTOMOVE = 0x7518; // Applies when automoving

    // Navmesh follow and leash pull back.
    private const float FollowDistance = 2f;
    private const float RepathDistance = 1f; // re-path once the goal has moved this far.
    private const int RepathIntervalMs = 500;
    private const float LeashReleaseFactor = 0.75f; // pull back to this fraction of the range before letting go.

    private readonly record struct MoveState(bool MustWalk, bool WasWalking);

    private readonly PlayerControlCache _cache;
    private readonly HcTaskManager _hcTasks;
    private readonly MovementDetours _detours;
    private readonly HousingNavmesh _navmesh;

    // Fields useful for forced-follow behavior.
    private static Stopwatch _timeoutTracker = new Stopwatch();
    private Vector3 _lastPos = Vector3.Zero;
    private MoveState _moveState;
    private bool _freezePlayer = false;

    // Navmesh follow and leash state.
    private bool _navFollowing; // the navmesh, not /follow, drives the follow.
    private List<Vector3>? _followPath; // null while standing.
    private Vector3 _followGoal;
    private long _nextRepath;
    private bool _debugFollow;
    private Func<Vector3?>? _leashLeader;
    private Leash _leash;

    public MovementController(ILogger<MovementController> logger, GagspeakMediator mediator,
        PlayerControlCache cache, HcTaskManager hcTasks, MovementDetours detours, HousingNavmesh navmesh)
        : base(logger, mediator)
    {
        _cache = cache;
        _hcTasks = hcTasks;
        _detours = detours;
        _navmesh = navmesh;
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

    /// <summary> If forced follow should path over the navmesh instead of using /follow. </summary>
    public bool UseNavFollow => _navmesh.Ready;

    // reference for Auto-Unlock timer.
    public static TimeSpan TimeIdleDuringFollow => _timeoutTracker.Elapsed;

    private void UpdateHardcoreStatus()
    {
        // if our states to have an unfollow hook are met, but it isnt active, activate it.
        if (_cache.PreventUnfollowing && !_detours.NoUnfollowingActive)
        {
            _detours.NoUnfollowingActive = true;
            Logger.LogInformation("Activating Unfollow prevention due to hardcore status.", LogFilter.HardcoreMovement);
        }
        //if our states to have an unfollow hook are not met and it is active, disable it.
        else if (!_cache.PreventUnfollowing && _detours.NoUnfollowingActive)
        {
            _detours.NoUnfollowingActive = false;
            Logger.LogInformation("Deactivating Unfollow prevention due to change in hardcore status.", LogFilter.HardcoreMovement);
        }

        // if we were not set to require walking, but should be walking, enforce it.
        if (_cache.BlockRunning && !_moveState.MustWalk)
        {
            _moveState = new MoveState(true, IsWalking());
            Logger.LogInformation("Enforcing walking due to hardcore status.", LogFilter.HardcoreMovement);
        }
        // if there is no need to ban running, but we are forced to walk, revert it, along with the state.
        else if (!_cache.BlockRunning && _moveState.MustWalk)
        {
            Logger.LogInformation("Releasing walking restriction due to change in hardcore status.", LogFilter.HardcoreMovement);
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
            Logger.LogInformation("Freezing player due to hardcore status.", LogFilter.HardcoreMovement);
            _freezePlayer = true;
        }
        // If the player should not be immobilized, but the local value does match, update it!
        else if (!_cache.FreezePlayer && _freezePlayer)
        {
            Logger.LogInformation("Unfreezing player due to change in hardcore status.", LogFilter.HardcoreMovement);
            _freezePlayer = false;
            _detours.DisableFullMovementLock();
        }

        var shouldBlock = _cache.BlockMovementKeys || _cache.InLifestreamTask;
        var areBlocksActive = _detours.NoAutoMoveActive && _detours.NoMouseMovementActive;
        if (shouldBlock && !areBlocksActive)
        {
            Logger.LogInformation("Activating movement key blocking due to hardcore status.", LogFilter.HardcoreMovement);
            _detours.NoAutoMoveActive = true;
            _detours.NoMouseMovementActive = true;
        }
        else if (!shouldBlock && areBlocksActive)
        {
            Logger.LogInformation("Deactivating movement key blocking due to change in hardcore status.", LogFilter.HardcoreMovement);
            _detours.NoAutoMoveActive = false;
            _detours.NoMouseMovementActive = false;
        }
    }

    private async void EnsureConfinement()
    {
        // Occurs whenever we change zones.
        if (ClientData.Hardcore is not { } hcState)
        {
            Logger.LogInformation("Not ensuring confinement due to null hardcore state.", LogFilter.HardcoreMovement);
            return;
        }
        if (!hcState.IsEnabled(HcAttribute.Confinement))
        {
            Logger.LogInformation("Not ensuring confinement due to confinement hardcore status being disabled.", LogFilter.HardcoreMovement);
            return;
        }

        // Wait for us to load in.
        await GagspeakEx.WaitForPlayerLoading().ConfigureAwait(false);

        // By reaching this point, we have addressed we're in confinement,
        // meaning we are already on the way to our destination.
        // If we can identify the nearest node from this point, we almost
        // garentee it is our destination.
        Logger.LogInformation("Ensuring confinement by checking if we can approach nearest housing node.", LogFilter.HardcoreMovement);
        if (HcApproachNearestHousing.TargetNearestHousingNode())
        {
            // We could, so we should enqueue the task to re-enter.
            Logger.LogInformation("Enqueuing approach nearest housing task due to confinement hardcore status.", LogFilter.HardcoreMovement);
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

        if (_timeoutTracker.IsRunning || _debugFollow)
            UpdateNavFollow();
        else if (_leashLeader is not null)
            UpdateLeash();

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

    /// <summary>
    ///   Keeps within <see cref="FollowDistance"/> of the target over the housing navmesh. Hands back to /follow
    ///   once the mesh is gone.
    /// </summary>
    private void UpdateNavFollow()
    {
        if (!_navmesh.Ready)
        {
            if (_navFollowing)
            {
                StopWalking();
                // /follow is throttled, so retry until it goes out.
                _navFollowing = HcTaskUtils.FollowTarget() is not true;
            }
            return;
        }

        _navFollowing = true;
        if (!PlayerData.Available || Svc.Targets.Target is not { } target)
            return;

        if (PlayerData.DistanceTo(target.Position) <= FollowDistance)
            StopWalking();
        else
            WalkToward(target.Position, FollowDistance, false);
    }

    /// <summary>
    ///   Walks toward <paramref name="goal"/>, re-pathing as it moves, until within <paramref name="proximity"/>.
    ///   Without a navmesh path, walks straight there only if <paramref name="straightFallback"/>.
    /// </summary>
    private void WalkToward(Vector3 goal, float proximity, bool straightFallback)
    {
        var now = Environment.TickCount64;
        if (now >= _nextRepath && (_followPath is null || Vector3.Distance(goal, _followGoal) > RepathDistance))
        {
            _nextRepath = now + RepathIntervalMs;
            _followGoal = goal;
            _followPath = _navmesh.FindPath(PlayerData.Position, goal) ?? (straightFallback ? [goal] : null);
        }

        if (_followPath is not null && StaticDetours.MoveOverrides.FollowPath(_followPath, proximity, 0) is true)
            StopWalking();
    }

    private void StopWalking()
    {
        if (_followPath is null)
            return;
        StaticDetours.MoveOverrides.Disable();
        _followPath = null;
    }

    public bool Leashed => _leashLeader is not null;

    /// <summary> <paramref name="leader"/> gives the leader's position, or null while they are not around. </summary>
    public void StartLeash(Leash leash, Func<Vector3?> leader)
    {
        _leash = leash;
        _leashLeader = leader;
    }

    public void StopLeash()
    {
        _leashLeader = null;
        Leash.DebugActive = false;
        StopWalking();
        _nextRepath = 0;
    }

    /// <summary> Walks us back toward the leader once past the leash range. Rests while riding pillion. </summary>
    private void UpdateLeash()
    {
        if (_leashLeader?.Invoke() is not { } leaderPos || !PlayerData.Available || Svc.Condition[ConditionFlag.RidingPillion])
        {
            StopWalking();
            return;
        }

        // Start pulling past the range, stop once well inside it.
        var distance = PlayerData.DistanceTo(leaderPos);
        var release = _leash.Range * LeashReleaseFactor;
        if (distance <= (_followPath is null ? _leash.Range : release))
            StopWalking();
        else
            WalkToward(leaderPos, release, true);
    }

    /// <summary> Debug: navmesh-follow the current target without Locked Follow, to test the follow solo. </summary>
    public bool DebugFollow
    {
        get => _debugFollow;
        set
        {
            _debugFollow = value;
            if (!value)
            {
                StopWalking();
                _navFollowing = false;
            }
        }
    }

    public void RestartTimeoutTracker()
        => _timeoutTracker.Restart();

    public void ResetTimeoutTracker()
    {
        _timeoutTracker.Reset();
        _lastPos = Vector3.Zero;
        StopWalking();
        _navFollowing = false;
        _nextRepath = 0;
    }

    [Obsolete("This is retained for informational purposes, use IsWalking.")]
    private unsafe bool IsWalkingMarshal() => Marshal.ReadByte((nint)Control.Instance(), CONTROL_WALKING_OFFSET_NORMAL) == 0x1;
    private unsafe bool IsWalking() => Control.Instance()->IsWalking;
    private unsafe bool IsWalkingDuringAutorun() => Control.Instance()->IsWalkingDuringAutorun;

    private unsafe void ForceWalking()
    {
        // get the control instance
        var inst = Control.Instance();

        // set the character's walk state to walking.
        inst->IsWalking = true;
        inst->IsWalkingDuringAutorun = true;
    }
    private unsafe void ForceRunning()
    {
        // get the control instance
        var inst = Control.Instance();

        // set the character's walk state to running.
        inst->IsWalking = false;
        inst->IsWalkingDuringAutorun = false;
    }
}
