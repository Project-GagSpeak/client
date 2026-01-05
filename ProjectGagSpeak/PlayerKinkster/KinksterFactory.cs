using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Watchers;
using GagspeakAPI.Network;

namespace GagSpeak.Kinksters;

public class KinksterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly FavoritesConfig _favorites;
    private readonly NicksConfig _nicks;
    private readonly IpcManager _ipc;
    private readonly CharaObjectWatcher _watcher;

    public KinksterFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        MainConfig config, FavoritesConfig favorites, NicksConfig nicks,
        IpcManager ipc, CharaObjectWatcher watcher)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _config = config;
        _favorites = favorites;
        _nicks = nicks;
        _ipc = ipc;
        _watcher = watcher;
    }

    /// <summary> 
    ///     Creates a new Kinkster from the KinksterPair
    /// </summary>
    public Kinkster Create(KinksterPair kinksterPair)
        => new(kinksterPair, _loggerFactory.CreateLogger<Kinkster>(), _mediator, _config, _favorites, _nicks, this);

    /// <summary>
    ///     Handles the current visible state of the Kinkster.
    /// </summary>
    public KinksterHandler Create(Kinkster kinkster)
        => new KinksterHandler(kinkster, _loggerFactory.CreateLogger<KinksterHandler>(), _mediator, _ipc, _watcher);
}
