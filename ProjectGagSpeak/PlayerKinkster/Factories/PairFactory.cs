using GagSpeak.Kinksters.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Network;

namespace GagSpeak.Kinksters.Factories;

public class PairFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly ServerConfigManager _serverConfigs;
    private readonly CosmeticService _cosmetics;

    public PairFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        PairHandlerFactory cachedPlayerFactory, ServerConfigManager serverConfigs, 
        CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _cachedPlayerFactory = cachedPlayerFactory;
        _serverConfigs = serverConfigs;
        _cosmetics = cosmetics;
    }

    /// <summary> Creates a new Pair object from the KinksterPair</summary>
    /// <param name="KinksterPair"> The data transfer object of a user pair</param>
    /// <returns> A new Pair object </returns>
    public Pair Create(KinksterPair kinksterPair)
        => new(kinksterPair, _loggerFactory.CreateLogger<Pair>(), _mediator, _cachedPlayerFactory, _serverConfigs, _cosmetics);
}
