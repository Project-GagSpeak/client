using Dalamud.Game.ClientState.Objects.Types;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Factories;
using GagSpeak.Services.Mediator;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.Connection;
using Microsoft.Extensions.Hosting;

namespace GagSpeak.PlayerData.Handlers;

/// <summary>
/// The handler for a client pair.
/// </summary>
public sealed class PairHandler : DisposableMediatorSubscriberBase
{
    private readonly OnFrameworkService _frameworkUtil;
    private readonly GameObjectHandlerFactory _gameObjectHandlerFactory;
    private readonly IpcManager _ipcManager;
    private readonly IHostApplicationLifetime _lifetime;
    private Task? _applicationTask;
    private CancellationTokenSource? _applicationCTS = new();

    // the cached data for the paired player.
    private CharaIPCData? _cachedIpcData = null;

    // primarily used for initialization and address checking for visibility
    private GameObjectHandler? _charaHandler;

    private bool _isVisible;

    public PairHandler(ILogger<PairHandler> logger, OnlineUserIdentDto onlineUser,
        GameObjectHandlerFactory gameObjectHandlerFactory, IpcManager ipcManager,
        OnFrameworkService dalamudUtil, IHostApplicationLifetime lifetime,
        GagspeakMediator mediator) : base(logger, mediator)
    {
        OnlineUser = onlineUser;
        _gameObjectHandlerFactory = gameObjectHandlerFactory;
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
                // publish a refresh ui message to the mediator
                Mediator.Publish(new RefreshUiMessage());
                // push latest list details to Moodles.
                Mediator.Publish(new MoodlesUpdateNotifyMessage());
            }
        }
    }

    public OnlineUserIdentDto OnlineUser { get; private set; }  // the online user Dto. Set when pairhandler is made for the cached player in the pair object.
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
        Logger.LogDebug("Disposing " + name + " (" + OnlineUser + ")", LoggerType.PairHandlers);
        try
        {
            Guid applicationId = Guid.NewGuid();
            _applicationCTS?.CancelDispose();
            _applicationCTS = null;
            _charaHandler?.Dispose();
            _charaHandler = null;

            // if the hosted service lifetime is ending, return
            if (_lifetime.ApplicationStopping.IsCancellationRequested) return;

            // if we are not zoning, or in a cutscene, but this player is being disposed, they are leaving a zone.
            // Because this is happening, we need to make sure that we revert their IPC data and toggle their address & visibility.
            if (!_frameworkUtil.Zoning && !string.IsNullOrEmpty(name))
            {
                Logger.LogTrace("[" + applicationId + "] Restoring State for [" + name + "] (" + OnlineUser + ")", LoggerType.PairHandlers);

                // They are visible but being disposed, so revert their applied customization data
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                try
                {
                    RevertIpcDataAsync(name, address, applicationId, cts.Token).GetAwaiter().GetResult();
                }
                catch (InvalidOperationException ex)
                {
                    Logger.LogWarning(ex, "Error Reverting character during disposal {name}", name);
                }

                cts.CancelDispose();
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error on disposal of {name}", name);
        }
        finally
        {
            PlayerName = null;
            _cachedIpcData = null;
        }
    }

    /// <summary> 
    /// Method responsible for applying the stored character data to the paired user.
    /// <para> 
    /// This is usually handled by Mare, but in the case that mare is not installed, 
    /// GagSpeak will be able to take over for the supported IPC it can work with.
    /// </para>
    /// </summary>
    public void ApplyCharacterData(Guid applicationBase, CharaIPCData characterData)
    {
        // publish the message to the mediator that we are applying character data
        Logger.LogDebug("Applying Character IPC Data for (" + PlayerName + ")", LoggerType.PairHandlers);

        // check update data to see what character data we will need to update.
        // We pass in _cachedIpcData?.DeepClone() to send what the past data was,
        // so we can compare it against the new data to know if its different.
        var charaDataChangesToUpdate = characterData.CheckUpdatedData(applicationBase, _cachedIpcData?.DeepClone() ?? new(), Logger, this);

        Logger.LogDebug("Applying IPC data for [" + this + "] (" + PlayerName + ")", LoggerType.PairHandlers);

        // process the changes to the character data (look further into the purpose of the deep cloning at a later time)
        if (!charaDataChangesToUpdate.Any())
        {
            Logger.LogDebug("Nothing to update for [" + applicationBase + "] (" + PlayerName + ")", LoggerType.PairHandlers);
            return;
        }

        // recreate the application cancellation token source
        _applicationCTS = _applicationCTS.CancelRecreate() ?? new CancellationTokenSource();
        var token = _applicationCTS.Token;

        // run the application task in async to apply the customization data to the paired user.
        _applicationTask = Task.Run(async () =>
        {
            // await for the customization data to be applied
            await CallAlterationsToIpcAsync(charaDataChangesToUpdate, characterData, token).ConfigureAwait(false);
            // throw if canceled
            token.ThrowIfCancellationRequested();

            // update the cachedData 
            _cachedIpcData = characterData;

            Logger.LogDebug("ApplyData finished for (" + PlayerName + ")", LoggerType.PairHandlers);
        }, token);
    }

    /// <summary>
    /// Applies the visible alterations to a character's IPC data.
    /// This means only IPC related changes should be monitored here. Not anything else.
    /// </summary>
    private async Task CallAlterationsToIpcAsync(HashSet<PlayerChanges> changes, CharaIPCData charaData, CancellationToken token)
    {
        if (PairAddress == nint.Zero) return;
        // pointer address to playerCharacter address, which is equal to the gameobject handlers address.
        var ptr = PairAddress;
        var handler = _charaHandler!;
        try
        {
            // verify game object address is not zero
            if (handler.Address == nint.Zero) { return; }

            // otherwise, log that we are applying the customization data for the handlers
            Logger.LogDebug("Applying visual customization changes for [" + handler + "]", LoggerType.PairHandlers);

            // otherwise, for each change in the changes, apply the changes
            foreach (var change in changes.OrderBy(p => (int)p))
            {
                // log that we are processing the change for the handler
                Logger.LogDebug("Processing " + change + " for [" + handler + "]", LoggerType.PairHandlers);
                switch (change)
                {
                    case PlayerChanges.Customize:
                        // If mare ever removes access to this just recreate it through here.
                        break;
                    case PlayerChanges.Moodles:
                        await _ipcManager.Moodles.SetStatusAsync(handler.NameWithWorld, charaData.MoodlesData).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
                token.ThrowIfCancellationRequested();
            }
        }
        finally
        {
            if (handler != _charaHandler) handler.Dispose();
        }
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
            Logger.LogDebug("One-Time Initialized " + this + "(" + pc.Name + ")", LoggerType.PairHandlers);
        }

        // if the game object for this pair has a pointer that is not zero (meaning they are present) but the pair is marked as not visible
        if (_charaHandler?.Address != nint.Zero && !IsVisible) // in other words, we apply this the first time they render into our view
        {
            // then we need to create appData for it.
            Guid appData = Guid.NewGuid();
            // and update their visibility to true
            IsVisible = true;
            if (_charaHandler != null) _charaHandler.UpdatePlayerCharacterRef();
            // publish the pairHandlerVisible message to the mediator, passing in this pair handler object
            Mediator.Publish(new PairHandlerVisibleMessage(this));
            // if the pairs cachedData is not null
            if (_cachedIpcData != null)
            {
                Logger.LogTrace("[BASE-" + appData + "] " + this + " visibility changed, now: " + IsVisible + ", cached IPC data exists", LoggerType.PairHandlers);
                // then we should apply it to the character data
                _ = Task.Run(() =>
                {
                    ApplyCharacterData(appData, _cachedIpcData!);
                });
            }
            else
            {
                // otherwise, do not apply it to the character as they are not present
                Logger.LogTrace(this + " visibility changed, now: " + IsVisible + " (No Ipc Data)", LoggerType.PairHandlers);
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

    /// <summary> 
    /// Method responsible for reverting all VISIBLE IPC Changes done to players on disconnect or plugin toggle.
    /// When reverting, ensure to check against the address so we avoid reverting changes there mare still holds valid.
    /// </summary>
    /// <param name="name">the name of the pair to dispose</param>
    /// <param name="applicationId">the ID of the pair Handler</param>
    /// <param name="cancelToken">the CancellationToken</param>
    private async Task RevertIpcDataAsync(string name, nint address, Guid applicationId, CancellationToken cancelToken)
    {
        // if the address is zero, return
        if (name == string.Empty) return;

        Logger.LogDebug("[" + applicationId + "] Reverting all Customization for " + OnlineUser.User.AliasOrUID, LoggerType.PairHandlers);

        // first, we should validate if they are a mare player or not.
        bool isMareUser = await IsMareUser(address).ConfigureAwait(false);

        // Handle C+ Revert if not a mare user (LATER)

        // Handle Moodle Revert
        if (isMareUser)
        {
            Logger.LogDebug(name + " Is a Mare user. Retaining Moodles for " + OnlineUser.User.AliasOrUID, LoggerType.PairHandlers);
        }
        else
        {
            Logger.LogDebug(name + " is not a Mare user. Clearing Moodles for " + OnlineUser.User.AliasOrUID, LoggerType.PairHandlers);
            await _ipcManager.Moodles.ClearStatusAsync(name).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Informs us if the pair is also a Mare User. If true, prevent clearing of data applied by GagSpeak that syncs with Mare.
    /// </summary>
    private async Task<bool> IsMareUser(nint address)
    {
        var handledMarePlayers = await _ipcManager.Mare.GetHandledMarePlayers().ConfigureAwait(false);
        if (handledMarePlayers == null) return false;

        // log the mare players.
        Logger.LogDebug("Mare Players: " + string.Join(", ", handledMarePlayers), LoggerType.IpcMare);
        return handledMarePlayers.Any(playerAddress => playerAddress == address);
    }
}
