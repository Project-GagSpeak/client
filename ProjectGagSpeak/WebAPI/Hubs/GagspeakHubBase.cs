using GagSpeak.Services.Mediator;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using System.Reflection;
using GagSpeak.PlayerClient;
using GagSpeak.Services;

namespace GagSpeak.WebAPI;

/// <summary> For right now, this is abstract to help support additional Hubs. This will no longer be the case </summary>
/// <remarks> Later you can optimize this predicament, but for now it works just fine. </remarks>
public abstract class GagspeakHubBase : DisposableMediatorSubscriberBase
{
    // make any accessible classes in here protected.
    protected readonly TokenProvider _tokenProvider;
    protected readonly PlayerData _player;
    protected readonly OnFrameworkService _frameworkUtils;

    public GagspeakHubBase(ILogger logger, GagspeakMediator mediator, 
        TokenProvider tokenProvider, PlayerData clientMonitor, 
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _tokenProvider = tokenProvider;
        _player = clientMonitor;
        _frameworkUtils = frameworkUtils;

        // Should fire to all overrides.
        Mediator.Subscribe<DalamudLoginMessage>(this, (_) => OnLogin());
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) => OnLogout());
    }
    protected static Version ClientVersion => Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
    protected static Version ExpectedClientVersion = new Version(0, 0, 0, 0); // Set upon each connection
    protected static int ExpectedApiVersion = 0; // Set upon each connection
    protected static bool IsClientVersionOutdated => ExpectedClientVersion > ClientVersion;
    protected static bool IsClientApiOutdated => ExpectedApiVersion != IGagspeakHub.ApiVersion;
    public static string ClientVerString => "[Client: v" + ClientVersion + " (Api " + IGagspeakHub.ApiVersion + ")]";
    public static string ExpectedVerString => "[Server: v" + ExpectedClientVersion + " (Api " + ExpectedApiVersion + ")]";

    private static ConnectionResponse? _ConnectionResponse = null;
    public static ConnectionResponse? ConnectionResponse
    {
        get => _ConnectionResponse;
        set
        {
            _ConnectionResponse = value;
            if (value != null)
            {
                ExpectedClientVersion = _ConnectionResponse?.CurrentClientVersion ?? new Version(0, 0, 0, 0);
                ExpectedApiVersion = _ConnectionResponse?.ServerVersion ?? 0;
            }
        }
    }
    protected static ServerInfoResponse? ServerInfo = null;
    protected string? LastToken;
    protected bool SuppressNextNotification = false;
    public static string AuthFailureMessage = string.Empty;
    public static int MainOnlineUsers => ServerInfo?.OnlineUsers ?? 0;

    /// <summary> Creates a connection to our GagSpeakHub. </summary>
    /// <remarks> Not valid if secret key is not valid, account is not created, not logged in, or if client is connected already. </remarks>
    public abstract Task Connect();

    /// <summary> Disconnects us from the GagSpeakHub. </summary>
    /// <remarks> Sends a final update of our Achievement Save Data to the server before disconnecting. </remarks>
    public abstract Task Disconnect(ServerState disconnectionReason, bool saveAchievements = true);

    /// <summary> Performs an automatic disconnection then reconnection, if overridden. </summary>
    public virtual Task Reconnect(bool saveAchievements = true) { return Task.CompletedTask; }

    /// <summary> Determines if we meet the necessary requirements to connect to the hub. </summary>
    protected abstract bool ShouldClientConnect(out string fetchedSecretKey);

    /// <summary> Initializes the API Hooks for the respective GagSpeakHub. </summary>
    protected abstract void InitializeApiHooks();

    /// <summary> Checks to see if our client is outdated after fetching the connection DTO. </summary>
    /// <returns> True if the client is outdated, false if it is not. </returns>
    protected abstract Task<bool> ConnectionResponseAndVersionIsValid();

    /// <summary> Pings GagSpeakHub every 30s to update its status in the Redi's Pool. (Ensures connection is maintained) </summary>
    /// <remarks> If 2 checks fail, totaling 60s timeout, client will get disconnected by the server, requiring us to reconnect. </remarks>
    /// <param name="ct"> The Cancellation token for the Health Check. </param>
    protected abstract Task ClientHealthCheckLoop(CancellationToken ct);

    /// <summary> Establishes a connection upon login. </summary>
    protected abstract void OnLogin();

    /// <summary> Ensure that we disconnect from and dispose of the GagSpeakHub-Main upon logout. </summary>
    protected abstract void OnLogout();

    /// <summary> Locate the pairs online out of the pairs fetched, and set them to online. </summary>
    protected abstract Task LoadOnlinePairs();

    /// <summary> Our Hub Instance has notified us that it's Closed, so perform hub-close logic. </summary>
    protected abstract void HubInstanceOnClosed(Exception? arg);

    /// <summary> Our Hub Instance has notified us that it's reconnecting, so perform reconnection logic. </summary>
    protected abstract void HubInstanceOnReconnecting(Exception? arg);

    /// <summary> Our Hub Instance has notified us that it's reconnected, so perform reconnected logic. </summary>
    protected abstract Task HubInstanceOnReconnected();

    /// <summary> Grabs the token from our token provider using the currently applied secret key we are using.
    /// <remarks> If using a different SecretKey from the previous check, it wont be equal to the lastUsedToken, and will refresh. </remarks>
    /// <returns> True if we require a reconnection (token updated, AuthFailure, token refresh failed) </returns>
    protected async Task<bool> RefreshToken(CancellationToken ct)
    {
        Logger.LogTrace("Checking token", LoggerType.JwtTokens);
        var requireReconnect = false;
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
    /// Awaits for the player to be present, ensuring that they are logged in before this fires.
    /// There is a possibility we wont need this anymore with the new system, so attempt it without it once this works!
    /// </summary>
    /// <param name="token"> token that when canceled will stop the while loop from occurring, preventing infinite reloads/waits </param>
    protected async Task WaitForWhenPlayerIsPresent(CancellationToken token)
    {
        while (!await _player.IsPresentAsync().ConfigureAwait(false) && !token.IsCancellationRequested)
        {
            Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
    }

    /// <summary> A helper method to ensure the action is executed safely, and if an exception is thrown, it is logged. </summary>
    /// <param name="act">the action to execute</param>
    protected void ExecuteSafely(Action act)
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
