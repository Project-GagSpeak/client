using CkCommons;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State;
using GagSpeak.State.Caches;
using GagSpeak.Utils;
using GagspeakAPI.Data.Permissions;
using System.Runtime.InteropServices;

namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls how player movement is modified, based on <see cref="PlayerControlSource"/>'s values.
/// </summary>
public sealed class MovementController : DisposableMediatorSubscriberBase
{
    private readonly TraitsCache _traitCache;
    private readonly MovementDetours _moveDtor;

    // Fields useful for forced-follow behavior.
    private Stopwatch _timeoutTracker;
    private Vector3 _lastPos;

    // Dictates what is currently controlling the player's movement.
    private PlayerControlSource _sources;

    // Any automatic movement task that could be occuring.
    private Task? _movementTask;
    private bool _forceRunDuringTask = false;

    public MovementController(ILogger<KeystateController> logger, GagspeakMediator mediator,
        TraitsCache traitsCache, MovementDetours moveDtor)
        : base(logger, mediator)
    {
        _traitCache = traitsCache;
        _moveDtor = moveDtor;

        _timeoutTracker = new Stopwatch();
        _lastPos = Vector3.Zero;
        _sources = PlayerControlSource.None;

        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    public bool IsMoveTaskRunning => _movementTask is not null && !_movementTask.IsCompleted;
    public PlayerControlSource Sources => _sources;
    public bool BanUnfollowing => _sources.HasAny(PlayerControlSource.ForcedFollow);
    public bool BanAnyMovement => _sources.HasAny(PlayerControlSource.ForcedEmote);
    public bool BanMouseAutoMove => !_forceRunDuringTask && (_sources & (PlayerControlSource.ForcedEmote | PlayerControlSource.Immobile)) != 0;
    public bool BanRunning => !_forceRunDuringTask && (_sources & (PlayerControlSource.ForcedFollow | PlayerControlSource.Weighty)) != 0;

    private unsafe void FrameworkUpdate()
    {
        // ForceFollow Spesific Logic.
        if (BanUnfollowing && _timeoutTracker.IsRunning)
            HandleTimeoutTracking();

        // Handle Unfollow Hooks.
        if(BanUnfollowing)
            _moveDtor.EnableUnfollowHook();
        else
            _moveDtor.DisableUnfollowHook();

        // Handle Full Movement Lock
        if (BanAnyMovement)
            _moveDtor.EnableFullMovementLock();
        else
            _moveDtor.DisableFullMovementLock();

        // Handle Mouse Auto Move Hooks.
        if (BanMouseAutoMove)
            _moveDtor.EnableMouseAutoMoveHook();
        else
            _moveDtor.DisableMouseAutoMoveHook();

        // Force Walking.
        if (BanRunning && !IsWalking())
            ForceWalking();
    }

    public void AddControlSources(PlayerControlSource sources)
        => _sources |= sources;

    public void RemoveControlSources(PlayerControlSource sources)
        => _sources &= ~sources;

    public void RestartTimeoutTracker()
        => _timeoutTracker.Restart();

    public void ResetTimeoutTracker()
    {
        _timeoutTracker.Reset();
        _lastPos = Vector3.Zero;
    }

    private unsafe bool IsWalkingMarshal() => Marshal.ReadByte((nint)Control.Instance(), 30243) == 0x1;
    private unsafe bool IsWalking() => Control.Instance()->IsWalking;
    private unsafe void ForceWalking() => Marshal.WriteByte((nint)Control.Instance(), 30243, 0x1);
    private unsafe void ForceRunning() => Marshal.WriteByte((nint)Control.Instance(), 30243, 0x0);
    
    private void HandleTimeoutTracking()
    {
        if (!_timeoutTracker.IsRunning)
            return;

        if (PlayerData.Object!.Position != _lastPos)
            RestartTimeoutTracker();
        if (_timeoutTracker.Elapsed > TimeSpan.FromSeconds(6))
            HandleNaturalExpiration();
    }
    
    /// <summary>
    ///     Case where the ForceFollow timer has someone remaining in place for 6+ seconds.
    ///     Triggering a automatic disable.
    /// </summary>
    private void HandleNaturalExpiration()
    {
        // Forcibly remove the control source.
        _sources &= ~PlayerControlSource.ForcedFollow;
        ResetTimeoutTracker();
        Mediator.Publish(new PushGlobalPermChange(nameof(GlobalPerms.ForcedFollow), string.Empty));
    }
}
