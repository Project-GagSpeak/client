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

    // Penumbra collection (for when we manage pcp's)
    // private Guid _penumbraCollectionId;
    private Guid? _activeCustomize = null;

    // Cached, nullable data.
    private CharaIpcDataFull? _appearance = null;
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
        // _penumbraCollectionId = _ipc.Penumbra.CreateTemporaryCollection(OnlineUser.User.UID).ConfigureAwait(false).GetAwaiter().GetResult();

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
            // safely dispose of the kinkster game object.
            _gameObject?.Dispose();
            _gameObject = null;

            // if the pair was a visible kinkster, publish their disposal.
            if (!string.IsNullOrEmpty(name))
                Mediator.Publish(new EventMessage(new(name, OnlineUser.User.UID, InteractionType.VisibilityChange, "Disposing Kinkster Handler")));

            // if the hosted service lifetime is ending, return
            if (_lifetime.ApplicationStopping.IsCancellationRequested)
                return;

            // If not zoning, and the player is being disposed, this kinkster has left the zone, and we need to invalidate them.
            if (!PlayerData.IsZoning && !string.IsNullOrEmpty(name))
            {
                Logger.LogTrace($"Restoring Vanilla state for Kinkster: {name} ({OnlineUser})", LoggerType.PairHandlers);
                // if they are not not visible (have no valid pointer) we need to revert them by name.
                if (!IsVisible)
                {
                    // Glamourer is special as it will not revert the data if they are not present.
                    Logger.LogDebug($"Reverting Glamour to Vanilla state for Kinkster: {name}", LoggerType.PairHandlers);
                    _ipc.Glamourer.ReleaseKinksterByName(name).GetAwaiter().GetResult();
                }
                // otherwise, revert ALL data if they are visible!
                else
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(30));
                    Logger.LogInformation($"Is Kinkster IPCData null? {_appearance is null}", LoggerType.PairHandlers);
                    // catch inside inner exception just incase.
                    RevertAppearanceData(name).GetAwaiter().GetResult();
                }
            }
        });

        // Once over, null the data.
        PlayerName = null;
        _appearance = null;
        Logger.LogDebug($"Disposal complete for Kinkster: {name} ({OnlineUser})", LoggerType.PairHandlers);
    }

    public async Task ApplyAppearanceData(CharaIpcDataFull newData)
    {
        if (PairAddress == nint.Zero) return;
        if (_appearance is null) _appearance = new CharaIpcDataFull();

        // may need to process a cancellation token here if overlap occurs, but it shouldnt due to updates being 1s apart.

        // Process the application of all non-null data.
        Logger.LogDebug($"Updating appearance for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
        // maybe wait for redraw finish? idk..
        await ApplyUpdatedAppearance(newData).ConfigureAwait(false);

        // maybe some redraw logic here, maybe not, we'll see.

        // Mark as updated.
        _appearance.UpdateNonNull(newData);
        Logger.LogInformation($"Updated appearance for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
    }

    public async Task ApplyAppearanceSingle(DataSyncKind type, string newDataString)
    {
        if (PairAddress == nint.Zero) return;
        if (_appearance is null) return;
        // apply the new data.
        switch (type)
        {
            case DataSyncKind.Glamourer when !newDataString.Equals(_appearance.GlamourerBase64):
                await _ipc.Glamourer.ApplyKinksterGlamour(this, newDataString).ConfigureAwait(false);
                break;
            case DataSyncKind.CPlus when !newDataString.Equals(_appearance.CustomizeProfile):
                _activeCustomize = await _ipc.CustomizePlus.SetKinksterProfile(this, newDataString).ConfigureAwait(false);
                break;
            case DataSyncKind.Heels when !newDataString.Equals(_appearance.HeelsOffset):
                await _ipc.Heels.SetKinksterOffset(this, newDataString).ConfigureAwait(false);
                break;
            case DataSyncKind.Honorific when !newDataString.Equals(_appearance.HonorificTitle):
                await _ipc.Honorific.SetTitleAsync(this, newDataString).ConfigureAwait(false);
                break;
            case DataSyncKind.PetNames when !newDataString.Equals(_appearance.PetNicknames):
                await _ipc.PetNames.SetKinksterPetNames(this, newDataString).ConfigureAwait(false);
                break;
            default:
                return;
        }
        // update the appearance data.
        Logger.LogDebug($"Updated {type} for Kinkster: {PlayerName} ({OnlineUser.User.AliasOrUID})", LoggerType.PairHandlers);
        _appearance.UpdateNewData(type, newDataString);
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

    private async Task ApplyUpdatedAppearance(CharaIpcDataFull newData, bool force = false)
    {
        // Apply Glamour if different.
        if (newData.GlamourerBase64 != null && (force || !newData.GlamourerBase64.Equals(_appearance!.GlamourerBase64)))
            await _ipc.Glamourer.ApplyKinksterGlamour(this, newData.GlamourerBase64).ConfigureAwait(false);
        
        // Apply Customize+ if different.
        if (newData.CustomizeProfile != null && (force || !newData.CustomizeProfile.Equals(_appearance!.CustomizeProfile)))
        {
            // update the active profile to what we set, we should do this if there is a difference in what profile to enforce.
            // this should also revert if the new string is empty.
            _activeCustomize = await _ipc.CustomizePlus.SetKinksterProfile(this, newData.CustomizeProfile).ConfigureAwait(false);
        }
        // might need to do an else if or something here as C+ works wierd with how it does reverts.

        // Apply Heels if different.
        if (newData.HeelsOffset != null && (force || !newData.HeelsOffset.Equals(_appearance!.HeelsOffset)))
            await _ipc.Heels.SetKinksterOffset(this, newData.HeelsOffset).ConfigureAwait(false);

        // Apply Honorific if different. (will clear title if string.empty)
        if (newData.HonorificTitle != null && (force || !newData.HonorificTitle.Equals(_appearance!.HonorificTitle)))
            await _ipc.Honorific.SetTitleAsync(this, newData.HonorificTitle).ConfigureAwait(false);

        // Apply Pet Nicknames if different. (will clear nicks if string.empty)
        if (newData.PetNicknames != null && (force || !newData.PetNicknames.Equals(_appearance!.PetNicknames)))
            await _ipc.PetNames.SetKinksterPetNames(this, newData.PetNicknames).ConfigureAwait(false);
    }

    private async Task RevertAppearanceData(string name)
    {
        // ensure the correct address is obtained (to cover this being called in disposal)
        nint addr = _frameworkUtil.GetKinksterAddrFromCache(OnlineUser.Ident);
        // ret if the address is zero/null;
        if (addr == nint.Zero) return;
        // if our cached data is null, nothing is set, so we have nothing to revert.
        if (_appearance is null) return;

        // Begin the Kinkster revert process.
        Logger.LogDebug($"Reverting ALL appearance data for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.PairHandlers);
        if (_appearance.GlamourerBase64 is not null)
        {
            Logger.LogTrace($"Reverting Glamourer state for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.Glamourer.ReleaseKinkster(this).ConfigureAwait(false);
        }
        if (_appearance.HeelsOffset is not null)
        {
            Logger.LogTrace($"Reverting Heels state for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.Heels.RestoreKinksterOffset(this).ConfigureAwait(false);
        }
        if (_activeCustomize is not null)
        {
            Logger.LogTrace($"Reverting Customize+ Data for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.CustomizePlus.RevertKinksterProfile(_activeCustomize).ConfigureAwait(false);
        }
        if (_appearance.HonorificTitle is not null)
        {
            Logger.LogTrace($"Reverting Honorific Title for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.Honorific.ClearTitleAsync(this).ConfigureAwait(false);
        }
        if (_appearance.PetNicknames is not null)
        {
            Logger.LogTrace($"Reverting Pet Nicknames for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.PetNames.ClearKinksterPetNames(this).ConfigureAwait(false);
        }
        if (_statusManagerStr is not null)
        {
            Logger.LogTrace($"Reverting Moodles Data for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.GameObjects);
            await _ipc.Moodles.ClearStatus(name).ConfigureAwait(false);
        }

        Logger.LogInformation($"Reverted ALL appearance data for Kinkster: {name} ({OnlineUser.User.AliasOrUID})", LoggerType.PairHandlers);
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
            if (_appearance is not null)
                _ = Task.Run(() => ApplyUpdatedAppearance(_appearance, true));
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

        Mediator.Subscribe<HonorificReady>(this, async _ =>
        {
            if (string.IsNullOrEmpty(_appearance?.HonorificTitle)) 
                return;
            Logger.LogTrace($"Reapplying Honorific Title for {PlayerName}");
            await _ipc.Honorific.SetTitleAsync(this, _appearance.HonorificTitle).ConfigureAwait(false);
        });

        Mediator.Subscribe<PetNamesReady>(this, async _ =>
        {
            if (string.IsNullOrEmpty(_appearance?.PetNicknames))
                return;
            Logger.LogTrace($"Reapplying Pet Nicknames for {PlayerName}");
            await _ipc.PetNames.SetKinksterPetNames(this, _appearance.PetNicknames).ConfigureAwait(false);
        });
    }
}
