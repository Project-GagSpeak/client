using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerControl;

public sealed class HousingNavmesh : DisposableMediatorSubscriberBase
{
    public HousingNavmesh(ILogger<HousingNavmesh> logger, GagspeakMediator mediator) : base(logger, mediator)
    { }

    public bool Ready => false;

    public List<Vector3>? FindPath(Vector3 from, Vector3 to)
        => null;
}
