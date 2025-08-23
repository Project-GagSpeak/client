using CkCommons;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using GagSpeak.Kinksters.Factories;
using GagSpeak.Kinksters.Handlers;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Interop;
using Penumbra.GameData.Structs;
using TerraFX.Interop.Windows;
using static Lumina.Data.Parsing.Layer.LayerCommon;

namespace GagSpeak.Kinksters;

/// <summary> Stores information about a paired Kinkster. Managed by PairManager. </summary>
/// <remarks> Created by the PairFactory. PairHandler keeps tabs on the cachedPlayer. </remarks>
public class Kinkster : IComparable<Kinkster>
{
    private readonly ILogger<Kinkster> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly PairHandlerFactory _cachedPlayerFactory;
    private readonly SemaphoreSlim _creationSemaphore = new(1);
    private readonly ServerConfigManager _nickConfig;

    private CancellationTokenSource _appearanceCTS = new CancellationTokenSource();
    private CancellationTokenSource _moodlesCTS = new CancellationTokenSource();
    private OnlineKinkster? _OnlineKinkster = null;

    public Kinkster(KinksterPair pair, ILogger<Kinkster> logger, GagspeakMediator mediator,
        PairHandlerFactory factory, ServerConfigManager nicks)
    {
        _logger = logger;
        _mediator = mediator;
        _cachedPlayerFactory = factory;
        _nickConfig = nicks;

        UserPair = pair;
    }

    // Permissions
    public KinksterPair UserPair { get; set; }
    public UserData UserData => UserPair.User;
    public PairPerms OwnPerms => UserPair.OwnPerms;
    public PairPermAccess OwnPermAccess => UserPair.OwnAccess;
    public GlobalPerms PairGlobals => UserPair.Globals;
    public HardcoreState PairHardcore => UserPair.Hardcore;
    public PairPerms PairPerms => UserPair.Perms;
    public PairPermAccess PairPermAccess => UserPair.Access;

    // Latest cached data for this pair.
    private PairHandler? CachedPlayer { get; set; }

    // Active States
    public CharaIpcDataFull LastAppearanceData { get; private set; } = new CharaIpcDataFull();
    public CharaMoodleData LastMoodlesData { get; private set; } = new CharaMoodleData();
    public CharaActiveGags ActiveGags { get; private set; } = new CharaActiveGags();
    public CharaActiveRestrictions ActiveRestrictions { get; private set; } = new CharaActiveRestrictions();
    public CharaActiveRestraint ActiveRestraint { get; private set; } = new CharaActiveRestraint();
    public CharaActiveCollar ActiveCollar { get; private set; } = new CharaActiveCollar();
    public List<ToyBrandName> ValidToys { get; private set; } = new();
    public List<Guid> ActiveCursedItems { get; private set; } = new();
    public AliasStorage LastGlobalAliasData { get; private set; } = new AliasStorage();
    public NamedAliasStorage LastPairAliasData { get; private set; } = new NamedAliasStorage();
    public Guid ActivePattern { get; private set; } = Guid.Empty;
    public List<Guid> ActiveAlarms { get; private set; } = new();
    public List<Guid> ActiveTriggers { get; private set; } = new();
    
    // Internal Data.
    public KinksterCache LightCache { get; private set; } = new KinksterCache();

    // Helpers.
    public bool HasCachedPlayer => CachedPlayer != null && !string.IsNullOrEmpty(CachedPlayer.PlayerName) && _OnlineKinkster != null;
    public OnlineKinkster CachedPlayerOnlineDto => CachedPlayer!.OnlineUser;
    public bool IsPaused => UserPair.OwnPerms.IsPaused;
    public bool IsOnline => CachedPlayer != null;
    public bool IsVisible => CachedPlayer?.IsVisible ?? false;
    public bool HasShockCollar => PairGlobals.HasValidShareCode() || PairPerms.HasValidShareCode();
    public IGameObject? VisiblePairGameObject => IsVisible ? (CachedPlayer?.PairObject ?? null) : null;
    public string PlayerName => CachedPlayer?.PlayerName ?? UserData.AliasOrUID ?? string.Empty;  // Name of pair player. If empty, (pair handler) CachedData is not initialized yet.
    public string PlayerNameWithWorld => CachedPlayer?.PlayerNameWithWorld ?? string.Empty;

    // maybe remove this later or something i dunno.
    public Dictionary<EquipSlot, (EquipItem, string)> LockedSlots { get; private set; } = new(); // the locked slots of the pair. Used for quick reference in profile viewer.

    // IComparable satisfier
    public int CompareTo(Kinkster? other)
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
        //var subMenu = new MenuItem();
        //subMenu.IsSubmenu = true;
        //subMenu.Name = "SubMenu Test Item";
        //subMenu.PrefixChar = 'G';
        //subMenu.PrefixColor = 561;
        //subMenu.OnClicked += args => OpenSubMenuTest(args, _logger);
        //args.AddMenuItem(subMenu);
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
            OnClicked = (a) => { _mediator.Publish(new KinksterInteractionUiChangeMessage(this, InteractionsTab.Interactions)); },
        });
    }

    #region Kinkster Appearance
    public void ApplyLatestAppearance(CharaIpcDataFull newAppearance)
    {
        _appearanceCTS = _appearanceCTS.SafeCancelRecreate();
        LastAppearanceData.UpdateNonNull(newAppearance);
        ApplyLatestInternal(_appearanceCTS, ApplyLastReceivedAppearance);
    }

    public void ApplyLatestAppearance(CharaIpcLight newAppearance)
    {
        _appearanceCTS = _appearanceCTS.SafeCancelRecreate();
        LastAppearanceData.UpdateNonNull(newAppearance);
        ApplyLatestInternal(_appearanceCTS, ApplyLastReceivedAppearance);
    }
    public void ApplyLatestActorState(string actorBase64)
    {
        LastAppearanceData.GlamourerBase64 = actorBase64;
        // if the cached player is null, do the wait, otherwise, do the direct.
        if (CachedPlayer is null)
        {
            _appearanceCTS = _appearanceCTS.SafeCancelRecreate();
            ApplyLatestInternal(_appearanceCTS, ApplyLastReceivedAppearance);
        }
        else
        {
            CachedPlayer.UpdateGlamour(actorBase64);
        }
    }
    public void ApplyLatestModManips(string modManipsBase64)
    {
        // do nothing yet.
    }

    public void ApplyLatestMoodles(UserData enactor, string dataString, IEnumerable<MoodlesStatusInfo> dataInfo)
    {
        _moodlesCTS = _moodlesCTS.SafeCancelRecreate();
        LastMoodlesData.UpdateDataInfo(dataString, dataInfo);
        ApplyLatestInternal(_moodlesCTS, ApplyLastReceivedMoodles);
    }

    private void ApplyLatestInternal(CancellationTokenSource cts, Action applyAction)
    {
        if (CachedPlayer is null)
        {
            _logger.LogDebug($"Waiting for ({GetNickAliasOrUid()}) to have a valid cache before applying!", LoggerType.PairDataTransfer);
            _ = WaitForValidCacheAndApply(cts, applyAction).ConfigureAwait(false);
        }
        else
        {
            applyAction();
        }
    }

    private async Task WaitForValidCacheAndApply(CancellationTokenSource cts, Action applyAction)
    {
        using var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(120));

        // create a new cancellation token source for the application token
        var appToken = cts.Token;
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, appToken);

        // while the cached player is still null and the combined token is not cancelled
        while (CachedPlayer is null && !combined.Token.IsCancellationRequested)
            await Task.Delay(250, combined.Token).ConfigureAwait(false);

        // if the combined token is not cancelled STILL
        if (!combined.IsCancellationRequested)
        {
            _logger.LogDebug($"Applying delayed data for {GetNickAliasOrUid()}.", LoggerType.PairDataTransfer);
            applyAction();
        }
    }

    public void ReapplyLatestData()
    {
        ApplyLastReceivedAppearance();
        ApplyLastReceivedMoodles();
    }

    private void ApplyLastReceivedAppearance()
    {
        if (CachedPlayer is null) return;
        if (LastAppearanceData is null) return;
        _logger.LogDebug($"Applying Appearance for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        CachedPlayer.ApplyAppearanceData(LastAppearanceData).ConfigureAwait(false);
    }

    private void ApplyLastReceivedMoodles()
    {
        if (CachedPlayer is null) return;
        if (LastMoodlesData is null) return;
        _logger.LogDebug($"Applying Moodles for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        CachedPlayer.UpdateMoodles(LastMoodlesData.DataString);
    }
    #endregion Kinkster Appearance

    public void SetNewMoodlesStatuses(UserData enactor, IEnumerable<MoodlesStatusInfo> statuses)
    {
        _logger.LogDebug($"{GetNickAliasOrUid()}'s moodle Statuses updated!", LoggerType.PairDataTransfer);
        LastMoodlesData.SetStatuses(statuses);
    }

    public void SetNewMoodlePresets(UserData enactor, IEnumerable<MoodlePresetInfo> newPresets)
    {
        _logger.LogDebug($"{GetNickAliasOrUid()}'s moodle Presets updated!", LoggerType.PairDataTransfer);
        LastMoodlesData.SetPresets(newPresets);
    }

    public void NewActiveCompositeData(CharaCompositeActiveData data, bool wasSafeword)
    {
        if (wasSafeword)
        {
            _logger.LogInformation($"{GetNickAliasOrUid()} used their safeword! Syncronizing their new composite data!", LoggerType.PairDataTransfer);
            ActiveGags = data.Gags;
            ActiveRestrictions = data.Restrictions;
            ActiveRestraint = data.Restraint;
            // Cursed loot the same?... (will likely add it in once i tackle cursed loot)
            ValidToys = data.ValidToys;
            ActivePattern = data.ActivePattern;
            ActiveAlarms = data.ActiveAlarms;
            ActiveTriggers = data.ActiveTriggers;
            // Do not update the cache, nothing is in it.
        }
        else
        {
            _logger.LogDebug("Received Character Composite Data from " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
            ActiveGags = data.Gags;
            ActiveRestrictions = data.Restrictions;
            ActiveRestraint = data.Restraint;
            ActiveCursedItems = data.ActiveCursedItems;
            LastGlobalAliasData = data.GlobalAliasData;
            if (data.PairAliasData.TryGetValue(UserData.UID, out var match))
                LastPairAliasData = match;
            ValidToys = data.ValidToys;
            ActivePattern = data.ActivePattern;
            ActiveAlarms = data.ActiveAlarms;
            ActiveTriggers = data.ActiveTriggers;

            // Update the kinkster cache with the light storage data.
            _logger.LogDebug($"Updating LightCache for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            LightCache = new KinksterCache(data.LightStorageData);
        }

        // Update the cached kinkplate data (although we will likely rework this soon)
        UpdateCachedLockedSlots();

        // Regarldess of the change, we should update the kinkster's latest data to the achievement handler.
        _logger.LogDebug($"Aligning Achievement Trackers in sync with {GetNickAliasOrUid()}'s latest composite data!", LoggerType.PairDataTransfer);
        _mediator.Publish(new PlayerLatestActiveItems(UserData, ActiveGags, ActiveRestrictions, ActiveRestraint)); // <-- Send whole composite?
    }

    public void NewActiveGagData(KinksterUpdateActiveGag data)
    {
        _logger.LogDebug($"Applying updated gag data for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActiveGags.GagSlots[data.AffectedLayer] = data.NewData;
        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.PreviousGag, false, data.Enactor.UID, this);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.NewData.GagItem, true, data.Enactor.UID, this);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.NewData.GagItem, true, data.Enactor.UID, this);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedLayer, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagLockStateChange, data.AffectedLayer, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairGagStateChange, data.AffectedLayer, data.PreviousGag, false, data.Enactor.UID, this);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void NewActiveRestrictionData(KinksterUpdateActiveRestriction data)
    {
        _logger.LogDebug("Applying updated restriction data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        ActiveRestrictions.Restrictions[data.AffectedLayer] = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.PreviousRestriction, false, data.Enactor.UID, UserData.UID);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionLockStateChange, data.NewData.Identifier, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                return;
            case DataUpdateType.Unlocked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionLockStateChange, data.NewData.Identifier, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                return;
            case DataUpdateType.Removed:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestrictionStateChange, data.PreviousRestriction, false, data.Enactor.UID, UserData.UID);
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void NewActiveRestraintData(KinksterUpdateActiveRestraint data)
    {
        _logger.LogDebug("Applying updated restraint data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        ActiveRestraint = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.PreviousRestraint, false, data.Enactor.UID, UserData.UID);
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                // Update internal cache to reflect latest changes for kinkplates and such.
                UpdateCachedLockedSlots();
                break;
            case DataUpdateType.Applied:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.NewData.Identifier, true, data.NewData.Enabler, UserData.UID);
                // Update internal cache to reflect latest changes for kinkplates and such.
                UpdateCachedLockedSlots();
                break;
            case DataUpdateType.Locked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.NewData.Identifier, data.NewData.Padlock, true, data.NewData.PadlockAssigner, UserData.UID);
                break;
            case DataUpdateType.Unlocked:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintLockChange, data.NewData.Identifier, data.PreviousPadlock, false, data.Enactor.UID, UserData.UID);
                break;
            case DataUpdateType.Removed:
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PairRestraintStateChange, data.PreviousRestraint, false, data.Enactor.UID, UserData.UID);
                // Update internal cache to reflect latest changes for kinkplates and such.
                UpdateCachedLockedSlots();
                break;
        }
    }

    public void NewActiveCollarData(KinksterUpdateActiveCollar data)
    {
        _logger.LogDebug($"Applying updated collar data for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActiveCollar = data.NewData;
        // Achievement and internal kinkplateCache updates based on type.
        switch (data.Type)
        {
            case DataUpdateType.RequestAccepted:
                // handle an accepted request here.
                break;
            case DataUpdateType.OwnersUpdated:
                // update owners and things here.
                break;
            case DataUpdateType.VisibilityChange:
                // process a toggle to visibility. Change always will inflict a toggle.
                break;
            case DataUpdateType.DyesChange:
                // process a change to the active collar's dyes.
                break;
            case DataUpdateType.CollarMoodleChange:
                // process a change to the active collar's Moodles.
                break;
            case DataUpdateType.CollarWritingChange:
                // process a change to the collar's writing,
                // and perhaps an enforced profile refresh?
                break;
            case DataUpdateType.CollarRemoved:
                // process collar removal.
                break;
        }
    }

    public void NewActiveCursedLoot(List<Guid> newActiveLoot, Guid changedItem)
    {
        _logger.LogDebug($"Updating ActiveCursedLoot for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActiveCursedItems = newActiveLoot;
        // Update internal cache to reflect latest changes for kinkplates and such.
        UpdateCachedLockedSlots();
    }

    public void NewValidToys(List<ToyBrandName> newValidToys)
    {
        _logger.LogDebug($"Updating Valid Toys for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ValidToys = newValidToys;
    }

    public void NewActivePattern(UserData enactor, Guid activePattern, DataUpdateType updateType)
    {
        _logger.LogDebug($"Applying NewActivePattern for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActivePattern = activePattern;
        // Handle any achievements for a kinkster's pattern changing states here, by tracking removed
        // and added patterns ext. [FOR FUTURE IMPLEMENTATION]
    }

    public void NewActiveAlarms(UserData enactor, List<Guid> activeAlarms, DataUpdateType updateType)
    {
        _logger.LogDebug($"Applying NewActiveAlarms for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActiveAlarms = activeAlarms;
        // Handle any achievements for a kinkster's alarm changing states here, by tracking removed
        // and added alarms ext. [FOR FUTURE IMPLEMENTATION]
    }

    public void NewActiveTriggers(UserData enactor, List<Guid> activeTriggers, DataUpdateType updateType)
    {
        _logger.LogDebug($"Applying NewActiveTriggers for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        ActiveTriggers = activeTriggers;
        // Handle any achievements for a kinkster's Triggers changing states here, by tracking removed
        // and added Triggers ext. [FOR FUTURE IMPLEMENTATION]
    }

    public void NewGlobalAlias(Guid id, AliasTrigger? newData)
    { 
        // Try and find the existing alias data by its ID.
        if (LastGlobalAliasData.Items.FirstOrDefault(a => a.Identifier == id) is { } match)
        {
            // If found, and the newData is null, remove it.
            if (newData is null)
            {
                _logger.LogDebug($"Removing Global Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
                LastGlobalAliasData.Items.Remove(match);
                return; // exit early since we removed it.
            }
            
            // Update it.
            _logger.LogDebug($"Updating Global Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            match = newData;
        }
        // Otherwise, if the ID was not found, the new data is not null, and we should add it.
        else if (newData != null)
        {
            _logger.LogDebug($"Adding Global Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            LastGlobalAliasData.Items.Add(newData);
        }
    }

    public void NewUniqueAlias(Guid id, AliasTrigger? newData)
    {
        // Try and find the existing alias data by its ID.
        if (LastPairAliasData.Storage.Items.FirstOrDefault(a => a.Identifier == id) is { } match)
        {
            // If found, and the newData is null, remove it.
            if (newData is null)
            {
                _logger.LogDebug($"Removing Unique Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
                LastPairAliasData.Storage.Items.Remove(match);
                return; // exit early since we removed it.
            }

            // Update it.
            _logger.LogDebug($"Updating Unique Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            match = newData;
        }
        // Otherwise, if the ID was not found, the new data is not null, and we should add it.
        else if (newData != null)
        {
            _logger.LogDebug($"Adding Unique Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            LastPairAliasData.Storage.Items.Add(newData);
        }
    }

    public void UpdateListenerName(string nameWithWorld)
    {
        _logger.LogDebug($"Updating Listener name to {nameWithWorld}", LoggerType.PairDataTransfer);
        LastPairAliasData.StoredNameWorld = nameWithWorld;
    }

    /// <summary> 
    ///     Method that creates the cached player (PairHandler) object for the client pair. <para />
    ///     This method is ONLY EVER CALLED BY THE PAIR MANAGER under the <c>MarkKinksterOnline</c> method! 
    /// </summary>
    /// <remarks> Until the CachedPlayer object is made, the client will not apply any data sent from this paired user. </remarks>
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

    // Update this method to be obtained by the Kinkster Cache.
    public void UpdateCachedLockedSlots()
    {
        var result = new Dictionary<EquipSlot, (EquipItem, string)>();
        // Rewrite this completely. It sucks, and it does nothing with the 2.0 structure.
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
    public void MarkOffline(bool showLog = true)
    {
        try
        {
            _creationSemaphore.Wait();
            _OnlineKinkster = null;
            LastMoodlesData = new CharaMoodleData();
            // set the pair handler player to the cached player, to safely null the CachedPlayer object.
            var player = CachedPlayer;
            CachedPlayer = null;
            player?.Dispose();

            if(showLog)
                _logger.LogTrace($"Marked {UserData.UID} as offline", LoggerType.PairManagement);
        }
        finally
        {
            _creationSemaphore.Release();
        }
    }
}
