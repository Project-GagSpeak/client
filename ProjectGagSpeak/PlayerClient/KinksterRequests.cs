using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Network;

namespace GagSpeak.PlayerClient;

/// <summary> 
///     The ClientPlayer's Kinkster Requests.
/// </summary>
public sealed class KinksterRequests
{
    private readonly ILogger<KinksterRequests> _logger;
    private readonly GagspeakMediator _mediator;
    public KinksterRequests(ILogger<KinksterRequests> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public HashSet<KinksterRequestEntry> CurrentRequests { get; set; } = new();
    public HashSet<KinksterRequestEntry> OutgoingRequests => CurrentRequests.Where(x => x.User.UID == MainHub.UID).ToHashSet();
    public HashSet<KinksterRequestEntry> IncomingRequests => CurrentRequests.Where(x => x.Target.UID == MainHub.UID).ToHashSet();

    public void AddPairRequest(KinksterRequestEntry dto)
    {
        CurrentRequests.Add(dto);
        _logger.LogInformation("New pair request added!", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiMessage());
    }

    public void RemovePairRequest(KinksterRequestEntry dto)
    {
        var res = CurrentRequests.RemoveWhere(x => x.User.UID == dto.User.UID && x.Target.UID == dto.Target.UID);
        _logger.LogInformation("Removed " + res + " pair requests.", LoggerType.PairManagement);
        _mediator.Publish(new RefreshUiMessage());
    }
}
