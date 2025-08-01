using CkCommons;
using Dalamud.Game.ClientState.Objects.Types;
using GagSpeak.Interop;
using GagSpeak.Kinksters.Factories;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Network;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.Kinksters.Handlers;

/// <summary> The handler for a client pair. </summary>
public sealed class PairHandler : DisposableMediatorSubscriberBase
{
    private readonly OnFrameworkService _frameworkUtil;
    private readonly GameObjectHandlerFactory _gameObjectHandlerFactory;
    private readonly IpcManager _ipcManager;
    private readonly IHostApplicationLifetime _lifetime;
    private CancellationTokenSource? _applicationCTS = new();

    // the cached data for the paired player.
    private string? _cachedMoodleDataStr = null;

    // primarily used for initialization and address checking for visibility
    private GameObjectHandler? _charaHandler;
    private bool _isVisible;

    public PairHandler(OnlineKinkster onlineUser,
        ILogger<PairHandler> logger,
        GagspeakMediator mediator,
        GameObjectHandlerFactory factory,
        IpcManager ipcManager,
        OnFrameworkService dalamudUtil,
        IHostApplicationLifetime lifetime)
        : base(logger, mediator)
    {
        OnlineUser = onlineUser;
        _gameObjectHandlerFactory = factory;
        _ipcManager = ipcManager;
        _frameworkUtil = dalamudUtil;
        _lifetime = lifetime;
        // subscribe to the framework update Message 
        Mediator.Subscribe<FrameworkUpdateMessage>(this, (_) => FrameworkUpdate());

        // Make our pair no longer visible if we begin zoning.
        Mediator.Subscribe<ZoneSwitchStartMessage>(this, (_) =>
        {
            _charaHandler?.Invalidate();
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
                Mediator.Publish(new RefreshUiMessage());
                Mediator.Publish(new VisibleKinkstersChanged());
            }
        }
    }

    public OnlineKinkster OnlineUser { get; private set; }  // the online user Dto. Set when pairhandler is made for the cached player in the pair object.
    public nint PairAddress => _charaHandler?.Address ?? nint.Zero; // the player character object address
    public IGameObject? PairObject => _charaHandler?.PlayerCharacterObjRef; // the player character object
    public string? PlayerName { get; private set; }
    public string PlayerNameWithWorld => _charaHandler?.NameWithWorld ?? string.Empty;
    public string PlayerNameHash => OnlineUser.Ident;

    public override string ToString()
    {
        return OnlineUser == null
            ? base.ToString() ?? string.Empty
            : "AliasOrUID: " + OnlineUser.User.AliasOrUID + "||" + (_charaHandler != null ? _charaHandler.ToString() : "NoHandler");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // store name and address to reference removal properly.
        var name = PlayerNameWithWorld;
        var address = _charaHandler?.Address ?? nint.Zero;
        Logger.LogDebug($"Disposing {name} ({OnlineUser})", LoggerType.PairHandlers);
        try
        {
            var applicationId = Guid.NewGuid();
            _applicationCTS.SafeCancelDispose();
            _applicationCTS = null;
            _charaHandler?.Dispose();
            _charaHandler = null;

            // if the hosted service lifetime is ending, return
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

            // if we are not zoning, or in a cutscene, but this player is being disposed, they are leaving a zone.
            // Because this is happening, we need to make sure that we revert their IPC data and toggle their address & visibility.
            if (!PlayerData.IsZoning && !string.IsNullOrEmpty(name))
            {
                Logger.LogTrace($"[{applicationId}] Restoring State for [{name}] ({OnlineUser})", LoggerType.PairHandlers);
                // They are visible but being disposed, so revert their applied customization data
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                Logger.LogDebug($"[{applicationId}] Reverting all IPC for {OnlineUser.User.AliasOrUID}", LoggerType.PairHandlers);
                // make sure we only revert if they are not a Mare user.
                if (!IsMareUser(address))
                {
                    Logger.LogDebug(name + " is not a Mare user. Clearing Moodles for " + OnlineUser.User.AliasOrUID, LoggerType.PairHandlers);
                    _ipcManager.Moodles.ClearStatus(name).ConfigureAwait(false);
                }
                cts.Cancel();
                cts.Dispose();
            }
        }
        catch (Bagagwa ex)
        {
            Logger.LogWarning($"Error on disposal of {name}: {ex}");
        }
        finally
        {
            PlayerName = null;
            _cachedMoodleDataStr = null;
        }
    }

    /// <summary> Method responsible for applying the stored character data to the paired user. </summary>
    /// <remarks> This method helps act as an override for mare to apply the moodles data to other non-mare players. </remarks>
    public void ApplyMoodlesToPlayer(Guid applicationBase, string newDataString)
    {
        var oldMoodleData = _cachedMoodleDataStr ?? string.Empty;
        if (oldMoodleData.Equals(newDataString))
            return;

        // Changes were made
        if (PairAddress == nint.Zero)
            return;

        Logger.LogDebug($"Applying Kinksters Moodle Data for ({PlayerName})", LoggerType.PairHandlers);
        if (_charaHandler is not null && _charaHandler.Address != nint.Zero)
            _ipcManager.Moodles.SetStatus(_charaHandler.NameWithWorld, newDataString).ConfigureAwait(false);

        // Update the cachedData
        _cachedMoodleDataStr = newDataString;
        Logger.LogDebug($"ApplyData finished for ({PlayerName})", LoggerType.PairHandlers);
    }

    private void FrameworkUpdate()
    {
        // if the player name is null or empty
        if (string.IsNullOrEmpty(PlayerName))
        {
            // then try and find the name by the online user identity
            var pc = _frameworkUtil.FindPlayerByNameHash(OnlineUser.Ident);

            // if the player character is null, return
            if (pc == default((string, nint))) return;

            // otherwise, call a one-time initialization
            Logger.LogDebug("One-Time Initializing " + this, LoggerType.PairHandlers);
            // initialize the player character
            Initialize(pc.Name);
            if (_charaHandler != null) _charaHandler.UpdatePlayerCharacterRef();
            Logger.LogDebug($"One-Time Initialized {this} ({pc.Name})", LoggerType.PairHandlers);
        }

        // if the game object for this pair has a pointer that is not zero (meaning they are present) but the pair is marked as not visible
        if (_charaHandler?.Address != nint.Zero && !IsVisible)
        {
            var appData = Guid.NewGuid();
            // and update their visibility to true
            IsVisible = true;
            if (_charaHandler is not null) 
                _charaHandler.UpdatePlayerCharacterRef();

            // publish the pairHandlerVisible message to the mediator, passing in this pair handler object
            Mediator.Publish(new PairHandlerVisibleMessage(this));
            // if the pairs cachedData is not null
            if (_cachedMoodleDataStr is not null)
            {
                Logger.LogTrace($"[BASE-{appData}] {this} visibility changed, now: {IsVisible}, cached IPC data exists", LoggerType.PairHandlers);
                _ = Task.Run(() => ApplyMoodlesToPlayer(appData, _cachedMoodleDataStr));
            }
            else
            {
                Logger.LogTrace($"{this} visibility changed, now: {IsVisible} (No Ipc Data)", LoggerType.PairHandlers);
            }
        }
        // if the player address is 0 but they are visible, invalidate them
        else if (_charaHandler?.Address == nint.Zero && IsVisible)
        {
            // set is visible to false and invalidate the pair handler
            _charaHandler.Invalidate();
            IsVisible = false;
            Logger.LogTrace(this + " visibility changed, now: " + IsVisible, LoggerType.PairHandlers);
        }
    }

    /// <summary> Initializes a pair handler object </summary>
    private void Initialize(string name)
    {
        Logger.LogTrace("Initializing " + this, LoggerType.PairHandlers);
        // set the player name to the name
        PlayerName = name;
        // create a new game object handler for the player character
        Logger.LogTrace("Creating CharaHandler for " + this, LoggerType.PairHandlers);
        _charaHandler = _gameObjectHandlerFactory.Create(() =>
            _frameworkUtil.GetIPlayerCharacterFromCachedTableByIdent(OnlineUser.Ident), isWatched: false).GetAwaiter().GetResult();
    }

    /// <summary> Informs us if the pair is also a Mare User. </summary>
    /// <returns> If true, prevents clearing of data applied by GagSpeak that syncs with Mare. </returns>
    private bool IsMareUser(nint address)
    {
        var handledMarePlayers = _ipcManager.Mare.GetHandledMarePlayers();
        return handledMarePlayers.Any(playerAddress => playerAddress == address);
    }
}
