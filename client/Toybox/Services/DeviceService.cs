using Buttplug.Client;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Data;
using GagSpeak.UI;

namespace GagSpeak.Toybox.Controllers;

/// <summary>
/// handles the connected devices and the socket connection to the Intiface server.
/// </summary>
public class DeviceService : DisposableMediatorSubscriberBase
{
    private ButtplugClient ButtPlugClient;
    public ButtplugWebsocketConnector WebsocketConnector;
    private CancellationTokenSource? BatteryCheckCTS = new();

    // likely will include API controller and other things later. Otherwise they will be in ToyboxHandler.
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ToyboxFactory _deviceFactory;

    private readonly List<ConnectedDevice> Devices = new List<ConnectedDevice>();
    private readonly Dictionary<string, int> ActiveDeviceAndMotors = new Dictionary<string, int>();

    // maybe store triggers here in the future, or in the trigger handler, but not now.

    public DeviceService(ILogger<DeviceService> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfiguration, ToyboxFactory deviceFactory)
        : base(logger, mediator)
    {
        _clientConfigs = clientConfiguration;
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

    // public accessors.
    public const string IntifaceClientName = "Connected To Intiface";
    public bool ConnectedToIntiface => ButtPlugClient != null && ButtPlugClient.Connected;
    public bool AnyDeviceConnected => ButtPlugClient.Connected && ButtPlugClient.Devices.Any();
    public List<ConnectedDevice> ConnectedDevices => Devices;
    public int ConnectedDevicesCount => Devices.Count;
    public bool ScanningForDevices { get; private set; }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Ensure ButtplugClient is not null before trying to unsubscribe and dispose
        if (ButtPlugClient != null)
        {
            // Unsubscribe from events
            ButtPlugClient.DeviceAdded -= (sender, args) => OnDeviceAdded(args.Device);
            ButtPlugClient.DeviceRemoved -= (sender, args) => OnDeviceRemoved(args.Device);
            ButtPlugClient.ScanningFinished -= (sender, args) => OnScanningFinished();
            ButtPlugClient.ServerDisconnect -= (sender, args) => OnButtplugClientDisconnected();

            // Disconnect and dispose ButtPlugClient
            ButtPlugClient.DisconnectAsync().Wait();
            ButtPlugClient.Dispose();
            // dispose the connector
            WebsocketConnector.Dispose();
        }
        // cancel the battery check token
        BatteryCheckCTS?.Cancel();
    }

    public ConnectedDevice? GetDeviceByName(string DeviceName)
        => Devices.FirstOrDefault(x => x.DeviceName == DeviceName);

    private ButtplugWebsocketConnector NewWebsocketConnection()
    {
        return _clientConfigs.GagspeakConfig.IntifaceConnectionSocket != null
                    ? new ButtplugWebsocketConnector(new Uri($"{_clientConfigs.GagspeakConfig.IntifaceConnectionSocket}"))
                    : new ButtplugWebsocketConnector(new Uri("ws://localhost:12345"));
    }

    #region EventHandling
    // handles event where device is added to Intiface Central
    private void OnDeviceAdded(ButtplugClientDevice addedDevice)
    {
        try
        {
            // use our factory to create the new device
            ConnectedDevice newDevice = _deviceFactory.CreateConnectedDevice(addedDevice);
            // set that it is successfully connected and append it
            newDevice.IsConnected = true;
            Devices.Add(newDevice);
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
            int IndexInDeviceListToRemove = Devices.FindIndex((ConnectedDevice device) => device.DeviceIdx == removedDevice.Index);
            // see if the index is valid.
            if (IndexInDeviceListToRemove > -1)
            {
                // log the removal and remove it
                Logger.LogInformation($"Device "+Devices[IndexInDeviceListToRemove]+" removed from device list.", LoggerType.ToyboxDevices);
                // create shallow copy
                ConnectedDevice device2 = Devices[IndexInDeviceListToRemove];
                // remove from list
                Devices.RemoveAt(IndexInDeviceListToRemove);
                // disconnect.
                device2.IsConnected = false;
                // we call in thos order so that if it ever fails to disconnect, it will be caught in the
                // try catch block, and still be marked as connected.
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error removing device from device list. "+ex.Message, LoggerType.ToyboxDevices);
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
            Devices.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error clearing devices from device list. {ex.Message}");
        }

        // do not dispose of the client once disconnected, we want to stay linked so that we can reconnect faster.
        BatteryCheckCTS?.Cancel();
    }

    #endregion ConnectionHandle

    /// <summary> 
    /// Asynchronous method that continuously checks the battery health of the client 
    /// until canceled at a set interval
    /// </summary>
    private async Task BatteryHealthCheck(CancellationToken ct)
    {
        // while the cancellation token is not requested and the hub is not null
        while (!ct.IsCancellationRequested && ConnectedToIntiface)
        {
            // wait for 60 seconds. The longer between checks, the better on a toys battery life.
            await Task.Delay(TimeSpan.FromSeconds(60), ct).ConfigureAwait(false);
            
            // log that we are checking the client health state
            Logger.LogTrace("Scheduled Battery Check on connected devices", LoggerType.ToyboxDevices);
            
            // if we need to reconnect, break out of the loop
            if (!ConnectedToIntiface) break;
            // we can perform the check, so fetch battery from all devices
            try
            {
                foreach (ConnectedDevice device in Devices)
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
        foreach (ConnectedDevice device in Devices)
        {
            try
            {
                device.StopInTheNameOfTheVibe();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error stopping device {device.DeviceName} | {ex.Message}");
            }
        }
    }

    public void ExecuteVibeTrigger(Trigger trigger)
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
        foreach (ConnectedDevice device in Devices)
        {
            if (device.CanVibrate)
                device.SendVibration(intensity);

            if (device.CanRotate)
                device.SendRotate(intensity);
        }
    }

    public void SendVibrateToDevice(ConnectedDevice device, byte intensity, int motorId = -1)
    {
        device.SendVibration(intensity, motorId);
    }

    public void SendRotateToDevice(ConnectedDevice device, byte intensity, bool clockwise = true, int motorId = -1)
    {
        device.SendRotate(intensity, clockwise, motorId);
    }

    public void SendStopRequestToDevice(ConnectedDevice device)
    {
        device.StopInTheNameOfTheVibe();
    }
}
