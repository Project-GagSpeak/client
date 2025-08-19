using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;

namespace GagSpeak.Kinksters.Factories;

public class PairFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly ServerConfigManager _serverConfigs;

    public PairFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        PairHandlerFactory cachedPlayerFactory, ServerConfigManager serverConfigs)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _cachedPlayerFactory = cachedPlayerFactory;
        _serverConfigs = serverConfigs;
    }

    /// <summary> Creates a new Pair object from the KinksterPair</summary>
    /// <param name="KinksterPair"> The data transfer object of a user pair</param>
    /// <returns> A new Pair object </returns>
    public Kinkster Create(KinksterPair kinksterPair)
        => new(kinksterPair, _loggerFactory.CreateLogger<Kinkster>(), _mediator, _cachedPlayerFactory, _serverConfigs);
}
