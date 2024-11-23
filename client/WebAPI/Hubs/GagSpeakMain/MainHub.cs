using Dalamud.Interface.ImGuiNotification;
using Dalamud.Utility;
using GagSpeak.Achievements;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;
#pragma warning disable MA0040
/// <summary>
/// This connections class maintains the responsibilities for how we connect, disconnect, and reconnect.
/// Manages GagSpeak Hub.
/// </summary>
public sealed partial class MainHub : GagspeakHubBase, IGagspeakHubClient
{
    private readonly HubFactory _hubFactory;
    private readonly PairManager _pairs;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly GagspeakConfigService _mainConfig;
    private readonly ClientCallbackService _clientCallbacks;

    // Cancellation Token Sources
    private CancellationTokenSource HubConnectionCTS;
    private CancellationTokenSource? HubHealthCTS = new();

    public MainHub(ILogger<MainHub> logger, GagspeakMediator mediator, HubFactory hubFactory,
        TokenProvider tokenProvider, PairManager pairs, ServerConfigurationManager serverConfigs, 
        GagspeakConfigService mainConfig, ClientCallbackService callbackService, ClientMonitorService clientService,
        OnFrameworkService frameworkUtils) : base(logger, mediator, tokenProvider, clientService, frameworkUtils)
    {
        _hubFactory = hubFactory;
        _pairs = pairs;
        _serverConfigs = serverConfigs;
        _mainConfig = mainConfig;
        _clientCallbacks = callbackService;

        // Create our CTS for the hub connection
        HubConnectionCTS = new CancellationTokenSource();

        // main hub connection subscribers
        Mediator.Subscribe<MainHubClosedMessage>(this, (msg) => HubInstanceOnClosed(msg.Exception));
        Mediator.Subscribe<MainHubReconnectedMessage>(this, (msg) => _ = HubInstanceOnReconnected());
        Mediator.Subscribe<MainHubReconnectingMessage>(this, (msg) => HubInstanceOnReconnecting(msg.Exception));

        // if we are already logged in, then run the login function
/*        if (_clientService.IsLoggedIn)
            OnLogin();*/
    }

    public static UserData PlayerUserData => ConnectionDto!.User;
    public static string DisplayName => ConnectionDto?.User.AliasOrUID ?? string.Empty;
    public static string UID => ConnectionDto?.User.UID ?? string.Empty;

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
                StaticLogger.Logger.LogDebug("(Hub-Main): New ServerState: " + value + ", prev ServerState: " + _serverStatus, LoggerType.ApiCore);
                _serverStatus = value;
            }
        }
    }

    public static bool IsConnected => ServerStatus is ServerState.Connected;
    public static bool IsOnUnregistered => ServerStatus is ServerState.NoSecretKey;
    public static bool IsServerAlive => ServerStatus is ServerState.Connected or ServerState.Unauthorized or ServerState.Disconnected;
    public bool ClientHasConnectionPaused => _serverConfigs.CurrentServer?.FullPause ?? false;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _ = DisposeAsync();
    }

    /// <summary>
    /// Async Disposal helps ensure we post achievement Save Data prior to disposing of the connection instance.
    /// </summary>
    public async Task DisposeAsync()
    {
        Logger.LogInformation("Disposal Called! Closing down GagSpeakHub-Main!", LoggerType.ApiCore);
        HubHealthCTS?.Cancel();
        await Disconnect(ServerState.Disconnected).ConfigureAwait(false);
        HubConnectionCTS?.Cancel();
    }

    public override async Task Connect()
    {
        Logger.LogInformation("Client Wished to Connect to the server", LoggerType.ApiCore);
        if (!ShouldClientConnect(out string? secretKey))
        {
            Logger.LogInformation("Client was not in a valid state to connect to the server.", LoggerType.ApiCore);
            HubConnectionCTS?.Cancel();
            return;
        }

        Logger.LogInformation("Connection Validation Approved, Creating Connection with [" + _serverConfigs.CurrentServer.ServerName + "]", LoggerType.ApiCore);
        // if the current state was offline, change it to disconnected.
        if (ServerStatus is ServerState.Offline)
            ServerStatus = ServerState.Disconnected;

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

                if (await ConnectionDtoAndVersionIsValid() is false)
                {
                    Logger.LogWarning("Connection was not valid, disconnecting.", LoggerType.ApiCore);
                    return;
                }

                // if we reach here it means we are officially connected to the server
                Logger.LogInformation("Successfully Connected to GagSpeakHub-Main", LoggerType.ApiCore);
                ServerStatus = ServerState.Connected;
                Mediator.Publish(new MainHubConnectedMessage());

                // Load in our initial pairs, then the online ones.
                await LoadInitialPairs().ConfigureAwait(false);
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
        if (ServerStatus is ServerState.Connected && AchievementManager.HadFailedAchievementDataLoad is false && saveAchievements)
        {
            Logger.LogInformation("Sending Final Achievement SaveData Update before Hub Instance Disposal.", LoggerType.Achievements);
            await UserUpdateAchievementData(new(new(UID), AchievementManager.GetSaveDataDtoString()));
        }

        // Set new state to Disconnecting.
        ServerStatus = ServerState.Disconnecting;
        Logger.LogInformation("Disposing of GagSpeakHub-Main's Hub Instance", LoggerType.ApiCore);

        // Obliterate the GagSpeakHub-Main into the ground, erase it out of existence .
        await _hubFactory.DisposeHubAsync(HubType.MainHub).ConfigureAwait(false);

        // If our hub was already initialized by the time we call this, reset all values monitoring it.
        // After this connection revision this should technically ALWAYS be true, so if it isnt log it as an error.
        if (GagSpeakHubMain is not null)
        {
            Logger.LogInformation("Instance disposed of in '_hubFactory', but still exists in MainHub.cs, " +
                "clearing all other variables for [" + _serverConfigs.CurrentServer.ServerName + "]", LoggerType.ApiCore);
            // Clear the Health check so we stop pinging the server, set Initialized to false, publish a disconnect.
            Initialized = false;
            HubHealthCTS?.Cancel();
            Mediator.Publish(new MainHubDisconnectedMessage());
            // set the connectionDto and hub to null.
            GagSpeakHubMain = null;
            ConnectionDto = null;
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
            (string, string) accountDetails = await GagSpeakHubMain.InvokeAsync<(string, string)>("OneTimeUseAccountGeneration");

            Logger.LogInformation("New Account Details Fetched.", LoggerType.ApiCore);
            return accountDetails;
        }
        catch (HubException ex) // Assuming MissingClaimException is a custom exception you've defined
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

        // ensure we have a proper template for the active character.
        if (_serverConfigs.CharacterHasSecretKey() is false)
            if (_serverConfigs.AuthExistsForCurrentLocalContentId() is false)
                _serverConfigs.GenerateAuthForCurrentCharacter();

        // if we are not logged in, we should not be able to connect.
        if (!_clientService.IsLoggedIn)
        {
            Logger.LogDebug("Attempted to connect while not logged in, this shouldnt be possible! Aborting!", LoggerType.ApiCore);
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
            if (ConnectionDto is not null)
                Logger.LogWarning("Connection DTO is somehow not null, but no secret key is set for the" +
                    " current character. This is a problem.", LoggerType.ApiCore);

            ConnectionDto = null; // This shouldnt even not be null?

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
        if (GagSpeakHubMain is null)
            return;

        Logger.LogDebug("Initializing data", LoggerType.ApiCore);
        // [ WHEN GET SERVER CALLBACK ] --------> [PERFORM THIS FUNCTION]
        OnReceiveServerMessage((sev, msg) => _ = Client_ReceiveServerMessage(sev, msg));
        OnReceiveHardReconnectMessage((sev, msg, state) => _ = Client_ReceiveHardReconnectMessage(sev, msg, state));
        OnUpdateSystemInfo(dto => _ = Client_UpdateSystemInfo(dto));

        OnUserAddClientPair(dto => _ = Client_UserAddClientPair(dto));
        OnUserRemoveClientPair(dto => _ = Client_UserRemoveClientPair(dto));
        OnUpdateUserIndividualPairStatusDto(dto => _ = Client_UpdateUserIndividualPairStatusDto(dto));

        OnUserApplyMoodlesByGuid(dto => _ = Client_UserApplyMoodlesByGuid(dto));
        OnUserApplyMoodlesByStatus(dto => _ = Client_UserApplyMoodlesByStatus(dto));
        OnUserRemoveMoodles(dto => _ = Client_UserRemoveMoodles(dto));
        OnUserClearMoodles(dto => _ = Client_UserClearMoodles(dto));

        OnUserUpdateSelfAllGlobalPerms(dto => _ = Client_UserUpdateSelfAllGlobalPerms(dto));
        OnUserUpdateSelfAllUniquePerms(dto => _ = Client_UserUpdateSelfAllUniquePerms(dto));
        OnUserUpdateSelfPairPermsGlobal(dto => _ = Client_UserUpdateSelfPairPermsGlobal(dto));
        OnUserUpdateSelfPairPerms(dto => _ = Client_UserUpdateSelfPairPerms(dto));
        OnUserUpdateSelfPairPermAccess(dto => _ = Client_UserUpdateSelfPairPermAccess(dto));
        OnUserUpdateOtherAllPairPerms(dto => _ = Client_UserUpdateOtherAllPairPerms(dto));
        OnUserUpdateOtherAllGlobalPerms(dto => _ = Client_UserUpdateOtherAllGlobalPerms(dto));
        OnUserUpdateOtherAllUniquePerms(dto => _ = Client_UserUpdateOtherAllUniquePerms(dto));
        OnUserUpdateOtherPairPermsGlobal(dto => _ = Client_UserUpdateOtherPairPermsGlobal(dto));
        OnUserUpdateOtherPairPerms(dto => _ = Client_UserUpdateOtherPairPerms(dto));
        OnUserUpdateOtherPairPermAccess(dto => _ = Client_UserUpdateOtherPairPermAccess(dto));

        OnUserReceiveCharacterDataComposite(dto => _ = Client_UserReceiveCharacterDataComposite(dto));
        OnUserReceiveOwnDataIpc(dto => _ = Client_UserReceiveOwnDataIpc(dto));
        OnUserReceiveOtherDataIpc(dto => _ = Client_UserReceiveOtherDataIpc(dto));
        OnUserReceiveOwnDataAppearance(dto => _ = Client_UserReceiveOwnDataAppearance(dto));
        OnUserReceiveOtherDataAppearance(dto => _ = Client_UserReceiveOtherDataAppearance(dto));
        OnUserReceiveOwnDataWardrobe(dto => _ = Client_UserReceiveOwnDataWardrobe(dto));
        OnUserReceiveOtherDataWardrobe(dto => _ = Client_UserReceiveOtherDataWardrobe(dto));
        OnUserReceiveOwnDataAlias(dto => _ = Client_UserReceiveOwnDataAlias(dto));
        OnUserReceiveOtherDataAlias(dto => _ = Client_UserReceiveOtherDataAlias(dto));
        OnUserReceiveOwnDataToybox(dto => _ = Client_UserReceiveOwnDataToybox(dto));
        OnUserReceiveOtherDataToybox(dto => _ = Client_UserReceiveOtherDataToybox(dto));
        OnUserReceiveOwnLightStorage(dto => _ = Client_UserReceiveOwnLightStorage(dto));
        OnUserReceiveOtherLightStorage(dto => _ = Client_UserReceiveOtherLightStorage(dto));

        OnUserReceiveShockInstruction(dto => _ = Client_UserReceiveShockInstruction(dto));
        OnGlobalChatMessage(dto => _ = Client_GlobalChatMessage(dto));
        OnUserSendOffline(dto => _ = Client_UserSendOffline(dto));
        OnUserSendOnline(dto => _ = Client_UserSendOnline(dto));
        OnUserUpdateProfile(dto => _ = Client_UserUpdateProfile(dto));
        OnDisplayVerificationPopup(dto => _ = Client_DisplayVerificationPopup(dto));

        // create a new health check token
        HubHealthCTS?.Cancel();
        HubHealthCTS?.Dispose();
        HubHealthCTS = new CancellationTokenSource();
        // Start up our health check loop.
        _ = ClientHealthCheckLoop(HubHealthCTS!.Token);
        // set us to initialized (yippee!!!)
        Initialized = true;
    }

    protected override async Task<bool> ConnectionDtoAndVersionIsValid()
    {
        // Grab the latest ConnectionDTO from the server.
        ConnectionDto = await GetConnectionDto().ConfigureAwait(false);
        // Validate case where it's null.
        if (ConnectionDto is null)
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

    public async Task<bool> CheckMainClientHealth() => await GagSpeakHubMain!.InvokeAsync<bool>(nameof(CheckMainClientHealth)).ConfigureAwait(false);
    public async Task<ConnectionDto> GetConnectionDto() => await GagSpeakHubMain!.InvokeAsync<ConnectionDto>(nameof(GetConnectionDto)).ConfigureAwait(false);

    protected override async Task ClientHealthCheckLoop(CancellationToken ct)
    {
        // Ensure the hub connection is initialized before starting the loop
        if (GagSpeakHubMain is null)
        {
            Logger.LogError("HubConnection is null. Cannot perform main client health check.", LoggerType.Health);
            return;
        }

        // Initialize this while loop with our HubHealthCTS token.
        while (!ct.IsCancellationRequested && GagSpeakHubMain is not null)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                Logger.LogTrace("Checking Main Server Client Health State", LoggerType.Health);

                // Refresh and update our token, checking for if we will need to reconnect.
                bool requireReconnect = await RefreshToken(ct).ConfigureAwait(false);

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
        Logger.LogInformation("Starting connection on login");
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
        foreach (var userPair in pairs)
            _pairs.AddUserPair(userPair);

        Logger.LogDebug("Initial Pairs Loaded: [" + string.Join(", ", pairs.Select(x => x.User.AliasOrUID)) + "]", LoggerType.ApiCore);
    }

    protected override async Task LoadOnlinePairs()
    {
        var onlinePairs = await UserGetOnlinePairs().ConfigureAwait(false);
        foreach (var entry in onlinePairs)
            _pairs.MarkPairOnline(entry, sendNotif: false);

        Logger.LogDebug("Online Pairs: [" + string.Join(", ", onlinePairs.Select(x => x.User.AliasOrUID)) + "]", LoggerType.ApiCore);
        Mediator.Publish(new OnlinePairsLoadedMessage());
    }

    /* ================ Main Hub SignalR Functions ================ */
    protected override void HubInstanceOnClosed(Exception? arg)
    {
        // Log the closure, cancel the health token, and publish that we have been disconnected.
        Logger.LogWarning("GagSpeakHub-Main was Closed by its Hub-Instance");
        HubHealthCTS?.Cancel();
        Mediator.Publish(new MainHubDisconnectedMessage());
        ServerStatus = ServerState.Offline;
        // if an argument for this was passed in, we should provide the reason.
        if (arg is not null)
            Logger.LogWarning("There Was an Exception that caused this Hub Closure: " + arg);
    }

    protected override void HubInstanceOnReconnecting(Exception? arg)
    {
        // Cancel our HubHealthCTS, set status to reconnecting, and suppress the next sent notification.
        SuppressNextNotification = true;
        HubHealthCTS?.Cancel();
        ServerStatus = ServerState.Reconnecting;

        // Flag the achievement Manager to not apply SaveData obtained on reconnection if it was caused by an exception.
        if (arg is System.Net.WebSockets.WebSocketException)
        {
            Logger.LogInformation("System closed unexpectedly, flagging Achievement Manager to not set data on reconnection.");
            AchievementManager._lastDisconnectTime = DateTime.UtcNow;
        }

        Logger.LogWarning("Connection to " + _serverConfigs.CurrentServer.ServerName + " Closed... Reconnecting. (Reason: " + arg, LoggerType.ApiCore);
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
                await LoadInitialPairs().ConfigureAwait(false);
                await LoadOnlinePairs().ConfigureAwait(false);
                Mediator.Publish(new MainHubConnectedMessage());
            }
        }
        catch (Exception ex) // Failed to connect, to stop connection.
        {
            Logger.LogError("Failure to obtain Data after reconnection to GagSpeakHub-Main. Reason: " + ex);
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
