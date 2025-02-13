using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.CkCommons;
using GagSpeak.PlayerData.Data;
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
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using GagspeakAPI.Extensions;
using OtterGui.Text.Widget.Editors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.PlayerData.Pairs;

/// <summary> Stores information about a paired user of the client.
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

    /// <summary> The object that is responsible for handling the state of the pairs game object handler. </summary>
    private PairHandler? CachedPlayer { get; set; }

    /// <summary> This UserPairDto object, contains ALL of the pairs global, pair, and edit access permissions. </summary>
    /// <remarks> Thus, any permission modified will be accessing this object directly, and is defined upon a pair being added or marked online. </remarks>
    public UserPairDto UserPair { get; set; }
    public UserData UserData => UserPair.User;
    public bool OnlineToyboxUser { get; private set; } = false;
    public UserPairPermissions OwnPerms => UserPair.OwnPairPerms;
    public UserEditAccessPermissions OwnPermAccess => UserPair.OwnEditAccessPerms;
    public UserPairPermissions PairPerms => UserPair.OtherPairPerms;
    public UserEditAccessPermissions PairPermAccess => UserPair.OtherEditAccessPerms;
    public UserGlobalPermissions PairGlobals => UserPair.OtherGlobalPerms;

    // Latest cached data for this pair.
    public CharaIPCData LastIpcData { get; set; } = new();
    public CharaActiveGags LastAppearanceData { get; set; } = new();
    public CharaActiveRestrictions LastRestrictionsData { get; set; } = new();
    public CharaActiveRestraint LastRestraintData { get; set; } = new();
    public List<Guid> ActiveCursedItems { get; set; } = new();
    public CharaOrdersData LastOrdersData { get; set; } = new();
    public CharaAliasData LastAliasData { get; set; } = new();
    public CharaToyboxData LastToyboxData { get; set; } = new();
    public CharaLightStorageData LastLightStorage { get; set; } = new();

    // Most of these attributes should be self explanatory, but they are public methods you can fetch from the pair manager.
    public bool HasCachedPlayer => CachedPlayer != null && !string.IsNullOrEmpty(CachedPlayer.PlayerName) && _onlineUserIdentDto != null;
    public OnlineUserIdentDto CachedPlayerOnlineDto => CachedPlayer!.OnlineUser;
    public bool IsPaused => UserPair.OwnPairPerms.IsPaused;
    public bool IsOnline => CachedPlayer != null;
    public bool IsVisible => CachedPlayer?.IsVisible ?? false;
    public IGameObject? VisiblePairGameObject => IsVisible ? (CachedPlayer?.PairObject ?? null) : null;
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
    public void UpdateVisibleData(CallbackIpcDataDto data)
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

    public void LoadCompositeData(OnlineUserCompositeDataDto dto)
    {
        _logger.LogDebug("Received Character Composite Data from " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastAppearanceData = dto.CompositeData.Gags;
        LastRestrictionsData = dto.CompositeData.Restrictions;
        LastRestraintData = dto.CompositeData.Restraint;
        ActiveCursedItems = dto.CompositeData.CursedItems;
        LastOrdersData = new CharaOrdersData();
        LastToyboxData = dto.CompositeData.ToyboxData;
        LastLightStorage = dto.CompositeData.LightStorageData;
        // Update Kinkplate display.
        UpdateCachedLockedSlots();
        // publish a mediator message that is listened to by the achievement manager for duration cleanup.
        _mediator.Publish(new PlayerLatestActiveItems(UserData, LastAppearanceData, LastRestrictionsData, LastRestraintData));

        if (dto.WasSafeword)
            return;

        // Deterministic AliasData setting.
        var hasUser = dto.CompositeData.AliasData.ContainsKey(UserData.UID);
        LastAliasData = hasUser ? dto.CompositeData.AliasData[UserData.UID] : new CharaAliasData();
    }

    public void UpdateGagData(CallbackGagDataDto data)
    {
        _logger.LogDebug("Applying updated gag data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastAppearanceData.GagSlots[(int)data.AffectedSlot] = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedSlot, data.PreviousGag, false, data.Enactor.UID, UserData.UID);
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedSlot, data.NewData.GagItem, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedSlot, data.NewData.GagItem, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedSlot, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedSlot, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                UnlocksEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedSlot, data.PreviousGag, false, data.Enactor.UID, UserData.UID);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void UpdateRestrictionData(CallbackRestrictionDataDto data)
    {
        _logger.LogDebug("Applying updated restriction data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastRestrictionsData.Restrictions[data.AffectedIndex] = data.NewData;

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

    public void UpdateRestraintData(CallbackRestraintDataDto data)
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

    public void UpdateCursedLootData(CallbackCursedLootDto data)
    {
        _logger.LogDebug("Applying updated orders data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        ActiveCursedItems = data.NewActiveItems;
        UpdateCachedLockedSlots();
    }

    public void UpdateOrdersData(CallbackOrdersDataDto data)
    {
        _logger.LogDebug("Applying updated orders data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastOrdersData = data.NewData;
    }

    public void UpdateAliasData(CallbackAliasDataDto data)
    {
        _logger.LogDebug("Applying updated alias data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        switch(data.Type)
        {
            case DataUpdateType.AliasListUpdated:
                LastAliasData.AliasList = data.NewData.AliasList;
                return;
            case DataUpdateType.NameRegistered:
                _logger.LogTrace("Updating Listener name to " + data.NewData.ListenerName + " and HasStored to " + data.NewData.HasNameStored, LoggerType.PairDataTransfer);
                LastAliasData.HasNameStored = data.NewData.HasNameStored;
                LastAliasData.ListenerName = data.NewData.ListenerName;
                return;
            default:
                _logger.LogWarning("Invalid Update Type!");
                break;
        }
    }

    public void UpdateToyboxData(CallbackToyboxDataDto data)
    {
        _logger.LogDebug("Applying updated toybox data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastToyboxData = data.NewData;
    }

    public void UpdateLightStorageData(CallbackLightStorageDto data)
    {
        _logger.LogDebug("Applying updated light storage data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        LastLightStorage = data.LightStorage;
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
    public void CreateCachedPlayer(OnlineUserIdentDto? dto = null)
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

            // if the Dto sent to us by the server is null, and the pairs onlineUserIdentDto is null, dispose of the cached player and return.
            if (dto is null && _onlineUserIdentDto is null)
            {
                // dispose of the cached player and set it to null before returning
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
            CachedPlayer?.Dispose();
            CachedPlayer = _cachedPlayerFactory.Create(new OnlineUserIdentDto(UserData, _onlineUserIdentDto!.Ident));
        }
        finally
        {
            _creationSemaphore.Release();
        }
    }

    public void UpdateCachedLockedSlots()
    {
        var result = new Dictionary<EquipSlot, (EquipItem, string)>();
        // return false instantly if the wardrobe data or light data is null.
        if (LastRestraintData is null || LastRestrictionsData is null || LastLightStorage is null || LastAppearanceData is null)
        {
            _logger.LogDebug("Wardrobe or LightStorage Data is null for " + UserData.UID, LoggerType.PairDataTransfer);
            return;
        }

        // we must check in the priority of Cursed Items -> Gag -> Restrictions -> Restraints

        // If the pair has any cursed items active.
        if (ActiveCursedItems.Any())
        {
            // iterate through the active cursed items, and stop at the first
            foreach (var cursedItem in ActiveCursedItems)
            {
                // locate the light cursed item associated with the ID.
                var lightCursedItem = LastLightStorage.CursedItems.FirstOrDefault(x => x.Id == cursedItem);
                if (lightCursedItem != null)
                {
                    // if the cursed item is a Gag, locate the gag glamour first.
                    if (lightCursedItem.Type is RestrictionType.Gag)
                    {
/*                        if (LastLightStorage.GagItems.TryGetValue(lightCursedItem.GagType, out var gagItem))
                        {
                            // if the gag item's slot is not yet occupied by anything, add it, otherwise, skip.
                            if (!result.ContainsKey((EquipSlot)gagItem.Slot))
                                result.Add(
                                    (EquipSlot)gagItem.Slot,
                                    (ItemIdVars.Resolve((EquipSlot)gagItem.Slot, new CustomItemId(gagItem.CustomItemId)),
                                    gagItem.Tooltip));
                        }*/
                        continue; // Move to next item. (Early Skip)
                    }

                    // Cursed Item should be referenced by its applied item instead, so check to see if its already in the dictionary, if it isnt, add it.
/*                    if (!result.ContainsKey((EquipSlot)lightCursedItem.AffectedSlot.Slot))
                        result.Add(
                            (EquipSlot)lightCursedItem.AffectedSlot.Slot,
                            (ItemIdVars.Resolve((EquipSlot)lightCursedItem.AffectedSlot.Slot, new CustomItemId(lightCursedItem.AffectedSlot.CustomItemId)),
                            lightCursedItem.AffectedSlot.Tooltip));*/
                }
            }
        }
/*
        // next iterate through our locked gags, adding any gag glamour's to locked slots if they are present.
        foreach (var gagSlot in LastAppearanceData.GagSlots.Where(x => x.GagItem is not GagType.None))
        {
            // if the pairs stored gag items contains a glamour for that item, attempt to add it, if possible.
            if (LastLightStorage. .TryGetValue(gagSlot.GagItem, out var gagItem))
                if (!result.ContainsKey((EquipSlot)gagItem.Slot))
                    result.Add((EquipSlot)gagItem.Slot, (ItemIdVars.Resolve((EquipSlot)gagItem.Slot, new CustomItemId(gagItem.CustomItemId)), gagItem.Tooltip));
        }

        // finally, locate the active restraint set, and iterate through the restraint item glamours, adding them if not already a part of the dictionary.
        if (!LastRestraintData.ActiveSetId.IsEmptyGuid())
        {
            var activeSet = LastLightStorage.Restraints.FirstOrDefault(x => x.Identifier == LastWardrobeData.ActiveSetId);
            if (activeSet is not null)
            {
                foreach (var restraintAffectedSlot in activeSet.AffectedSlots)
                    if (!result.ContainsKey((EquipSlot)restraintAffectedSlot.Slot))
                        result.Add((EquipSlot)restraintAffectedSlot.Slot, (ItemIdVars.Resolve((EquipSlot)restraintAffectedSlot.Slot, new CustomItemId(restraintAffectedSlot.CustomItemId)), restraintAffectedSlot.Tooltip));
            }
        }*/
        _logger.LogDebug("Updated Locked Slots for " + UserData.UID, LoggerType.PairInfo);
        LockedSlots = result;
    }

    /// <summary> Get the nicknames for the user. </summary>
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

    /// <summary> Marks the pair as offline. </summary>
    public void MarkOffline()
    {
        try
        {
            _creationSemaphore.Wait();
            _onlineUserIdentDto = null;
            LastIpcData = null;
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

    public void MarkToyboxOffline() => OnlineToyboxUser = false;

    public void MarkToyboxOnline() => OnlineToyboxUser = true;
}
