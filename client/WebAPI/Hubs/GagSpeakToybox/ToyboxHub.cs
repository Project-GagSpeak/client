using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.Achievements;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Reflection;

namespace GagSpeak.WebAPI;

#pragma warning disable MA0040


/// <summary>
/// This connections class maintains the responsibilities for how we connect, disconnect, and reconnect.
/// Manages GagSpeakHub-Toybox.
/// </summary>
public sealed partial class ToyboxHub : GagspeakHubBase, IToyboxHubClient
{
    private readonly HubFactory _hubFactory;
    private readonly PrivateRoomManager _privateRooms;
    private readonly PairManager _pairs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly GagspeakConfigService _mainConfig;
    private readonly ClientCallbackService _callbackService;

    // Cancellation Token Sources
    private CancellationTokenSource HubConnectionCTS;
    private CancellationTokenSource? HubHealthCTS = new();

    // Private Variables (keep unique? Don't think safe to make abstract)
    public static ToyboxConnectionDto? ToyboxConnectionDto = null;

    public ToyboxHub(ILogger<ToyboxHub> logger, GagspeakMediator mediator, HubFactory hubFactory, 
        TokenProvider tokenProvider, PairManager pairs, ServerConfigurationManager serverConfigs, 
        GagspeakConfigService mainConfig, ClientCallbackService clientCallbacks,
        PrivateRoomManager privateRooms, ClientMonitorService clientService,
        OnFrameworkService frameworkUtils) : base(logger, mediator, tokenProvider, clientService, frameworkUtils)
    {
        _hubFactory = hubFactory;
        _privateRooms = privateRooms;
        _pairs = pairs;
        _serverConfigs = serverConfigs;
        _mainConfig = mainConfig;
        _callbackService = clientCallbacks;

        // Create our CTS for the hub connection
        HubConnectionCTS = new CancellationTokenSource();

        // toybox hub connection subscribers
        Mediator.Subscribe<ToyboxHubClosedMessage>(this, (msg) => HubInstanceOnClosed(msg.Exception));
        Mediator.Subscribe<ToyboxHubReconnectedMessage>(this, (msg) => _ = HubInstanceOnReconnected());
        Mediator.Subscribe<ToyboxHubReconnectingMessage>(this, (msg) => HubInstanceOnReconnecting(msg.Exception));

        // Set ToyboxFullPause to true by default.
        _serverConfigs.CurrentServer.ToyboxFullPause = true;
    }

    // Information related to version details.
    public static UserData PlayerUserData => ConnectionDto!.User;
    public static string DisplayName => ConnectionDto?.User.AliasOrUID ?? string.Empty;
    public static string UID => ConnectionDto?.User.UID ?? string.Empty;

    // Information gathered from our hub connection.
    private HubConnection? GagSpeakHubToybox;
    public bool Initialized { get; private set; } = false;
    private static ServerState _serverStatus = ServerState.Offline;
    public static ServerState ServerStatus
    {
        get => _serverStatus;
        private set
        {
            if (_serverStatus != value)
            {
                StaticLogger.Logger.LogDebug("(Hub-Toybox): New ServerState: " + value + ", prev ServerState: " + _serverStatus, LoggerType.ApiCore);
                _serverStatus = value;
            }
        }
    }
    public static bool IsConnected => ServerStatus is ServerState.Connected;
    public static bool IsServerAlive => ServerStatus is ServerState.Connected or ServerState.Unauthorized or ServerState.Disconnected;
    public bool ClientHasConnectionPaused => _serverConfigs.CurrentServer?.ToyboxFullPause ?? false;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Logger.LogInformation("Disposal Called! Closing down GagSpeakHub-Toybox!", LoggerType.ApiCore);
        HubHealthCTS?.Cancel();
        Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        HubConnectionCTS?.Cancel();
    }

    public override async Task Connect()
    {
        Logger.LogInformation("Client Wished to Connect to the server", LoggerType.ApiCore);
        if (!ShouldClientConnect(out string? secretKey))
        {
            Logger.LogInformation("Client was not in a valid state to connect to the server.", LoggerType.ApiCore);
            return;
        }

        Logger.LogInformation("Connection Validation Approved, Creating Connection with [" + _serverConfigs.CurrentServer.ServerName + "]", LoggerType.ApiCore);
        // Debug the current state here encase shit hits the fan.
        Logger.LogDebug("Current ServerState during this Connection Attempt: " + ServerStatus, LoggerType.ApiCore);
        // Recreate the ConnectionCTS.
        HubConnectionCTS?.Cancel();
        HubConnectionCTS?.Dispose();
        HubConnectionCTS = new CancellationTokenSource();
        CancellationToken connectionToken = HubConnectionCTS.Token;

        // While we are still waiting to connect to the server, do the following:
        while (ServerStatus is not ServerState.Connected && !connectionToken.IsCancellationRequested)
        {
            AuthFailureMessage = string.Empty;

            Logger.LogInformation("Attempting to Connect to GagSpeakHub-Toybox", LoggerType.ApiCore);
            ServerStatus = ServerState.Connecting;
            try
            {
                try
                {
                    LastToken = await _tokenProvider.GetOrUpdateToken(connectionToken).ConfigureAwait(false);
                }
                catch (GagspeakAuthFailureException ex)
                {
                    AuthFailureMessage = ex.Reason;
                    throw new HttpRequestException("Error during authentication", ex, System.Net.HttpStatusCode.Unauthorized);
                }

                // Ensure the player is like, presently logged in and visible on the screen and stuff before starting connection.
                await WaitForWhenPlayerIsPresent(connectionToken);

                // (do it here incase the wait for the player is long or the token is cancelled during the wait)
                if (connectionToken.IsCancellationRequested || LastToken is null)
                {
                    Logger.LogWarning("GagSpeakHub-Toybox's ConnectionToken was cancelled during connection. Aborting!", LoggerType.ApiCore);
                    return;
                }

                // Init & Startup GagSpeakHub-Toybox
                GagSpeakHubToybox = _hubFactory.GetOrCreate(connectionToken, HubType.ToyboxHub, LastToken);
                InitializeApiHooks();
                await GagSpeakHubToybox.StartAsync(connectionToken).ConfigureAwait(false);

                if (await ConnectionDtoAndVersionIsValid() is false)
                {
                    Logger.LogWarning("Connection was not valid, disconnecting.", LoggerType.ApiCore);
                    return;
                }

                // if we reach here it means we are officially connected to the server
                Logger.LogInformation("Successfully Connected to GagSpeakHub-Toybox", LoggerType.ApiCore);
                ServerStatus = ServerState.Connected;
                Mediator.Publish(new ToyboxHubConnectedMessage());

                // Load in our initial pairs, then the online ones.
                await LoadOnlinePairs().ConfigureAwait(false);

                // Save that this connection with this secret key was valid.
                _serverConfigs.SetSecretKeyAsValid(secretKey);
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Connection attempt cancelled");
                return; // (Prevent further reconnections)
            }
            catch (HttpRequestException ex) // GagSpeakAuthException throws here
            {
                Logger.LogWarning("HttpRequestException on Connection:" + ex.Message);
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await Disconnect(ServerState.Unauthorized).ConfigureAwait(false);
                    return; // (Prevent further reconnections)
                }

                // Another HTTP Exception type, so disconnect, then attempt reconnection.
                Logger.LogWarning("Failed to establish connection, retrying");
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
                // Reconnect in 5-20 seconds. (prevents server overload)
                ServerStatus = ServerState.Reconnecting;
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 20)), connectionToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning("InvalidOperationException on connection: " + ex.Message);
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
                return; // (Prevent further reconnections)
            }
            catch (Exception ex)
            {
                // if we had any other exception, log it and attempt to reconnect
                Logger.LogWarning("Exception on Connection (Attempting Reconnection soon): " + ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 20)), connectionToken).ConfigureAwait(false);
            }
        }
    }

    public override async Task Disconnect(ServerState disconnectionReason, bool saveAchievements = true)
    {
        // Set the new state to be Disconnecting.
        ServerStatus = ServerState.Disconnecting;
        Logger.LogInformation("Disposing of GagSpeakHub-Toybox's Hub Instance", LoggerType.ApiCore);

        // Obliterate the GagSpeakHub-Toybox into the ground, erase it out of existence .
        await _hubFactory.DisposeHubAsync(HubType.ToyboxHub).ConfigureAwait(false);

        // If our hub was already initialized by the time we call this, reset all values monitoring it.
        // After this connection revision this should technically ALWAYS be true, so if it isnt log it as an error.
        if (GagSpeakHubToybox is not null)
        {
            Logger.LogInformation("Instance disposed of in '_hubFactory', but still exists in ToyboxHub.cs, " +
                "clearing all other variables for [GagSpeakHub-Toybox]", LoggerType.ApiCore);
            // Clear the Health check so we stop pinging the server, set Initialized to false, publish a disconnect.
            Initialized = false;
            HubHealthCTS?.Cancel();
            Mediator.Publish(new ToyboxHubDisconnectedMessage());
            // set the connectionDto and hub to null.
            GagSpeakHubToybox = null;
            ToyboxConnectionDto = null;
        }

        Logger.LogInformation("GagSpeakHub-Toybox disconnected due to: [" + disconnectionReason + "]", LoggerType.ApiCore);
        ServerStatus = disconnectionReason;
    }

    protected override bool ShouldClientConnect(out string fetchedSecretKey)
    {
        fetchedSecretKey = string.Empty;

        if (!_clientService.IsLoggedIn)
        {
            Logger.LogWarning("Attempted to connect while not logged in, this shouldnt be possible! Aborting!", LoggerType.ApiCore);
            return false;
        }

        // if we have not yet made an account, abort this connection.
        if (!_mainConfig.Current.AccountCreated)
        {
            Logger.LogDebug("Account not yet created, Aborting Connection.", LoggerType.ApiCore);
            return false;
        }

        // If the client wishes to not be connected to the server, return.
        if (ClientHasConnectionPaused)
        {
            Logger.LogDebug("You Have your connection to server paused. Stopping any attempt to connect!", LoggerType.ApiCore);
            return false;
        }

        // Obtain stored ServerKey for the current Character we are logged into.
        fetchedSecretKey = _serverConfigs.GetSecretKeyForCharacter() ?? string.Empty;
        if (fetchedSecretKey.IsNullOrEmpty())
        {
            // log a warning that no secret key is set for the current character
            Logger.LogWarning("No secret key set for current character, aborting Connection with [NoSecretKey]", LoggerType.ApiCore);

            // If for WHATEVER reason the connectionDTO is not null here, log it.
            if (ToyboxConnectionDto is not null)
                Logger.LogWarning("Connection DTO is somehow not null, but no secret key is set for the" +
                    " current character. This is a problem.", LoggerType.ApiCore);

            ToyboxConnectionDto = null; // This shouldnt even not be null?

            // Set our new ServerState to NoSecretKey and reject connection.
            ServerStatus = ServerState.NoSecretKey;
            HubConnectionCTS?.Cancel();
            return false;
        }
        else // Log the successful fetch.
        {
            Logger.LogInformation("Secret Key fetched for current character", LoggerType.ApiCore);
            return true;
        }
    }

    protected override void InitializeApiHooks()
    {
        if (GagSpeakHubToybox is null)
            return;

        // otherwise, log that we are initializing the data, and initialize it.
        Logger.LogDebug("Initializing Toybox API Hooks", LoggerType.ApiCore);

        // On the left is the function from the GagspeakHubClient.cs in the API, on the right is the function to be called in the API controller.
        OnReceiveToyboxServerMessage((sev, msg) => _ = Client_ReceiveToyboxServerMessage(sev, msg));
        OnUserReceiveRoomInvite(dto => _ = Client_UserReceiveRoomInvite(dto));
        OnPrivateRoomJoined(dto => _ = Client_PrivateRoomJoined(dto));
        OnPrivateRoomOtherUserJoined(dto => _ = Client_PrivateRoomOtherUserJoined(dto));
        OnPrivateRoomOtherUserLeft(dto => _ = Client_PrivateRoomOtherUserLeft(dto));
        OnPrivateRoomRemovedUser(dto => _ = Client_PrivateRoomRemovedUser(dto));
        OnPrivateRoomUpdateUser(dto => _ = Client_PrivateRoomUpdateUser(dto));
        OnPrivateRoomMessage(dto => _ = Client_PrivateRoomMessage(dto));
        OnPrivateRoomReceiveUserDevice(dto => _ = Client_PrivateRoomReceiveUserDevice(dto));
        OnPrivateRoomDeviceUpdate(dto => _ = Client_PrivateRoomDeviceUpdate(dto));
        OnPrivateRoomClosed(dto => _ = Client_PrivateRoomClosed(dto));

        OnToyboxUserSendOnline(dto => _ = Client_ToyboxUserSendOnline(dto));
        OnToyboxUserSendOffline(dto => _ = Client_ToyboxUserSendOffline(dto));

        // create a new health check token
        HubHealthCTS?.Cancel();
        HubHealthCTS?.Dispose();
        HubHealthCTS = new CancellationTokenSource();
        // Start up our health check loop.
        _ = ClientHealthCheckLoop(HubHealthCTS.Token);
        // set us to initialized (yippee!!!)
        Initialized = true;
    }

    protected override async Task<bool> ConnectionDtoAndVersionIsValid()
    {
        // Grab the latest ConnectionDTO from the server.
        ToyboxConnectionDto = await GetToyboxConnectionDto().ConfigureAwait(false);
        // Validate case where it's null.
        if (ToyboxConnectionDto is null)
        {
            Logger.LogError("UnAuthorized to access VibeService Servers.");
            await Disconnect(ServerState.Unauthorized).ConfigureAwait(false);
            return false;
        }

        Logger.LogTrace("Checking if Client Connection is Outdated", LoggerType.ApiCore);
        Logger.LogInformation(ClientVerString + " - " + ExpectedVerString, LoggerType.ApiCore);
        if (IsClientApiOutdated || IsClientVersionOutdated)
        {
            Mediator.Publish(new NotificationMessage("Client outdated", "Outdated: " + ClientVerString + " - " + ExpectedVerString + "Please keep Gagspeak up-to-date.", NotificationType.Warning));
            Logger.LogInformation("Client Was Outdated in either its API or its Version, Disconnecting.", LoggerType.ApiCore);
            await Disconnect(ServerState.VersionMisMatch).ConfigureAwait(false);
            return false;
        }
        // Client is up to date!
        return true;
    }

    public async Task<bool> CheckToyboxClientHealth() 
        => await GagSpeakHubToybox!.InvokeAsync<bool>(nameof(CheckToyboxClientHealth)).ConfigureAwait(false);
    public async Task<ToyboxConnectionDto> GetToyboxConnectionDto()
        => await GagSpeakHubToybox!.InvokeAsync<ToyboxConnectionDto>(nameof(GetToyboxConnectionDto)).ConfigureAwait(false);

    protected override async Task ClientHealthCheckLoop(CancellationToken ct)
    {
        // Ensure the hub connection is initialized before starting the loop
        if (GagSpeakHubToybox is null)
        {
            Logger.LogError("HubConnection is null. Cannot perform toybox client health check.", LoggerType.Health);
            return;
        }

        // Initialize this while loop with our HubHealthCTS token.
        while (!ct.IsCancellationRequested && GagSpeakHubToybox is not null)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
            Logger.LogTrace("Checking Toybox Server Client Health State", LoggerType.Health);

            // Refresh and update our token, checking for if we will need to reconnect.
            bool requireReconnect = await RefreshToken(ct).ConfigureAwait(false);

            // If we do need to reconnect, it means we have just disconnected from the server.
            // Thus, this check is no longer valid and we should break out of the health check loop.
            if (requireReconnect)
            {
                Logger.LogDebug("Disconnecting From GagSpeakHub-Toybox due to updated token", LoggerType.ApiCore);
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
                break;
            }

            // If the Hub is still valid by this point, then send a ping to the vibeService servers and see if we get a pong back.
            // (we don't need to know the return value, as long as its a valid call we keep our connection maintained)
            if (GagSpeakHubToybox is not null)
                _ = await CheckToyboxClientHealth().ConfigureAwait(false);
            else
            {
                Logger.LogError("VibeService HubConnection became null during health check loop.", LoggerType.Health);
                break;
            }
        }
    }

    protected override void OnLogin()
    {
        Logger.LogInformation("Detected Login!", LoggerType.ApiCore);
    }

    protected override async void OnLogout()
    {
        Logger.LogInformation("Stopping VibeService connection on logout", LoggerType.ApiCore);
        await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        // switch the server state to offline.
        ServerStatus = ServerState.Offline;
    }

    protected override async Task LoadOnlinePairs()
    {
        var uidList = _pairs.GetOnlineUserUids();
        var onlineToyboxPairs = await ToyboxUserGetOnlinePairs(uidList).ConfigureAwait(false);
        // for each online user pair in the online user pairs list
        foreach (var entry in onlineToyboxPairs)
            _pairs.MarkPairToyboxOnline(entry);
        Logger.LogDebug("VibeService Online Pairs: [" + string.Join(", ", onlineToyboxPairs.Select(x => x.AliasOrUID)) + "]", LoggerType.ApiCore);
    }

    /* ================ SignalR Functions ================ */
    protected override void HubInstanceOnClosed(Exception? arg)
    {
        // Log the closure, cancel the health token, and publish that we have been disconnected.
        Logger.LogWarning("GagSpeakHub-Toybox was Closed by its Hub-Instance");
        HubHealthCTS?.Cancel();
        Mediator.Publish(new ToyboxHubDisconnectedMessage());
        ServerStatus = ServerState.Offline;
        // if an argument for this was passed in, we should provide the reason.
        if (arg is not null)
            Logger.LogWarning("There Was an Exception that caused VibeService Closure: " + arg);
    }

    protected override void HubInstanceOnReconnecting(Exception? arg)
    {
        // Cancel our HubHealthCTS, set status to reconnecting, and suppress the next sent notification.
        SuppressNextNotification = true;
        HubHealthCTS?.Cancel();
        ServerStatus = ServerState.Reconnecting;
        Logger.LogWarning("Connection to VibeService Servers was Closed... Reconnecting. (Reason: " + arg, LoggerType.ApiCore);
    }

    protected override async Task HubInstanceOnReconnected()
    {
        // Update our ServerStatus to show that we are reconnecting, and will soon be reconnected.
        ServerStatus = ServerState.Reconnecting;
        try
        {
            // Re-Initialize our API Hooks for the new hub instance.
            InitializeApiHooks();
            // Obtain the new connectionDto and validate if we are out of date or not.
            if (await ConnectionDtoAndVersionIsValid())
            {
                ServerStatus = ServerState.Connected;
                await LoadOnlinePairs().ConfigureAwait(false);
                Mediator.Publish(new ToyboxHubConnectedMessage());
            }
        }
        catch (Exception ex) // Failed to connect, to stop connection.
        {
            Logger.LogError("Failure to obtain Data after reconnection to GagSpeakHub-Toybox. Reason: " + ex);
            await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        }
    }

    protected override void CheckConnection()
    {
        if (ServerStatus is not (ServerState.Connected or ServerState.Connecting or ServerState.Reconnecting))
            throw new InvalidDataException("GagSpeakHub-Toybox Not connected");
    }

    /// <summary> 
    /// A helper method to ensure the action is executed safely, and if an exception is thrown, it is logged.
    /// </summary>
    /// <param name="act">the action to execute</param>
    private void ExecuteSafely(Action act)
    {
        try
        {
            act();
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Error on executing safely");
        }
    }
}
#pragma warning restore MA0040
