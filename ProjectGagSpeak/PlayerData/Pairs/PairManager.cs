using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Factories;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.UserPair;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerData.Pairs;

/// <summary>
/// General note to self, pairs used to have "own permissions" and "other permissions" but they were removed.
/// <para> If down the line something like this is an answer to a problem of mine, then find a way to utilize it.</para>
/// </summary>
public sealed partial class PairManager : DisposableMediatorSubscriberBase
{
    private readonly ConcurrentDictionary<UserData, Pair> _allClientPairs;  // concurrent dictionary of all paired paired to the client.
    private readonly GagspeakConfigService _mainConfig;                     // main gagspeak config
    private readonly ServerConfigurationManager _serverConfigs;             // for nick handling.
    private readonly PairFactory _pairFactory;                              // the pair factory for creating new pair objects
    private readonly IContextMenu _contextMenu;                             // adds GagSpeak options when right clicking players.
    
    private Lazy<List<Pair>> _directPairsInternal;                          // the internal direct pairs lazy list for optimization
    public List<Pair> DirectPairs => _directPairsInternal.Value;            // the direct pairs the client has with other users.

    public PairManager(ILogger<PairManager> logger, GagspeakMediator mediator,
        PairFactory pairFactory, GagspeakConfigService mainConfig, 
        ServerConfigurationManager serverConfigs, IContextMenu contextMenu) : base(logger, mediator)
    {
        _allClientPairs = new(UserDataComparer.Instance);
        _pairFactory = pairFactory;
        _mainConfig = mainConfig;
        _serverConfigs = serverConfigs;
        _contextMenu = contextMenu;

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (_) => ClearPairs());
        Mediator.Subscribe<CutsceneEndMessage>(this, (_) => ReapplyPairData());

        _directPairsInternal = DirectPairsLazy();
        _contextMenu.OnMenuOpened += OnOpenPairContextMenu;
    }

    private void OnOpenPairContextMenu(IMenuOpenedArgs args)
    {
        // make sure its a player context menu
        Logger.LogInformation("Opening Pair Context Menu of type "+args.MenuType, LoggerType.ContextDtr);

        if (args.MenuType is ContextMenuType.Inventory) return;
        
        // don't open if we don't want to show context menus
        if (!_mainConfig.Config.ShowContextMenus) return;

        // otherwise, locate the pair and add the context menu args to the visible pairs.
        foreach (var pair in _allClientPairs.Where((p => p.Value.IsVisible)))
        {
            pair.Value.AddContextMenu(args);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _contextMenu.OnMenuOpened -= OnOpenPairContextMenu;
        // dispose of the pairs
        DisposePairs();
    }


    /// <summary> Appends a new pair to our pair list after a two-way contact has been established. </summary>
    /// <remarks> This occurs upon initial connection while retrieving your pair list of established pairings. </remarks>
    public void AddUserPair(UserPairDto dto)
    {
        // if the user is not in the client's pair list, create a new pair for them.
        if (!_allClientPairs.ContainsKey(dto.User))
        {
            Logger.LogDebug("User "+dto.User.UID+" not found in client pairs, creating new pair", LoggerType.PairManagement);
            _allClientPairs[dto.User] = _pairFactory.Create(dto);
        }
        // if the user is in the client's pair list, apply the last received data to the pair.
        else
        {
            Logger.LogDebug("User " + dto.User.UID + " found in client pairs, applying last received data instead.", LoggerType.PairManagement);
            _allClientPairs[dto.User].ApplyLastIpcData();
        }
        // recreate the lazy list of direct pairs.
        RecreateLazy();
    }

    /// <summary> Appends a new pair to our pair list after a two-way contact has been established. </summary>
    /// <remarks> Fired by a server callback upon someone accepting your pair request, or after you accept theirs. </remarks>
    public void AddNewUserPair(UserPairDto dto)
    {
        if (!_allClientPairs.ContainsKey(dto.User))
            _allClientPairs[dto.User] = _pairFactory.Create(dto);

        // finally, be sure to apply the last received data to this user's Pair object.
        _allClientPairs[dto.User].ApplyLastIpcData();
        RecreateLazy();
        // we just added a pair, so ping the achievement manager that a pair was added!
        UnlocksEventManager.AchievementEvent(UnlocksEvent.PairAdded);
    }

    /// <summary> Clears all pairs from the client's pair list.</summary>
    public void ClearPairs()
    {
        Logger.LogDebug("Clearing all Pairs", LoggerType.PairManagement);
        // dispose of all our pairs
        DisposePairs();
        // clear the client's pair list
        _allClientPairs.Clear();
        // recreate the lazy list of direct pairs
        RecreateLazy();
    }

    /// <summary> Fetches the filtered list of user pair objects where only users that are currently online are returned.</summary>
    public List<Pair> GetOnlineUserPairs()
        => _allClientPairs.Where(p => !string.IsNullOrEmpty(p.Value.GetPlayerNameHash())).Select(p => p.Value).ToList();

    /// <summary> Fetches all online userPairs, but returns the key instead of value like above.</summary>
    public List<UserData> GetOnlineUserDatas()
        => _allClientPairs.Where(p => !string.IsNullOrEmpty(p.Value.GetPlayerNameHash())).Select(p => p.Key).ToList();

    /// <summary> fetches the total number of online users that are also visible to the client.</summary>
    public int GetVisibleUserCount() => _allClientPairs.Count(p => p.Value.IsVisible);

    /// <summary> Fetches the list of userData UIDS for the pairs that are currently visible to the client.</summary>
    public List<UserData> GetVisibleUsers() => _allClientPairs.Where(p => p.Value.IsVisible).Select(p => p.Key).ToList();

    /// <summary> Gets all pairs where IsVisible is true and returns their game objects in a list form, excluding null values. </summary>
    public List<IGameObject> GetVisiblePairGameObjects()
        => _allClientPairs.Select(p => p.Value.VisiblePairGameObject).Where(gameObject => gameObject != null).ToList()!;

    /// <summary> Fetch the list of userData UID's for all pairs who have OnlineToyboxUser to true.</summary>
    public List<Pair> GetOnlineToyboxUsers() => _allClientPairs.Where(p => p.Value.OnlineToyboxUser).Select(p => p.Value).ToList();

    // fetch the list of all online user pairs via their UID's
    public List<string> GetOnlineUserUids() => _allClientPairs.Select(p => p.Key.UID).ToList();

    // Fetch a user's UserData off of their UID
    public UserData? GetUserDataFromUID(string uid) => _allClientPairs.Keys.FirstOrDefault(p => string.Equals(p.UID, uid, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Useful for cases where you have the UID but you dont have the pair object and need a way to get the nickname/alias without iterating 
    /// through the pair objects.
    /// </summary>
    public bool TryGetNickAliasOrUid(string uid, [NotNullWhen(true)] out string? nickAliasUid)
    {
        nickAliasUid = _serverConfigs.GetNicknameForUid(uid) ?? _allClientPairs.Keys.FirstOrDefault(p => p.UID == uid)?.AliasOrUID;
        return !string.IsNullOrWhiteSpace(nickAliasUid);
    }


    public (MoodlesGSpeakPairPerms, MoodlesGSpeakPairPerms) GetMoodlePermsForPairByName(string nameWithWorld)
    {
        var pair = _allClientPairs.FirstOrDefault(p => p.Value.PlayerNameWithWorld == nameWithWorld).Value;
        if (pair == null || pair.OwnPerms == null || pair.PairPerms == null)
        {
            return (new MoodlesGSpeakPairPerms(), new MoodlesGSpeakPairPerms());
        }

        var ownPerms = (
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou),
            pair.OwnPerms.MaxMoodleTime,
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles)
        );

        var uniquePerms = (
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou),
            pair.OwnPerms.MaxMoodleTime,
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles),
            pair.OwnPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles)
        );

        return (ownPerms, uniquePerms);
    }

    /// <summary> Marks a user pair as offline.</summary>
    public void MarkPairOffline(UserData user)
    {
        if (_allClientPairs.TryGetValue(user, out var pair))
        {
            Mediator.Publish(new ClearProfileDataMessage(pair.UserData));
            pair.MarkOffline();
        }
        RecreateLazy();
    }

    public void MarkPairToyboxOffline(UserData user)
    {
        if (_allClientPairs.TryGetValue(user, out var pair))
            pair.MarkToyboxOffline();

        RecreateLazy();
    }

    public void MarkPairToyboxOnline(UserData user)
    {
        if (_allClientPairs.TryGetValue(user, out var pair))
            pair.MarkToyboxOnline();

        RecreateLazy();
    }

    /// <summary> Called by ApiController.Callbacks, and marks our pair online, if cached and offline. </summary>
    /// <remarks> This sends the client an OnlineUserIdentDto, meaning they were in the clients pair list and are now online. </remarks>
    public void MarkPairOnline(OnlineUserIdentDto dto, bool sendNotification = true)
    {
        if (!_allClientPairs.ContainsKey(dto.User)) 
            throw new InvalidOperationException("No user found for " + dto);

        Mediator.Publish(new ClearProfileDataMessage(dto.User));
        var pair = _allClientPairs[dto.User];
        if (pair.HasCachedPlayer)
        {
            Logger.LogDebug("Pair "+dto.User.UID+" already has a cached player, recreating the lazy list of direct pairs.", LoggerType.PairManagement);
            RecreateLazy();
            return;
        }

        // if send notification is on, then we should send the online notification to the client.
        if (sendNotification && _mainConfig.Config.NotifyForOnlinePairs && (_mainConfig.Config.NotifyLimitToNickedPairs && !string.IsNullOrEmpty(pair.GetNickname())))
        {
            // get the nickname from the pair, if it is not null, set the nickname to the pair's nickname.
            var nickname = pair.GetNickname();
            // create a message to send to the client.
            var msg = !string.IsNullOrEmpty(nickname)
                ? $"{nickname} ({pair.UserData.AliasOrUID}) is now online"
                : $"{pair.UserData.AliasOrUID} is now online";
            // publish a notification message to the client that a paired user is now online.
            Logger.LogInformation(msg, LoggerType.PairManagement);
        }

        // create a cached player for the pair using the Dto
        pair.CreateCachedPlayer(dto);
        // push our composite data to them.
        Mediator.Publish(new PairWentOnlineMessage(dto.User));
        RecreateLazy();
    }

    /// <summary> Only called upon a safeword or initial connection for load. Not called otherwise. </summary>
    public void ReceiveCompositeData(OnlineUserCompositeDataDto dto, string clientUID)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].LoadCompositeData(dto);
    }

    public void ReceiveIpcData(CallbackIpcDataDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateVisibleData(dto);
    }

    public void ReceiveGagData(CallbackGagDataDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateGagData(dto);
    }

    public void ReceiveRestrictionData(CallbackRestrictionDataDto dto)
    {
        if(!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateRestrictionData(dto);
    }

    public void ReceiveCharaWardrobeData(CallbackRestraintDataDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateRestraintData(dto);
    }

    public void ReceiveCharaCursedLootData(CallbackCursedLootDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateCursedLootData(dto);
    }

    public void ReceiveCharaAliasData(CallbackAliasDataDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateAliasData(dto);
    }

    public void ReceiveCharaToyboxData(CallbackToyboxDataDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateToyboxData(dto);
    }

    /// <summary> Method similar to compositeData, but this will only update the latest Light Storage Data of the user pair. </summary>
    public void ReceiveCharaLightStorageData(CallbackLightStorageDto dto)
    {
        if (!_allClientPairs.TryGetValue(dto.User, out var pair)) 
            throw new InvalidOperationException("No user found for " + dto.User);

        _allClientPairs[dto.User].UpdateLightStorageData(dto);
    }

    /// <summary> Removes a user pair from the client's pair list.</summary>
    public void RemoveUserPair(UserDto dto)
    {
        // try and get the value from the client's pair list
        if (_allClientPairs.TryGetValue(dto.User, out var pair))
        {
            pair.MarkOffline();
            _allClientPairs.TryRemove(dto.User, out _);
        }
        Mediator.Publish(new PairWasRemovedMessage(dto.User));
        RecreateLazy();
    }

    /// <summary> The lazy list of direct pairs, remade from the _allClientPairs</summary>
    private Lazy<List<Pair>> DirectPairsLazy() => new(() => _allClientPairs.Select(k => k.Value).ToList());

    /// <summary> Disposes of all the pairs in the client's pair list.</summary>
    private void DisposePairs()
    {
        Logger.LogDebug("Disposing all Pairs", LoggerType.PairManagement);
        Parallel.ForEach(_allClientPairs, item => { item.Value.MarkOffline(); });
        RecreateLazy();
    }

    /// <summary> Reapplies the last received data to all the pairs in the client's pair list.</summary>
    private void ReapplyPairData()
    {
        foreach (var pair in _allClientPairs.Select(k => k.Value))
            pair.ApplyLastIpcData(forced: true);
    }

    /// <summary> Recreates the lazy list of direct pairs.</summary>
    private void RecreateLazy(bool PushUiRefresh = true)
    {
        _directPairsInternal = DirectPairsLazy();
        if (PushUiRefresh)
            Mediator.Publish(new RefreshUiMessage());
    }
}
