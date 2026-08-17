using CkCommons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Services.Controller;

public class ImprisonmentController : DisposableMediatorSubscriberBase
{
    private const string ReturnToCageName = "RETURN_TO_CAGE";

    // Threshold for arriving at the cage. This prevents a inf loop on entering and leaving the cage.
    private const float ArrivalFactor = 0.75f;

    private readonly HcTaskManager _hcTasks;

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
            // update our imprisonment data if we have any.
            CageTerritoryId = (uint)hc.ImprisonedTerritory;
            CageOrigin = ClientData.GetImprisonmentPos();
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
        
        if (PlayerData.DistanceTo(new Vector2(CageOrigin.X, CageOrigin.Z)) <= CageRadius)
            return;

        // snapshot the cage, so a task outliving FullStopImprisonment can't retarget to the world origin.
        var origin = CageOrigin;
        var arrival = CageRadius * ArrivalFactor;
        _hcTasks.InsertTask(() => StaticDetours.MoveOverrides.MoveToPoint(origin, arrival), ReturnToCageName, HcTaskConfiguration.Default with { OnEnd = () => StaticDetours.MoveOverrides.Disable(), Flags = State.HcTaskControl.BlockMovementKeys });
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
    }
}
