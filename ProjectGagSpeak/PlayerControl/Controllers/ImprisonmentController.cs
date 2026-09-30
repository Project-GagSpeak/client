using CkCommons;
using Dalamud.Interface.ImGuiNotification;
using GagSpeak.GameInternals.Detours;
using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.Services.Controller;

public class ImprisonmentController : DisposableMediatorSubscriberBase
{
    private const string ReturnToCageName = "RETURN_TO_CAGE";
    private const float ArrivalFactor = 0.75f;
    private const int StallTimeoutMs = 5000;
    private const int RetryCooldownMs = 15000;
    private const float RetryDistanceMargin = 1f;
    private const float DivergenceMargin = 3f;
    private const int ZoneCheckIntervalMs = 250;
    private const float ArrivalHeightTolerance = 2f;

    private readonly HcTaskManager _hcTasks;
    private readonly IpcCallerVnavmesh _vnav;
    private float? _gaveUpAtDistance;
    private bool _pendingZoneCheck;
    private long _nextZoneCheck;
    private long _gaveUpAtTick;
    // If the active vnavmesh return has had its path request accepted yet.
    private bool _navIssued;

    public ImprisonmentController(ILogger<ImprisonmentController> logger, GagspeakMediator mediator,
        HcTaskManager hcTasks, IpcCallerVnavmesh vnav) : base(logger, mediator)
    {
        _hcTasks = hcTasks;
        _vnav = vnav;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => OnHcCacheStateChange());
        Mediator.Subscribe<TerritoryChanged>(this, _ => _pendingZoneCheck = true);
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    public bool ShouldBeImprisoned { get; private set; } = false;
    public bool IsImprisoned { get; private set; } = false;
    public uint CageTerritoryId { get; private set; } = 0;
    public Vector3 CageOrigin { get; private set; } = Vector3.Zero;
    public float CageRadius { get; private set; } = 1f;

    /// <summary> If we are still being brought to the cage (loading or confinement travel), so not being imprisoned yet isn't a failure. </summary>
    public bool AwaitingArrival => ShouldBeImprisoned && (_pendingZoneCheck
        || _hcTasks.HasTask(PlayerCtrlHandler.ConfinementTaskName)
        || _hcTasks.HasTask(HcApproachNearestHousing.CollectionName));

    private Vector2 CageOriginXZ => new(CageOrigin.X, CageOrigin.Z);

    /// <summary> Not imprisoned here, or already inside the cage. False until the zone is re-checked. </summary>
    public bool IsCageSatisfied => !_pendingZoneCheck && (!IsImprisoned || PlayerData.DistanceTo(CageOriginXZ) <= CageRadius);

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
        // Re-check the cage once loaded into a new zone. Throttled, as the loaded check does addon lookups.
        if (_pendingZoneCheck && Environment.TickCount64 >= _nextZoneCheck)
        {
            _nextZoneCheck = Environment.TickCount64 + ZoneCheckIntervalMs;
            if (GagspeakEx.IsPlayerFullyLoaded())
            {
                _pendingZoneCheck = false;
                OnHcCacheStateChange();
            }
        }

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

        // Prefer pathing around obstacles when vnavmesh has a mesh for this zone, otherwise walk straight.
        var useNav = _vnav.IsReady();
        _navIssued = false;
        Func<bool?> walk = useNav ? () => ReturnToCageNav(origin, arrival) : () => ReturnToCage(origin, arrival);
        Action stopWalking = useNav ? _vnav.Stop : () => StaticDetours.MoveOverrides.Disable();

        // Walking breaks a locked emote, so stand first and restore it once back inside.
        _hcTasks.CreateCollection(ReturnToCageName, HcTaskConfiguration.Collection with { OnEnd = stopWalking, Flags = State.HcTaskControl.BlockMovementKeys | State.HcTaskControl.AllowMovement })
            .Add(new HardcoreTask(StandIfEmoteLocked, "StandForCage", HcTaskConfiguration.Quick))
            .Add(new HardcoreTask(walk, "WalkToCage", HcTaskConfiguration.Default with { OnEnd = stopWalking }))
            .Add(new HardcoreTask(RestoreLockedEmote, "RestoreLockedEmote"))
            .Insert();
    }

    private static bool StandIfEmoteLocked()
    {
        if (!ClientData.Hardcore.IsEnabled(HcAttribute.EmoteState) || !EmoteService.IsSittingAny(EmoteService.CurrentEmoteId(PlayerData.Address)))
            return true;

        if (!PlayerData.IsAnimationLocked && NodeThrottler.Throttle("Imprisonment.Stand", 500))
            EmoteService.ExecuteEmote(51); // 51 is the stand emote.
        return false;
    }

    private static bool RestoreLockedEmote()
        => ClientData.Hardcore is not { } hc || !hc.IsEnabled(HcAttribute.EmoteState)
        || HcCommonTaskFuncs.PerformExpectedEmote(hc.EmoteId, hc.EmoteCyclePose);

    /// <summary>
    ///   Walks back toward the cage. Returns null once we have stopped making progress, which the
    ///   task manager treats as a failure rather than grinding against a wall until the timeout.
    /// </summary>
    private bool? ReturnToCage(Vector3 origin, float arrival)
    {
        var result = StaticDetours.MoveOverrides.MoveToPointOrFail(origin, arrival, StallTimeoutMs, DivergenceMargin);
        // Succeeding subtasks don't End() in a collection, so release the overrides here.
        if (result is true)
            StaticDetours.MoveOverrides.Disable();
        if (result is not null)
            return result;

        RecordGiveUp(origin, $"stalled {StaticDetours.MoveOverrides.StalledFor}ms, diverged {StaticDetours.MoveOverrides.DivergedBy:F1} yalms");
        return null;
    }

    /// <summary>
    ///   Paths back toward the cage through vnavmesh. vnavmesh handles its own stuck detection, so
    ///   once it stops walking while we are still outside, the way back was not reachable.
    /// </summary>
    private bool? ReturnToCageNav(Vector3 origin, float arrival)
    {
        if (!PlayerData.Available)
            return false;

        var pos = PlayerData.Position;
        var inRange = new Vector2(pos.X - origin.X, pos.Z - origin.Z).LengthSquared() <= arrival * arrival;

        // Succeeding subtasks don't End() in a collection, so stop vnavmesh here.
        if (inRange && MathF.Abs(pos.Y - origin.Y) <= ArrivalHeightTolerance)
        {
            _vnav.Stop();
            return true;
        }

        // Plugin went away mid-return, end this attempt so the next one walks the straight line instead.
        if (!IpcCallerVnavmesh.APIAvailable)
            return null;

        // Keep asking until it accepts, it declines while a previous request is still being computed.
        if (!_navIssued)
        {
            _navIssued = _vnav.MoveCloseTo(origin, arrival);
            return false;
        }

        if (_vnav.IsBusy())
            return false;
        
        if (inRange)
            return true;

        RecordGiveUp(origin, "vnavmesh stopped short");
        return null;
    }

    /// <summary> Record where we stopped so we know what counts as 'they moved further out' later on. </summary>
    private void RecordGiveUp(Vector3 origin, string reason)
    {
        _gaveUpAtDistance = PlayerData.DistanceTo(CageOriginXZ);
        _gaveUpAtTick = Environment.TickCount64;
        Logger.LogWarning($"Could not reach the cage at {origin:F2} (gave up {_gaveUpAtDistance:F1} yalms out, {reason}). " +
            $"Retrying in {RetryCooldownMs / 1000}s, or sooner if you move further away.");
        Mediator.Publish(new NotificationMessage("Imprisonment", "Something is blocking the way back to your cage!", NotificationType.Warning));
    }

    /// <summary>
    ///   After a failed return we hold off re-trying until either the cooldown elapses or the
    ///   player has moved meaningfully further out than where the attempt was abandoned.
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
        _navIssued = false;

        ShouldBeImprisoned = false;
        IsImprisoned = false;
        CageTerritoryId = 0;
        CageOrigin = Vector3.Zero;
        CageRadius = 1f;
        _gaveUpAtDistance = null;
        _gaveUpAtTick = 0;
    }
}
