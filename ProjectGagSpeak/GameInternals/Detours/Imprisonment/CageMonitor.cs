using CkCommons;
using GagSpeak.GameInternals.Structs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.GameInternals.Detours;
public class ImprisonmentController : DisposableMediatorSubscriberBase
{
    public const string ThrottlerName = "FollowPathTime";

    private CameraOverride _camera = new();
    private MovementOverride _movement = new();
    private long TimeoutAt = 0;
    public float Tolerance = 0.25f;

    public uint CageTerritoryId = 0;
    public Vector3 CageOrigin { get; private set; } = Vector3.Zero;
    public ImprisonmentController(ILogger<ImprisonmentController> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        Mediator.Subscribe<HcStateCacheChanged>(this, _ => OnHcCacheStateChange());

    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _camera.Dispose();
        _movement.Dispose();
    }

    private void OnHcCacheStateChange()
    {
        Svc.Logger.Warning("I need to be implemented!");
    }

    public void UpdateTimeout(int seconds) 
        => TimeoutAt = Environment.TickCount64 + seconds * 1000;

    public unsafe bool UpdateMovement()
    {
        if (!PlayerData.Available)
        {
            Stop();
            return true;
        }

        // update the timeout if it hit 0.
        if (TimeoutAt == 0)
            TimeoutAt = Environment.TickCount64 + 30000;

        // Break if timed out.
        if (Environment.TickCount64 > TimeoutAt)
        {
            Stop();
            Svc.Logger.Warning($"Movement has timed out.");
            return true;
        }

        // get the next position to go to.
        var toNext = CageOrigin - PlayerData.Position;
        toNext.Y = 0;

        if (toNext.LengthSquared() <= Tolerance * Tolerance)
        {
            Stop();
            return true;
        }

        _movement.Enabled = true;
        _movement.DesiredPosition = CageOrigin;
        _camera.Enabled = true;
        _camera.SpeedH = _camera.SpeedV = 360.Degrees();
        _camera.DesiredAzimuth = Angle.FromDirectionXZ(_movement.DesiredPosition - PlayerData.Position) + 180.Degrees();
        _camera.DesiredAltitude = -30.Degrees();
        return false;
    }

    public void Stop()
    {
        _movement.Enabled = false;
        _camera.Enabled = true;
        _camera.SpeedH = _camera.SpeedV = default;
        _movement.DesiredPosition = PlayerData.Position;
    }

    public void UpdateCagePosition(Vector3 pos)
    {
        TimeoutAt = 0;
        CageTerritoryId = PlayerContent.TerritoryIdInstanced;
        CageOrigin = pos;
    }
}
