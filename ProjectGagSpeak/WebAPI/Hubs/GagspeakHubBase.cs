using GagSpeak.Achievements;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.Services;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Enums;
using GagspeakAPI.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GagSpeak.WebAPI;

public abstract class GagspeakHubBase : DisposableMediatorSubscriberBase
{
    // make any accessible classes in here protected.
    protected readonly TokenProvider _tokenProvider;
    protected readonly ClientMonitorService _clientService;
    protected readonly OnFrameworkService _frameworkUtils;

    public GagspeakHubBase(ILogger logger, GagspeakMediator mediator, 
        TokenProvider tokenProvider, ClientMonitorService clientService, 
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _tokenProvider = tokenProvider;
        _clientService = clientService;
        _frameworkUtils = frameworkUtils;

        // Should fire to all overrides.
        Mediator.Subscribe<DalamudLoginMessage>(this, (_) => OnLogin());
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) => OnLogout());
    }

    // Shared Hub Servers Variables.
    public const string MainServer = "GagSpeak Main";
    public const string MainServiceUri = "wss://gagspeak.kinkporium.studio";

    public static Version ClientVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    public static Version ExpectedClientVersion { get; private set; } = new Version(0, 0, 0, 0); // Set upon each connection
    public static int ExpectedApiVersion { get; private set; } = 0; // Set upon each connection
    public static bool IsClientVersionOutdated => ExpectedClientVersion > ClientVersion;
    public static bool IsClientApiOutdated => ExpectedApiVersion != IGagspeakHub.ApiVersion;
    public static string ClientVerString => "[Client: v" + ClientVersion + " (Api " + IGagspeakHub.ApiVersion + ")]";
    public static string ExpectedVerString => "[Server: v" + ExpectedClientVersion + " (Api " + ExpectedApiVersion + ")]";

    // The GagSpeakHub-Main's ConnectionDto
    private static ConnectionDto? _connectionDto = null;
    public static ConnectionDto? ConnectionDto
    {
        get => _connectionDto;
        set
        {
            _connectionDto = value;
            if (value != null)
            {
                ExpectedClientVersion = _connectionDto?.CurrentClientVersion ?? new Version(0, 0, 0, 0);
                ExpectedApiVersion = _connectionDto?.ServerVersion ?? 0;
            }
        }
    }
    protected static SystemInfoDto? ServerSystemInfo = null;
    protected string? LastToken;
    protected bool SuppressNextNotification = false;
    public static string AuthFailureMessage = string.Empty;
    public static int MainOnlineUsers => ServerSystemInfo?.OnlineUsers ?? 0;
    public static int ToyboxOnlineUsers => ServerSystemInfo?.OnlineToyboxUsers ?? 0;

    /// <summary>
    /// Creates a connection to our GagSpeakHub-HUBTYPE.
    /// <para>
    /// Will not be valid if secret key is not valid, account is not created, not logged in, or if client is connected already.
    /// </para>
    /// </summary>
    public abstract Task Connect();

    /// <summary>
    /// Disconnects us from the GagSpeakHub-HUBTYPE.
    /// Sends a final update of our Achievement Save Data to the server before disconnecting.
    /// </summary>
    public abstract Task Disconnect(ServerState disconnectionReason, bool saveAchievements = true);

    // Unknown purpose yet.
    public virtual Task Reconnect(bool saveAchievements = true) { return Task.CompletedTask; }

    /// <summary>
    /// Determines if we meet the necessary requirements to connect to the hub.
    /// </summary>
    protected abstract bool ShouldClientConnect(out string fetchedSecretKey);

    /// <summary>
    /// Awaits for the player to be present, ensuring that they are logged in before this fires.
    /// There is a possibility we wont need this anymore with the new system, so attempt it without it once this works!
    /// </summary>
    /// <param name="token"> token that when canceled will stop the while loop from occurring, preventing infinite reloads/waits </param>
    protected async Task WaitForWhenPlayerIsPresent(CancellationToken token)
    {
        // wait to connect to the server until they have logged in with their player character and make sure the cancelation token has not yet been called
        while (!await _clientService.IsPresentAsync().ConfigureAwait(false) && !token.IsCancellationRequested)
        {
            Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Initializes the API Hooks for the respective GagSpeakHub-HUBTYPE.
    /// </summary>
    protected abstract void InitializeApiHooks();

    /// <summary>
    /// Checks to see if our client is outdated after fetching the connection DTO. 
    /// Because it's static, we can set it inside and it will apply outside.
    /// </summary>
    /// <returns> True if the client is outdated, false if it is not. </returns>
    protected abstract Task<bool> ConnectionDtoAndVersionIsValid();

    /// <summary>
    /// will pink the GagSpeakHub-HUBTAPE every 30 seconds to update its status in the Redi's Pool. (Ensures connection is maintained)
    /// <para>
    /// If 2 checks fail, totaling 60s timeout, client will get disconnected by the server, requiring us to reconnect.
    /// </para>
    /// </summary>
    /// <param name="ct"> The Cancellation token for the Health Check. YOU MUST MAKE THIS [HubHealthCTS]'s TOKEN </param>
    protected abstract Task ClientHealthCheckLoop(CancellationToken ct);

    /// <summary> 
    /// Ensures that we have a valid Auth entry prior to starting up an initial connection upon login from our current character.
    /// </summary>
    protected abstract void OnLogin();

    /// <summary>
    /// Ensure that we disconnect from and dispose of the GagSpeakHub-Main upon logout.
    /// </summary>
    protected abstract void OnLogout();

    /// <summary> 
    /// Locate the pairs online out of the pairs fetched, and set them to online. 
    /// </summary>
    protected abstract Task LoadOnlinePairs();

    /// <summary> 
    /// Our Hub Instance has notified us that it's Closed, so perform hub-close logic.
    /// </summary>
    protected abstract void HubInstanceOnClosed(Exception? arg);

    /// <summary> 
    /// Our Hub Instance has notified us that it's reconnecting, so perform reconnection logic.
    /// </summary>
    protected abstract void HubInstanceOnReconnecting(Exception? arg);

    /// <summary> 
    /// Our Hub Instance has notified us that it's reconnected, so perform reconnected logic.
    /// </summary>
    protected abstract Task HubInstanceOnReconnected();

    /// <summary>
    /// Grabs the token from our token provider using the currently applied secret key we are using.
    /// <para>
    /// If we are using a different SecretKey from the one we had in our previous check, 
    /// it wont be equal to the lastUsedToken, meaning we need a refresh.
    /// </para>
    /// </summary>
    /// <param name="ct"> A provided CancelationToken that is passed down to stop at any point. </param>
    /// <returns> True if we require a reconnection (token updated, AuthFailure, token refresh failed) </returns>
    protected async Task<bool> RefreshToken(CancellationToken ct)
    {
        Logger.LogTrace("Checking token", LoggerType.JwtTokens);
        bool requireReconnect = false;
        try
        {
            // Grab token from token provider, which uses our secret key that is currently in use
            var token = await _tokenProvider.GetOrUpdateToken(ct).ConfigureAwait(false);
            if (!string.Equals(token, LastToken, StringComparison.Ordinal))
            {
                // The token was different due to changing secret keys between checks. 
                SuppressNextNotification = true;
                requireReconnect = true;
            }
        }
        catch (GagspeakAuthFailureException ex) // Failed to acquire authentication. Means our key was banned or removed.
        {
            Logger.LogDebug("Exception During Token Refresh. (Key was banned or removed from DB)", LoggerType.ApiCore);
            AuthFailureMessage = ex.Reason;
            requireReconnect = true;
        }
        catch (Exception ex) // Other generic exception, force a reconnect.
        {
            Logger.LogWarning(ex, "Could not refresh token, forcing reconnect");
            SuppressNextNotification = true;
            requireReconnect = true;
        }
        // return if it was required or not at the end of this logic.
        return requireReconnect;
    }

    /// <summary> 
    /// A helper function to check if the connection is valid. 
    /// 
    /// To be honest im not quite sure how helpful this function is, 
    /// try removing it later and see if anything faults.
    /// </summary>
    protected abstract void CheckConnection();
}
