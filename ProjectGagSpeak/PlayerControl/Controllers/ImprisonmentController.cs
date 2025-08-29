using CkCommons;
using GagSpeak.GameInternals.Detours;
using GagSpeak.GameInternals.Structs;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerControl;
using GagSpeak.Services.Mediator;

namespace GagSpeak.Services.Controller;
public class ImprisonmentController : DisposableMediatorSubscriberBase
{
    private const string ReturnToCageName = "RETURN_TO_CAGE";
    private readonly HcTaskManager _hcTasks;

    private bool _returningToCage => StaticDetours.MoveOverrides.InMoveTask;
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
            _hcTasks.RemoveIfPresent("MoveToPoint");
            IsImprisoned = false;
            Logger.LogDebug($"Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
            return;
        }

        // if we are meant to be imprisoned, but are not, assign imprisonment.
        if (hc.Imprisonment.Length > 0)
        {
            var newPos = ClientData.GetImprisonmentPos();
            // invalidate if we are too far from current position.
            if (PlayerData.DistanceToInstanced(newPos) > 15)
            {
                _hcTasks.RemoveIfPresent("MoveToPoint");
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
        if (!IsImprisoned)
            return;

        // if we are not in a return task, check if we are further from the allowed area.
        if (!_returningToCage && PlayerData.Available)
        {
            // if the distance is larger than the cage radius, begin returning to cage.
            if (PlayerData.DistanceTo(CageOrigin) > CageRadius)
                _hcTasks.InsertTask(() => StaticDetours.MoveOverrides.MoveToPoint(CageOrigin, CageRadius), ReturnToCageName, HcTaskConfiguration.Default with { OnEnd = () => StaticDetours.MoveOverrides.Disable() });
        }
    }

    public void FullStopImprisonment()
    {
        ShouldBeImprisoned = false;
        IsImprisoned = false;
        CageTerritoryId = 0;
        CageOrigin = Vector3.Zero;
        CageRadius = 1f;
        _hcTasks.RemoveIfPresent("MoveToPoint");
    }
}
