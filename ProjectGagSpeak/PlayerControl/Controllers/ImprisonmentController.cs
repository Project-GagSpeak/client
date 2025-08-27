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

    // Temporary overrides.
    private CameraOverride _camera = new();
    private MovementOverride _movement = new();

    private bool _returningToCage = false; // could also be _hcTask.ContainsTask(ReturnToCageName) but whatever.
    public ImprisonmentController(ILogger<ImprisonmentController> logger, GagspeakMediator mediator, 
        HcTaskManager hcTasks) : base(logger, mediator)
    {
        _hcTasks = hcTasks;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => OnHcCacheStateChange());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _camera.Dispose();
        _movement.Dispose();
    }

    public bool ShouldBeImprisoned { get; private set; } = false;
    public bool IsImprisoned { get; private set; } = false;
    public uint CageTerritoryId { get; private set; } = 0;
    public Vector3 CageOrigin { get; private set; } = Vector3.Zero;
    public float CageRadius { get; private set; } = 1f;

    private void OnHcCacheStateChange()
    {
        Logger.LogInformation("HcStateCacheChanged fired, checking imprisonment state.");
        // if clientData.Hardcore is not valid, should turn off imprisonment.
        if (ClientData.Hardcore is not { } hc)
        {
            StopDetoursAndControl();
            return;
        }
        // if disabled, disable imprisonment.
        if (hc.Imprisonment.Length is 0)
        {
            FullStopImprisonment();
            return;
        }

        // stop if the territory is different.
        var currentTerritory = PlayerContent.TerritoryIdInstanced;
        if (hc.ImprisonedTerritory != currentTerritory)
        {
            StopDetoursAndControl();
            return;
        }

        // if we are meant to be imprisoned, but are not, assign imprisonment.
        if (hc.Imprisonment.Length > 0)
        {
            // Always process ShouldBeImprisoned regardless.
            ShouldBeImprisoned = true;

            var newPos = ClientData.GetImprisonmentPos();
            // invalidate if we are too far from current position.
            if (PlayerData.DistanceTo(newPos) > 15)
            {
                StopDetoursAndControl();
                return;
            }
            // update our imprisonment data if we have any.
            CageTerritoryId = (uint)hc.ImprisonedTerritory;
            CageOrigin = ClientData.GetImprisonmentPos();
            CageRadius = hc.ImprisonedRadius;
            IsImprisoned = true;
        }
        Logger.LogInformation($"Imprisonment State Updated: IsImprisoned={IsImprisoned}, CageTerritoryId={CageTerritoryId}, CageOrigin={CageOrigin}, CageRadius={CageRadius}");
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
            {
                _returningToCage = true;
                EnqueueCageReturnTask();
            }
        }
    }

    private void StopDetoursAndControl()
    {
        _hcTasks.RemoveIfPresent(ReturnToCageName);
        _returningToCage = false;
        _movement.Enabled = false;
        _camera.Enabled = false;
        _camera.SpeedH = _camera.SpeedV = default;
        _movement.DesiredPosition = Vector3.Zero;
        IsImprisoned = false;
    }

    public void FullStopImprisonment()
    {
        ShouldBeImprisoned = false;
        CageTerritoryId = 0;
        CageOrigin = Vector3.Zero;
        CageRadius = 1f;
        StopDetoursAndControl();
    }

    private void EnqueueCageReturnTask()
    {
        _hcTasks.CreateGroup(ReturnToCageName, new(State.HcTaskControl.BlockMovementKeys | State.HcTaskControl.Weighted, 3000))
            .Add(() =>
            {
                if (!PlayerData.Available)
                    return false;

                // get where to go
                var toNext = CageOrigin - PlayerData.Position;
                toNext.Y = 0;

                // if we are within the cage origin, stop and return complete.
                if (toNext.LengthSquared() <= CageRadius * CageRadius)
                {
                    StopDetoursAndControl();
                    IsImprisoned = true; // still imprisoned, just back in cage.
                    return true; // task complete: player is within radius
                }

                // otherwise, movement towards the cage origin.
                _movement.Enabled = true;
                _movement.DesiredPosition = CageOrigin;
                _camera.Enabled = true;
                _camera.SpeedH = _camera.SpeedV = 360.Degrees();
                _camera.DesiredAzimuth = Angle.FromDirectionXZ(_movement.DesiredPosition - PlayerData.Position) + 180.Degrees();
                _camera.DesiredAltitude = -30.Degrees();
                return false;
            })
            .Add(() => _returningToCage = false)
            .Insert(); // force it to have priority and run immediately.
    }
}
