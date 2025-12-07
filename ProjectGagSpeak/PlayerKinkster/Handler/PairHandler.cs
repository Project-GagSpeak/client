using CkCommons;
using Dalamud.Game.ClientState.Objects.Types;
using GagSpeak.Interop;
using GagSpeak.Kinksters.Factories;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Network;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Kinksters.Handlers;

/// <summary> The handler for a client pair. </summary>
public sealed class PairHandler : DisposableMediatorSubscriberBase
{
    private readonly KinksterGameObjFactory _factory;
    private readonly IpcManager _ipc;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly IHostApplicationLifetime _lifetime;

    private CancellationTokenSource? _appCTS = new();

    // Cached, nullable data.
    private string? _statusManagerStr = null;

    private KinksterGameObj? _gameObject;

    // if this kinkster is currently visible.
    private bool _isVisible;

    public PairHandler(OnlineKinkster onlineUser, ILogger<PairHandler> logger, GagspeakMediator mediator,
        KinksterGameObjFactory gen, IpcManager ipc, OnFrameworkService frameworkUtil, IHostApplicationLifetime app)
        : base(logger, mediator)
    {
        OnlineUser = onlineUser;
        _factory = gen;
        _ipc = ipc;
        _frameworkUtil = frameworkUtil;
        _lifetime = app;

        // Can easily create a temporary collection here to manage pcp's for if need be,
        // or even manage them with helper functions.
        //_penumbraCollectionId = _ipc.Penumbra.CreateKinksterCollection(OnlineUser.User.UID).ConfigureAwait(false).GetAwaiter().GetResult();
        //Mediator.Subscribe<PenumbraInitialized>(this, _ =>
        //{
        //    _penumbraCollectionId = _ipc.Penumbra.CreateKinksterCollection(OnlineUser.User.UID).ConfigureAwait(false).GetAwaiter().GetResult();
        //    if (!IsVisible && _gameObject != null)
        //    {
        //        PlayerName = string.Empty;
        //        _gameObject.Dispose();
        //        _gameObject = null;
        //    }
        //});
        // subscribe to the framework update Message 
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());
        // Invalidate our kinkster pairs whenever we begin changing zones.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) =>
        {
            _gameObject?.Invalidate();
            IsVisible = false;
        });
    }

    // determines if a paired user is visible. (if they are in render range)
    public bool IsVisible
    {
        get => _isVisible;
        private set
        {
            if (_isVisible != value)
            {
                _isVisible = value;
                Logger.LogTrace("User Visibility Changed, now: " + (_isVisible ? "Is Visible" : "Is not Visible"), LoggerType.PairHandlers);
                Mediator.Publish(new RefreshUiKinkstersMessage());
                Mediator.Publish(new VisibleKinkstersChanged());
            }
        }
    }

    public OnlineKinkster OnlineUser { get; private set; }  // the online user Dto. Set when pairhandler is made for the cached player in the pair object.
    public nint PairAddress => _gameObject?.Address ?? nint.Zero; // the player character object address
    public IGameObject? PairObject => _gameObject?.PlayerCharacterObjRef; // the player character object
    public string? PlayerName { get; private set; }
    public string PlayerNameWithWorld => _gameObject?.NameWithWorld ?? string.Empty;
    public string PlayerNameHash => OnlineUser.Ident;

    public override string ToString()
        => OnlineUser is null ? base.ToString() ?? string.Empty
            : $"AliasOrUID: ({OnlineUser.User.AliasOrUID}, {(_gameObject != null ? _gameObject.ToString() : "NoHandler")}";

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // store name and address to reference removal properly.
        var name = PlayerNameWithWorld;
        var address = _gameObject?.Address ?? nint.Zero;
        Logger.LogDebug($"Disposing Kinkster: {name} ({OnlineUser})", LoggerType.PairHandlers);
        // Safely dispose.
        Generic.Safe(() =>
        {
            // safely cancel any running vsual application tasks.
            _appCTS.SafeCancelDispose();
            _appCTS = null;
            // if the pair was a visible kinkster, publish their disposal.
            if (!string.IsNullOrEmpty(name))
                Mediator.Publish(new EventMessage(new(name, OnlineUser.User.UID, InteractionType.VisibilityChange, "Disposing Kinkster Handler")));

            // if the hosted service lifetime is ending, return
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
            {
                // TODO: check if sundouleia is already handling this kinkster's moodle sync
                _ipc.Moodles.ClearStatus(name).ConfigureAwait(false);
                return;
            }
        });

        // safely dispose of the kinkster game object.
        _gameObject?.Dispose();
        _gameObject = null;
        PlayerName = null;
        Logger.LogDebug($"Disposal complete for Kinkster: {name} ({OnlineUser})", LoggerType.PairHandlers);
    }

    public void UpdateMoodles(string newDataString)
    {
        if (PairAddress == nint.Zero || PlayerNameWithWorld.Length == 0)
            return;
        Logger.LogDebug($"Updating moodles for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID})", LoggerType.PairHandlers);
        _ipc.Moodles.SetStatus(PlayerNameWithWorld, newDataString).ConfigureAwait(false);
        // update the string.
        _statusManagerStr = newDataString;
    }

    private void FrameworkUpdate()
    {
        // Perform first time initializations on Kinksters if not initialized.
        if (string.IsNullOrEmpty(PlayerName))
        {
            // get name/address from cache in framework utils by Ident. Return if it's not found / default.
            var nameAndAddr = _frameworkUtil.FindPlayerByNameHash(OnlineUser.Ident);
            if (nameAndAddr == default((string, nint)))
                return;
            // Perform Initialization for the Kinkster. (This sets the PlayerName, making this only happen once).
            Logger.LogDebug($"One-Time Initializing [{this}]", LoggerType.PairHandlers);
            Initialize(nameAndAddr.Name);
            Logger.LogDebug($"One-Time Initialized [{this}] ({nameAndAddr.Name})", LoggerType.PairHandlers);
        }

        // If the monitored objects address is valid, but IsVisible is false, apply their data.
        if (_gameObject?.Address != nint.Zero && !IsVisible)
        {
            IsVisible = true;
            // publish that this Kinkster is not visible.
            Mediator.Publish(new PairHandlerVisibleMessage(this));
            // if they have non-null cached data, reapply it to them.
            Logger.LogTrace($"Visibility changed for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID}). Now: {IsVisible}", LoggerType.PairHandlers);
            if (_statusManagerStr is not null)
                _ = Task.Run(() => UpdateMoodles(_statusManagerStr));
        }
        // otherwise, if they went from visible to not visible, we should invalidate them.
        else if (_gameObject?.Address == nint.Zero && IsVisible)
        {
            IsVisible = false;
            _gameObject.Invalidate();
            Logger.LogTrace($"Invalidating as Visibility changed for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID}). Now: {IsVisible}", LoggerType.PairHandlers);
        }
    }

    /// <summary>
    ///     Initializes a pair handler object
    /// </summary>
    private void Initialize(string name)
    {
        Logger.LogTrace("Initializing " + this, LoggerType.PairHandlers);
        // set the player name to the name
        PlayerName = name;
        // create a new game object handler for the player character
        Logger.LogTrace("Creating CharaHandler for " + this, LoggerType.PairHandlers);
        _gameObject = _factory.Create(() => _frameworkUtil.GetKinksterAddrFromCache(OnlineUser.Ident)).GetAwaiter().GetResult();
        // GetAwaiter().GetResult() ensures that _gameObject is fully created before proceeding.

        // _ipc.Penumbra.AssignKinksterCollection(_penumbraCollectionId, _gameObject.PlayerCharacterObjRef!.ObjectIndex).GetAwaiter().GetResult();
    }
}
