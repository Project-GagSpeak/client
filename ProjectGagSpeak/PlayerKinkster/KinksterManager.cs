using CkCommons;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.ImGuiNotification;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Comparer;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Kinksters;

/// <summary>
/// General note to self, pairs used to have "own permissions" and "other permissions" but they were removed.
/// <para> If down the line something like this is an answer to a problem of mine, then find a way to utilize it.</para>
/// </summary>
public sealed partial class KinksterManager : DisposableMediatorSubscriberBase
{
    private readonly ConcurrentDictionary<UserData, Kinkster> _allKinksters = new(UserDataComparer.Instance);
    private readonly MainConfig _config;
    private readonly NicksConfig _nicks;
    private readonly KinksterFactory _pairFactory;
    
    private Lazy<List<Kinkster>> _directPairsInternal;
    public List<Kinkster> DirectPairs => _directPairsInternal.Value;

    public KinksterManager(ILogger<KinksterManager> logger, GagspeakMediator mediator,
        MainConfig config, NicksConfig nicks, KinksterFactory factory) 
        : base(logger, mediator)
    {
        _allKinksters = new(UserDataComparer.Instance);
        _config = config;
        _nicks = nicks;
        _pairFactory = factory;

        Mediator.Subscribe<DisconnectedMessage>(this, _ => OnClientDisconnected(_.Intent));
        Mediator.Subscribe<CutsceneEndMessage>(this, _ => ReapplyAllRendered());
        Mediator.Subscribe<TargetKinksterMessage>(this, _ => TargetKinkster(_.Kinkster));

        _directPairsInternal = new(() => _allKinksters.Select(k => k.Value).ToList());

        Svc.ContextMenu.OnMenuOpened += OnContextMenuOpened;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Svc.ContextMenu.OnMenuOpened -= OnContextMenuOpened;
        // dispose of the pairs
        DisposeKinksters();
    }

    /// <summary>
    ///     Adds a Kinkster to the manager. Called by GetPairedUsers upon connection.
    ///     Also called when a kinkster goes online, or after accepting a kinkster request.
    /// </summary>
    /// <remarks>
    ///     Because Kinkster are used in other classes and combos, we must retain their
    ///     reference here even when reconnecting. Instead, check for existence upon adding.
    /// </remarks>
    public void AddKinkster(KinksterPair dto)
    {
        var exists = _allKinksters.ContainsKey(dto.User);
        Logger.LogDebug($"Kinkster ({dto.User.UID}) {(exists ? "found, applying latest!" : "not found. Creating!")}.", LoggerType.PairManagement);
        
        // Determine if we perform a reapplication, or a creation. (Maybe change later)
        if (exists) _allKinksters[dto.User].ReapplyAlterations();
        else _allKinksters[dto.User] = _pairFactory.Create(dto);

        RecreateLazy();
    } 

    /// <inheritdoc cref="AddKinkster(KinksterPair)"/>
    public void AddKinksters(IEnumerable<KinksterPair> list)
    {
        var created = new List<string>();
        var refreshed = new List<string>();
        foreach (var dto in list)
        {
            if (!_allKinksters.ContainsKey(dto.User))
            {
                _allKinksters[dto.User] = _pairFactory.Create(dto);
                created.Add(dto.User.UID);
            }
            else
            {
                _allKinksters[dto.User].ReapplyAlterations();
                refreshed.Add(dto.User.UID);
            }
        }
        RecreateLazy();

        if (created.Count > 0) Logger.LogDebug($"Created: {string.Join(", ", created)}", LoggerType.PairManagement);
        if (refreshed.Count > 0) Logger.LogDebug($"Refreshed: {string.Join(", ", refreshed)}", LoggerType.PairManagement);
    }

    /// <summary>
    ///     Performs a hard-removal of a kinkster from the manager.
    ///     Usually called when unpairing from a kinkster, or when a 
    ///     kinkster unpaired you.
    /// </summary>
    public void RemoveKinkster(KinksterBase dto)
    {
        // try and get the value from the client's pair list
        if (!_allKinksters.TryGetValue(dto.User, out var pair))
            return;
        // Perform a full disposal of the kinkster, then remove them from the dictionary.
        pair.DisposeData();
        _allKinksters.TryRemove(dto.User, out _);
        Mediator.Publish(new KinksterRemovedMessage(dto.User));
        RecreateLazy();
    }

    private void DisposeKinksters()
    { 
        Logger.LogInformation("Disposing all Kinksters", LoggerType.PairManagement);
        var pairCount = _allKinksters.Count;
        Parallel.ForEach(_allKinksters, k => k.Value.DisposeData());
        _allKinksters.Clear();
        Logger.LogDebug($"Disposed {pairCount} Kinksters.", LoggerType.PairManagement);
        RecreateLazy();
    }

    // Upgrades a temporary kinkster to a permanent one. (Include this if we ever do add temp kinkster pairing)
    public void UpdateToPermanent(KinksterBase dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            return;
        // Update their UserPair state to permanent from temporary, if applicable.
        // kinkster.MarkAsPermanent();
    }

    /// <summary>
    ///     Occurs whenever our client disconnects from the SundouleiaServer. <para />
    ///     What actions are taken depend on the disconnection intent.
    /// </summary>
    public void OnClientDisconnected(DisconnectIntent intent)
    {
        Logger.LogInformation($"Client disconnected with intent: {intent}", LoggerType.PairManagement);
        switch (intent)
        {
            // For normal or unexpected disconnects, simply mark all as offline. (but do not dispose)
            case DisconnectIntent.Normal:
            case DisconnectIntent.Unexpected:
                Parallel.ForEach(_allKinksters, s => s.Value.MarkOffline());
                RecreateLazy();
                break;

            // Reloads or logouts should revert and clear all kinksters.
            case DisconnectIntent.Reload:
                // Perform the same as the above, except with an immidiate revert.
                Parallel.ForEach(_allKinksters, s => s.Value.MarkOffline());
                RecreateLazy();
                break;

            case DisconnectIntent.Logout:
                // Dispose of all kinksters properly upon logout.
                Logger.LogInformation("Client in Logout, disposing all Kinksters.", LoggerType.PairManagement);
                DisposeKinksters();
                break;

            case DisconnectIntent.Shutdown:
                // If we logged out, there is no reason to have any pairs anymore.
                // However, it also should trigger the managers disposal. If it doesn't, something's wrong.
                Logger.LogInformation("Client in Logout/Shutdown, disposal will handle cleanup.");
                break;
        }
    }

    /// <summary> 
    ///     A Kinkster has just come online. We should mark this, and also run a check for
    ///     player visibility against CharaObjectWatcher.
    /// </summary>
    public void MarkKinksterOnline(OnlineKinkster dto, bool notify = true)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"No user found [{dto}]");

        // Refresh any existing profile data.
        Mediator.Publish(new ClearKinkPlateDataMessage(dto.User));

        // If they were already online simply recreate the list.
        if (kinkster.IsOnline)
        {
            RecreateLazy();
            return;
        }

        // Init the proper first-time online message. (also prevent reload spamming logs)
        if (notify && _config.Current.OnlineNotifications)
        {
            var nick = kinkster.GetNickname();
            // Do not show if we limit it to nicked pairs and there is no nickname.
            if (!(_config.Current.NotifyLimitToNickedPairs && string.IsNullOrEmpty(nick)))
            {
                var msg = !string.IsNullOrEmpty(nick) ? $"{nick} ({dto.User.AliasOrUID}) is now online" : $"{dto.User.AliasOrUID} is now online";
                Mediator.Publish(new NotificationMessage("Kinkster Online", msg, NotificationType.Info, TimeSpan.FromSeconds(2)));
            }
        }

        Logger.LogTrace($"Marked {kinkster.PlayerName}({kinkster.GetNickAliasOrUid()}) as online", LoggerType.PairManagement);
        // Mark online internally, and then recreate the whitelist display.
        kinkster.MarkOnline(dto);
        RecreateLazy();
    }

    /// <summary>
    ///     Marks a kinkster as offline. This will clear their OnlineUser. <para />
    ///     A Kinkster's Chara* can still be valid while offline, and they can still download updates.
    /// </summary>
    public void MarkKinksterOffline(UserData user)
    {
        if (_allKinksters.TryGetValue(user, out var pair))
        {
            Logger.LogTrace($"Marked {pair.PlayerName}({pair.GetNickAliasOrUid()}) as offline", LoggerType.PairManagement);
            Mediator.Publish(new ClearKinkPlateDataMessage(pair.UserData));
            pair.MarkOffline();
            RecreateLazy();
        }
    }

    private void ReapplyAllRendered()
    {
        foreach (var pair in _allKinksters.Select(k => k.Value).Where(p => p.IsRendered))
            pair.ReapplyAlterations();
    }

    private void TargetKinkster(Kinkster k)
    {
        if (PlayerData.InPvP || !k.IsRendered) return;
        unsafe
        {
            if (_config.Current.TargetWithFocus)
                TargetSystem.Instance()->FocusTarget = (GameObject*)k.PlayerAddress;
            else
                TargetSystem.Instance()->SetHardTarget((GameObject*)k.PlayerAddress);
        }
    }

    public void RecreateLazy()
    {
        _directPairsInternal = new(() => _allKinksters.Select(k => k.Value).ToList());
        Mediator.Publish(new FolderUpdateKinkster());
    }

    #region ManagerHelpers
    /// <summary>
    ///     Kinksters that we have an OnlineUser DTO of, implying they are connected.
    /// </summary>
    public List<Kinkster> GetOnlineKinksters() => _allKinksters.Where(p => !string.IsNullOrEmpty(p.Value.Ident)).Select(p => p.Value).ToList();

    /// <summary>
    ///     Kinksters that we have an OnlineUser DTO of, implying they are connected.
    /// </summary>
    public List<UserData> GetOnlineUserDatas() => _allKinksters.Where(p => !string.IsNullOrEmpty(p.Value.Ident)).Select(p => p.Key).ToList();

    /// <summary>
    ///     The number of kinksters that are in our render range. <para />
    ///     NOTE: This does not mean that they have applied data!
    /// </summary>
    public int GetVisibleCount() => _allKinksters.Count(p => p.Value.IsRendered);

    /// <summary>
    ///     Get the <see cref="UserData"/> for all rendered kinksters. <para />
    ///     <b>NOTE: It is possible for a visible kinksters to be offline!</b>
    /// </summary>
    public List<UserData> GetVisible() => _allKinksters.Where(p => p.Value.IsRendered).Select(p => p.Key).ToList();

    /// <summary>
    ///     Get the <see cref="UserData"/> for all rendered kinksters that are connected.
    /// </summary>
    public List<UserData> GetVisibleConnected() => _allKinksters.Where(p => p.Value.IsRendered && p.Value.IsOnline).Select(p => p.Key).ToList();

    /// <summary>
    ///     If a Kinkster exists given their UID.
    /// </summary>
    public bool ContainsKinkster(string uid) => _allKinksters.ContainsKey(new(uid));

    public bool TryGetKinkster(UserData user, [NotNullWhen(true)] out Kinkster? kinkster)
        => _allKinksters.TryGetValue(user, out kinkster);

    /// <summary>
    ///     Useful for cases where you have the UID but you dont have the pair object and 
    ///     need a way to get the nickname/alias without iterating through them all.
    /// </summary>
    public bool TryGetNickAliasOrUid(string uid, [NotNullWhen(true)] out string? nickAliasUid)
    {
        nickAliasUid = _nicks.GetNicknameForUid(uid) ?? _allKinksters.Keys.FirstOrDefault(p => p.UID == uid)?.AliasOrUID;
        return !string.IsNullOrWhiteSpace(nickAliasUid);
    }

    public bool TryGetNickAliasOrUid(UserData user, [NotNullWhen(true)] out string? nickAliasUid)
    {
        nickAliasUid = _allKinksters.TryGetValue(user, out var s) ? s.GetNickAliasOrUid() : null;
        return !string.IsNullOrWhiteSpace(nickAliasUid);
    }

    /// <summary>
    ///     Attempt to retrieve a kinkster by <see cref="UserData"/>. If failed, null is returned.
    /// </summary>
    public Kinkster? GetUserOrDefault(UserData user) => _allKinksters.TryGetValue(user, out var kinkster) ? kinkster : null;

    // Fetch a user's UserData off of their UID
    public UserData? GetUserDataFromUID(string uid) => _allKinksters.Keys.FirstOrDefault(p => string.Equals(p.UID, uid, StringComparison.OrdinalIgnoreCase));

    #endregion ManagerHelpers

    #region Updates
    public void ReceiveMoodleData(UserData target, MoodleData newMoodleData)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster)) 
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"Received MoodleData from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.SetMoodlesData(newMoodleData);
    }

    public void ReceiveSMUpdate(UserData target, string dataString, List<MoodlesStatusInfo> dataInfo)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"{kinkster.GetNickAliasOrUid()}'s StatusManager updated!", LoggerType.Callbacks);
        kinkster.SetMoodlesData(dataString, dataInfo);
    }

    public void ReceiveMoodleStatuses(UserData target, List<MoodlesStatusInfo> newStatuses)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"Received updated MoodleStatusList from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.SetMoodleStatusData(newStatuses);
    }

    public void ReceiveMoodlePresets(UserData target, List<MoodlePresetInfo> newPresets)
    { 
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"Received updated MoodlePresetList from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.SetMoodlePresetData(newPresets);
    }

    public void ReceiveMoodleStatusUpdate(UserData target, MoodlesStatusInfo status, bool deleted)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"Received MoodleStatus data update from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.UpdateMoodleStatusData(status, deleted);
    }

    public void ReceiveMoodlePresetUpdate(UserData target, MoodlePresetInfo preset, bool deleted)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogTrace($"Received MoodlePreset data update from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.UpdateMoodlePresetData(preset, deleted);
    }

    public void NewActiveComposite(UserData target, CharaCompositeActiveData data, bool safeword)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        Logger.LogDebug($"Received Composite Active Data from {kinkster.GetNickAliasOrUid()}!", LoggerType.Callbacks);
        kinkster.NewActiveCompositeData(data, safeword);
    }

    public void NewActiveGags(KinksterUpdateActiveGag dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveGagData(dto);
    }

    public void NewActiveRestriction(KinksterUpdateActiveRestriction dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveRestrictionData(dto);
    }

    public void NewActiveRestraint(KinksterUpdateActiveRestraint dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveRestraintData(dto);
    }

    public void NewActiveCollar(KinksterUpdateActiveCollar dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveCollarData(dto);
    }

    public void NewActiveCursedLoot(KinksterUpdateActiveCursedLoot dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveCursedLoot(dto.ActiveItems, dto.ChangedItem);
    }

    // Update this later as we integrate further UI Logic*
    public void UpdateAlias(UserData targetUser, Guid id, AliasTrigger? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.NewAlias(id, newData);
    }

    public void NewValidToys(UserData targetUser, List<ToyBrandName> validToys)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.NewValidToys(validToys);
    }

    public void NewActivePattern(KinksterUpdateActivePattern dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActivePattern(dto.Enactor, dto.ActivePattern, dto.Type);
    }

    public void NewActiveAlarms(KinksterUpdateActiveAlarms dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveAlarms(dto.Enactor, dto.ActiveAlarms, dto.Type);
    }

    public void NewActiveTriggers(KinksterUpdateActiveTriggers dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        kinkster.NewActiveTriggers(dto.Enactor, dto.ActiveTriggers, dto.Type);
    }

    public void CachedGagDataChange(UserData targetUser, GagType gagItem, LightGag? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateGagItem(gagItem, newData);
    }

    public void CachedRestrictionDataChange(UserData targetUser, Guid itemId, LightRestriction? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateRestrictionItem(itemId, newData);
    }

    public void CachedRestraintDataChange(UserData targetUser, Guid itemId, LightRestraint? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateRestraintItem(itemId, newData);
    }

    public void CachedCollarDataChange(UserData targetUser, LightCollar? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateCollarItem(newData);
    }

    public void CachedCursedLootDataChange(UserData targetUser, Guid itemId, LightCursedLoot? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateLootItem(itemId, newData);
    }

    public void CachedPatternDataChange(UserData targetUser, Guid itemId, LightPattern? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdatePatternItem(itemId, newData);
    }

    public void CachedAlarmDataChange(UserData targetUser, Guid itemId, LightAlarm? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateAlarmItem(itemId, newData);
    }

    public void CachedTriggerDataChange(UserData targetUser, Guid itemId, LightTrigger? newData)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateTriggerItem(itemId, newData);
    }

    public void CachedAllowancesChange(UserData targetUser, GSModule module, List<string> newAllowances)
    {
        if (!_allKinksters.TryGetValue(targetUser, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{targetUser.AliasOrUID}] not found.");
        kinkster.LightCache.UpdateAllowances(module, newAllowances);
    }
    #endregion DataUpdates

    #region Permissions
    public void PermBulkChangeGlobal(BulkChangeGlobal dto)
    {
        if (!_allKinksters.TryGetValue(dto.User, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{dto.User.AliasOrUID}] not found.");
        // cache prev state.
        var prevGlobals = kinkster.PairGlobals;

        // update them.
        kinkster.UserPair.Globals = dto.NewPerms;
        kinkster.UserPair.Hardcore = dto.NewState;

        Logger.LogDebug($"BulkChangeGlobal for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        // use comparisons to fire various achievements related to global permissions. (or just make some handler process it idk)
    }

    public void PermChangeGlobal(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        if (!PropertyChanger.TrySetProperty(kinkster.PairGlobals, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        Logger.LogDebug($"PermChangeGlobal for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // use comparisons to fire various achievements related to global permissions.
    }

    public void PermBulkChangeUniqueOwn(UserData target, PairPerms newPerms, PairPermAccess newAccess)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        // cache prev state.
        var prevPerms = kinkster.OwnPerms with { };
        var prevAccess = kinkster.OwnPermAccess with { };

        // Update.
        kinkster.UserPair.OwnPerms = newPerms;
        kinkster.UserPair.OwnAccess = newAccess;

        Logger.LogDebug($"OWN BulkChangeUnique for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        var MoodlesChanged = (prevPerms.MoodleAccess != newPerms.MoodleAccess) || (prevPerms.MaxMoodleTime != newPerms.MaxMoodleTime);
        if (kinkster.IsRendered && MoodlesChanged)
            Mediator.Publish(new MoodleAccessPermsChanged(kinkster));

        // Handle achievements with changes here.
    }

    public void PermBulkChangeUniqueOther(UserData target, PairPerms newPerms, PairPermAccess newAccess)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevPerms = kinkster.PairPerms with { };
        var prevAccess = kinkster.PairPermAccess with { };

        // Update.
        kinkster.UserPair.Perms = newPerms;
        kinkster.UserPair.Access = newAccess;

        Logger.LogDebug($"OTHER BulkChangeUnique for [{kinkster.GetNickAliasOrUid()}]", LoggerType.PairDataTransfer);
        // Handle informing moodles of permission changes.
        var MoodlesChanged = (prevPerms.MoodleAccess != newPerms.MoodleAccess) || (prevPerms.MaxMoodleTime != newPerms.MaxMoodleTime);
        if (kinkster.IsRendered && MoodlesChanged)
            Mediator.Publish(new MoodleAccessPermsChanged(kinkster));

        // Handle achievements with changes here.
    }

    public void PermChangeUnique(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        // cache prev state.
        var prevPuppetPerms = kinkster.OwnPerms.PuppetPerms;

        // Perform change.
        if (!PropertyChanger.TrySetProperty(kinkster.OwnPerms, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        Logger.LogDebug($"OWN PermChangeUnique for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);

        if (permName.Equals(nameof(PairPerms.MoodleAccess)) || permName.Equals(nameof(PairPerms.MaxMoodleTime)))
            Mediator.Publish(new MoodleAccessPermsChanged(kinkster));

        // Achievement if permissions were granted.
        if (permName.Equals(nameof(PairPerms.PuppetPerms)) && (kinkster.OwnPerms.PuppetPerms & ~prevPuppetPerms) != 0)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.PuppeteerAccessGiven, (kinkster.OwnPerms.PuppetPerms & ~prevPuppetPerms));
    }

    public void PermChangeUniqueOther(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");


        if (!PropertyChanger.TrySetProperty(kinkster.PairPerms, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        Logger.LogDebug($"OTHER SingleChangeUnique for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        
        // If moodle permissions updated, notify IpcProvider (Moodles) that we have a change.
        if (permName.Equals(nameof(PairPerms.MoodleAccess)) || permName.Equals(nameof(PairPerms.MaxMoodleTime)))
            Mediator.Publish(new MoodleAccessPermsChanged(kinkster));
    }

    public void PermChangeAccess(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        if (!PropertyChanger.TrySetProperty(kinkster.OwnPermAccess, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        Logger.LogDebug($"OWN PermChangeAccess for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // process distinct handles here.
    }

    public void PermChangeAccessOther(UserData target, UserData enactor, string permName, object newValue)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");

        if (!PropertyChanger.TrySetProperty(kinkster.PairPermAccess, permName, newValue, out var finalVal) || finalVal is null)
            throw new InvalidOperationException($"Failed to set property '{permName}' on {kinkster.GetNickAliasOrUid()} with value '{newValue}'");

        Logger.LogDebug($"OTHER PermChangeAccess for [{kinkster.GetNickAliasOrUid()}] set [{permName}] to [{finalVal}]", LoggerType.PairDataTransfer);
        // process distinct handles here.
    }

    // Not really sure how I want to revise this but I really hate the readonly global permissions lol.
    public void StateChangeHardcore(UserData target, UserData enactor, HcAttribute attribute, HardcoreStatus newData)
    {
        if (!_allKinksters.TryGetValue(target, out var kinkster))
            throw new InvalidOperationException($"Kinkster [{target.AliasOrUID}] not found.");
        // cache prev state.
        var prevState = kinkster.PairHardcore;

        // make changes based on type.
        switch (attribute)
        {
            case HcAttribute.Follow:
                kinkster.PairHardcore.LockedFollowing = newData.LockedFollowing;
                break;

            case HcAttribute.EmoteState:
                kinkster.PairHardcore.LockedEmoteState = newData.LockedEmoteState;
                kinkster.PairHardcore.EmoteExpireTime = newData.EmoteExpireTime;
                kinkster.PairHardcore.EmoteId = newData.EmoteId;
                kinkster.PairHardcore.EmoteCyclePose = newData.EmoteCyclePose;
                break;

            case HcAttribute.Confinement:
                kinkster.PairHardcore.IndoorConfinement = newData.IndoorConfinement;
                kinkster.PairHardcore.ConfinementTimer = newData.ConfinementTimer;
                kinkster.PairHardcore.ConfinedWorld = newData.ConfinedWorld;
                kinkster.PairHardcore.ConfinedCity = newData.ConfinedCity;
                kinkster.PairHardcore.ConfinedWard = newData.ConfinedWard;
                kinkster.PairHardcore.ConfinedPlaceId = newData.ConfinedPlaceId;
                kinkster.PairHardcore.ConfinedInApartment = newData.ConfinedInApartment;
                kinkster.PairHardcore.ConfinedInSubdivision = newData.ConfinedInSubdivision;
                break;

            case HcAttribute.Imprisonment:
                kinkster.PairHardcore.Imprisonment = newData.Imprisonment;
                kinkster.PairHardcore.ImprisonmentTimer = newData.ImprisonmentTimer;
                kinkster.PairHardcore.ImprisonedTerritory = newData.ImprisonedTerritory;
                kinkster.PairHardcore.ImprisonedPos = newData.ImprisonedPos;
                kinkster.PairHardcore.ImprisonedRadius = newData.ImprisonedRadius;
                break;

            case HcAttribute.HiddenChatBox:
                kinkster.PairHardcore.ChatBoxesHidden = newData.ChatBoxesHidden;
                kinkster.PairHardcore.ChatBoxesHiddenTimer = newData.ChatBoxesHiddenTimer;
                break;

            case HcAttribute.HiddenChatInput:
                kinkster.PairHardcore.ChatInputHidden = newData.ChatInputHidden;
                kinkster.PairHardcore.ChatInputHiddenTimer = newData.ChatInputHiddenTimer;
                break;

            case HcAttribute.BlockedChatInput:
                kinkster.PairHardcore.ChatInputBlocked = newData.ChatInputBlocked;
                kinkster.PairHardcore.ChatInputBlockedTimer = newData.ChatInputBlockedTimer;
                break;

            case HcAttribute.HypnoticEffect:
                kinkster.PairHardcore.HypnoticEffect = newData.HypnoticEffect;
                kinkster.PairHardcore.HypnoticEffectTimer = newData.HypnoticEffectTimer;
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(attribute), attribute, "Invalid Hardcore State attribute to change.");
        }
    }
    #endregion Updates

    /// <summary>
    ///     Logic for ensuring that correct pairs display a context menu when right-clicked.
    /// </summary>
    private void OnContextMenuOpened(IMenuOpenedArgs args)
    {
        Logger.LogInformation("Opening Pair Context Menu of type " + args.MenuType, LoggerType.PairManagement);
        if (args.MenuType is ContextMenuType.Inventory) return;
        if (!_config.Current.ShowContextMenus) return;
        if (args.Target is not MenuTargetDefault target || target.TargetObjectId == 0) return;
        // Find the kinkster that matches this and display the results.
        if (DirectPairs.FirstOrDefault(p => p.IsRendered && p.PlayerObjectId == target.TargetObjectId) is not { } match)
            return;

        Logger.LogDebug($"Found matching pair for context menu: {match.GetNickAliasOrUid()}", LoggerType.PairManagement);
        // This only works when you create it prior to adding it to the args,
        // otherwise the += has trouble calling. (it would fall out of scope)

        //var subMenu = new MenuItem();
        //subMenu.IsSubmenu = true;
        //subMenu.Name = "GagSpeak";
        //subMenu.PrefixChar = 'G';
        //subMenu.PrefixColor = 561;
        //subMenu.OnClicked += args => OpenSubMenuTest(args, _logger);
        //args.AddMenuItem(subMenu);
        OpenSubMenu(match, args);
    }
    /// <summary>
    ///     Required to show the nested menu in the opened context menus.
    /// </summary>
    private void OpenSubMenu(Kinkster kinkster, IMenuOpenedArgs args)
    {
        // If using a sub-menu, build from the below commented line.
        // args.OpenSubmenu("GagSpeak Options", [ new MenuItem()

        args.AddMenuItem(new MenuItem()
        {
            Name = new SeStringBuilder().AddText("Open KinkPlate").Build(),
            PrefixChar = 'G',
            PrefixColor = 708,
            OnClicked = (a) => { Mediator.Publish(new KinkPlateCreateOpenMessage(kinkster)); },
        });
        args.AddMenuItem(new MenuItem()
        {
            Name = new SeStringBuilder().AddText("Pair Actions").Build(),
            PrefixChar = 'G',
            PrefixColor = 708,
            OnClicked = (a) => { Mediator.Publish(new OpenKinksterSidePanel(kinkster, true)); },
        });
    }
}
