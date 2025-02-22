using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Dto.UserPair;

namespace GagSpeak.PlayerData.Factories;

public class PairFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly CosmeticService _cosmetics;

    public PairFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        PairHandlerFactory cachedPlayerFactory, ServerConfigurationManager serverConfigs, 
        CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _cachedPlayerFactory = cachedPlayerFactory;
        _serverConfigs = serverConfigs;
        _cosmetics = cosmetics;
    }

    /// <summary> Creates a new Pair object from the UserPairDto</summary>
    /// <param name="userPairDto"> The data transfer object of a user pair</param>
    /// <returns> A new Pair object </returns>
    public Pair Create(UserPairDto userPairDto)
    {
        return new Pair(_loggerFactory.CreateLogger<Pair>(), userPairDto, _mediator,
            _cachedPlayerFactory, _serverConfigs, _cosmetics);
    }
}
