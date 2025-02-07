using Dalamud.Interface.ImGuiNotification;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagspeakAPI.Routes;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Reflection;

namespace GagSpeak.WebAPI;

public sealed class TokenProvider : DisposableMediatorSubscriberBase
{
    private readonly HttpClient _httpClient;
    private readonly ClientMonitorService _clientService;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly ServerConfigurationManager _serverManager;
    private readonly ConcurrentDictionary<JwtIdentifier, string> _tokenCache;

    private JwtIdentifier? _lastJwtIdentifier;

    public TokenProvider(ILogger<TokenProvider> logger, GagspeakMediator mediator,
        ClientMonitorService clientService, ServerConfigurationManager serverManager,
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _clientService = clientService;
        _serverManager = serverManager;
        _frameworkUtil = frameworkUtils;
        _httpClient = new HttpClient();
        _tokenCache = new ConcurrentDictionary<JwtIdentifier, string>();
        var ver = Assembly.GetExecutingAssembly().GetName().Version;

        // subscribe to the login and logout messages, making sure to clear the token cache on logout and login
        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) =>
        {
            _lastJwtIdentifier = null;
            _tokenCache.Clear();
        });
        Mediator.Subscribe<DalamudLoginMessage>(this, (_) =>
        {
            _lastJwtIdentifier = null;
            _tokenCache.Clear();
        });
        // append a user agent to the http clients request headers to identify the client
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GagSpeak", ver!.Major + "." + ver!.Minor + "." + ver!.Build + "." + ver!.Revision));
    }

    /// <summary> Disposes of the token provider, unsubscribing from all events related to the token provider class </summary>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _httpClient.Dispose();
    }

    /// <summary> Prints the tokens in the token cache to the logger </summary>
    public void PrintTokens()
    {
        foreach (var entry in _tokenCache)
        {
            Logger.LogInformation($"Identifier: {entry.Key}, Token: {entry.Value}", LoggerType.JwtTokens);
        }
    }

    public void ResetTokenCache()
    {
        _lastJwtIdentifier = null;
        _tokenCache.Clear();
    }


    /// <summary>
    /// Gets a new token from the server, either by requesting a new token or renewing an existing token.
    /// This should only occur once every 6 hours.
    /// </summary>
    /// <param name="isRenewal">if the token is a renewal</param>
    /// <param name="identifier">the identifier requesting a new token</param>
    /// <param name="token">the cancelation token for the task</param>
    /// <exception cref="GagspeakAuthFailureException">If the authentication fails</exception>
    /// <exception cref="InvalidOperationException">If an invalid operation is attempted</exception>
    public async Task<string> GetNewToken(bool isRenewal, JwtIdentifier identifier, CancellationToken token)
    {
        // create a URI for the token request
        Uri tokenUri;
        string response = string.Empty;
        HttpResponseMessage result;

        // If for some god awful reason we have horrible timing and our character happens to be zoning during this 6 hourly interval, wait.
        try
        {
            while (!await _clientService.IsPresentAsync().ConfigureAwait(false) && !token.IsCancellationRequested)
            {
                Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
                await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            if (token.IsCancellationRequested) Logger.LogWarning("GetOrUpdateToken: Timeout reached while waiting for player to load in");
            // if we somehow reach a timeout here we should almost certainly throw.
            throw new GagspeakAuthFailureException("You had a 30 second timeout while zoning and attempting to fetch a new key!");
        }

        // now, lets try and get our new token
        try
        {
            // start by seeing if the token is not a renewal. If it is, we should skip this part.
            if (!isRenewal)
            {
                // if we are not renewing, we are requesting a new token
                Logger.LogDebug("GetNewToken: Requesting new token", LoggerType.JwtTokens);

                // check the identifier type.
                if (identifier is SecretKeyJwtIdentifier secretKeyIdentifier)
                {
                    Logger.LogDebug("Calling the SecretKeyJwtIdentifier", LoggerType.JwtTokens);
                    // Use the secret key for authentication
                    var secretKey = secretKeyIdentifier.SecretKey;
                    var forceMain = secretKeyIdentifier.ExpectPrimary.ToString();
                    Logger.LogDebug("GetNewToken: SecretKey {secretKey}", secretKey);
                    // var auth = secretKey.GetHash256(); // leaving out this because i took out double encryption to just single for now

                    // Set the token URI to the appropriate endpoint for secret key authentication
                    tokenUri = GagspeakAuth.AuthFullPath(new Uri(_serverManager.CurrentApiUrl
                        .Replace("wss://", "https://", StringComparison.OrdinalIgnoreCase)
                        .Replace("ws://", "http://", StringComparison.OrdinalIgnoreCase)));

                    Logger.LogTrace("Token URI: "+tokenUri, LoggerType.JwtTokens);
                    result = await _httpClient.PostAsync(tokenUri, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("charaIdent", await _frameworkUtil.GetPlayerNameHashedAsync().ConfigureAwait(false)),
                        new KeyValuePair<string, string>("authKey", secretKey),
                        new KeyValuePair<string, string>("forceMain", forceMain),
                    }), token).ConfigureAwait(false);
                }
                else if (identifier is LocalContentIDJwtIdentifier localContentIDIdentifier)
                {
                    Logger.LogDebug("Calling the LocalContentIDJwtIdentifier", LoggerType.JwtTokens);
                    // Use the local content ID for authentication
                    var localContentID = localContentIDIdentifier.LocalContentID;

                    // Set the token URI to the appropriate endpoint for local content ID authentication
                    tokenUri = GagspeakAuth.TempTokenFullPath(new Uri(_serverManager.CurrentApiUrl
                        .Replace("wss://", "https://", StringComparison.OrdinalIgnoreCase)
                        .Replace("ws://", "http://", StringComparison.OrdinalIgnoreCase)));

                    Logger.LogTrace("Token URI: "+tokenUri, LoggerType.JwtTokens);
                    result = await _httpClient.PostAsync(tokenUri, new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("charaIdent", await _frameworkUtil.GetPlayerNameHashedAsync().ConfigureAwait(false)),
                        new KeyValuePair<string, string>("localContentID", localContentID),
                    }), token).ConfigureAwait(false);
                }
                else
                {
                    throw new InvalidOperationException("Unsupported JwtIdentifier type.");
                }
            }
            else
            {
                // we are renewing
                Logger.LogDebug("GetNewToken: Renewal", LoggerType.JwtTokens);
                // set the token URI to GagspeakAuth's full path, with the base URI being the
                // server's current API URL, with https:// replaced with wss://
                // (calling RenewTokenFullPath is different from AuthFullPath
                tokenUri = GagspeakAuth.RenewTokenFullPath(new Uri(_serverManager.CurrentApiUrl
                    .Replace("wss://", "https://", StringComparison.OrdinalIgnoreCase)
                    .Replace("ws://", "http://", StringComparison.OrdinalIgnoreCase)));

                // create a new HttpRequestMessage with the tokenUri and a GET method
                HttpRequestMessage request = new(HttpMethod.Get, tokenUri.ToString());

                // add the authorization header to the request, with the bearer token being the token from the cache
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenCache[identifier]);

                // await the result from the request
                result = await _httpClient.SendAsync(request, token).ConfigureAwait(false);
            }
            response = await result.Content.ReadAsStringAsync().ConfigureAwait(false);

            // ensure the response was successful
            Logger.LogDebug("GetNewToken: Response "+response);
            result.EnsureSuccessStatusCode();
            // add the response to the token cache
            _tokenCache[identifier] = response;
        }
        catch (HttpRequestException ex)
        {
            // if we run into an exception, we need to remove the identifier from the tokencache as it is invalid.
            _tokenCache.TryRemove(identifier, out _);
            // log the error
            Logger.LogError(ex, "GetNewToken: Failure to get token");
            // set the status code to unauthorized
            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // if it was a renewal, log the notification message
                if (isRenewal)
                    Mediator.Publish(new NotificationMessage("Error refreshing token", "Your authentication token could not be renewed. " +
                        "Try reconnecting to Gagspeak manually.", NotificationType.Error));
                // otherwise, log the notification that it errored while generating the token.
                else
                    Mediator.Publish(new NotificationMessage("Error generating token", "Your authentication token could not be generated. " +
                        "Check GagSpeak's main UI to see the error message.", NotificationType.Error));

                // publish a disconnected message and throw an exception.
                Mediator.Publish(new MainHubDisconnectedMessage());
                throw new GagspeakAuthFailureException(response);
            }

            // if the exception was not unauthorized, throw the exception
            throw;
        }

        // at the end of all this, create a handler for the JWT token, and read the token from the response
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(response);
        // log it
        Logger.LogTrace("GetNewToken: JWT "+response, LoggerType.JwtTokens);
        Logger.LogDebug("GetNewToken: Valid until " + jwtToken.ValidTo + ", ValidClaim until " +
            new DateTime(long.Parse(jwtToken.Claims.Single(c => string.Equals(c.Type, "expiration_date", StringComparison.Ordinal)).Value), DateTimeKind.Utc), LoggerType.JwtTokens);
        // check if the token is valid by seeing if the token is within 10 minutes of the current time
        var dateTimeMinus10 = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        var dateTimePlus10 = DateTime.UtcNow.Add(TimeSpan.FromMinutes(10));

        // if the token is not valid, remove the identifier from the cache and throw an exception
        var tokenTime = jwtToken.ValidTo.Subtract(TimeSpan.FromHours(6));
        if (tokenTime <= dateTimeMinus10 || tokenTime >= dateTimePlus10)
        {
            // remove the token from the cache based on the identifier
            _tokenCache.TryRemove(identifier, out _);
            // publish the notification message to the mediator
            Mediator.Publish(new NotificationMessage("Invalid system clock", "The clock of your computer is invalid. " +
                "Gagspeak will not function properly if the time zone is not set correctly. " +
                "Please set your computers time zone correctly and keep your clock synchronized with the internet.",
                NotificationType.Error));
            // throw the exception.
            throw new InvalidOperationException($"JwtToken is behind DateTime.UtcNow, DateTime.UtcNow is possibly wrong. DateTime.UtcNow is {DateTime.UtcNow}, JwtToken.ValidTo is {jwtToken.ValidTo}");
        }

        // otherwise, we got a valid new token, so return it!
        return response;
    }

    /// <summary> Method to fetch the identifier for the JWT token </summary>
    /// <returns>the JWT identifier object for the token</returns>
    private JwtIdentifier? GetIdentifier()
    {
        var tempLocalContentID = _clientService.ContentIdAsync().GetAwaiter().GetResult();
        try
        {
            var secretKey = string.Empty;
            var expectingPrimary = false;
            // Attempt to get the secret key and isPrimary attributes as well.
            if (_serverManager.TryGetAuthForCharacter(out Authentication auth))
            {
                secretKey = auth.SecretKey.Key;
                expectingPrimary = auth.IsPrimary;
            }

            // get the remaining attributes.
            var apiUrl = _serverManager.CurrentApiUrl;
            var charaHash = _frameworkUtil.GetPlayerNameHashedAsync().GetAwaiter().GetResult();
            // Example logic to decide which identifier to use.
            if (!string.IsNullOrEmpty(secretKey))
            {
                // Logger.LogDebug("GetIdentifier: SecretKey {secretKey}", secretKey);
                // fired if the secret key exists, meaning we are registered
                var newIdentifier = new SecretKeyJwtIdentifier(apiUrl, charaHash, secretKey, expectingPrimary);
                _lastJwtIdentifier = newIdentifier; // Safeguarding the new identifier
                return newIdentifier;
            }
            else if (tempLocalContentID != 0)
            {
                // Logger.LogDebug("GetIdentifier: LocalContentID {localContentID}", tempLocalContentID);
                // fired if the local content ID is not 0, meaning we are not registered
                var newIdentifier = new LocalContentIDJwtIdentifier(apiUrl, charaHash, tempLocalContentID.ToString());
                _lastJwtIdentifier = newIdentifier; // Safeguarding the new identifier
                return newIdentifier;
            }
            else if (_lastJwtIdentifier is LocalContentIDJwtIdentifier localContentIDIdentifier)
            {
                // Logger.LogDebug("GetIdentifier: Using last known good LocalContentID");
                // Use LocalContentIDJwtIdentifier if it's a one-time use case, e.g., after a specific event
                // This assumes _lastJwtIdentifier is set to a LocalContentIDJwtIdentifier in such cases
                return localContentIDIdentifier;
            }
            else
            {
                // If no specific conditions are met, check if there's a last known good identifier to fall back on
                if (_lastJwtIdentifier != null)
                {
                    Logger.LogWarning("Falling back to the last known good JwtIdentifier.");
                    return _lastJwtIdentifier;
                }
                else
                {
                    Logger.LogError("Unable to create a new JwtIdentifier and no last known good identifier to fall back on.");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating JWT identifier. Exception: {Message}, StackTrace: {StackTrace}", ex.Message, ex.StackTrace);
            // Fallback to the last known good identifier if an exception occurs
            if (_lastJwtIdentifier != null)
            {
                return _lastJwtIdentifier;
            }
            else
            {
                return null;
            }
        }
    }


    /// <summary> Unlike the <c>GetToken()</c> method, this both gets and updates the token</summary>
    /// <param name="ct">The cancelation token for the task</param>
    /// <returns>the token to be returned.</returns>
    public async Task<string?> GetOrUpdateToken(CancellationToken ct, string tokenKey = "")
    {
        // Define a timeout period (e.g.,  seconds)
        var timeout = TimeSpan.FromSeconds(30);
        var timeoutCTS = new CancellationTokenSource(timeout);
        var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCTS.Token);
        // await for the player to be present to get the JWT token
        try
        {
            while (!await _clientService.IsPresentAsync().ConfigureAwait(false) && !linkedCTS.Token.IsCancellationRequested)
            {
                Logger.LogDebug("Player not loaded in yet, waiting", LoggerType.ApiCore);
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCTS.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            if (timeoutCTS.Token.IsCancellationRequested)
            {
                Logger.LogWarning("GetOrUpdateToken: Timeout reached while waiting for player to load in", LoggerType.ApiCore);
            }
            else
            {
                Logger.LogWarning("GetOrUpdateToken: Player not loaded in yet, waiting", LoggerType.ApiCore);
            }
            return null;
        }

        // get the JWT identifier
        JwtIdentifier? jwtIdentifier = GetIdentifier();
        // if it is null, return null
        if (jwtIdentifier == null) 
            return null;

        // assume we dont need a renewal
        bool renewal = false;
        if (_tokenCache.TryGetValue(jwtIdentifier, out var token))
        {
            // create a new handler for the JWT token
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo == DateTime.MinValue || jwt.ValidTo.Subtract(TimeSpan.FromMinutes(5)) > DateTime.UtcNow)
            {
                // token was valid, so return it LOG NOTE: This is very spammy to the logs if left unchecked.
                Logger.LogTrace("GetOrUpdate: Returning Valid token from cache", LoggerType.JwtTokens);
                return token;
            }

            // token expired, requires renewal.
            Logger.LogDebug("GetOrUpdate: Cached token was found but requires renewal, token valid to: "+jwt.ValidTo+" UTC is now: "+DateTime.UtcNow, LoggerType.JwtTokens);
            renewal = true;
        }
        // if we did not find the token in the cache, log that we did not find it.
        else
        {
            Logger.LogDebug("GetOrUpdate: Did not find token in cache, requesting a new one", LoggerType.JwtTokens);
        }

        // if we are renewing, log that we are getting a new token, and return it
        Logger.LogTrace("GetOrUpdate: Getting new token", LoggerType.JwtTokens);

        // log if the identifier is secretkey or contentid
        if (jwtIdentifier is SecretKeyJwtIdentifier secretKeyIdentifier) { Logger.LogDebug("GetOrUpdate: Using SecretKeyIdentifier", LoggerType.JwtTokens); }
        else if (jwtIdentifier is LocalContentIDJwtIdentifier localContentIDIdentifier) { Logger.LogDebug("GetOrUpdate: Using LocalContentIdIdentifier", LoggerType.JwtTokens); }

        return await GetNewToken(renewal, jwtIdentifier, ct).ConfigureAwait(false);
    }
}
