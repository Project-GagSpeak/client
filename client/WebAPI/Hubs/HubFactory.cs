using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.SignalR;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace GagSpeak.WebAPI;

public class HubFactory : MediatorSubscriberBase
{
    private readonly ILoggerProvider _loggingProvider;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly TokenProvider _tokenProvider;
    private HubConnection? _instance;   // The instance of the hub connection we have with the main server
    private bool _isDisposed = false;   // if the hub factory is disposed of or not.
    private HubConnection? _instanceToybox; // The instance of the hub connection we have with the toybox server
    private bool _isToyboxDisposed = false; // if the toybox hub factory is disposed of or not.

    public HubFactory(ILogger<HubFactory> logger, GagspeakMediator gagspeakMediator,
        ServerConfigurationManager serverConfigManager, TokenProvider tokenProvider, 
        ILoggerProvider pluginLog) : base(logger, gagspeakMediator)
    {
        _serverConfigs = serverConfigManager;
        _tokenProvider = tokenProvider;
        _loggingProvider = pluginLog;
    }

    /// <summary>
    /// Disposes of the current HubConnection.
    /// </summary>
    public async Task DisposeHubAsync(HubType hubType = HubType.MainHub)
    {
        if (hubType == HubType.MainHub)
        {
            // if our instance is null or we have already disposed, then just return, as we have no need.
            if (_instance == null || _isDisposed) return;
            // Otherwise, log that we are disposing the current HubConnection
            Logger.LogDebug("Disposing current connection with GagSpeakHub-Main", LoggerType.HubFactory);
            // Set the _isDisposed flag to true, as we are disposing of the current HubConnection
            _isDisposed = true;
            // unsubscribe from the Closed, Reconnecting, and Reconnected events
            _instance.Closed -= HubOnClosed;
            _instance.Reconnecting -= HubOnReconnecting;
            _instance.Reconnected -= HubOnReconnected;
            // stop and dispose instance after unsubscribing from events
            await _instance.StopAsync().ConfigureAwait(false);
            await _instance.DisposeAsync().ConfigureAwait(false);
            _instance = null;
            Logger.LogDebug("GagSpeakHub-Main Finished Disposing", LoggerType.HubFactory);
        }
        else
        {
            // if our instance is null or we have already disposed, then just return, as we have no need.
            if (_instanceToybox == null || _isToyboxDisposed) return;
            // Otherwise, log that we are disposing the current HubConnection
            Logger.LogDebug("Disposing current Toybox HubConnection", LoggerType.HubFactory);
            // Set the _isDisposed flag to true, as we are disposing of the current HubConnection
            _isToyboxDisposed = true;
            // unsubscribe from the Closed, Reconnecting, and Reconnected events
            _instanceToybox.Closed -= ToyboxHubOnClosed;
            _instanceToybox.Reconnecting -= ToyboxHubOnReconnecting;
            _instanceToybox.Reconnected -= ToyboxHubOnReconnected;
            // stop and dispose instance after unsubscribing from events
            await _instanceToybox.StopAsync().ConfigureAwait(false);
            await _instanceToybox.DisposeAsync().ConfigureAwait(false);
            _instanceToybox = null;
            Logger.LogDebug("Current Toybox HubConnection disposed", LoggerType.HubFactory);
        }
    }

    /// <summary> 
    /// Gets or creates a new HubConnection. 
    /// </summary>
    public HubConnection GetOrCreate(CancellationToken ct, HubType hubType = HubType.MainHub, string token = "")
    {
        if (hubType == HubType.MainHub)
        {
            // if we are not disposed and the instance is not null, then return the instance.
            if (!_isDisposed && _instance != null) return _instance;
            // otherwise, build a new HubConnection and return it.
            return BuildHubConnection(ct, HubType.MainHub);
        }
        else
        {
            // if we are not disposed and the instance is not null, then return the instance.
            if (!_isToyboxDisposed && _instanceToybox != null) return _instanceToybox;
            // otherwise, build a new HubConnection and return it.
            return BuildHubConnection(ct, HubType.ToyboxHub, token);
        }
    }

    /// <summary> 
    /// Builds a new HubConnection. 
    /// </summary>
    private HubConnection BuildHubConnection(CancellationToken ct, HubType hubType = HubType.MainHub, string token = "")
    {
        // Log that we are building a new HubConnection
        Logger.LogDebug("Building new HubConnection", LoggerType.HubFactory);
        var connectionURI = _serverConfigs.CurrentApiUrl
            + (hubType == HubType.MainHub ? IGagspeakHub.Path : IToyboxHub.Path);

        Logger.LogDebug($"Attempting to connect to URI: {connectionURI}", LoggerType.HubFactory);
        // create the instance, based on the hub type.
        if (hubType == HubType.MainHub)
        {
            _instance = new HubConnectionBuilder()
                // give it the appropriate URL for the connection.
                .WithUrl(connectionURI, options =>
                {
                    // set the accessTokenProvider and transport options for the connection
                    options.AccessTokenProvider = () => _tokenProvider.GetOrUpdateToken(ct);
                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                })
                .AddMessagePackProtocol(opt => // add in a message pack protocol
                {
                    // create a new resolver for the message pack protocol
                    var resolver = CompositeResolver.Create(
                        StandardResolverAllowPrivate.Instance, // use the standard resolver
                        BuiltinResolver.Instance,              // use the built in resolver
                        AttributeFormatterResolver.Instance,   // use the attribute formatter resolver
                                                               // replace enum resolver
                        DynamicEnumAsStringResolver.Instance,  // use the dynamic enum as string resolver
                        DynamicGenericResolver.Instance,       // use the dynamic generic resolver
                        DynamicUnionResolver.Instance,         // use the dynamic union resolver
                        DynamicObjectResolver.Instance,        // use the dynamic object resolver
                        PrimitiveObjectResolver.Instance,      // use the primitive object resolver
                                                               // final fallback(last priority)
                        StandardResolver.Instance);            // use the standard resolver

                    // set the serialize options for the message pack protocol
                    opt.SerializerOptions =
                        MessagePackSerializerOptions.Standard
                            .WithCompression(MessagePackCompression.Lz4Block)
                            .WithResolver(resolver);
                })
                .WithAutomaticReconnect(new ForeverRetryPolicy(Mediator)) // automatic reconnecting
                .ConfigureLogging(a => // configure the logging for the hub connection
                {
                    // clear the providers and add the logging provider
                    a.ClearProviders().AddProvider(_loggingProvider);
                    // set the minimum log level to information
                    a.SetMinimumLevel(LogLevel.Information);
                })
                // finally, build the connection
                .Build();

            // and add our subscribers for the connection
            _instance.Closed += HubOnClosed;
            _instance.Reconnecting += HubOnReconnecting;
            _instance.Reconnected += HubOnReconnected;

            // and claim it to not be disposed since it is established.
            _isDisposed = false;
            // return the instance
            return _instance;
        }
        else // toybox instance (must use lastusedToken)
        {
            // obtain the token
            _instanceToybox = new HubConnectionBuilder()
                // give it the appropriate URL for the connection.
                .WithUrl(connectionURI, options =>
                {
                    // set the accessTokenProvider and transport options for the connection
                    options.AccessTokenProvider = () => _tokenProvider.GetOrUpdateToken(ct, token);
                    options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
                })
                .AddMessagePackProtocol(opt => // add in a message pack protocol
                {
                    // create a new resolver for the message pack protocol
                    var resolver = CompositeResolver.Create(
                        StandardResolverAllowPrivate.Instance, // use the standard resolver
                        BuiltinResolver.Instance,              // use the built in resolver
                        AttributeFormatterResolver.Instance,   // use the attribute formatter resolver
                                                               // replace enum resolver
                        DynamicEnumAsStringResolver.Instance,  // use the dynamic enum as string resolver
                        DynamicGenericResolver.Instance,       // use the dynamic generic resolver
                        DynamicUnionResolver.Instance,         // use the dynamic union resolver
                        DynamicObjectResolver.Instance,        // use the dynamic object resolver
                        PrimitiveObjectResolver.Instance,      // use the primitive object resolver
                                                               // final fallback(last priority)
                        StandardResolver.Instance);            // use the standard resolver

                    // set the serialize options for the message pack protocol
                    opt.SerializerOptions =
                        MessagePackSerializerOptions.Standard
                            .WithCompression(MessagePackCompression.Lz4Block)
                            .WithResolver(resolver);
                })
                .WithAutomaticReconnect(new ForeverRetryPolicy(Mediator)) // automatic reconnecting
                .ConfigureLogging(a => // configure the logging for the hub connection
                {
                    // clear the providers and add the logging provider
                    a.ClearProviders().AddProvider(_loggingProvider);
                    // set the minimum log level to information
                    a.SetMinimumLevel(LogLevel.Information);
                })
                // finally, build the connection
                .Build();

            // and add our subscribers for the connection
            _instanceToybox.Closed += ToyboxHubOnClosed;
            _instanceToybox.Reconnecting += ToyboxHubOnReconnecting;
            _instanceToybox.Reconnected += ToyboxHubOnReconnected;

            // and claim it to not be disposed since it is established.
            _isToyboxDisposed = false;
            // return the instance
            return _instanceToybox;
        }
    }

    /* ------------- Main Hub Connection Methods ------------- */
    private Task HubOnClosed(Exception? arg)
    {
        Mediator.Publish(new MainHubClosedMessage(arg));
        return Task.CompletedTask;
    }

    private Task HubOnReconnecting(Exception? arg)
    {
        Mediator.Publish(new MainHubReconnectingMessage(arg));
        return Task.CompletedTask;
    }

    private Task HubOnReconnected(string? arg)
    {
        Mediator.Publish(new MainHubReconnectedMessage(arg));
        return Task.CompletedTask;
    }

    /* ------------- Toybox Hub Connection Methods ------------- */
    private Task ToyboxHubOnClosed(Exception? arg)
    {
        Mediator.Publish(new ToyboxHubClosedMessage(arg));
        return Task.CompletedTask;
    }

    private Task ToyboxHubOnReconnecting(Exception? arg)
    {
        Mediator.Publish(new ToyboxHubReconnectingMessage(arg));
        return Task.CompletedTask;
    }

    private Task ToyboxHubOnReconnected(string? arg)
    {
        Mediator.Publish(new ToyboxHubReconnectedMessage(arg));
        return Task.CompletedTask;
    }
}
