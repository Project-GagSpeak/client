using Buttplug.Client;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Data;
using GagSpeak.UpdateMonitoring;
using GagSpeak.UpdateMonitoring.Triggers;
using GagSpeak.WebAPI;

namespace GagSpeak.UI;

// we need a factory to create new instances of Device objects whenever a device is added.
public class ToyboxFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainHub _hub;
    private readonly OnFrameworkService _frameworkUtils;
    public ToyboxFactory(ILoggerFactory loggerFactory, MainHub hub, 
        GagspeakMediator mediator, OnFrameworkService frameworkUtils)
    {
        _loggerFactory = loggerFactory;
        _hub = hub;
        _mediator = mediator;
        _frameworkUtils = frameworkUtils;
    }

    public ConnectedDevice CreateConnectedDevice(ButtplugClientDevice newDevice)
    {
        return new ConnectedDevice(_loggerFactory.CreateLogger<ConnectedDevice>(), newDevice);
    }

    public MonitoredPlayerState CreatePlayerMonitor(IPlayerCharacter player)
    {
        return new MonitoredPlayerState(player);
    }
}
