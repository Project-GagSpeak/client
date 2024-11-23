using Buttplug.Client;
using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.Toybox.Data;
using GagSpeak.UpdateMonitoring.Triggers;

namespace GagSpeak.UI;

// we need a factory to create new instances of Device objects whenever a device is added.
public class ToyboxFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public ToyboxFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
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
