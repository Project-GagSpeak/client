using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.CkCommons;
using GagSpeak.Kinkster.Factories;
using GagSpeak.Kinkster.Handlers;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Kinksters;

/// <summary> Stores information about a paired Kinkster. Managed by PairManager. </summary>
/// <remarks> Created by the PairFactory. PairHandler keeps tabs on the cachedPlayer. </remarks>
public class Pair : IComparable<Pair>
{
    private readonly ILogger<Pair> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly SemaphoreSlim _creationSemaphore = new(1);
    private readonly ServerConfigManager _nickConfig;
    private readonly CosmeticService _cosmetics;

    private CancellationTokenSource _applicationCts = new CancellationTokenSource();
    private OnlineKinkster? _OnlineKinkster = null;

    public Pair(KinksterPair pair, ILogger<Pair> logger, GagspeakMediator mediator,
        PairHandlerFactory factory, ServerConfigManager nicks, CosmeticService cosmetics)
    {
        _logger = logger;
        _mediator = mediator;
        _cachedPlayerFactory = factory;
        _nickConfig = nicks;
        _cosmetics = cosmetics;

        UserPair = pair;
    }

    // Permissions
    public KinksterPair UserPair { get; set; }
    public UserData UserData => UserPair.User;
    public PairPerms OwnPerms => UserPair.OwnPerms;
    public PairPermAccess OwnPermAccess => UserPair.OwnAccess;
    public GlobalPerms PairGlobals => UserPair.Globals;
    public PairPerms PairPerms => UserPair.Perms;
    public PairPermAccess PairPermAccess => UserPair.Access;

    // Latest cached data for this pair.
    private PairHandler? CachedPlayer { get; set; }

    public CharaIPCData LastIpcData { get; set; } = new();
    public CharaActiveGags LastGagData { get; set; } = new();
    public CharaActiveRestrictions LastRestrictionsData { get; set; } = new();
    public CharaActiveRestraint LastRestraintData { get; set; } = new();
    public IEnumerable<Guid> ActiveCursedItems { get; set; } = new List<Guid>();
    public AliasStorage LastGlobalAliasData { get; set; } = new();
    public NamedAliasStorage LastPairAliasData { get; set; } = new();
    public CharaToyboxData LastToyboxData { get; set; } = new();
    public CharaLightStorageData LastLightStorage { get; set; } = new();

    // Most of these attributes should be self explanatory, but they are public methods you can fetch from the pair manager.
    public bool HasCachedPlayer => CachedPlayer != null && !string.IsNullOrEmpty(CachedPlayer.PlayerName) && _OnlineKinkster != null;
    public OnlineKinkster CachedPlayerOnlineDto => CachedPlayer!.OnlineUser;
    public bool IsPaused => UserPair.OwnPerms.IsPaused;
    public bool IsOnline => CachedPlayer != null;
    public bool IsVisible => CachedPlayer?.IsVisible ?? false;
    public IGameObject? VisiblePairGameObject => IsVisible ? (CachedPlayer?.PairObject ?? null) : null;
    public string PlayerName => CachedPlayer?.PlayerName ?? UserData.AliasOrUID ?? string.Empty;  // Name of pair player. If empty, (pair handler) CachedData is not initialized yet.
    public string PlayerNameWithWorld => CachedPlayer?.PlayerNameWithWorld ?? string.Empty;
    public string CachedPlayerString() => CachedPlayer?.ToString() ?? "No Cached Player"; // string representation of the cached player.
    public Dictionary<EquipSlot, (EquipItem, string)> LockedSlots { get; private set; } = new(); // the locked slots of the pair. Used for quick reference in profile viewer.

    // IComparable satisfier
    public int CompareTo(Pair? other)
    {
        if (other is null)
            return 1;
        return string.Compare(UserData.UID, other.UserData.UID, StringComparison.Ordinal);
    }

    public void AddContextMenu(IMenuOpenedArgs args)
    {
        // if the visible player is not cached, not our target, or not a valid object, or paused, don't display.
        if (CachedPlayer == null || (args.Target is not MenuTargetDefault target) || target.TargetObjectId != VisiblePairGameObject?.GameObjectId || IsPaused) return;

        _logger.LogDebug("Adding Context Menu for " + UserData.UID, LoggerType.ContextDtr);

        // This only works when you create it prior to adding it to the args,
        // otherwise the += has trouble calling. (it would fall out of scope)
        /*var subMenu = new MenuItem();
        subMenu.IsSubmenu = true;
        subMenu.Name = "SubMenu Test Item";
        subMenu.PrefixChar = 'G';
        subMenu.PrefixColor = 561;
        subMenu.OnClicked += args => OpenSubMenuTest(args, _logger);
        args.AddMenuItem(subMenu);*/
        args.AddMenuItem(new MenuItem()
        {
            Name = new SeStringBuilder().AddText("Open KinkPlate").Build(),
            PrefixChar = 'G',
            PrefixColor = 561,
            OnClicked = (a) => { _mediator.Publish(new KinkPlateOpenStandaloneMessage(this)); },
        });

        args.AddMenuItem(new MenuItem()
        {
            Name = new SeStringBuilder().AddText("Pair Actions").Build(),
            PrefixChar = 'G',
            PrefixColor = 561,
            OnClicked = (a) => { _mediator.Publish(new OpenPairPerms(this, StickyWindowType.PairActionFunctions, true)); },
        });
    }

    private static unsafe void OpenSubMenuTest(IMenuItemClickedArgs args, ILogger logger)
    {
        // create some dummy test items.
        var menuItems = new List<MenuItem>();

        // dummy item 1
        var menuItem = new MenuItem();
        menuItem.Name = "SubMenu Test Item 1";
        menuItem.PrefixChar = 'G';
        menuItem.PrefixColor = 706;
        menuItem.OnClicked += clickedArgs => logger.LogInformation("Submenu Item 1 Clicked!", LoggerType.ContextDtr);

        menuItems.Add(menuItem);


        var menuItem2 = new MenuItem();
        menuItem2.Name = "SubMenu Test Item 2";
        menuItem2.PrefixChar = 'G';
        menuItem2.PrefixColor = 706;
        menuItem2.OnClicked += clickedArgs => logger.LogInformation("Submenu Item 2 Clicked!", LoggerType.ContextDtr);

        menuItems.Add(menuItem2);

        if (menuItems.Count > 0)
            args.OpenSubmenu(menuItems);
    }


    /// <summary> Update IPC Data </summary>
    public void UpdateVisibleData(KinksterUpdateIpc data)
    {
        _applicationCts = _applicationCts.CancelRecreate();
        LastIpcData = data.NewData;

        // if the cached player is null
        if (CachedPlayer is null)
        {
            // log that we received data for the user, but the cached player does not exist, and we are waiting.
            _logger.LogDebug("Received Data for " + data.User.UID + " but CachedPlayer does not exist, waiting", LoggerType.PairDataTransfer);
            // asynchronously run the following code
            _ = Task.Run(async () =>
            {
                using var timeoutCts = new CancellationTokenSource();
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(120));

                // create a new cancellation token source for the application token
                var appToken = _applicationCts.Token;
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, appToken);

                // while the cached player is still null and the combined token is not cancelled
                while (CachedPlayer is null && !combined.Token.IsCancellationRequested)
                    await Task.Delay(250, combined.Token).ConfigureAwait(false);

                // if the combined token is not cancelled STILL
                if (!combined.IsCancellationRequested)
                {
                    // apply the last received data
                    _logger.LogDebug("Applying delayed data for " + data.User.UID, LoggerType.PairDataTransfer);
                    ApplyLastIpcData(); // in essence, this means apply the character data send in the Dto
                }
            });
            return;
        }

        // otherwise, just apply the last received data.
        ApplyLastIpcData();
    }

    public void LoadCompositeData(KinksterUpdateComposite dto)
    {
        _logger.LogDebug("Received Character Composite Data from " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastGagData = dto.Data.Gags;
        LastRestrictionsData = dto.Data.Restrictions;
        LastRestraintData = dto.Data.Restraint;
        ActiveCursedItems = dto.Data.ActiveCursedItems;
        LastGlobalAliasData = dto.Data.GlobalAliasData;
        LastToyboxData = dto.Data.ToyboxData;
        LastLightStorage = dto.Data.LightStorageData;
        // Update KinkPlate display.
        UpdateCachedLockedSlots();
        // publish a mediator message that is listened to by the achievement manager for duration cleanup.
        _mediator.Publish(new PlayerLatestActiveItems(UserData, LastGagData, LastRestrictionsData, LastRestraintData));

        if (dto.WasSafeword)
            return;

        // Deterministic AliasData setting.
        if (dto.Data.PairAliasData.TryGetValue(UserData.UID, out var match))
            LastPairAliasData = match;
    }

    public void UpdateGagData(KinksterUpdateGagSlot data)
    {
        _logger.LogDebug("Applying updated gag data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastGagData.GagSlots[data.AffectedLayer] = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.PreviousGag, false, data.Enactor.UID, UserData.UID);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.NewData.GagItem, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.NewData.GagItem, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedLayer, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedLayer, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.PreviousGag, false, data.Enactor.UID, UserData.UID);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void UpdateRestrictionData(KinksterUpdateRestriction data)
    {
        _logger.LogDebug("Applying updated restriction data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastRestrictionsData.Restrictions[data.AffectedLayer] = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.PreviousRestriction, false, data.Enactor.UID, UserData.UID);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionLockStateChange, data.NewData.Identifier, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionLockStateChange, data.NewData.Identifier, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.PreviousRestriction, false, data.Enactor.UID, UserData.UID);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void UpdateRestraintData(KinksterUpdateRestraint data)
    {
        _logger.LogDebug("Applying updated restraint data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastRestraintData = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.PreviousRestraint, false, data.Enactor.UID, UserData.UID);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.NewData.Identifier, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.NewData.Identifier, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.PreviousRestraint, false, data.Enactor.UID, UserData.UID);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void UpdateCursedLootData(KinksterUpdateCursedLoot data)
    {
        _logger.LogDebug("Applying updated orders data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        ActiveCursedItems = data.ActiveItems;
        UpdateCachedLockedSlots();
    }

    public void UpdateToyboxData(KinksterUpdateToybox data)
    {
        _logger.LogDebug("Applying updated toybox data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastToyboxData = data.NewData;
    }

    public void UpdateGlobalAlias(AliasTrigger newData)
    {
        if (LastGlobalAliasData.FirstOrDefault(a => a.Identifier == newData.Identifier) is { } match)
        {
            _logger.LogDebug("Updating Global Alias for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
            match = newData;
            return;
        }
    }

    public void UpdateUniqueAlias(AliasTrigger newData)
    {
        if (LastPairAliasData.Storage.FirstOrDefault(a => a.Identifier == newData.Identifier) is { } match)
        {
            _logger.LogDebug("Updating Global Alias for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
            match = newData;
            return;
        }
    }

    public void UpdateListenerName(string nameWithWorld)
    {
        _logger.LogDebug("Updating Listener name to " + nameWithWorld, LoggerType.PairDataTransfer);
        LastPairAliasData.StoredNameWorld = nameWithWorld;
    }

    public void UpdateLightStorageData(KinksterUpdateLightStorage data)
    {
        _logger.LogDebug("Applying updated light storage data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastLightStorage = data.NewData;
    }

    public void ApplyLastIpcData(bool forced = false)
    {
        // ( This implies that the pair object has had its CreateCachedPlayer method called )
        if (CachedPlayer is null || LastIpcData is null) 
            return;
        // we have satisfied the conditions to apply the character data to our paired user, so apply it.
        CachedPlayer.ApplyCharacterData(Guid.NewGuid(), LastIpcData);
    }

    /// <summary> Method that creates the cached player (PairHandler) object for the client pair.
    /// <para> This method is ONLY EVER CALLED BY THE PAIR MANAGER under the <c>MarkPairOnline</c> method! </para>
    /// <remarks> Until the CachedPlayer object is made, the client will not apply any data sent from this paired user. </remarks>
    /// </summary>
    public void CreateCachedPlayer(OnlineKinkster? dto = null)
    {
        try
        {
            _creationSemaphore.Wait();
            // If the cachedPlayer is already stored for this pair, we do not need to create it again, so return.
            if (CachedPlayer != null)
            {
                _logger.LogDebug("CachedPlayer already exists for " + UserData.UID, LoggerType.PairInfo);
                return;
            }

            // if the Dto sent to us by the server is null, and the pairs OnlineKinkster is null, dispose of the cached player and return.
            if (dto is null && _OnlineKinkster is null)
            {
                // dispose of the cached player and set it to null before returning
                _logger.LogDebug("No DTO provided for {uid}, and OnlineKinkster object in Pair class is null. Disposing of CachedPlayer", UserData.UID);
                CachedPlayer?.Dispose();
                CachedPlayer = null;
                return;
            }

            // if the OnlineKinkster contains information, we should update our pairs _OnlineKinkster to the dto
            if (dto != null)
            {
                _logger.LogDebug("Updating OnlineKinkster for " + UserData.UID, LoggerType.PairInfo);
                _OnlineKinkster = dto;
            }

            _logger.LogTrace("Disposing of existing CachedPlayer to create a new one for " + UserData.UID, LoggerType.PairInfo);
            CachedPlayer?.Dispose();
            CachedPlayer = _cachedPlayerFactory.Create(new(UserData, _OnlineKinkster!.Ident));
        }
        finally
        {
            _creationSemaphore.Release();
        }
    }

    public void UpdateCachedLockedSlots()
    {
        var result = new Dictionary<EquipSlot, (EquipItem, string)>();

        // Rewrite this completely.
        _logger.LogDebug("Updated Locked Slots for " + UserData.UID, LoggerType.PairInfo);
        LockedSlots = result;
    }

    /// <summary> Get the nicknames for the user. </summary>
    public string? GetNickname()
    {
        return _nickConfig.GetNicknameForUid(UserData.UID);
    }

    public string GetNickAliasOrUid() => GetNickname() ?? UserData.AliasOrUID;

    /// <summary> Get the player name hash. </summary>
    public string GetPlayerNameHash()
    {
        return CachedPlayer?.PlayerNameHash ?? string.Empty;
    }

    /// <summary> Marks the pair as offline. </summary>
    public void MarkOffline()
    {
        try
        {
            _creationSemaphore.Wait();
            _OnlineKinkster = null;
            LastIpcData = new();
            // set the pair handler player to the cached player, to safely null the CachedPlayer object.
            var player = CachedPlayer;
            CachedPlayer = null;
            player?.Dispose();
            _logger.LogTrace("Marked " + UserData.UID + " as offline", LoggerType.PairManagement);
        }
        finally
        {
            _creationSemaphore.Release();
        }
    }
}
