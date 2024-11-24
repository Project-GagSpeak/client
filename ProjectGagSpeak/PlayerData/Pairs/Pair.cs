using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.PlayerData.Factories;
using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Utils;
using GagSpeak.WebAPI.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerData.Pairs;

/// <summary> 
/// Stores information about a paired user of the client.
/// <para> 
/// The Pair object is created by the PairFactory, which is responsible for generating pair objects.
/// These pair objects are then created and deleted via the pair manager 
/// The pair handler is what helps with the management of the CachedPlayer.
/// </para>
/// </summary>
public class Pair
{
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly SemaphoreSlim _creationSemaphore = new(1);
    private readonly ILogger<Pair> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly ServerConfigurationManager _serverConfigs;
    private readonly CosmeticService _cosmetics;

    private CancellationTokenSource _applicationCts = new CancellationTokenSource();
    private OnlineUserIdentDto? _onlineUserIdentDto = null;

    public Pair(ILogger<Pair> logger, UserPairDto userPair,
        PairHandlerFactory cachedPlayerFactory, GagspeakMediator mediator,
        ServerConfigurationManager serverConfigs, CosmeticService cosmetics)
    {
        _logger = logger;
        _cachedPlayerFactory = cachedPlayerFactory;
        _mediator = mediator;
        _serverConfigs = serverConfigs;
        _cosmetics = cosmetics;
        UserPair = userPair;
    }

    /// <summary> 
    /// The object that is responcible for handling the state of the pairs gameobject handler.
    /// </summary>
    private PairHandler? CachedPlayer { get; set; }

    /// <summary> 
    /// THE IMPORTANT DATA FOR THE PAIR OBJECT
    /// <para>
    /// This UserPairDto object, while simple, contains ALL of the pairs global, pair, and edit access permissions.
    /// This means any permission that is being modified will be accessing this object directly, 
    /// and is set whenever the pair is added. Initialized in the constructor on pair creation
    /// </para>
    /// </summary>
    public UserPairDto UserPair { get; set; }
    public UserData UserData => UserPair.User;
    public bool OnlineToyboxUser { get; private set; } = false;
    public UserPairPermissions OwnPerms => UserPair.OwnPairPerms;
    public UserEditAccessPermissions OwnPermAccess => UserPair.OwnEditAccessPerms;
    public UserPairPermissions PairPerms => UserPair.OtherPairPerms;
    public UserEditAccessPermissions PairPermAccess => UserPair.OtherEditAccessPerms;
    public UserGlobalPermissions PairGlobals => UserPair.OtherGlobalPerms;

    // Latest cached data for this pair.
    public CharaIPCData? LastIpcData { get; set; }
    public CharaAppearanceData? LastAppearanceData { get; set; }
    public CharaWardrobeData? LastWardrobeData { get; set; }
    public CharaAliasData? LastAliasData { get; set; }
    public CharaToyboxData? LastToyboxData { get; set; }
    public CharaStorageData? LastLightStorage { get; set; }


    // Most of these attributes should be self explanatory, but they are public methods you can fetch from the pair manager.
    public bool HasCachedPlayer => CachedPlayer != null && !string.IsNullOrEmpty(CachedPlayer.PlayerName) && _onlineUserIdentDto != null;
    public IndividualPairStatus IndividualPairStatus => UserPair.IndividualPairStatus;  // the individual pair status of the pair in relation to the client.
    public bool IsDirectlyPaired => IndividualPairStatus != IndividualPairStatus.None;  // if the pair is directly paired.
    public bool IsOneSidedPair => IndividualPairStatus == IndividualPairStatus.OneSided; // if the pair is one sided.
    public OnlineUserIdentDto CachedPlayerOnlineDto => CachedPlayer!.OnlineUser;       // the online user ident dto of the cached player.
    public bool IsPaired => IndividualPairStatus == IndividualPairStatus.Bidirectional; // if the user is paired bidirectionally.
    public bool IsPaused => UserPair.OwnPairPerms.IsPaused;
    public bool IsOnline => CachedPlayer != null;                                       // lets us know if the paired user is online. 
    public bool IsVisible => CachedPlayer?.IsVisible ?? false;                          // if the paired user is visible.
    public IGameObject? VisiblePairGameObject => IsVisible ? (CachedPlayer?.PairObject ?? null) : null; // the visible pair game object.
    public string PlayerName => CachedPlayer?.PlayerName ?? UserData.AliasOrUID ?? string.Empty;  // Name of pair player. If empty, (pair handler) CachedData is not initialized yet.
    public string PlayerNameWithWorld => CachedPlayer?.PlayerNameWithWorld ?? string.Empty;
    public string CachedPlayerString() => CachedPlayer?.ToString() ?? "No Cached Player"; // string representation of the cached player.
    public Dictionary<EquipSlot, (EquipItem, string)> LockedSlots { get; private set; } = new(); // the locked slots of the pair. Used for quick reference in profile viewer.


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
            OnClicked = (a) => { _mediator.Publish(new OpenUserPairPermissions(this, StickyWindowType.PairActionFunctions, true)); },
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
    public void ApplyVisibleData(OnlineUserCharaIpcDataDto data)
    {
        _applicationCts = _applicationCts.CancelRecreate();
        // set the last received character data to the data.CharaData
        LastIpcData = data.IPCData;

        // if the cached player is null
        if (CachedPlayer == null)
        {
            // log that we received data for the user, but the cached player does not exist, and we are waiting.
            _logger.LogDebug("Received Data for " + data.User.UID + " but CachedPlayer does not exist, waiting", LoggerType.PairDataTransfer);
            // asynchronously run the following code
            _ = Task.Run(async () =>
            {
                // create a new cancellation token source
                using var timeoutCts = new CancellationTokenSource();
                // auto cancel it after 120 seconds
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(120));
                // create a new cancellation token source for the application token
                var appToken = _applicationCts.Token;
                // create a new linked token source with the timeout token and the application token
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, appToken);

                // while the cached player is still null and the combined token is not cancelled
                while (CachedPlayer == null && !combined.Token.IsCancellationRequested)
                {
                    // wait for 250 milliseconds
                    await Task.Delay(250, combined.Token).ConfigureAwait(false);
                }

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

    /// <summary>
    /// Applied updated Gag Appearance Data for the user pair. 
    /// This is sent to all online players, not just visible.
    /// </summary>
    public void ApplyAppearanceData(OnlineUserCharaAppearanceDataDto data)
    {
        _logger.LogDebug("Applying updated appearance data for " + data.User.UID, LoggerType.PairDataTransfer);
        LastAppearanceData = data.AppearanceData;

        // update the locked slots.
        if (data.UpdateKind is DataUpdateKind.AppearanceGagAppliedLayerOne or DataUpdateKind.AppearanceGagAppliedLayerTwo or DataUpdateKind.AppearanceGagAppliedLayerThree
            or DataUpdateKind.AppearanceGagRemovedLayerOne or DataUpdateKind.AppearanceGagRemovedLayerTwo or DataUpdateKind.AppearanceGagRemovedLayerThree)
        {
            UpdateCachedLockedSlots();
        }
    }

    /// <summary>
    /// Applied updated Gag Appearance Data for the user pair. 
    /// This is sent to all online players, not just visible.
    /// </summary>
    public void ApplyWardrobeData(OnlineUserCharaWardrobeDataDto data)
    {
        _logger.LogDebug("Applying updated wardrobe data for " + data.User.UID, LoggerType.PairDataTransfer);
        var previousSetId = LastWardrobeData?.ActiveSetId ?? Guid.Empty;
        var previousLock = LastWardrobeData?.Padlock.ToPadlock() ?? Padlocks.None;

        LastWardrobeData = data.WardrobeData;

        // depend on the EnabledBy field to know if we applied.
        if (data.UpdateKind is DataUpdateKind.WardrobeRestraintApplied)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintApplied, data.WardrobeData.ActiveSetId, true, data.WardrobeData.ActiveSetEnabledBy);

        // We can only detect the lock uid by listening for the assigner UID. Unlocks are processed via the actions tab.
        if (data.UpdateKind is DataUpdateKind.WardrobeRestraintLocked)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.WardrobeData.ActiveSetId, data.WardrobeData.Padlock.ToPadlock(), true, data.WardrobeData.Assigner, UserData.UID);

        // We can only detect the unlock uid by listening for the assigner UID. Unlocks are processed via the actions tab.
        if (data.UpdateKind is DataUpdateKind.WardrobeRestraintUnlocked)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.WardrobeData.ActiveSetId, previousLock, false, data.Enactor.UID, UserData.UID);

        // For removal
        if (data.UpdateKind is DataUpdateKind.WardrobeRestraintDisabled)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PairRestraintApplied, previousSetId, false, data.Enactor.UID);

        // if the type was apply or removal, we should update the locked slots.
        if (data.UpdateKind is DataUpdateKind.WardrobeRestraintApplied or DataUpdateKind.WardrobeRestraintDisabled)
            UpdateCachedLockedSlots();

    }

    /// <summary>
    /// Applies the restraint set information the user pair has allowed you to see.
    /// This is sent to all online players, not just visible.
    /// </summary>
    public void ApplyAliasData(OnlineUserCharaAliasDataDto data)
    {
        _logger.LogDebug("Applying updated alias data for " + data.User.UID, LoggerType.PairDataTransfer);
        // update either the name associated to the list, or the list itself.
        if (LastAliasData == null)
        {
            LastAliasData = data.AliasData;
        }

        // otherwise, update the appropriate part.
        if (data.UpdateKind is DataUpdateKind.PuppeteerAliasListUpdated)
        {
            LastAliasData.AliasList = data.AliasData.AliasList;
        }
        else if (data.UpdateKind is DataUpdateKind.PuppeteerPlayerNameRegistered)
        {
            LastAliasData.CharacterName = data.AliasData.CharacterName;
            LastAliasData.CharacterWorld = data.AliasData.CharacterWorld;
        }
        else if (data.UpdateKind is DataUpdateKind.FullDataUpdate)
        {
            LastAliasData = data.AliasData;
        }
        else
        {
            _logger.LogWarning("Unknown Set Type: " + data.UpdateKind);
        }
    }

    public void ApplyToyboxData(OnlineUserCharaToyboxDataDto data)
    {
        _logger.LogDebug("Applying updated toybox data for " + data.User.UID, LoggerType.PairDataTransfer);
        LastToyboxData = data.ToyboxInfo;
    }

    public void ApplyLightStorageData(OnlineUserStorageUpdateDto data)
    {
        _logger.LogDebug("Applying updated light storage data for " + data.User.UID, LoggerType.PairDataTransfer);
        LastLightStorage = data.LightStorage;
    }

    public void ApplyLastIpcData(bool forced = false)
    {
        // ( This implies that the pair object has had its CreateCachedPlayer method called )
        if (CachedPlayer == null) return;

        // if the last received character data is null, return and do not apply.
        if (LastIpcData == null) return;

        // we have satisfied the conditions to apply the character data to our paired user, so apply it.
        CachedPlayer.ApplyCharacterData(Guid.NewGuid(), LastIpcData);
    }

    /// <summary> 
    /// Method that creates the cached player (PairHandler) object for the client pair.
    /// <para> 
    /// This method is ONLY EVER CALLED BY THE PAIR MANAGER under the <c>MarkPairOnline</c> method!
    /// </para>
    /// <para> 
    /// Until the CachedPlayer object is made, the client will not apply any data sent from this paired user.
    /// </para>
    /// </summary>
    public void CreateCachedPlayer(OnlineUserIdentDto? dto = null)
    {
        try
        {
            // wait for the creation semaphore to be available
            _creationSemaphore.Wait();

            // If the cachedPlayer is already stored for this pair, we do not need to create it again, so return.
            if (CachedPlayer != null)
            {
                _logger.LogDebug("CachedPlayer already exists for " + UserData.UID, LoggerType.PairInfo);
                return;
            }

            // if the Dto sent to us by the server is null, and the pairs onlineUserIdentDto is null, dispose of the cached player and return.
            if (dto == null && _onlineUserIdentDto == null)
            {
                // dispose of the cachedplayer and set it to null before returning
                _logger.LogDebug("No DTO provided for {uid}, and OnlineUserIdentDto object in Pair class is null. Disposing of CachedPlayer", UserData.UID);
                CachedPlayer?.Dispose();
                CachedPlayer = null;
                return;
            }

            // if the OnlineUserIdentDto contains information, we should update our pairs _onlineUserIdentDto to the dto
            if (dto != null)
            {
                _logger.LogDebug("Updating OnlineUserIdentDto for " + UserData.UID, LoggerType.PairInfo);
                _onlineUserIdentDto = dto;
            }

            _logger.LogTrace("Disposing of existing CachedPlayer to create a new one for " + UserData.UID, LoggerType.PairInfo);
            // not we can dispose of the cached player
            CachedPlayer?.Dispose();
            // and create a new one from our _cachedPlayerFactory (the pair handler factory)
            CachedPlayer = _cachedPlayerFactory.Create(new OnlineUserIdentDto(UserData, _onlineUserIdentDto!.Ident));
        }
        finally
        {
            // release the creation semaphore
            _creationSemaphore.Release();
        }
    }

    public void UpdateCachedLockedSlots()
    {
        var result = new Dictionary<EquipSlot, (EquipItem, string)>();
        // return false instantly if the wardrobe data or light data is null.
        if (LastWardrobeData is null || LastLightStorage is null || LastAppearanceData is null)
        {
            _logger.LogWarning("Wardrobe or LightStorage Data is null for " + UserData.UID);
            return;
        }

        // we must check in the priority of Cursed Items -> Blindfold -> Gag -> Restraints

        // If the pair has any cursed items active.
        if (LastWardrobeData.ActiveCursedItems.Any())
        {
            // iterate through the active cursed items, and stop at the first
            foreach (var cursedItem in LastWardrobeData.ActiveCursedItems)
            {
                // locate the light cursed item associated with the ID.
                var lightCursedItem = LastLightStorage.CursedItems.FirstOrDefault(x => x.Identifier == cursedItem);
                if (lightCursedItem != null)
                {
                    // if the cursed item is a Gag, locate the gag glamour first.
                    if (lightCursedItem.IsGag)
                    {
                        if (LastLightStorage.GagItems.TryGetValue(lightCursedItem.GagType, out var gagItem))
                        {
                            // if the gag item's slot is not yet occupied by anything, add it, otherwise, skip.
                            if (!result.ContainsKey((EquipSlot)gagItem.Slot))
                                result.Add(
                                    (EquipSlot)gagItem.Slot,
                                    (ItemIdVars.Resolve((EquipSlot)gagItem.Slot, new CustomItemId(gagItem.CustomItemId)),
                                    gagItem.Tooltip));
                        }
                        continue; // Move to next item. (Early Skip)
                    }

                    // Cursed Item should be referenced by its applied item instead, so check to see if its already in the dictionary, if it isnt, add it.
                    if (!result.ContainsKey((EquipSlot)lightCursedItem.AffectedSlot.Slot))
                        result.Add(
                            (EquipSlot)lightCursedItem.AffectedSlot.Slot,
                            (ItemIdVars.Resolve((EquipSlot)lightCursedItem.AffectedSlot.Slot, new CustomItemId(lightCursedItem.AffectedSlot.CustomItemId)),
                            lightCursedItem.AffectedSlot.Tooltip));
                }
            }
        }

        // Next check the blindfold item if they are blindfolded.
        if (PairGlobals.IsBlindfolded())
        {
            // add the blindfold item to a slot if it is not occupied.
            if (!result.ContainsKey((EquipSlot)LastLightStorage.BlindfoldItem.Slot))
                result.Add(
                    (EquipSlot)LastLightStorage.BlindfoldItem.Slot,
                    (ItemIdVars.Resolve((EquipSlot)LastLightStorage.BlindfoldItem.Slot, new CustomItemId(LastLightStorage.BlindfoldItem.CustomItemId)),
                    LastLightStorage.BlindfoldItem.Tooltip));
        }

        // next iterate through our locked gags, adding any gag glamour's to locked slots if they are present.
        foreach (var gagSlot in LastAppearanceData.GagSlots.Where(x => x.GagType.ToGagType() is not GagType.None))
        {
            // if the pairs stored gag items contains a glamour for that item, attempt to add it, if possible.
            if (LastLightStorage.GagItems.TryGetValue(gagSlot.GagType.ToGagType(), out var gagItem))
                if (!result.ContainsKey((EquipSlot)gagItem.Slot))
                    result.Add((EquipSlot)gagItem.Slot, (ItemIdVars.Resolve((EquipSlot)gagItem.Slot, new CustomItemId(gagItem.CustomItemId)), gagItem.Tooltip));
        }

        // finally, locate the active restraint set, and iterate through the restraint item glamours, adding them if not already a part of the dictionary.
        if (!LastWardrobeData.ActiveSetId.IsEmptyGuid())
        {
            var activeSet = LastLightStorage.Restraints.FirstOrDefault(x => x.Identifier == LastWardrobeData.ActiveSetId);
            if (activeSet is not null)
            {
                foreach (var restraintAffectedSlot in activeSet.AffectedSlots)
                    if (!result.ContainsKey((EquipSlot)restraintAffectedSlot.Slot))
                        result.Add((EquipSlot)restraintAffectedSlot.Slot, (ItemIdVars.Resolve((EquipSlot)restraintAffectedSlot.Slot, new CustomItemId(restraintAffectedSlot.CustomItemId)), restraintAffectedSlot.Tooltip));
            }
        }
        _logger.LogDebug("Updated Locked Slots for " + UserData.UID, LoggerType.PairInfo);
        LockedSlots = result;
    }

    /// <summary> Get the nicknames for the user. (still dont know how this is meant to have any value at all) </summary>
    public string? GetNickname()
    {
        return _serverConfigs.GetNicknameForUid(UserData.UID);
    }

    public string GetNickAliasOrUid() => GetNickname() ?? UserData.AliasOrUID;

    /// <summary> Get the player name hash. </summary>
    public string GetPlayerNameHash()
    {
        return CachedPlayer?.PlayerNameHash ?? string.Empty;
    }

    /// <summary> A boolean of if the pair has any connection to the client. </summary>
    public bool HasAnyConnection()
    {
        return UserPair.IndividualPairStatus != IndividualPairStatus.None;
    }

    /// <summary> Marks the pair as offline. </summary>
    public void MarkOffline()
    {
        try
        {
            // block current thread until it can enter the semaphore
            _creationSemaphore.Wait();
            // set the online user ident dto to null
            _onlineUserIdentDto = null;
            // set the last received character data to null
            LastIpcData = null;
            // set the pair handler player = to the cached player
            var player = CachedPlayer;
            // set the cached player to null
            CachedPlayer = null;
            // dispose of the player object.
            player?.Dispose();
            // log the pair as offline
            _logger.LogTrace("Marked " + UserData.UID + " as offline", LoggerType.PairManagement);
        }
        finally
        {
            // release the creation semaphore
            _creationSemaphore.Release();
        }
    }

    public void MarkToyboxOffline() => OnlineToyboxUser = false;

    public void MarkToyboxOnline() => OnlineToyboxUser = true;
}
