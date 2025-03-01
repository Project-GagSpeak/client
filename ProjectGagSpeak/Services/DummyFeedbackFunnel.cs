using GagSpeak.PlayerState.Listener;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;

namespace GagSpeak.PlayerData.Services;

/// <summary> Routes all intended server calls back to self for testing purposes. </summary>
public sealed class DummyFeedbackFunnel : DisposableMediatorSubscriberBase
{
    private readonly VisualStateListener _visualListener;
    private readonly PuppeteerListener _puppeteerListener;
    private readonly ToyboxStateListener _toyboxListener;
    private readonly ConfigFileProvider _fileNames;

    public DummyFeedbackFunnel(ILogger<DummyFeedbackFunnel> logger, GagspeakMediator mediator,
        VisualStateListener visualListener, PuppeteerListener puppeteerListener,
        ToyboxStateListener toyboxListener, ConfigFileProvider fileNames) : base(logger, mediator)
    {
        _visualListener = visualListener;
        _puppeteerListener = puppeteerListener;
        _toyboxListener = toyboxListener;
        _fileNames = fileNames;
    }
}
