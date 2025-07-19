using GagSpeak.FileSystems;
using GagSpeak.Interop;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.State.Handlers;

// remove this if possible as i think if anything it only makes things more confusing.
public sealed class RemoteHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerIntiface _ipc;
    private readonly BuzzToyManager _manager;
    private readonly RemoteService _service;

    public RemoteHandler(ILogger<RemoteHandler> logger, GagspeakMediator mediator,
        IpcCallerIntiface ipc, BuzzToyManager manager, RemoteService service)
        : base(logger, mediator)
    {
        _ipc = ipc;
        _manager = manager;
        _service = service;
    }


    public void StartPattern(Guid patternId)
        => StartPattern(patternId, TimeSpan.Zero, TimeSpan.Zero);

    public void StartPattern(Guid patternId, TimeSpan customStart, TimeSpan customDuration)
    {
        // Handle Logic Later
        Logger.LogDebug($"Beginning pattern {patternId} on all toys.");
        // would need to find from storage or something.
    }

    public void StartPattern(Pattern pattern)
        => StartPattern(pattern, pattern.StartPoint, pattern.PlaybackDuration);

    public void StartPattern(Pattern pattern, TimeSpan customStart, TimeSpan customDuration)
    {
        // Handle Logic Later
        Logger.LogDebug($"Beginning pattern {pattern.Label} on all toys.");
    }

    public void StopActivePattern()
    {
        // Handle logic later
        Logger.LogDebug($"Stopping active pattern for all toys.");
    }
}





