using CkCommons;
using GagSpeak.Interop;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagspeakAPI.Chat;
using GagspeakAPI.Connection;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.User;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace GagSpeak.WebAPI;
/// <summary>
///   Facilitates interactions with the GagSpeak Hub connection. <para />
///   To ensure that interactions with vibe lobbies function correctly, <see cref="_hubConnection"/> will be static.
/// </summary>
public partial class MainHub : DisposableMediatorSubscriberBase, IGagspeakHubClient, IHostedService
{
    private readonly ClientAchievements _achievements;
    private readonly HubFactory _hubFactory;
    private readonly TokenProvider _tokenProvider;
    private readonly MainConfig _config;
    private readonly AccountManager _accounts;

    private readonly GlobalChatLog _globalChat;
    private readonly KinksterManager _kinksters;
    private readonly RequestsManager _requests;
    private readonly CollarManager _collarManager;
    private readonly PairService _pairService;

    private readonly IpcCallerLoci _loci;

    private readonly ClientDataListener _clientDatListener;
    private readonly CallbackHandler _callbackHandler;
    private readonly ToyboxStateListener _toyboxListener;
    private readonly PiShockProvider _shockies;
    private readonly ConnectionSyncService _dataSync;

    // Static private accessors (persistant across singleton instantiations for other static accessors.)
    private static ServerState _serverStatus = ServerState.Offline;
    private static Version _clientVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    private static Version _expectedVersion = new Version(0, 0, 0, 0);
    private static int _expectedApiVersion = 0;
    private static bool _apiHooksInitialized = false;

    private static ConnectionResponse? _connectionResponce = null;
    private static ServerInfoResponse? _serverInfo = null;

    // Private accessors (handled within the singleton instance)
    private CancellationTokenSource _hubConnectionCTS = new();
    private CancellationTokenSource? _hubHealthCTS = new();
    private HubConnection? _hubConnection = null;
    private string? _latestToken = null;
    private bool _suppressNextNotification = false;

    public MainHub(ILogger<MainHub> logger,
        GagspeakMediator mediator,
        ClientAchievements achievements,
        HubFactory hubFactory,
        TokenProvider tokenProvider,
        MainConfig config,
        AccountManager accounts,
        GlobalChatLog globalChat,
        KinksterManager kinksters,
        RequestsManager requests,
        CollarManager collarManager,
        PairService pairService,
        IpcCallerLoci loci,
        ClientDataListener clientDatListener,
        CallbackHandler callbackHandler,
        ToyboxStateListener toybox,
        PiShockProvider shockies,
        ConnectionSyncService dataSync)
        : base(logger, mediator)
    {
        _achievements = achievements;
        _hubFactory = hubFactory;
        _tokenProvider = tokenProvider;
        _config = config;
        _accounts = accounts;
        _globalChat = globalChat;
        _kinksters = kinksters;
        _requests = requests;
        _collarManager = collarManager;
        _pairService = pairService;
        _loci = loci;
        _clientDatListener = clientDatListener;
        _callbackHandler = callbackHandler;
        _toyboxListener = toybox;
        _shockies = shockies;
        _dataSync = dataSync;

        // Subscribe to the things.
        Mediator.Subscribe<HubClosedMessage>(this, (msg) => HubInstanceOnClosed(msg.Exception));
        Mediator.Subscribe<ReconnectedMessage>(this, (msg) => _ = HubInstanceOnReconnected());
        Mediator.Subscribe<ReconnectingMessage>(this, (msg) => HubInstanceOnReconnecting(msg.Exception));
        // Mediator.Subscribe<SendTempRequestMessage>(this, _ => OnSendTempRequest(_.TargetUser)); // Temp Request Placeholder.

        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.Logout += (_, _) => OnLogout();

        // If already logged in, begin.
        if (PlayerData.IsLoggedIn)
            OnLogin();
    }

    // Public static accessors.
    public static string ClientVerString => $"[Client: v{_clientVersion} (Api {IGagspeakHub.ApiVersion})]";
    public static string ExpectedVerString => $"[Server: v{_expectedVersion} (Api {_expectedApiVersion})]";
    public static ConnectionResponse? ConnectionResponse
    {
        get => _connectionResponce;
        set
        {
            _connectionResponce = value;
            if (value != null)
            {
                _expectedVersion = _connectionResponce?.CurrentClientVersion ?? new Version(0, 0, 0, 0);
                _expectedApiVersion = _connectionResponce?.ServerVersion ?? 0;
            }
        }
    }

    public static string AuthFailureMessage { get; private set; } = string.Empty;
    public static int OnlineUsers => _serverInfo?.OnlineUsers ?? 0;
    public static UserData OwnUserData => ConnectionResponse!.User;
    public static string AliasOrUID => ConnectionResponse?.User.AliasOrUID ?? string.Empty;
    public static string UID => ConnectionResponse?.User.UID ?? string.Empty;
    public static UserReputation Reputation => ConnectionResponse?.Reputation ?? new();
    public static HashSet<string> SharehubTags = [];
    public static ServerState ServerStatus
    {
        get => _serverStatus;
        private set
        {
            if (_serverStatus != value)
            {
                Svc.Logger.Debug($"[Hub-Main]: New ServerState: {value}, prev ServerState: {_serverStatus}", LoggerType.ApiCore);
                _serverStatus = value;
            }
        }
    }

    public static bool IsConnectionDataSynced => _serverStatus is ServerState.ConnectedDataSynced;
    public static bool IsConnected => _serverStatus is ServerState.Connected or ServerState.ConnectedDataSynced;
    public static bool IsServerAlive => _serverStatus is ServerState.ConnectedDataSynced or ServerState.Connected or ServerState.Unauthorized or ServerState.Disconnected;
    public bool IsFullPaused => _config.ServerPaused;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ClientState.Login -= OnLogin;
        Svc.ClientState.Logout -= (_, _) => OnLogout();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MainHub is starting.");
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("MainHub is stopping. Closing down GagSpeakHub-Main!", LoggerType.ApiCore);
        _hubHealthCTS?.Cancel();
        await Disconnect(ServerState.Disconnected, DisconnectIntent.Shutdown).ConfigureAwait(false);
        _hubConnectionCTS?.Cancel();
        return;
    }

    //private async void OnSendTempRequest(UserData user)
    //{
    //    var msg = $"Temporary Request from {OwnUserData.AnonName}";
    //    var ret = await UserSendRequest(new(new(user.UID), true, msg)).ConfigureAwait(false);
    //    if (ret.ErrorCode is SundouleiaApiEc.Success && ret.Value is { } request)
    //    {
    //        Logger.LogInformation($"Temporary request sent to {user.AnonName}.", LoggerType.RadarData);
    //        // Add to our requests, updating the requests manager.
    //        _requests.AddNewRequest(request);
    //        return;
    //    }

    //    Logger.LogWarning($"Failed to send temporary pair request to {user.AnonName} [{ret.ErrorCode}]", LoggerType.RadarData);
    //}


    private async void OnLogin()
    {
        Logger.LogInformation("Starting connection on login after fully loaded...");
        await GagspeakEx.WaitForPlayerLoading();
        Logger.LogInformation("Client fully loaded in, Connecting.");
        // Run the call to attempt a connection to the server.
        await Connect().ConfigureAwait(false);
    }

    private async void OnLogout()
    {
        Logger.LogInformation("Stopping connection on logout", LoggerType.ApiCore);
        // as we are changing characters, we should fully unload any kinksters from the manager, and other chara spesific data.
        await Disconnect(ServerState.Disconnected, DisconnectIntent.Logout).ConfigureAwait(false);
        // switch the server state to offline.
        ServerStatus = ServerState.Offline;
    }

    private void InitializeApiHooks()
    {
        if (_hubConnection is null)
            return;

        Logger.LogDebug("Initializing data", LoggerType.ApiCore);
        OnServerMessage((sev, msg) => _ = Callback_ServerMessage(sev, msg));
        OnHardReconnectMessage((sev, msg, state) => _ = Callback_HardReconnectMessage(sev, msg, state));
        OnUserFlaggedForReport((kind, msg) => _ = Callback_UserFlaggedForReport(kind, msg));
        OnReputationInfo((rep, msg) => _ = Callback_ReputationInfo(rep, msg));
        OnServerInfo(dto => _ = Callback_ServerInfo(dto));

        OnAddPair(dto => _ = Callback_AddPair(dto));
        OnRemovePair(dto => _ = Callback_RemovePair(dto));
        OnPersistPair(dto => _ = Callback_PersistPair(dto));
        OnAddPairRequest(dto => _ = Callback_AddPairRequest(dto));
        OnRemovePairRequest(dto => _ = Callback_RemovePairRequest(dto));
        OnAddCollarRequest(dto => _ = Callback_AddCollarRequest(dto));
        OnRemoveCollarRequest(dto => _ = Callback_RemoveCollarRequest(dto));

        OnLociDataUpdated(dto => _ = Callback_LociDataUpdated(dto));
        OnLociStatusesUpdate(dto => _ = Callback_LociStatusesUpdate(dto));
        OnLociPresetsUpdate(dto => _ = Callback_LociPresetsUpdate(dto));
        OnLociStatusModified(dto => _ = Callback_LociStatusModified(dto));
        OnLociPresetModified(dto => _ = Callback_LociPresetModified(dto));
        OnLociApplyDataById(dto => _ = Callback_LociApplyDataById(dto));
        OnLociApplyStatus(dto => _ = Callback_LociApplyStatus(dto));
        OnLociRemoveData(dto => _ = Callback_LociRemoveData(dto));
        OnLociClearData(dto => _ = Callback_LociClearData(dto));

        OnBulkChangeGlobal(dto => _ = Callback_BulkChangeGlobal(dto));
        OnBulkChangeUnique(dto => _ = Callback_BulkChangeUnique(dto));
        OnSingleChangeGlobal(dto => _ = Callback_SingleChangeGlobal(dto));
        OnSingleChangeUnique(dto => _ = Callback_SingleChangeUnique(dto));
        OnSingleChangeAccess(dto => _ = Callback_SingleChangeAccess(dto));
        OnStateChangeHardcore(dto => _ = Callback_StateChangeHardcore(dto));

        OnKinksterUpdateComposite(dto => _ = Callback_KinksterUpdateComposite(dto));
        OnKinksterUpdateActiveGag(dto => _ = Callback_KinksterUpdateActiveGag(dto));
        OnKinksterUpdateActiveRestriction(dto => _ = Callback_KinksterUpdateActiveRestriction(dto));
        OnKinksterUpdateActiveRestraint(dto => _ = Callback_KinksterUpdateActiveRestraint(dto));
        OnKinksterUpdateActiveCollar(dto => _ = Callback_KinksterUpdateActiveCollar(dto));
        OnKinksterChangeEnabledItem(dto => _ = Callback_KinksterChangeEnabledItem(dto));
        OnKinksterChangeEnabledGag(dto => _ = Callback_KinksterChangeEnabledGag(dto));
        OnKinksterChangeEnabledToy(dto => _ = Callback_KinksterChangeEnabledToy(dto));
        OnKinksterChangeEnabledItems(dto => _ = Callback_KinksterChangeEnabledItems(dto));
        OnKinksterChangeEnabledGags(dto => _ = Callback_KinksterChangeEnabledGags(dto));
        OnKinksterChangeEnabledToys(dto => _ = Callback_KinksterChangeEnabledToys(dto));
        OnListenerName(dto => _ = Callback_ListenerName(dto));
        OnShockInstruction(dto => _ = Callback_ShockInstruction(dto));
        OnHypnoticEffect(dto => _ = Callback_HypnoticEffect(dto));

        OnKinksterNewGagData(dto => _ = Callback_KinksterNewGagData(dto));
        OnKinksterNewRestrictionData(dto => _ = Callback_KinksterNewRestrictionData(dto));
        OnKinksterNewRestraintData(dto => _ = Callback_KinksterNewRestraintData(dto));
        OnKinksterNewCollarData(dto => _ = Callback_KinksterNewCollarData(dto));
        OnKinksterNewLootData(dto => _ = Callback_KinksterNewLootData(dto));
        OnKinksterNewAliasData(dto => _ = Callback_KinksterNewAliasData(dto));
        OnKinksterNewPatternData(dto => _ = Callback_KinksterNewPatternData(dto));
        OnKinksterNewAlarmData(dto => _ = Callback_KinksterNewAlarmData(dto));
        OnKinksterNewTriggerData(dto => _ = Callback_KinksterNewTriggerData(dto));

        OnKinksterOnline(dto => _ = Callback_KinksterOnline(dto));
        OnKinksterOffline(dto => _ = Callback_KinksterOffline(dto));
        OnUserVanityUpdated(dto => _ = Callback_UserVanityUpdated(dto));
        OnUserProfileUpdated(dto => _ = Callback_UserProfileUpdated(dto));
        OnShowVerification(dto => _ = Callback_ShowVerification(dto));
        OnChatMessageReceived(dto => _ = Callback_ChatMessageReceived(dto));

        OnRoomJoin(dto => _ = Callback_RoomJoin(dto));
        OnRoomLeave(dto => _ = Callback_RoomLeave(dto));
        OnRoomAddInvite(dto => _ = Callback_RoomAddInvite(dto));
        OnRoomHostChanged(dto => _ = Callback_RoomHostChanged(dto));
        OnRoomDeviceUpdate((dto, data) => _ = Callback_RoomDeviceUpdate(dto, data));
        OnRoomIncDataStream(dto => _ = Callback_RoomIncDataStream(dto));
        OnRoomAccessGranted(dto => _ = Callback_RoomAccessGranted(dto));
        OnRoomAccessRevoked(dto => _ = Callback_RoomAccessRevoked(dto));
        OnRoomChatMessage((dto, message) => _ = Callback_RoomChatMessage(dto, message));

        // recreate a new health check token
        _hubHealthCTS = _hubHealthCTS.SafeCancelRecreate();
        // Start up our health check loop.
        _ = ClientHealthCheckLoop(_hubHealthCTS!.Token);
        // set us to initialized (yippee!!!)
        _apiHooksInitialized = true;
    }

    private async Task LoadInitialConnectionData()
    {
        // Update the mention regexes for chat.
        ChatService.UpdateMentionRegex();

        var sw = Stopwatch.StartNew();

        // Gather up our DirectPairs, and then our SanctionPairs,
        var kinksters = await GetAllKinksterPairs().ConfigureAwait(false);
        _kinksters.AddKinksters(kinksters);
        Logger.LogDebug($"Initial Kinksters Loaded: [{string.Join(", ", kinksters.Select(x => x.User.AliasOrUID))}]", LoggerType.ApiCore);
#if DEBUG
        Logger.LogInformation($"[Performance] Fetched pairs in {sw.ElapsedMilliseconds}ms");
        sw.Restart();
#endif

        var onlineKinksters = await GetOnlineKinksters().ConfigureAwait(false);
        foreach (var entry in onlineKinksters)
            _kinksters.MarkKinksterOnline(entry, false);
        Logger.LogDebug($"Online Kinksters: [{string.Join(", ", onlineKinksters.Select(x => x.User.AliasOrUID))}]", LoggerType.ApiCore);
#if DEBUG
        Logger.LogInformation($"[Performance] Retrieved OnlineKinksters in {sw.ElapsedMilliseconds}ms");
        sw.Restart();
#endif

        // Gather up all online data (OnlineUsers, PauseStates, ext)
        var onlineData = await GetOnlineKinksters().ConfigureAwait(false);
        Logger.LogDebug($"Online Users: [{string.Join(", ", onlineData.Select(x => x.User.AliasOrUID))}]", LoggerType.ApiCore);
#if DEBUG
        Logger.LogInformation($"[Performance] Retrieved ConnectionData in {sw.ElapsedMilliseconds}ms");
        sw.Restart();
#endif


        // retrieve any current kinkster requests.
        var requests = await GetRequests().ConfigureAwait(false);
//#if DEBUG
//        // Generate some dummy entries.
//        var dummyRequests = new List<KinksterRequest>();
//        for (int i = 0; i < 5; i++)
//        {
//            dummyRequests.Add(new KinksterRequest(new($"Dummy Sender {i}"), OwnUserData, false, "Wawa", DateTime.Now));
//            dummyRequests.Add(new KinksterRequest(OwnUserData, new($"Dummy Recipient {i}"), false, "Wawa", DateTime.Now));
//        }
//        requests.KinksterRequests.AddRange(dummyRequests);
//#endif
        _requests.AddNewRequests(requests.KinksterRequests);
        _collarManager.LoadServerRequests(requests.CollarRequests);

        Logger.LogDebug($"Loaded {requests.KinksterRequests.Count} KinksterRequests and {requests.CollarRequests.Count} CollarRequests", LoggerType.ApiCore);
#if DEBUG
        Logger.LogInformation($"[Performance] Retrieved Requests in {sw.ElapsedMilliseconds}ms");
        sw.Restart();
#endif
        // Retrieve the DM's and GlobalChat history.
        var chatData = await GetChatHistory(new ChatHistoryRequest([], true, 75)).ConfigureAwait(false);
#if DEBUG
        foreach (var chat in chatData.ChatHistory)
            Logger.LogInformation($"[Performance] Retrieved {chat.Key} ChatHistory with {chat.Value.Count} messages in {sw.ElapsedMilliseconds}ms");
#endif
        _globalChat.LoadChatHistory(chatData.ChatHistory.GetValueOrDefault(ChatlogId.GlobalChat, []));
    }

    public async Task<bool> HealthCheck()
        => await _hubConnection!.InvokeAsync<bool>(nameof(HealthCheck)).ConfigureAwait(false);

    public async Task<ConnectionResponse> GetConnectionResponse()
        => await _hubConnection!.InvokeAsync<ConnectionResponse>(nameof(GetConnectionResponse)).ConfigureAwait(false);

    public async Task<List<KinksterPair>> GetAllKinksterPairs()
        => await _hubConnection!.InvokeAsync<List<KinksterPair>>(nameof(GetAllKinksterPairs)).ConfigureAwait(false);

    public async Task<List<OnlineKinkster>> GetOnlineKinksters()
        => await _hubConnection!.InvokeAsync<List<OnlineKinkster>>(nameof(GetOnlineKinksters)).ConfigureAwait(false);

    public async Task<ActiveRequests> GetRequests()
        => await _hubConnection!.InvokeAsync<ActiveRequests>(nameof(GetRequests)).ConfigureAwait(false);

    public async Task<LobbyAndHubInfoResponse> GetShareHubAndLobbyInfo()
        => await _hubConnection!.InvokeAsync<LobbyAndHubInfoResponse>(nameof(GetShareHubAndLobbyInfo)).ConfigureAwait(false);

    public async Task<ChatHistoryResult> GetChatHistory(ChatHistoryRequest dto)
        => await _hubConnection!.InvokeAsync<ChatHistoryResult>(nameof(GetChatHistory), dto).ConfigureAwait(false);

    public async Task<KinkPlateFull> GetKinkplate(UserDto dto)
        => await _hubConnection!.InvokeAsync<KinkPlateFull>(nameof(GetKinkplate), dto).ConfigureAwait(false);

    public async Task<List<KinkPlateFull>> GetKinkplates(UserListDto dto)
        => await _hubConnection!.InvokeAsync<List<KinkPlateFull>>(nameof(GetKinkplates), dto).ConfigureAwait(false);

    /// <summary>
    ///   Awaits for the player to be present, ensuring that they are 
    ///   logged in before this fires. <para/>
    ///   
    ///   There is a possibility we wont need this anymore with the new system,
    ///   so attempt it without it once this works!
    /// </summary>
    private async Task WaitForWhenPlayerIsPresent(CancellationToken token)
    {
        while (!PlayerData.Available && !token.IsCancellationRequested)
        {
            Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
    }
}
