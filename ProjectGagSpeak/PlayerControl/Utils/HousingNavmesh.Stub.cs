using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerControl;

public sealed class HousingNavmesh : DisposableMediatorSubscriberBase
{
    public HousingNavmesh(ILogger<HousingNavmesh> logger, GagspeakMediator mediator) : base(logger, mediator)
    { }

    public bool Ready => false;

    public bool DrawCollision, DrawWalkable, DrawJumps, DrawPath;
    public float DrawRadius = 10f;

    public List<Vector3>? FindPath(Vector3 from, Vector3 to)
        => null;

    public bool IsJumpStart(Vector3 point)
        => false;
}
