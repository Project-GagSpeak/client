using Buttplug.Client;
using Buttplug.Core;
using Buttplug.Core.Messages;
using CkCommons;
using Dalamud.Plugin.Ipc;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Data.Struct;
using System.Net.WebSockets;

namespace GagSpeak.Interop;

public sealed class IpcCallerIntiface : IDisposable, IIpcCaller
{
    public static readonly string ClientName = "Connected To Intiface";

    private readonly ILogger<IpcCallerIntiface> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;

    private static ButtplugClient client = new ButtplugClient(ClientName);

    private CancellationTokenSource _scannerCTS = new();

    public IpcCallerIntiface(ILogger<IpcCallerIntiface> logger, GagspeakMediator mediator, MainConfig config)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;

        // Subscribe to the main hub connected message to open and connect.
        client.DeviceAdded += (_, args) => OnDeviceAdded(args.Device);
        client.DeviceRemoved += (_, args) => OnDeviceRemoved(args.Device);
        client.ScanningFinished += (_, args) => OnScanFinished();
        client.ServerDisconnect += (_, args) => OnIntifaceDisconnected();
        CheckAPI();
    }

    public static bool APIAvailable { get; private set; } = false;
    public static bool ScanningForDevices { get; private set; } = false;
    public static bool IsConnected => client.Connected;
    public bool AutoConnect => _config.Current.IntifaceAutoConnect;

    public void CheckAPI()
    {
        APIAvailable = IsConnected;
    }

    public void Dispose()
    {
        _scannerCTS.SafeCancel();
        if (client is not null)
        {
            // Unsubscribe from events to prevent memory leaks.
            client.DeviceAdded -= (_, args) => OnDeviceAdded(args.Device);
            client.DeviceRemoved -= (_, args) => OnDeviceRemoved(args.Device);
            client.ScanningFinished -= (_, args) => OnScanFinished();
            client.ServerDisconnect -= (_, args) => OnIntifaceDisconnected();

            try
            {
                // Handle actions to perform upon disconnect.
                client.DisconnectAsync().Wait();
                client.Dispose();
                ScanningForDevices = false;
            }
            catch (ButtplugException ex) { _logger.LogDebug($"Dumb ButtplugException: {ex}"); }
            catch (Exception ex)
            {
                _logger.LogError($"Error disconnecting from Intiface Central: {ex.Message}");
            }
        }

        _scannerCTS.SafeDispose();
    }

    private void OnDeviceAdded(ButtplugClientDevice device)
    {
        _logger.LogInformation($"Device Added: [{device.Name}] ({device.DisplayName}) at index {device.Index}", LoggerType.Toys);
        _mediator.Publish(new BuzzToyAdded(device));
    }

    private void OnDeviceRemoved(ButtplugClientDevice device)
    {
        _logger.LogInformation($"Device Removed: [{device.Name}] ({device.DisplayName}) at index {device.Index}", LoggerType.Toys);
        _mediator.Publish(new BuzzToyRemoved(device));
    }

    private void OnScanFinished()
        => _mediator.Publish(new DeviceScanFinished());

    private void OnIntifaceDisconnected()
        => _mediator.Publish(new IntifaceClientDisconnected());

    private ButtplugWebsocketConnector CreateNewConnection()
        => _config.Current.IntifaceConnectionSocket is not null
        ? new ButtplugWebsocketConnector(new Uri($"{_config.Current.IntifaceConnectionSocket}"))
        : new ButtplugWebsocketConnector(new Uri("ws://localhost:12345"));

    public void OpenAndConnect()
    {
        // Early return if conditions are not satisfied.
        if (!AutoConnect || IsConnected)
            return;

        // If they are, forcibly locate the Intiface Central application path.
        if (string.IsNullOrEmpty(IntifaceCentral.AppPath))
            IntifaceCentral.GetApplicationPath();

        // Then forcibly open it, and connect.
        IntifaceCentral.OpenIntiface(false);
        Connect().ConfigureAwait(false);
    }

    public async Task Connect()
    {
        try
        {
            if (client is null)
            {
                _logger.LogError("ButtplugClient is null. Cannot connect to Intiface Central");
                return;
            }
            else if (client.Connected)
            {
                _logger.LogInformation("Already connected to Intiface Central", LoggerType.Toys);
                return;
            }

            // Attempt connection to server
            _logger.LogDebug("Attempting connection to Intiface Central", LoggerType.Toys);
            await client.ConnectAsync(CreateNewConnection());

            // let other classes know of the connection.
            _mediator.Publish(new IntifaceClientConnected());
        }
        catch (ButtplugException socketEx)
        {
            _logger.LogError($"Error Connecting to Websocket. Is your Intiface Opened? | {socketEx}");
            await Disconnect();
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error connecting to Intiface Central (Potentially timed out?) | {ex}");
            await Disconnect();
            return;
        }
    }

    public async Task Disconnect()
    {
        // try safely executing this incase Intiface Central goes explotano
        try
        {
            if (IsConnected)
            {
                // Handle actions to perform upon disconnect.
                await client.DisconnectAsync();
                // if we have successfully disconnected, handle the disconnect.
                _logger.LogInformation("Disconnected from Intiface Central", LoggerType.Toys);
                ScanningForDevices = false;
            }
        }
        catch (ButtplugException ex)
        {
            _logger.LogError($"Buttplug Exception while disconnecting: {ex.Message}", LoggerType.Toys);
        }
        catch (WebSocketException ex)
        {
            _logger.LogError($"WebSocket Exception while disconnecting: {ex.Message}", LoggerType.Toys);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error disconnecting from Intiface Central: {ex.Message}", LoggerType.Toys);
        }
    }

    public async Task DeviceScannerTask()
    {
        if (!client.Connected || ScanningForDevices)
            return;

        await Generic.Safe(async () =>
        {
            _logger.LogDebug("Scanning for new devices...", LoggerType.Toys);
            await client.StartScanningAsync();
            ScanningForDevices = true;

            await Task.Delay(2500); // Wait for 2.5 seconds to allow scanning to complete

            _logger.LogDebug("Stopping device scan...", LoggerType.Toys);
            await client.StopScanningAsync();
            ScanningForDevices = false;
        });
    }
}
