using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Services.Controller;

public class ImprisonmentController : DisposableMediatorSubscriberBase
{
    private const string ReturnToCageName = "RETURN_TO_CAGE";
    private const float ArrivalFactor = 0.75f;
    private const int StallTimeoutMs = 5000;
    private const int RetryCooldownMs = 15000;
    private const float RetryDistanceMargin = 1f;
    private const float DivergenceMargin = 3f;

    private readonly HcTaskManager _hcTasks;
    private float? _gaveUpAtDistance;
    private long _gaveUpAtTick;

    public ImprisonmentController(ILogger<ImprisonmentController> logger, GagspeakMediator mediator,
        HcTaskManager hcTasks) : base(logger, mediator)
    {
        _hcTasks = hcTasks;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => OnHcCacheStateChange());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    public bool ShouldBeImprisoned { get; private set; } = false;
    public bool IsImprisoned { get; private set; } = false;
    public uint CageTerritoryId { get; private set; } = 0;
    public Vector3 CageOrigin { get; private set; } = Vector3.Zero;
    public float CageRadius { get; private set; } = 1f;

    private Vector2 CageOriginXZ => new(CageOrigin.X, CageOrigin.Z);

    private void OnHcCacheStateChange()
    {
        Logger.LogDebug("HcStateCacheChanged fired, checking imprisonment state.");
        // if clientData.Hardcore is not valid, should turn off imprisonment.
        if (ClientData.Hardcore is not { } hc)
        {
            FullStopImprisonment();
            Logger.LogDebug($"Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
            return;
        }

        ShouldBeImprisoned = hc.Imprisonment.Length > 0;

        // if disabled, disable imprisonment.
        if (hc.Imprisonment.Length is 0)
        {
            FullStopImprisonment();
            Logger.LogDebug($"Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
            return;
        }

        // stop if the territory is different.
        var currentTerritory = PlayerContent.TerritoryIdInstanced;
        if (hc.ImprisonedTerritory != currentTerritory)
        {
            _hcTasks.RemoveIfPresent(ReturnToCageName);
            IsImprisoned = false;
            Logger.LogDebug($"Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
            return;
        }

        // if we are meant to be imprisoned, but are not, assign imprisonment.
        if (hc.Imprisonment.Length > 0)
        {
            var newPos = ClientData.GetImprisonmentPos();
            // invalidate if we are too far from current position.
            if (PlayerData.DistanceTo(newPos) > 15)
            {
                _hcTasks.RemoveIfPresent(ReturnToCageName);
                IsImprisoned = false;
                Logger.LogDebug($"Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
                return;
            }
            // A re-positioned cage is a different problem, so don't hold an earlier give-up against it.
            if (newPos != CageOrigin || !hc.ImprisonedRadius.Equals(CageRadius))
                _gaveUpAtDistance = null;

            // update our imprisonment data if we have any.
            CageTerritoryId = (uint)hc.ImprisonedTerritory;
            CageOrigin = newPos;
            CageRadius = hc.ImprisonedRadius;
            IsImprisoned = true;
        }
        Logger.LogDebug($"Imprisonment State Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
    }

    private void FrameworkUpdate()
    {
        if (!IsImprisoned || !PlayerData.Available)
            return;

        // already on our way back, don't stack another task on top of it.
        if (_hcTasks.HasTask(ReturnToCageName))
            return;

        var distance = PlayerData.DistanceTo(CageOriginXZ);
        if (distance <= CageRadius)
        {
            // Back inside, so any earlier failure is no longer interesting.
            _gaveUpAtDistance = null;
            return;
        }

        if (!CanRetryReturn(distance))
            return;

        _gaveUpAtDistance = null;

        // snapshot the cage, so a task outliving FullStopImprisonment can't retarget to the world origin.
        var origin = CageOrigin;
        var arrival = CageRadius * ArrivalFactor;
        _hcTasks.InsertTask(() => ReturnToCage(origin, arrival), ReturnToCageName,
            HcTaskConfiguration.Default with { OnEnd = () => StaticDetours.MoveOverrides.Disable(), Flags = State.HcTaskControl.BlockMovementKeys });
    }

    /// <summary>
    ///     Walks back toward the cage. Returns null once we have stopped making progress, which the
    ///     task manager treats as a failure rather than grinding against a wall until the timeout.
    /// </summary>
    private bool? ReturnToCage(Vector3 origin, float arrival)
    {
        var result = StaticDetours.MoveOverrides.MoveToPointOrFail(origin, arrival, StallTimeoutMs, DivergenceMargin);
        if (result is not null)
            return result;

        // Record where we stopped so we know what counts as 'they moved further out' later on.
        _gaveUpAtDistance = PlayerData.DistanceTo(CageOriginXZ);
        _gaveUpAtTick = Environment.TickCount64;
        Logger.LogWarning($"Could not reach the cage at {origin:F2} (gave up {_gaveUpAtDistance:F1} yalms out, " +
            $"stalled {StaticDetours.MoveOverrides.StalledFor}ms, diverged {StaticDetours.MoveOverrides.DivergedBy:F1} yalms). " +
            $"Retrying in {RetryCooldownMs / 1000}s, or sooner if you move further away.");
        Mediator.Publish(new NotificationMessage("Imprisonment", "Something is blocking the way back to your cage!", NotificationType.Warning));
        return null;
    }

    /// <summary>
    ///     After a failed return we hold off re-trying until either the cooldown elapses or the
    ///     player has moved meaningfully further out than where the attempt was abandoned.
    /// </summary>
    private bool CanRetryReturn(float distance)
    {
        if (_gaveUpAtDistance is not { } gaveUpAt)
            return true;

        if (distance > gaveUpAt + RetryDistanceMargin)
            return true;

        return Environment.TickCount64 - _gaveUpAtTick >= RetryCooldownMs;
    }

    public void FullStopImprisonment()
    {
        _hcTasks.RemoveIfPresent(ReturnToCageName);
        StaticDetours.MoveOverrides.Disable();

        ShouldBeImprisoned = false;
        IsImprisoned = false;
        CageTerritoryId = 0;
        CageOrigin = Vector3.Zero;
        CageRadius = 1f;
        _gaveUpAtDistance = null;
        _gaveUpAtTick = 0;
    }
}
