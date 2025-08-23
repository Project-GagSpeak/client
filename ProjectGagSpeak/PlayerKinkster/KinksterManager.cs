using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using GagSpeak.Kinksters.Factories;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.Network;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Kinksters;

/// <summary>
/// General note to self, pairs used to have "own permissions" and "other permissions" but they were removed.
/// <para> If down the line something like this is an answer to a problem of mine, then find a way to utilize it.</para>
/// </summary>
public sealed partial class KinksterManager : DisposableMediatorSubscriberBase
{
    private readonly ConcurrentDictionary<UserData, Kinkster> _allClientPairs;  // concurrent dictionary of all paired paired to the client.
    private readonly MainConfig _mainConfig;
    private readonly ServerConfigManager _serverConfigs;
    private readonly PairFactory _pairFactory;
    
    private Lazy<List<Kinkster>> _directPairsInternal;                          // the internal direct pairs lazy list for optimization
    public List<Kinkster> DirectPairs => _directPairsInternal.Value;            // the direct pairs the client has with other users.

    public KinksterManager(ILogger<KinksterManager> logger, GagspeakMediator mediator,
        PairFactory factory, MainConfig config, ServerConfigManager serverConfigs) 
        : base(logger, mediator)
    {
        _allClientPairs = new(UserDataComparer.Instance);
        _pairFactory = factory;
        _mainConfig = config;
        _serverConfigs = serverConfigs;

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (_) => ClearKinksters());
        Mediator.Subscribe<CutsceneEndMessage>(this, (_) => ReapplyPairData());

        _directPairsInternal = DirectPairsLazy();
        Svc.ContextMenu.OnMenuOpened += OnOpenPairContextMenu;
    }

    private void OnOpenPairContextMenu(IMenuOpenedArgs args)
    {
        // make sure its a player context menu
        Logger.LogInformation("Opening Pair Context Menu of type "+args.MenuType, LoggerType.ContextDtr);

        if (args.MenuType is ContextMenuType.Inventory) return;
        
        // don't open if we don't want to show context menus
        if (!_mainConfig.Current.ShowContextMenus) return;

        // otherwise, locate the pair and add the context menu args to the visible pairs.
        foreach (var pair in _allClientPairs.Where((p => p.Value.IsVisible)))
        {
            pair.Value.AddContextMenu(args);
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ContextMenu.OnMenuOpened -= OnOpenPairContextMenu;
        // dispose of the pairs
        DisposePairs();
    }


    /// <summary> Appends a new pair to our pair list after a two-way contact has been established. </summary>
    /// <remarks> This occurs upon initial connection while retrieving your pair list of established pairings. </remarks>
    public void AddKinksterPair(KinksterPair dto)
    {
        // if the user is not in the client's pair list, create a new pair for them.
        if (!_allClientPairs.ContainsKey(dto.User))
        {
            Logger.LogDebug($"Kinkster ({dto.User.UID}) not found. Creating new pair", LoggerType.PairManagement);
            _allClientPairs[dto.User] = _pairFactory.Create(dto);
        }
        // if the user is in the client's pair list, apply the last received data to the pair.
        else
        {
            Logger.LogDebug($"Kinkster ({dto.User.UID}) found in pairs, applying latest data!", LoggerType.PairManagement);
            _allClientPairs[dto.User].ReapplyLatestData();
        }
        // recreate the lazy list of direct pairs.
        RecreateLazy();
    }

    /// <summary> Method of addUserPair that allows multiple KinksterPair to be appended, with a single log output after. </summary>
    public void AddKinksterPairs(IEnumerable<KinksterPair> dtoList)
    {
        var created = new List<string>();
        var refreshed = new List<string>();

        foreach (var dto in dtoList)
        {
            if (!_allClientPairs.ContainsKey(dto.User))
            {
                _allClientPairs[dto.User] = _pairFactory.Create(dto);
                created.Add(dto.User.UID);
            }
            else
            {
                _allClientPairs[dto.User].ReapplyLatestData();
                refreshed.Add(dto.User.UID);
            }
        }

        RecreateLazy();

        if (created.Count > 0)
            Logger.LogDebug($"Created Pairs: {string.Join(", ", created)}", LoggerType.PairManagement);

        if (refreshed.Count > 0)
            Logger.LogDebug($"Refreshed Pairs: {string.Join(", ", refreshed)}", LoggerType.PairManagement);
    }

    /// <summary> 
    ///     Add a Kinkster to our pair list upon a sent request being accepted.
    /// </summary>
    public void AddNewKinksterPair(KinksterPair dto)
    {
        if (!_allClientPairs.ContainsKey(dto.User))
            _allClientPairs[dto.User] = _pairFactory.Create(dto);

        // finally, be sure to apply the last received data to this user's Pair object.
        _allClientPairs[dto.User].ReapplyLatestData();
        RecreateLazy();
        // we just added a pair, so ping the achievement manager that a pair was added!
        GagspeakEventManager.AchievementEvent(UnlocksEvent.PairAdded);
    }

    /// <summary> 
    ///     Clears all pairs from the client's pair list.
    /// </summary>
    public void ClearKinksters()
    {
        Logger.LogDebug("Clearing all Pairs", LoggerType.PairManagement);
        DisposePairs();
        _allClientPairs.Clear();
        RecreateLazy();
    }

    /// <summary> Fetches the filtered list of user pair objects where only users that are currently online are returned.</summary>
    public List<Kinkster> GetOnlineKinksterPairs()
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

    public bool TryGetKinkster(UserData user, [NotNullWhen(true)] out Kinkster? kinkster)
        => _allClientPairs.TryGetValue(user, out kinkster);

    public Kinkster? GetKinksterOrDefault(UserData user)
        => _allClientPairs.TryGetValue(user, out var kinkster) ? kinkster : null;

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
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.PositiveStatusTypes),
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.NegativeStatusTypes),
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.SpecialStatusTypes),
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyTheirMoodlesToYou),
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.PairCanApplyYourMoodlesToYou),
            pair.PairPerms.MaxMoodleTime,
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.PermanentMoodles),
            pair.PairPerms.MoodlePerms.HasAny(MoodlePerms.RemovingMoodles)
        );

        return (ownPerms, uniquePerms);
    }

    /// <summary> Marks a user pair as offline.</summary>
    public void MarkKinksterOffline(UserData user)
    {
        if (_allClientPairs.TryGetValue(user, out var pair))
        {
            Mediator.Publish(new ClearProfileDataMessage(pair.UserData));
            pair.MarkOffline();
        }
        RecreateLazy();
    }

    /// <summary> Called by ApiController.Callbacks, and marks our pair online, if cached and offline. </summary>
    /// <remarks> This sends the client an OnlineKinkster, meaning they were in the clients pair list and are now online. </remarks>
    public void MarkKinksterOnline(OnlineKinkster dto, bool sendNotification = true)
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
        if (sendNotification && _mainConfig.Current.NotifyForOnlinePairs && (_mainConfig.Current.NotifyLimitToNickedPairs && !string.IsNullOrEmpty(pair.GetNickname())))
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

    /// <summary> Removes a user pair from the client's pair list.</summary>
    public void RemoveKinksterPair(KinksterBase dto)
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
    private Lazy<List<Kinkster>> DirectPairsLazy() => new(() => _allClientPairs.Select(k => k.Value).ToList());

    /// <summary> Disposes of all the pairs in the client's pair list.</summary>
    private void DisposePairs()
    {
        Logger.LogDebug("Disposing all Pairs", LoggerType.PairManagement);
        var pairCount = _allClientPairs.Count;
        Parallel.ForEach(_allClientPairs, item => { item.Value.MarkOffline(false); });
        Logger.LogDebug($"Marked {pairCount} kinksters as offline", LoggerType.PairManagement);
        RecreateLazy();
    }

    /// <summary> Reapplies the last received data to all the pairs in the client's pair list.</summary>
    private void ReapplyPairData()
    {
        foreach (var pair in _allClientPairs.Select(k => k.Value))
            pair.ReapplyLatestData();
    }

    /// <summary> Recreates the lazy list of direct pairs.</summary>
    public void RecreateLazy(bool PushUiRefresh = true)
    {
        _directPairsInternal = DirectPairsLazy();
        if (PushUiRefresh)
            Mediator.Publish(new RefreshUiKinkstersMessage());
    }
}
