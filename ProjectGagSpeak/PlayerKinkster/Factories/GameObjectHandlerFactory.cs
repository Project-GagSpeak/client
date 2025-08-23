using GagSpeak.Services.Mediator;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services;

namespace GagSpeak.Kinksters.Factories;
/// <summary>
/// Class to help with the creation of game object handlers. Helps make the pair handler creation more modular.
/// </summary>
public class KinksterGameObjFactory
{
    private readonly OnFrameworkService _frameworkUtils;
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _gagspeakMediator;

    public KinksterGameObjFactory(ILoggerFactory loggerFactory, GagspeakMediator gagspeakMediator,
        OnFrameworkService frameworkUtils)
    {
        _loggerFactory = loggerFactory;
        _gagspeakMediator= gagspeakMediator;
        _frameworkUtils = frameworkUtils;
    }

    /// <summary> Responsible for creating a new KinksterGameObj object.</summary>
    public async Task<KinksterGameObj> Create(Func<nint> getAddressFunc)
    {
        return await _frameworkUtils.RunOnFrameworkThread(() => new KinksterGameObj(_loggerFactory.CreateLogger<KinksterGameObj>(),
            _gagspeakMediator, _frameworkUtils, getAddressFunc)).ConfigureAwait(false);
    }
}
