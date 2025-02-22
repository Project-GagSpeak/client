using Buttplug.Client;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Data;
using GagSpeak.UI;
using GagspeakAPI.Data.Interfaces;

namespace GagSpeak.PlayerState.Controllers;

/// <summary> Controls the connection to Intiface and its connected devices. </summary>
public class IntifaceController : DisposableMediatorSubscriberBase
{
    public const string IntifaceClientName = "Connected To Intiface";

    private ButtplugClient ButtPlugClient;
    public ButtplugWebsocketConnector WebsocketConnector;
    private CancellationTokenSource? BatteryCheckCTS = new();

    private readonly GagspeakConfigService _config;
    private readonly ToyboxFactory _deviceFactory;
    public IntifaceController(ILogger<IntifaceController> log, GagspeakMediator mediator,
        GagspeakConfigService config, ToyboxFactory deviceFactory) : base(log, mediator)
    {
        _config = config;
        _deviceFactory = deviceFactory;

        // create the WebSocket connector
        WebsocketConnector = NewWebsocketConnection();
        // initialize the client
        ButtPlugClient = new ButtplugClient(IntifaceClientName);
        // subscribe to the events we should subscribe to, and attach them to our mediator subscriber
        ButtPlugClient.DeviceAdded += (sender, args) => OnDeviceAdded(args.Device);
        ButtPlugClient.DeviceRemoved += (sender, args) => OnDeviceRemoved(args.Device);
        ButtPlugClient.ScanningFinished += (sender, args) => OnScanningFinished();
        ButtPlugClient.ServerDisconnect += (sender, args) => OnButtplugClientDisconnected();
    }

    // Cached information from Intiface.
    private readonly List<ButtPlugDevice> _devices = new List<ButtPlugDevice>();
    public List<ButtPlugDevice> ConnectedDevices => _devices;
    public bool ConnectedToIntiface => ButtPlugClient != null && ButtPlugClient.Connected;
    public bool AnyDeviceConnected => ConnectedToIntiface && ButtPlugClient.Devices.Any();
    public bool ScanningForDevices { get; private set; } = false;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Ensure ButtplugClient is not null before trying to unsubscribe and dispose
        if (ButtPlugClient != null)
        {
            ButtPlugClient.DeviceAdded -= (sender, args) => OnDeviceAdded(args.Device);
            ButtPlugClient.DeviceRemoved -= (sender, args) => OnDeviceRemoved(args.Device);
            ButtPlugClient.ScanningFinished -= (sender, args) => OnScanningFinished();
            ButtPlugClient.ServerDisconnect -= (sender, args) => OnButtplugClientDisconnected();

            ButtPlugClient.DisconnectAsync().Wait();
            ButtPlugClient.Dispose();
            WebsocketConnector.Dispose();
        }
        BatteryCheckCTS?.Cancel();
    }

    public ButtPlugDevice? GetDeviceByName(string DeviceName) => _devices.FirstOrDefault(x => x.DeviceName == DeviceName);

    private ButtplugWebsocketConnector NewWebsocketConnection()
    {
        return _config.Config.IntifaceConnectionSocket != null
            ? new ButtplugWebsocketConnector(new Uri($"{_config.Config.IntifaceConnectionSocket}"))
            : new ButtplugWebsocketConnector(new Uri("ws://localhost:12345"));
    }

    #region EventHandling
    // handles event where device is added to Intiface Central
    private void OnDeviceAdded(ButtplugClientDevice addedDevice)
    {
        try
        {
            // use our factory to create the new device
            var newDevice = _deviceFactory.CreateConnectedDevice(addedDevice);
            // set that it is successfully connected and append it
            newDevice.IsConnected = true;
            _devices.Add(newDevice);
            UnlocksEventManager.AchievementEvent(UnlocksEvent.DeviceConnected);
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error adding device to device list. {ex.Message}");
        }
    }

    private void OnDeviceRemoved(ButtplugClientDevice removedDevice)
    {
        try
        {
            // find the device in the list and remove it
            var IdxToRemove = _devices.FindIndex(device => device.DeviceIdx == removedDevice.Index);
            // see if the index is valid.
            if (IdxToRemove > -1)
            {
                // log the removal and remove it
                Logger.LogInformation($"Device " + _devices[IdxToRemove] + " removed from device list.", LoggerType.ToyboxDevices);
                // create shallow copy
                var device2 = _devices[IdxToRemove];
                // remove from list
                _devices.RemoveAt(IdxToRemove);
                // disconnect.
                device2.IsConnected = false;
                // we call in thos order so that if it ever fails to disconnect, it will be caught in the
                // try catch block, and still be marked as connected.
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error removing device from device list. " + ex.Message, LoggerType.ToyboxDevices);
        }
    }

    /// <summary> Fired when scanning for devices is finished </summary>
    private void OnScanningFinished()
    {
        Logger.LogInformation("Finished Scanning for new Devices", LoggerType.ToyboxDevices);
        ScanningForDevices = false;
    }

    private void OnButtplugClientDisconnected()
    {
        Logger.LogInformation("Intiface Central Disconnected", LoggerType.ToyboxDevices);
        HandleDisconnect();
    }

    #endregion EventHandling

    #region ConnectionHandle
    public async void ConnectToIntifaceAsync()
    {
        try
        {
            // if we satisfy any conditions to refuse connection, early return
            if (ButtPlugClient == null)
            {
                Logger.LogError("ButtplugClient is null. Cannot connect to Intiface Central");
                return;
            }
            else if (ButtPlugClient.Connected)
            {
                Logger.LogInformation("Already connected to Intiface Central", LoggerType.ToyboxDevices);
                return;
            }
            else if (WebsocketConnector == null)
            {
                Logger.LogError("WebsocketConnector is null. Cannot connect to Intiface Central");
                return;
            }
            if (ConnectedToIntiface)
            {
                Logger.LogInformation("Already connected to Intiface Central", LoggerType.ToyboxDevices);
                return;
            }
            // Attempt connection to server
            Logger.LogDebug("Attempting connection to Intiface Central", LoggerType.ToyboxDevices);
            await ButtPlugClient.ConnectAsync(WebsocketConnector);
        }
        catch (ButtplugClientConnectorException socketEx)
        {
            Logger.LogError($"Error Connecting to Websocket. Is your Intiface Opened? | {socketEx}");
            DisconnectFromIntifaceAsync();
            return;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error connecting to Intiface Central (Potentially timed out?) | {ex}");
            DisconnectFromIntifaceAsync();
            return;
        }

        // see if we sucessfully connected
        Logger.LogInformation("Connected to Intiface Central", LoggerType.ToyboxDevices);
        try
        {
            // scan for any devices for the next 2 seconds
            Logger.LogInformation("Scanning for devices over the next 2 seconds.", LoggerType.ToyboxDevices);
            await StartDeviceScanAsync();
            Thread.Sleep(2000);
            await StopDeviceScanAsync();

            // Reason to connect is valid, so reset the battery check token
            BatteryCheckCTS?.Cancel();
            BatteryCheckCTS?.Dispose();
            BatteryCheckCTS = new CancellationTokenSource();
            _ = BatteryHealthCheck(BatteryCheckCTS.Token);

            // see if we managed to fetch any devices
            if (AnyDeviceConnected)
            {
                // if we did, and that device had a stored intensity, set the intensity on that device.
                // TODO: Implement this logic.
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error scanning for devices after connecting to Intiface Central. {ex}");
        }
    }

    public async void DisconnectFromIntifaceAsync()
    {
        try
        {
            // see if we are currently conected to the server.
            if (ButtPlugClient.Connected)
            {
                // if we are, disconnect.
                await ButtPlugClient.DisconnectAsync();
                // if the disconnect was sucessful, handle the disconnect.
                if (!ButtPlugClient.Connected)
                {
                    Logger.LogInformation("Disconnected from Intiface Central", LoggerType.ToyboxDevices);
                    ScanningForDevices = false;
                    // no need to use handleDisconnect here since we execute that in the subscribed event.
                }
            }
            // recreate the websocket connector
            WebsocketConnector.Dispose();
            WebsocketConnector = NewWebsocketConnection();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error disconnecting from Intiface Central. {ex}");
        }
    }

    public void HandleDisconnect()
    {
        Logger.LogDebug("Client was properly disconnected from Intiface Central. Disconnecting Device Handler.", LoggerType.ToyboxDevices);
        try
        {
            _devices.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error clearing devices from device list. {ex.Message}");
        }

        // do not dispose of the client once disconnected, we want to stay linked so that we can reconnect faster.
        BatteryCheckCTS?.Cancel();
    }

    #endregion ConnectionHandle

    /// <summary> Continuously checks the battery health of the client until canceled at a set interval </summary>
    private async Task BatteryHealthCheck(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && ConnectedToIntiface)
        {
            await Task.Delay(TimeSpan.FromSeconds(60), ct).ConfigureAwait(false);
            Logger.LogTrace("Scheduled Battery Check on connected devices", LoggerType.ToyboxDevices);

            if (!ConnectedToIntiface)
                break;

            try
            {
                foreach (var device in _devices)
                    device.UpdateBatteryPercentage();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while fetching the battery level from devices: {ex.Message}");
            }
        }
    }


    /// <summary> Start scanning for devices asynchronously </summary>
    public async Task StartDeviceScanAsync()
    {
        // begin scan if we are connected
        if (!ButtPlugClient.Connected)
        {
            Logger.LogWarning("Cannot scan for devices if not connected to Intiface Central");
        }

        Logger.LogDebug("Now actively scanning for new devices...", LoggerType.ToyboxDevices);
        try
        {
            ScanningForDevices = true;
            await ButtPlugClient.StartScanningAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in ScanForDevicesAsync: {ex.ToString()}");
        }
    }

    /// <summary> Stop scanning for devices asynchronously </summary>
    public async Task StopDeviceScanAsync()
    {
        // stop the scan if we are connected
        if (!ButtPlugClient.Connected)
        {
            Logger.LogWarning("Cannot stop scanning for devices if not connected to Intiface Central");
        }

        Logger.LogDebug("Halting the scan for new devices to add", LoggerType.ToyboxDevices);
        try
        {
            await ButtPlugClient.StopScanningAsync();
            if (ScanningForDevices)
            {
                ScanningForDevices = false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error in StopScanForDevicesAsync: {ex.ToString()}");
        }
    }

    public void StopAllDevices()
    {
        // halt the vibration of all devices on all motors
        foreach (var device in _devices)
            device.StopInTheNameOfTheVibe();
    }

    public void ExecuteVibeTrigger(SexToyAction sexToyAction)
    {
        // if we are not connected do not allow
        if (!ConnectedToIntiface || ButtPlugClient == null)
        {
            Logger.LogWarning("Cannot execute trigger if not connected to Intiface Central");
            return;
        }

        Logger.LogInformation("Vibe Trigger Function Accessed. This would normally play a vibe by now!", LoggerType.ToyboxDevices);
    }

    public void SendVibeToAllDevices(byte intensity)
    {
        // if we are not connected do not allow
        if (!ConnectedToIntiface || ButtPlugClient == null)
        {
            Logger.LogWarning("Cannot send vibration to devices if not connected to Intiface Central");
            return;
        }
        // send the vibration to all devices on all motors
        foreach (var device in ConnectedDevices)
        {
            if (device.CanVibrate)
                device.SendVibration(intensity);

            if (device.CanRotate)
                device.SendRotate(intensity);
        }
    }

    public void SendVibrateToDevice(ButtPlugDevice device, byte intensity, int motorId = -1)
    {
        device.SendVibration(intensity, motorId);
    }

    public void SendRotateToDevice(ButtPlugDevice device, byte intensity, bool clockwise = true, int motorId = -1)
    {
        device.SendRotate(intensity, clockwise, motorId);
    }

    public void SendStopRequestToDevice(ButtPlugDevice device)
    {
        device.StopInTheNameOfTheVibe();
    }
}
