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

// TODO: Remove sources as control points, use other blocking methods.
public sealed class MovementController : DisposableMediatorSubscriberBase
{
    private readonly TraitsCache _traitCache;
    private readonly MovementDetours _moveDtor;

    // Fields useful for forced-follow behavior.
    private Stopwatch _timeoutTracker;
    private Vector3 _lastPos;

    // Dictates what is currently controlling the player's movement.
    private PlayerControlSource _sources;

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

    public PlayerControlSource Sources => _sources;
    public bool BanUnfollowing => _sources.HasAny(PlayerControlSource.LockedFollowing);
    public bool BanAnyMovement => _sources.HasAny(PlayerControlSource.LockedEmote);
    public bool BanMouseAutoMove => (_sources & (PlayerControlSource.LockedEmote | PlayerControlSource.Immobile)) != 0;
    public bool BanRunning => (_sources & (PlayerControlSource.LockedFollowing | PlayerControlSource.Weighty)) != 0;

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

    private unsafe bool IsWalkingMarshal() => Marshal.ReadByte((nint)Control.Instance(), 30259) == 0x1;
    private unsafe bool IsWalking() => Control.Instance()->IsWalking;
    private unsafe void ForceWalking() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x1);
    private unsafe void ForceRunning() => Marshal.WriteByte((nint)Control.Instance(), 30259, 0x0);
    
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
        _sources &= ~PlayerControlSource.LockedFollowing;
        ResetTimeoutTracker();
        Mediator.Publish(new PushGlobalPermChange(nameof(GlobalPerms.LockedFollowing), string.Empty));
    }
}
