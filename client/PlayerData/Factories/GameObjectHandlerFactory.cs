using GagSpeak.Services.Mediator;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.PlayerData.Factories;
/// <summary>
/// Class to help with the creation of game object handlers. Helps make the pair handler creation more modular.
/// </summary>
public class GameObjectHandlerFactory
{
    private readonly OnFrameworkService _frameworkUtils;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _gagspeakMediator;

    public GameObjectHandlerFactory(ILoggerFactory loggerFactory, GagspeakMediator gagspeakMediator,
        OnFrameworkService frameworkUtils)
    {
        _loggerFactory = loggerFactory;
        _gagspeakMediator= gagspeakMediator;
        _frameworkUtils = frameworkUtils;
    }

    /// <summary> Responsible for creating a new GameObjectHandler object.</summary>
    public async Task<GameObjectHandler> Create(Func<nint> getAddressFunc, bool isWatched = false)
    {
        return await _frameworkUtils.RunOnFrameworkThread(() => new GameObjectHandler(_loggerFactory.CreateLogger<GameObjectHandler>(),
            _gagspeakMediator, _frameworkUtils, getAddressFunc, isWatched)).ConfigureAwait(false);
    }
}
