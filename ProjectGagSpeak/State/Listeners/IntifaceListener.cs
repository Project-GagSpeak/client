using Buttplug.Client;
using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Listens for the Intiface IPC updates, and informs the manager accordingly.
/// </summary>
public sealed class IntifaceListener : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerIntiface _ipc;
    private readonly BuzzToyManager _manager;
    public IntifaceListener(ILogger<IntifaceListener> logger, GagspeakMediator mediator,
        IpcCallerIntiface ipc, BuzzToyManager manager) : base(logger, mediator)
    {
        _ipc = ipc;
        _manager = manager;

        Mediator.Subscribe<ConnectedMessage>(this, _ => _ipc.OpenAndConnect());

        Mediator.Subscribe<BuzzToyAdded>(this, msg => OnDeviceAdded(msg.Device));
        Mediator.Subscribe<BuzzToyRemoved>(this, msg => OnDeviceRemoved(msg.Device));
        Mediator.Subscribe<DeviceScanFinished>(this, _ => OnScanningFinished());
        Mediator.Subscribe<IntifaceClientConnected>(this, _ => OnPostConnected());
        Mediator.Subscribe<IntifaceClientDisconnected>(this, _ => OnPostDisconnect());
    }
    private void OnDeviceAdded(ButtplugClientDevice added)
        => _manager.AddOrUpdateDevice(added);

    private void OnDeviceRemoved(ButtplugClientDevice removed)
    {
        // Safely execute this so that if we have a disconnect during handling this process we dont explotano.
        Generic.Safe(() =>
        {
            // Get the idx of the removed device.
            Logger.LogInformation($"Device {removed.Name} removed from device list.", LoggerType.Toys);
            var realToys = _manager.Storage.Values.OfType<IntifaceBuzzToy>();
            if (realToys.FirstOrDefault(st => st.DeviceIdx == removed.Index) is { } match)
                _manager.RemoveDevice(match);
            else
                throw new Exception($"Device with index {removed.Index} not found in connected toys list.");
        });
    }

    /// <summary> Fired when scanning for devices is finished </summary>
    private void OnScanningFinished()
    {
        Logger.LogInformation("Finished Scanning for new Devices", LoggerType.Toys);
    }

    private async void OnPostConnected()
    {
        // see if we sucessfully connected
        Logger.LogInformation("Connected to Intiface Central", LoggerType.Toys);
        await _ipc.DeviceScannerTask();
        // begin the battery check loop
        _manager.StartBatteryCheck();
    }

    private void OnPostDisconnect()
        => _manager.StopBatteryCheck();
}
