using GagspeakAPI.Dto.User;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.ConfigurationServices;
using GagspeakAPI.Dto.UserPair;
using GagSpeak.Services.Textures;

namespace GagSpeak.PlayerData.Factories;

public class PairFactory
{
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _gagspeakMediator;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly CosmeticService _cosmetics;

    public PairFactory(ILoggerFactory loggerFactory, PairHandlerFactory cachedPlayerFactory,
        GagspeakMediator gagspeakMediator, ServerConfigurationManager serverConfigs, 
        CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _cachedPlayerFactory = cachedPlayerFactory;
        _gagspeakMediator = gagspeakMediator;
        _serverConfigs = serverConfigs;
        _cosmetics = cosmetics;
    }

    /// <summary> Creates a new Pair object from the UserPairDto</summary>
    /// <param name="userPairDto"> The data transfer object of a user pair</param>
    /// <returns> A new Pair object </returns>
    public Pair Create(UserPairDto userPairDto)
    {
        return new Pair(_loggerFactory.CreateLogger<Pair>(), new(userPairDto.User, userPairDto.IndividualPairStatus,
            userPairDto.OwnPairPerms, userPairDto.OwnEditAccessPerms, userPairDto.OtherGlobalPerms, userPairDto.OtherPairPerms,
            userPairDto.OtherEditAccessPerms), _cachedPlayerFactory, _gagspeakMediator, _serverConfigs, _cosmetics);
    }
}
