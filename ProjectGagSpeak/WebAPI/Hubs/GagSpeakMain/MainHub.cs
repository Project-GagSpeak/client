using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.PlayerClient;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using System.Net.WebSockets;
using GagSpeak.CkCommons;

namespace GagSpeak.WebAPI;
#pragma warning disable MA0040 
/// <summary>
/// This connections class maintains the responsibilities for how we connect, disconnect, and reconnect.
/// Manages GagSpeak Hub.
/// </summary>
public sealed partial class MainHub : GagspeakHubBase, IGagspeakHubClient, IHostedService
{
    private readonly HubFactory _hubFactory;
    private readonly ClientAchievements _achievements;
    private readonly KinksterRequests _requests;
    private readonly OwnGlobalsManager _globalPermManager;
    private readonly VisualStateListener _visualListener;
    private readonly PuppeteerListener _puppetListener;
    private readonly ToyboxStateListener _kinkListener;
    private readonly ConnectionSyncService _dataSyncronizer;
    private readonly PairManager _pairs;
    private readonly ServerConfigManager _serverConfigs;
    private readonly MainConfig _mainConfig;

    // Cancellation Token Sources
    private CancellationTokenSource _hubConnectionCTS = new();
    private CancellationTokenSource? _hubHealthCTS = new();
    public const string MainServer = "GagSpeak Main";
    public const string MainServiceUri = "wss://gagspeak.kinkporium.studio";

    public MainHub(
        ILogger<MainHub> logger,
        GagspeakMediator mediator,
        TokenProvider tokenProvider,
        OnFrameworkService frameworkUtils,
        HubFactory hubFactory,
        ClientAchievements achievements,
        KinksterRequests requests,
        OwnGlobalsManager globalPermManager,
        VisualStateListener visualListener,
        PuppeteerListener puppetListener,
        ToyboxStateListener kinkListener,
        ConnectionSyncService dataSyncronizer,
        PairManager pairs,
        ServerConfigManager serverConfigs,
        MainConfig mainConfig)
        : base(logger, mediator, tokenProvider, frameworkUtils)
    {
        _hubFactory = hubFactory;
        _requests = requests;
        _achievements = achievements;
        _globalPermManager = globalPermManager;
        _visualListener = visualListener;
        _puppetListener = puppetListener;
        _kinkListener = kinkListener;
        _dataSyncronizer = dataSyncronizer;
        _pairs = pairs;
        _serverConfigs = serverConfigs;
        _mainConfig = mainConfig;

        // main hub connection subscribers
        Mediator.Subscribe<MainHubClosedMessage>(this, (msg) => HubInstanceOnClosed(msg.Exception));
        Mediator.Subscribe<MainHubReconnectedMessage>(this, (msg) => _ = HubInstanceOnReconnected());
        Mediator.Subscribe<MainHubReconnectingMessage>(this, (msg) => HubInstanceOnReconnecting(msg.Exception));
    }

    public static UserData PlayerUserData => ConnectionResponse!.User;
    public static string DisplayName => ConnectionResponse?.User.AliasOrUID ?? string.Empty;
    public static string UID => ConnectionResponse?.User.UID ?? string.Empty;

    // Information gathered from our hub connection.
    private HubConnection? GagSpeakHubMain;
    public bool Initialized { get; private set; } = false;
    private static ServerState _serverStatus = ServerState.Offline;
    public static ServerState ServerStatus
    {
        get => _serverStatus;
        private set
        {
            if (_serverStatus != value)
            {
                Svc.Logger.Debug("(Hub-Main): New ServerState: " + value + ", prev ServerState: " + _serverStatus, LoggerType.ApiCore);
                _serverStatus = value;
            }
        }
    }

    public static bool IsConnectionDataSynced => ServerStatus is ServerState.ConnectedDataSynced;
    public static bool IsConnected => ServerStatus is ServerState.Connected or ServerState.ConnectedDataSynced;
    public static bool IsOnUnregistered => ServerStatus is ServerState.NoSecretKey;
    public static bool IsServerAlive => ServerStatus is ServerState.ConnectedDataSynced or ServerState.Connected or ServerState.Unauthorized or ServerState.Disconnected;
    public bool ClientHasConnectionPaused => _serverConfigs.ServerStorage.FullPause;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MainHub is starting.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MainHub is stopping. Closing down GagSpeakHub-Main!", LoggerType.ApiCore);
        _hubHealthCTS?.Cancel();
        await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        _hubConnectionCTS?.Cancel();
        return;
    }

    public override async Task Connect()
    {
        Logger.LogInformation("Client Wished to Connect to the server", LoggerType.ApiCore);
        if (!ShouldClientConnect(out var secretKey))
        {
            Logger.LogInformation("Client was not in a valid state to connect to the server.", LoggerType.ApiCore);
            _hubConnectionCTS?.Cancel();
            return;
        }

        Logger.LogInformation("Connection Validation Approved, Creating Connection with [" + _serverConfigs.ServerStorage.ServerName + "]", LoggerType.ApiCore);
        // if the current state was offline, change it to disconnected.
        if (ServerStatus is ServerState.Offline)
            ServerStatus = ServerState.Disconnected;

        // Debug the current state here encase shit hits the fan.
        Logger.LogDebug("Current ServerState during this Connection Attempt: " + ServerStatus, LoggerType.ApiCore);
        // Recreate the ConnectionCTS.
        _hubConnectionCTS = _hubConnectionCTS.CancelRecreate();
        var connectionToken = _hubConnectionCTS.Token;

        // While we are still waiting to connect to the server, do the following:
        while (!IsConnected && !connectionToken.IsCancellationRequested)
        {
            AuthFailureMessage = string.Empty;

            Logger.LogInformation("Attempting to Connect to GagSpeakHub-Main", LoggerType.ApiCore);
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
                if (connectionToken.IsCancellationRequested)
                {
                    Logger.LogWarning("GagSpeakHub-Main's ConnectionToken was cancelled during connection. Aborting!", LoggerType.ApiCore);
                    return;
                }

                // Init & Startup GagSpeakHub-Main
                GagSpeakHubMain = _hubFactory.GetOrCreate(connectionToken);
                InitializeApiHooks();
                await GagSpeakHubMain.StartAsync(connectionToken).ConfigureAwait(false);

                if (await ConnectionResponseAndVersionIsValid() is false)
                {
                    Logger.LogWarning("Connection was not valid, disconnecting.");
                    return;
                }

                // if we reach here it means we are officially connected to the server
                Logger.LogInformation("Successfully Connected to GagSpeakHub-Main", LoggerType.ApiCore);
                ServerStatus = ServerState.Connected;

                // Load in our initial pairs, then the online ones.
                await LoadInitialPairs().ConfigureAwait(false);
                await LoadOnlinePairs().ConfigureAwait(false);
                await LoadKinksterRequests().ConfigureAwait(false);
                await _dataSyncronizer.SetClientDataForProfile().ConfigureAwait(false);
                // once data is syncronized, update the serverStatus.
                ServerStatus = ServerState.ConnectedDataSynced;
                Mediator.Publish(new MainHubConnectedMessage());

                // Update our current authentication to reflect the information provided.
                _serverConfigs.UpdateAuthentication(secretKey, ConnectionResponse!);
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
                    Logger.LogWarning("This HTTP Exception was caused by GagSpeakAuthFailure. Message was: " + AuthFailureMessage, LoggerType.ApiCore);
                    await Disconnect(ServerState.Unauthorized).ConfigureAwait(false);
                    return; // (Prevent further reconnections)
                }

                try
                {
                    // Another HTTP Exception type, so disconnect, then attempt reconnection.
                    Logger.LogWarning("Failed to establish connection, retrying");
                    await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
                    // Reconnect in 5-20 seconds. (prevents server overload)
                    ServerStatus = ServerState.Reconnecting;
                    await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 20)), connectionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning("Operation Cancelled during Reconnection Attempt");
                    return; // (Prevent further reconnections)
                }

            }
            catch (InvalidOperationException ex)
            {
                Logger.LogWarning("InvalidOperationException on connection: " + ex.Message);
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
                return; // (Prevent further reconnections)
            }
            catch (Exception ex)
            {
                try
                {
                    Logger.LogWarning("Exception on Connection (Attempting Reconnection soon): " + ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(new Random().Next(5, 20)), connectionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    Logger.LogWarning("Operation Cancelled during Reconnection Attempt");
                    return; // (Prevent further reconnections)
                }
            }
        }
    }

    public override async Task Disconnect(ServerState disconnectionReason, bool saveAchievements = true)
    {
        // If our current state was Connected, be sure to fire, or at least attempt to fire, a final achievement save prior to disconnection.
        if (IsConnected && saveAchievements)
        {
            // only perform the following if SaveData is in a valid state for uploading on Disconnect.
            if(ClientAchievements.HasValidData && !ClientAchievements.HadUnhandledDC)
            {
                Logger.LogDebug("Sending Final Achievement SaveData Update before Hub Instance Disposal.", LoggerType.Achievements);
                await UserUpdateAchievementData(new(PlayerUserData, _achievements.SerializeData()));
            }
            else
            {
                Logger.LogWarning("CanUploadSaveData was false during disconnect. Skipping final save on disconnect.", LoggerType.Achievements);
            }
        }

        // Set new state to Disconnecting.
        ServerStatus = ServerState.Disconnecting;
        Logger.LogInformation("Disposing of GagSpeakHub-Main's Hub Instance", LoggerType.ApiCore);

        // Obliterate the GagSpeakHub-Main into the ground, erase it out of existence .
        await _hubFactory.DisposeHubAsync().ConfigureAwait(false);

        // If our hub was already initialized by the time we call this, reset all values monitoring it.
        // After this connection revision this should technically ALWAYS be true, so if it isnt log it as an error.
        if (GagSpeakHubMain is not null)
        {
            Logger.LogInformation("Instance disposed of in '_hubFactory', but still exists in MainHub.cs, " +
                "clearing all other variables for [" + _serverConfigs.ServerStorage.ServerName + "]", LoggerType.ApiCore);
            // Clear the Health check so we stop pinging the server, set Initialized to false, publish a disconnect.
            Initialized = false;
            _hubHealthCTS?.Cancel();
            Mediator.Publish(new MainHubDisconnectedMessage());
            // set the ConnectionResponse and hub to null.
            GagSpeakHubMain = null;
            ConnectionResponse = null;
        }

        // Update our server state to the necessary reason
        Logger.LogInformation("GagSpeakHub-Main disconnected due to: [" + disconnectionReason + "]", LoggerType.ApiCore);
        ServerStatus = disconnectionReason;
    }

    public override async Task Reconnect(bool saveAchievements = true)
    {
        // Disconnect, wait 3 seconds, then connect.
        await Disconnect(ServerState.Disconnected, saveAchievements).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(5));
        await Connect().ConfigureAwait(false);
    }

    /// <summary>
    /// A Temporary connection established without the Authorized Claim, but rather TemporaryAccess claim.
    /// This allows us to generate a fresh UID & SecretKey for our account upon its first creation.
    /// </summary>
    /// <returns> ([new UID for character],[new secretKey]) </returns>
    public async Task<(string, string)> FetchFreshAccountDetails()
    {
        // We are creating a temporary connection, so have an independent CTS for this.
        var freshAccountCTS = new CancellationTokenSource().Token;
        try
        {
            // Set our connection state to connecting.
            ServerStatus = ServerState.Connecting;
            Logger.LogDebug("Connecting to MainHub to fetch newly generated Account Details and disconnect.", LoggerType.ApiCore);
            try
            {
                // Fetch a fresh token for our brand new account. Catch any authentication exceptions that may occur.
                Logger.LogTrace("Fetching a fresh token for the new account from TokenProvider.", LoggerType.JwtTokens);
                LastToken = await _tokenProvider.GetOrUpdateToken(freshAccountCTS).ConfigureAwait(false);
            }
            catch (GagspeakAuthFailureException ex)
            {
                AuthFailureMessage = ex.Reason;
                throw new HttpRequestException("Error during authentication", ex, System.Net.HttpStatusCode.Unauthorized);
            }

            // Wait for player to be visible before we start the hub connection.
            await WaitForWhenPlayerIsPresent(freshAccountCTS);

            // Create instance of hub connection (with our temporary access token for the fresh account)
            Logger.LogDebug("Starting created hub instance", LoggerType.ApiCore);
            GagSpeakHubMain = _hubFactory.GetOrCreate(freshAccountCTS);
            await GagSpeakHubMain.StartAsync(freshAccountCTS).ConfigureAwait(false);

            // Obtain the fresh account details.
            Logger.LogDebug("Calling OneTimeUseAccountGeneration.", LoggerType.ApiCore);
            var accountDetails = await GagSpeakHubMain.InvokeAsync<(string, string)>("OneTimeUseAccountGeneration");

            Logger.LogInformation("New Account Details Fetched.", LoggerType.ApiCore);
            return accountDetails;
        }
        catch (HubException ex)
        {
            Logger.LogError($"Error fetching new account details: Missing claim in token. {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error fetching new account details: {ex.StackTrace}", LoggerType.ApiCore);
            throw;
        }
        finally
        {
            Logger.LogInformation("Disposing of GagSpeakHub-Main after obtaining account details.", LoggerType.ApiCore);
            if (GagSpeakHubMain is not null && GagSpeakHubMain.State is HubConnectionState.Connected)
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        }
    }

    protected override bool ShouldClientConnect(out string fetchedSecretKey)
    {
        fetchedSecretKey = string.Empty;

        // if we are not logged in, we should not be able to connect.
        if (PlayerData.IsLoggedIn is false)
        {
            Logger.LogDebug("Attempted to connect while not logged in, this shouldnt be possible! Aborting!", LoggerType.ApiCore);
            return false;
        }

        // if we have not yet made an account, abort this connection.
        if (_serverConfigs.AuthCount() <= 0)
        {
            Logger.LogDebug("No Authentications created. No Primary Account or Alt Account to connect with. Aborting!", LoggerType.ApiCore);
            return false;
        }

        // ensure we have a proper template for the active character.
        if (_serverConfigs.CharacterHasSecretKey() is false && _serverConfigs.AuthExistsForCurrentLocalContentId() is false)
        {
            // Generate a new auth entry for the current character if the primary one has already been made (this is for an alt basically)
            _serverConfigs.GenerateAuthForCurrentCharacter();
        }

        // If the client wishes to not be connected to the server, return.
        if (ClientHasConnectionPaused)
        {
            Logger.LogDebug("You have your connection to server paused. Stopping any attempt to connect!", LoggerType.ApiCore);
            return false;
        }

        // Obtain stored ServerKey for the current Character we are logged into.
        fetchedSecretKey = _serverConfigs.GetSecretKeyForCharacter() ?? string.Empty;
        if (fetchedSecretKey.IsNullOrEmpty())
        {
            // log a warning that no secret key is set for the current character
            Logger.LogWarning("No secret key set for current character, aborting Connection with [NoSecretKey]", LoggerType.ApiCore);

            // If for WHATEVER reason the ConnectionResponse is not null here, log it.
            if (ConnectionResponse is not null)
                Logger.LogWarning("Connection DTO is somehow not null, but no secret key is set for the" +
                    " current character. This is a problem.", LoggerType.ApiCore);

            ConnectionResponse = null; // This shouldnt even not be null?

            // Set our new ServerState to NoSecretKey and reject connection.
            ServerStatus = ServerState.NoSecretKey;
            _hubConnectionCTS?.Cancel();
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
        if (GagSpeakHubMain is null)
            return;

        Logger.LogDebug("Initializing data", LoggerType.ApiCore);
        // [ WHEN GET SERVER CALLBACK ] --------> [PERFORM THIS FUNCTION]
        OnServerMessage((sev, msg) => _ = Callback_ServerMessage(sev, msg));
        OnHardReconnectMessage((sev, msg, state) => _ = Callback_HardReconnectMessage(sev, msg, state));
        OnServerInfo(dto => _ = Callback_ServerInfo(dto));

        OnAddClientPair(dto => _ = Callback_AddClientPair(dto));
        OnRemoveClientPair(dto => _ = Callback_RemoveClientPair(dto));
        OnAddPairRequest(dto => _ = Callback_AddPairRequest(dto));
        OnRemovePairRequest(dto => _ = Callback_RemovePairRequest(dto));

        OnApplyMoodlesByGuid(dto => _ = Callback_ApplyMoodlesByGuid(dto));
        OnApplyMoodlesByStatus(dto => _ = Callback_ApplyMoodlesByStatus(dto));
        OnRemoveMoodles(dto => _ = Callback_RemoveMoodles(dto));
        OnClearMoodles(dto => _ = Callback_ClearMoodles(dto));

        OnBulkChangeAll(dto => _ = Callback_BulkChangeAll(dto));
        OnBulkChangeGlobal(dto => _ = Callback_BulkChangeGlobal(dto));
        OnBulkChangeUnique(dto => _ = Callback_BulkChangeUnique(dto));
        OnSingleChangeGlobal(dto => _ = Callback_SingleChangeGlobal(dto));
        OnSingleChangeUnique(dto => _ = Callback_SingleChangeUnique(dto));
        OnSingleChangeAccess(dto => _ = Callback_SingleChangeAccess(dto));

        OnKinksterUpdateComposite(dto => _ = Callback_KinksterUpdateComposite(dto));
        OnKinksterUpdateIpc(dto => _ = Callback_KinksterUpdateIpc(dto));
        OnKinksterUpdateGagSlot(dto => _ = Callback_KinksterUpdateGagSlot(dto));
        OnKinksterUpdateRestriction(dto => _ = Callback_KinksterUpdateRestriction(dto));
        OnKinksterUpdateRestraint(dto => _ = Callback_KinksterUpdateRestraint(dto));
        OnKinksterUpdateCursedLoot(dto => _ = Callback_KinksterUpdateCursedLoot(dto));
        OnKinksterUpdateAliasGlobal(dto => _ = Callback_KinksterUpdateAliasGlobal(dto));
        OnKinksterUpdateAliasUnique(dto => _ = Callback_KinksterUpdateAliasUnique(dto));
        OnKinksterUpdateToybox(dto => _ = Callback_KinksterUpdateToybox(dto));
        OnKinksterUpdateLightStorage(dto => _ = Callback_KinksterUpdateLightStorage(dto));
        OnListenerName((user, name) => _ = Callback_ListenerName(user, name));
        OnShockInstruction(dto => _ = Callback_ShockInstruction(dto));

        OnChatMessageGlobal(dto => _ = Callback_ChatMessageGlobal(dto));
        OnKinksterOffline(dto => _ = Callback_KinksterOffline(dto));
        OnKinksterOnline(dto => _ = Callback_KinksterOnline(dto));
        OnProfileUpdated(dto => _ = Callback_ProfileUpdated(dto));
        OnShowVerification(dto => _ = Callback_ShowVerification(dto));

        OnRoomJoin(dto => _ = Callback_RoomJoin(dto));
        OnRoomLeave(dto => _ = Callback_RoomLeave(dto));
        OnRoomDeviceUpdate((dto, data) => _ = Callback_RoomDeviceUpdate(dto, data));
        OnRoomIncDataStream(dto => _ = Callback_RoomIncDataStream(dto));
        OnRoomAccessGranted(dto => _ = Callback_RoomAccessGranted(dto));
        OnRoomAccessRevoked(dto => _ = Callback_RoomAccessRevoked(dto));
        OnRoomChatMessage((dto, message) => _ = Callback_RoomChatMessage(dto, message));

        // create a new health check token
        _hubHealthCTS = _hubHealthCTS?.CancelRecreate();
        // Start up our health check loop.
        _ = ClientHealthCheckLoop(_hubHealthCTS!.Token);
        // set us to initialized (yippee!!!)
        Initialized = true;
    }

    protected override async Task<bool> ConnectionResponseAndVersionIsValid()
    {
        // Grab the latest ConnectionResponse from the server.
        ConnectionResponse = await GetConnectionResponse().ConfigureAwait(false);
        // Validate case where it's null.
        if (ConnectionResponse is null)
        {
            Logger.LogError("Your SecretKey is likely no longer valid for this character and it failed to properly connect." + Environment.NewLine
                + "This likely means the key no longer exists in the database, you have been banned, or need to make a new one." + Environment.NewLine
                + "If this key happened to be your primary key and you cannot recover it, contact cordy.");
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

    public async Task<bool> CheckMainClientHealth() 
        => await GagSpeakHubMain!.InvokeAsync<bool>(nameof(CheckMainClientHealth)).ConfigureAwait(false);
    public async Task<ConnectionResponse> GetConnectionResponse() 
        => await GagSpeakHubMain!.InvokeAsync<ConnectionResponse>(nameof(GetConnectionResponse)).ConfigureAwait(false);

    protected override async Task ClientHealthCheckLoop(CancellationToken ct)
    {
        // Ensure the hub connection is initialized before starting the loop
        if (GagSpeakHubMain is null)
        {
            Logger.LogError("HubConnection is null. Cannot perform main client health check.", LoggerType.Health);
            return;
        }

        // Initialize this while loop with our _hubHealthCTS token.
        while (!ct.IsCancellationRequested && GagSpeakHubMain is not null)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                Logger.LogTrace("Checking Main Server Client Health State", LoggerType.Health);

                // Refresh and update our token, checking for if we will need to reconnect.
                var requireReconnect = await RefreshToken(ct).ConfigureAwait(false);

                // If we do need to reconnect, it means we have just disconnected from the server.
                // Thus, this check is no longer valid and we should break out of the health check loop.
                if (requireReconnect)
                {
                    Logger.LogDebug("Disconnecting From GagSpeakHub-Main due to updated token", LoggerType.ApiCore);
                    await Reconnect().ConfigureAwait(false);
                    break;
                }

                // If the Hub is still valid by this point, then send a ping to the gagspeak servers and see if we get a pong back.
                // (we don't need to know the return value, as long as its a valid call we keep our connection maintained)
                if (GagSpeakHubMain is not null)
                {
                    await CheckMainClientHealth().ConfigureAwait(false);
                }
                else
                {
                    Logger.LogError("HubConnection became null during health check loop.", LoggerType.Health);
                    break;
                }
            }
            catch (TaskCanceledException)
            {
                // Task was canceled, exit the loop gracefully
                Logger.LogInformation("Client health check loop was canceled.", LoggerType.Health);
                break;
            }
            catch (Exception ex)
            {
                // Log any other exceptions
                Logger.LogError($"Exception in ClientHealthCheckLoop: {ex}", LoggerType.Health);
            }
        }
    }

    protected override async void OnLogin()
    {
        Logger.LogWarning("Starting connection on login");
        // Run the call to attempt a connection to the server.
        await Connect().ConfigureAwait(false);
    }

    protected override async void OnLogout()
    {
        Logger.LogInformation("Stopping connection on logout", LoggerType.ApiCore);
        await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        // switch the server state to offline.
        ServerStatus = ServerState.Offline;
    }


    /// <summary> 
    /// Load the initial pairs our client has added
    /// </summary>
    private async Task LoadInitialPairs()
    {
        // Retrieve the pairs from the server that we have added, and add them to the pair manager.
        var pairs = await UserGetPairedClients().ConfigureAwait(false);
        _pairs.AddUserPair(pairs);

        Logger.LogDebug("Initial Pairs Loaded: [" + string.Join(", ", pairs.Select(x => x.User.AliasOrUID)) + "]", LoggerType.ApiCore);
    }

    protected override async Task LoadOnlinePairs()
    {
        var onlinePairs = await UserGetOnlinePairs().ConfigureAwait(false);
        foreach (var entry in onlinePairs)
            _pairs.MarkPairOnline(entry, sendNotification: false);

        Logger.LogDebug("Online Pairs: [" + string.Join(", ", onlinePairs.Select(x => x.User.AliasOrUID)) + "]", LoggerType.ApiCore);
    }

    private async Task LoadKinksterRequests()
    {
        // retrieve any current kinkster requests.
        var requests = await UserGetPairRequests().ConfigureAwait(false);
        _requests.CurrentRequests = requests.ToHashSet();
        Logger.LogDebug("Kinkster Requests Recieved. Found [" + requests.Count + "]", LoggerType.ApiCore);
        Mediator.Publish(new RefreshUiMessage());
    }

    /* ================ Main Hub SignalR Functions ================ */
    protected override void HubInstanceOnClosed(Exception? arg)
    {
        // Log the closure, cancel the health token, and publish that we have been disconnected.
        Logger.LogWarning("GagSpeakHub-Main was Closed by its Hub-Instance");
        _hubHealthCTS?.Cancel();
        Mediator.Publish(new MainHubDisconnectedMessage());
        ServerStatus = ServerState.Offline;
        // if an argument for this was passed in, we should provide the reason.
        if (arg is not null)
            Logger.LogWarning("There Was an Exception that caused this Hub Closure: " + arg);
    }

    protected override void HubInstanceOnReconnecting(Exception? arg)
    {
        // Cancel our _hubHealthCTS, set status to reconnecting, and suppress the next sent notification.
        SuppressNextNotification = true;
        _hubHealthCTS?.Cancel();
        ServerStatus = ServerState.Reconnecting;

        // Flag the achievement Manager to not apply SaveData obtained on reconnection if it was caused by an exception.
        if (arg is WebSocketException webException)
        {
            Logger.LogInformation("System closed unexpectedly, flagging Achievement Manager to not set data on reconnection.");
            _achievements.HadUnhandledDisconnect(webException);
        }

        Logger.LogWarning("Connection to " + _serverConfigs.ServerStorage.ServerName + " Closed... Reconnecting. (Reason: " + arg);
    }

    protected override async Task HubInstanceOnReconnected()
    {
        // Update our ServerStatus to show that we are reconnecting, and will soon be reconnected.
        ServerStatus = ServerState.Reconnecting;
        try
        {
            // Re-Initialize our API Hooks for the new hub instance.
            InitializeApiHooks();
            // Obtain the new ConnectionResponse and validate if we are out of date or not.
            if (await ConnectionResponseAndVersionIsValid())
            {
                ServerStatus = ServerState.Connected;
                await LoadInitialPairs().ConfigureAwait(false);
                await LoadOnlinePairs().ConfigureAwait(false);
                await _dataSyncronizer.SetClientDataForProfile().ConfigureAwait(false);
                // once data is syncronized, update the serverStatus.
                ServerStatus = ServerState.ConnectedDataSynced;
                Mediator.Publish(new MainHubConnectedMessage());
            }
        }
        catch (Exception ex) // Failed to connect, to stop connection.
        {
            Logger.LogError("Failure to obtain Data after reconnection to GagSpeakHub-Main. Reason: " + ex);
            // disconnect if a non-websocket related issue, otherwise, reconnect.
            if (ex is not WebSocketException || ex is not TimeoutException)
                {
                Logger.LogWarning("Disconnecting from GagSpeakHub-Main after failed reconnection in HubInstanceOnReconnected(). Websocket/Timeout Reason: " + ex);
                await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
            }
            else
            {
                Logger.LogWarning("Reconnecting to GagSpeakHub-Main after failed reconnection in HubInstanceOnReconnected(). Websocket/Timeout Reason: " + ex);
                await Reconnect().ConfigureAwait(false);
            }
        }
    }
}
#pragma warning restore MA0040
