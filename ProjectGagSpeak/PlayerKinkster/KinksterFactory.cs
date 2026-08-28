using GagSpeak.Interop;
using GagSpeak.Pairs;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.User;

namespace GagSpeak.Kinksters;

public class KinksterFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly FavoritesConfig _favorites;
    private readonly NicksConfig _nicks;
    private readonly OnlineKinksterManager _onlineUsers;
    private readonly IpcManager _ipc;
    private readonly VisibilityWatcher _watcher;

    public KinksterFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        MainConfig config, FavoritesConfig favorites, NicksConfig nicks,
        OnlineKinksterManager onlineUsers, IpcManager ipc, VisibilityWatcher watcher)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _config = config;
        _favorites = favorites;
        _nicks = nicks;
        _onlineUsers = onlineUsers;
        _ipc = ipc;
        _watcher = watcher;
    }

    /// <summary> 
    ///   Creates a new Kinkster from the KinksterPair
    /// </summary>
    public Kinkster Create(KinksterPair kinksterPair)
        => new(kinksterPair, _loggerFactory.CreateLogger<Kinkster>(), _mediator, _config, _nicks, this, _onlineUsers);

    /// <summary>
    ///   Handles the current visible state of the Kinkster.
    /// </summary>
    public KinksterHandler Create(Kinkster kinkster)
        => new KinksterHandler(kinkster, _loggerFactory.CreateLogger<KinksterHandler>(), _mediator, _ipc, _watcher);
}
