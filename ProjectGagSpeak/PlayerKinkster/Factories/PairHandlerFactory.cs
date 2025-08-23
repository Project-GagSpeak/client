using GagSpeak.Interop;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Kinksters.Factories;

public class PairHandlerFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly KinksterGameObjFactory _gameObjectHandlerFactory;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IpcManager _ipc;
    public PairHandlerFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        KinksterGameObjFactory objFactory, IpcManager ipcManager,
        OnFrameworkService frameworkUtils, IHostApplicationLifetime appLife)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _gameObjectHandlerFactory = objFactory;
        _ipc = ipcManager;
        _frameworkUtils = frameworkUtils;
        _hostApplicationLifetime = appLife;
    }

    /// <summary> This create method in the pair handler factory will create a new pair handler object.</summary>
    /// <param name="OnlineKinkster">The online user to create a pair handler for</param>
    /// <returns> A new PairHandler object </returns>
    public PairHandler Create(OnlineKinkster OnlineKinkster)
    {
        return new PairHandler(OnlineKinkster, _loggerFactory.CreateLogger<PairHandler>(), _mediator,
            _gameObjectHandlerFactory, _ipc, _frameworkUtils, _hostApplicationLifetime);
    }
}
