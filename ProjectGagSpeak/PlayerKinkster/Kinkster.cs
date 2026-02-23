using CkCommons;
using CkCommons.Gui;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.Localization;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using OtterGui;
using OtterGui.Text.Widget.Editors;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace GagSpeak.Kinksters;

/// <summary>
///     Stores information about a pairing between 2 Kinksters. <para />
///     The handlers associated with the kinkster must be disposed of when removing.
/// </summary>
public class Kinkster : IComparable<Kinkster>
{
    private readonly ILogger<Kinkster> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly NicksConfig _nicks;

    private OnlineKinkster? _onlineUser;

    // Tracks information about the Kinkster's Visible state.
    private KinksterHandler _player;

    public Kinkster(KinksterPair pair, ILogger<Kinkster> logger, GagspeakMediator mediator,
        MainConfig config, NicksConfig nicks, KinksterFactory factory)
    {
        _logger = logger;
        _mediator = mediator;
        _config = config;
        _nicks = nicks;

        UserPair = pair;
        // Initialize all handlers for the kinkster, holding their lifetime until disposal.
        // Create handlers for each of the objects.
        _player = factory.Create(this);
        _logger.LogTrace($"Initialized Kinkster for ({GetNickAliasOrUid()}).", LoggerType.PairManagement);
    }

    // Permissions
    public KinksterPair     UserPair { get; private set; }
    public UserData         UserData        => UserPair.User;
    public PairPerms        OwnPerms        => UserPair.OwnPerms;
    public PairPermAccess   OwnPermAccess   => UserPair.OwnAccess;
    public GlobalPerms      PairGlobals     => UserPair.Globals;
    public HardcoreStatus   PairHardcore    => UserPair.Hardcore;
    public PairPerms        PairPerms       => UserPair.Perms;
    public PairPermAccess   PairPermAccess  => UserPair.Access;

    // Active States (We can know this information regardless of visibility).
    public CharaActiveGags ActiveGags { get; private set; } = new CharaActiveGags();
    public CharaActiveRestrictions ActiveRestrictions { get; private set; } = new CharaActiveRestrictions();
    public CharaActiveRestraint ActiveRestraint { get; private set; } = new CharaActiveRestraint();
    public CharaActiveCollar ActiveCollar { get; private set; } = new CharaActiveCollar();
    public HashSet<ToyBrandName> ValidToys { get; private set; } = new();
    public HashSet<Guid> ActiveCursedItems { get; private set; } = new();
    public List<AliasTrigger> SharedAliases { get; private set; } = new();
    public bool IsListeningToClient { get; private set; } = false; // Idk a better way to do this, but it works.
    public Guid ActivePattern { get; private set; } = Guid.Empty;
    public HashSet<Guid> ActiveAlarms { get; private set; } = new();
    public HashSet<Guid> ActiveTriggers { get; private set; } = new();

    // Internal Data. (Useful for tooltip information and KinkPlates™
    public KinksterCache LightCache { get; private set; } = new KinksterCache();

    // Internal Helpers.
    // public bool IsTemporary => UserPair.IsTemporary; (Can implement this later maybe, idk)
    public bool IsRendered => _player.IsRendered;
    public bool IsOnline => _onlineUser != null;
    public bool IsFavorite => FavoritesConfig.Kinksters.Contains(UserData.UID);
    public string Ident => _onlineUser?.Ident ?? string.Empty;
    public string PlayerName => _player.NameString;
    public string PlayerNameWorld => _player.NameWithWorld;
    public IntPtr PlayerAddress => IsRendered ? _player.Address : IntPtr.Zero;
    public ulong PlayerEntityId => IsRendered ? _player.EntityId : ulong.MaxValue;
    public ulong PlayerObjectId => IsRendered ? _player.GameObjectId : ulong.MaxValue;
    public Vector3 PlayerPosition => IsRendered ? _player.DataState.Position : Vector3.Zero;
    public bool IsTargetable => IsRendered ? _player.DataState.GetIsTargetable() : false;

    // Additional Information.
    public MoodleData MoodleData => _player.MoodlesData; // Phase to a readonly or explicit getters maybe.
    public bool HasShockCollar => PairGlobals.HasValidShareCode() || PairPerms.HasValidShareCode();

    // Definitely change how this information is stored, possibly do so within some internal kinkplate cache or whatever.
    // It would need to hold an updated mini-cache of what we hold for the client themselves whenever a change to
    // bondage data occurs.
    public Dictionary<EquipSlot, (EquipItem, string)> LockedSlots { get; private set; } = new(); // the locked slots of the pair. Used for quick reference in profile viewer.

    // IComparable satisfier
    public int CompareTo(Kinkster? other)
    {
        if (other is null) return 1;
        return string.Compare(UserData.UID, other.UserData.UID, StringComparison.Ordinal);
    }

    public string? AlphabeticalSortKey()
        => (IsRendered && !string.IsNullOrEmpty(PlayerName)
        ? (_config.Current.NickOverPlayerName ? GetNickAliasOrUid() : PlayerName) : GetNickAliasOrUid());

    public string GetDisplayName()
    {
        var condition = IsRendered && !_config.Current.NickOverPlayerName && !string.IsNullOrEmpty(PlayerName);
        return condition ? PlayerName : GetNickAliasOrUid();
    }

    public string? GetNickname() 
        => _nicks.GetNicknameForUid(UserData.UID);

    public string GetNickAliasOrUid() 
        => _nicks.TryGetNickname(UserData.UID, out var n) ? n : UserData.AliasOrUID;

    public IPCMoodleAccessTuple ToAccessTuple()
        => new IPCMoodleAccessTuple(
            OwnPerms.MoodleAccess, (long)OwnPerms.MaxMoodleTime.TotalMilliseconds,
            PairPerms.MoodleAccess, (long)PairPerms.MaxMoodleTime.TotalMilliseconds);

    public float DistanceToPlayer() 
        => IsRendered ? PlayerData.DistanceTo(_player.DataState.Position) : float.MaxValue;



    #region Handler Updates
    public void SetMoodlesData(MoodleData newData)
        => _player.UpdateAndApplyMoodles(newData);

    public void SetMoodlesData(string dataString, IEnumerable<MoodlesStatusInfo> dataInfo)
        => _player.UpdateAndApplyMoodles(dataString, dataInfo);

    public void SetMoodleStatusData(List<MoodlesStatusInfo> statuses)
        => _player.MoodlesData.SetStatuses(statuses);
    
    public void SetMoodlePresetData(List<MoodlePresetInfo> presets)
        => _player.MoodlesData.SetPresets(presets);

    public void UpdateMoodleStatusData(MoodlesStatusInfo status, bool deleted)
    {
        if (deleted) _player.MoodlesData.Statuses.Remove(status.GUID);
        else _player.MoodlesData.AddOrUpdateStatus(status);
    }

    public void UpdateMoodlePresetData(MoodlePresetInfo preset, bool deleted)
    {
        if (deleted) _player.MoodlesData.Presets.Remove(preset.GUID);
        else _player.MoodlesData.AddOrUpdatePreset(preset);
    }

    #endregion Handler Updates

    /// <summary>
    ///     After a Kinkster is initialized / created, it will then be marked as
    ///     Online, if they are online. (Or after a reconnection, after being created)
    /// </summary>
    public void MarkOnline(OnlineKinkster dto)
    {
        _onlineUser = dto;
        // Inform mediator of Online update.
        _mediator.Publish(new KinksterOnline(this));
        // Check to see if they are visible, and if so, reapply alterations.
        _player.SetVisibleIfRendered().ConfigureAwait(false);
    }

    /// <summary>
    ///     Convert a temporary Kinkster to a permanent one. (Once we add this at least)
    /// </summary>
    public void MarkAsPermanent()
    {
        //if (!UserPair.IsTemporary)
        //{
        //    _logger.LogWarning($"Attempted to set a tmp kinkster ({GetNickAliasOrUid()}) to permanent, but they already are!", LoggerType.PairManagement);
        //    return;
        //}
        // Update the status to non-temporary.
        //UserPair = UserPair with { TempAccepterUID = string.Empty };
        _logger.LogInformation($"Kinkster [{PlayerName}] ({GetNickAliasOrUid()}) updated to a permanent pair.", LoggerType.PairManagement);
    }

    /// <summary> 
    ///     Mark a Kinkster as offline, reverting any visible state if applied.
    /// </summary>
    public void MarkOffline()
    {
        _onlineUser = null;
        _mediator.Publish(new KinksterOffline(this));
        // Revert any visible state alterations.
        _player.RevertAlterations().ConfigureAwait(false);
        _logger.LogTrace($"[{PlayerName}] ({GetNickAliasOrUid()}) went offline, reverting alterations.", LoggerType.PairManagement);
    }

    /// <summary>
    ///     Reapply cached Alterations to all visible OwnedObjects.
    /// </summary>
    public void ReapplyAlterations()
        => _player.RevertAlterations().ConfigureAwait(false);

    /// <summary>
    ///     Revert the visual alterations of the Kinkster, if rendered. <para/>
    ///     <b>This will clear the internal data.</b> (Maybe dont do this to support pausing or something idk)
    /// </summary>
    public async Task RevertRenderedAlterations()
    {
        _logger.LogDebug($"Reverting alterations for [{PlayerName}] ({GetNickAliasOrUid()}).", UserData.AliasOrUID);
        await _player.RevertAlterations().ConfigureAwait(false);
    }

    /// <summary>
    ///     Disposes of the Kinkster's Handlers, and all internal data. <para/>
    ///     <b>Should be called when intending to dispose a Kinkster ONLY.</b>
    /// </summary>
    public void DisposeData()
    {
        _logger.LogTrace($"Disposing data for {PlayerName}({GetNickAliasOrUid()})", LoggerType.PairManagement);
        // If online, just simply mark offline.
        if (IsOnline)
        {
            _onlineUser = null;
            _mediator.Publish(new KinksterOffline(this));
        }

        // The handler disposal methods effective perform a revert + data clear + final disposal state.
        // Because of this calling mark offline prior is not necessary.
        _player.Dispose();
    }

    #region Data Updates

    public void NewActiveCompositeData(CharaCompositeActiveData data, bool wasSafeword)
    {
        if (wasSafeword)
        {
            _logger.LogInformation($"{GetNickAliasOrUid()} used their safeword! Syncronizing their new composite data!", LoggerType.PairDataTransfer);
            ActiveGags = data.Gags;
            ActiveRestrictions = data.Restrictions;
            ActiveRestraint = data.Restraint;
            // Cursed loot the same?... (will likely add it in once i tackle cursed loot)
            ValidToys = data.ValidToys.ToHashSet();
            ActivePattern = data.ActivePattern;
            ActiveAlarms = data.ActiveAlarms.ToHashSet();
            ActiveTriggers = data.ActiveTriggers.ToHashSet();
            // Do not update the cache, nothing is in it.
        }
        else
        {
            _logger.LogDebug("Received Character Composite Data from " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
            ActiveGags = data.Gags;
            ActiveRestrictions = data.Restrictions;
            ActiveRestraint = data.Restraint;
            ActiveCursedItems = data.ActiveCursedItems.ToHashSet();
            // Filtered aliases
            SharedAliases = data.AliasData.Items.Where(a => a.CanView(MainHub.UID)).ToList();
            IsListeningToClient = data.ListeningTo.Contains(MainHub.UID);
            ValidToys = data.ValidToys.ToHashSet();
            ActivePattern = data.ActivePattern;
            ActiveAlarms = data.ActiveAlarms.ToHashSet();
            ActiveTriggers = data.ActiveTriggers.ToHashSet();

            // Update the kinkster cache with the light storage data.
            _logger.LogDebug($"Updating LightCache for {GetNickAliasOrUid()} " +
                $"[Shared Aliases: {SharedAliases.Count} | ListeningToYou: {IsListeningToClient}]", LoggerType.KinksterCache);

            _logger.LogInformation($"Initializing Kinkster Cache with " +
                $"{data.LightStorageData.GagItems.Count()} Gags, " +
                $"{data.LightStorageData.Restrictions.Count()} Restrictions, " +
                $"{data.LightStorageData.Restraints.Count()} Restraints, " +
                $"{data.LightStorageData.CursedItems.Count()} Cursed Items, " +
                $"{data.LightStorageData.Patterns.Count()} Patterns, " +
                $"{data.LightStorageData.Alarms.Count()} Alarms, " +
                $"and {data.LightStorageData.Triggers.Count()} Triggers.", LoggerType.KinksterCache);
            LightCache = new KinksterCache(data.LightStorageData);
        }

        // Notify nameplates of visible kinkster gag changes.
        _mediator.Publish(new KinksterActiveGagsChanged(this));

        // Regarldess of the change, we should update the kinkster's latest data to the achievement handler.
        _logger.LogDebug($"Aligning Achievement Trackers in sync with {GetNickAliasOrUid()}'s latest composite data!", LoggerType.PairDataTransfer);
        _mediator.Publish(new PlayerLatestActiveItems(UserData, ActiveGags, ActiveRestrictions, ActiveRestraint)); // <-- Send whole composite?
    }

    public void NewActiveGagData(KinksterUpdateActiveGag data)
    {
        _logger.LogDebug($"Applying updated gag data for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        var prev = ActiveGags.GagSlots[data.AffectedLayer] with { };
        ActiveGags.GagSlots[data.AffectedLayer] = data.NewData;
        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                _mediator.Publish(new GagStateChanged(NewState.Disabled, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                _mediator.Publish(new GagStateChanged(NewState.Enabled, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                _mediator.Publish(new GagStateChanged(NewState.Enabled, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                _mediator.Publish(new GagStateChanged(NewState.Locked, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                return;
            case DataUpdateType.Unlocked:
                _mediator.Publish(new GagStateChanged(NewState.Unlocked, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                return;
            case DataUpdateType.Removed:
                _mediator.Publish(new GagStateChanged(NewState.Disabled, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
        }

        // Notify nameplates of visible kinkster gag changes.
        _mediator.Publish(new KinksterActiveGagsChanged(this));
    }

    public void NewActiveRestrictionData(KinksterUpdateActiveRestriction data)
    {
        _logger.LogDebug("Applying updated restriction data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        var prev = ActiveRestrictions.Restrictions[data.AffectedLayer] with { };
        ActiveRestrictions.Restrictions[data.AffectedLayer] = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                _mediator.Publish(new RestrictionStateChanged(NewState.Disabled, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                _mediator.Publish(new RestrictionStateChanged(NewState.Enabled, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Applied:
                _mediator.Publish(new RestrictionStateChanged(NewState.Enabled, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
            case DataUpdateType.Locked:
                _mediator.Publish(new RestrictionStateChanged(NewState.Locked, data.AffectedLayer, data.NewData, data.Enactor.UID, UserData.UID));
                return;
            case DataUpdateType.Unlocked:
                _mediator.Publish(new RestrictionStateChanged(NewState.Unlocked, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                return;
            case DataUpdateType.Removed:
                _mediator.Publish(new RestrictionStateChanged(NewState.Disabled, data.AffectedLayer, prev, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                return;
        }
    }

    public void NewActiveRestraintData(KinksterUpdateActiveRestraint data)
    {
        _logger.LogDebug("Applying updated restraint data for " + GetNickAliasOrUid(), LoggerType.PairDataTransfer);
        var prev = ActiveRestraint with { };
        ActiveRestraint = data.NewData;

        switch (data.Type)
        {
            case DataUpdateType.Swapped:
                _mediator.Publish(new RestraintStateChanged(NewState.Disabled, prev, data.Enactor.UID, UserData.UID));
                _mediator.Publish(new RestraintStateChanged(NewState.Enabled, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                break;
            case DataUpdateType.Applied:
                _mediator.Publish(new RestraintStateChanged(NewState.Enabled, data.NewData, data.Enactor.UID, UserData.UID));
                UpdateCachedLockedSlots();
                break;
            case DataUpdateType.Locked:
                _mediator.Publish(new RestraintStateChanged(NewState.Locked, data.NewData, data.Enactor.UID, UserData.UID));
                break;
            case DataUpdateType.Unlocked:
                _mediator.Publish(new RestraintStateChanged(NewState.Unlocked, prev, data.Enactor.UID, UserData.UID));
                break;
            case DataUpdateType.Removed:
                _mediator.Publish(new RestraintStateChanged(NewState.Disabled, prev, data.Enactor.UID, UserData.UID));
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

    public void NewEnabledState(GagType gag, bool newState)
    {
        if (LightCache.Gags.TryGetValue(gag, out var item))
            item.IsEnabled = newState;
    }

    public void NewEnabledStates(IEnumerable<GagType> gags, bool newState)
    {
        foreach (var gag in gags)
            if (LightCache.Gags.TryGetValue(gag, out var item))
                item.IsEnabled = newState;
    }

    public void NewEnabledState(ToyBrandName toy, bool newState)
    {
        if (newState) ValidToys.Add(toy);
        else ValidToys.Remove(toy);
    }

    public void NewEnabledStates(IEnumerable<ToyBrandName> toys, bool newState)
    {
        if (newState) ValidToys.UnionWith(toys);
        else ValidToys.ExceptWith(toys);
    }

    public void NewEnabledState(GSModule module, Guid item, bool newState)
    {
        if (module is GSModule.Restriction && LightCache.Restrictions.TryGetValue(item, out var restriction))
        {
            restriction.IsEnabled = newState;
        }
        else if (module is GSModule.Restraint && LightCache.Restraints.TryGetValue(item, out var restraint))
        {
            restraint.IsEnabled = newState;
        }
        else if (module is GSModule.CursedLoot)
        {
            if (newState) ActiveCursedItems.Add(item);
            else ActiveCursedItems.Remove(item);
            UpdateCachedLockedSlots();
        }
        else if (module is GSModule.Puppeteer)
        {
            if (SharedAliases.FirstOrDefault(a => a.Identifier == item) is { } alias)
            {
                alias.Enabled = newState;
                _mediator.Publish(new FolderUpdateKinksterAliases(this));
            }
        }
        else if (module is GSModule.Pattern)
        {
            ActivePattern = newState ? item : Guid.Empty;
        }
        else if (module is GSModule.Alarm)
        {
            if (newState) ActiveAlarms.Add(item);
            else ActiveAlarms.Remove(item);
        }
        else if (module is GSModule.Trigger)
        {
            if (newState) ActiveTriggers.Add(item);
            else ActiveTriggers.Remove(item);
        }
    }

    public void NewEnabledStates(GSModule module, IEnumerable<Guid> items, bool newState)
    {
        if (module is GSModule.Restriction)
        {
            foreach (var item in items)
                if (LightCache.Restrictions.TryGetValue(item, out var restriction))
                    restriction.IsEnabled = newState;
        }
        else if (module is GSModule.Restraint)
        {
            foreach (var item in items)
                if(LightCache.Restraints.TryGetValue(item, out var restraint))
                    restraint.IsEnabled = newState;
        }
        else if (module is GSModule.CursedLoot)
        {
            if (newState) ActiveCursedItems.UnionWith(items);
            else ActiveCursedItems.ExceptWith(items);
            UpdateCachedLockedSlots();
        }
        else if (module is GSModule.Puppeteer)
        {
            var itemsSet = items.ToHashSet();
            foreach (var alias in SharedAliases)
                alias.Enabled = itemsSet.Contains(alias.Identifier);
            _mediator.Publish(new FolderUpdateKinksterAliases(this));
        }
        else if (module is GSModule.Alarm)
        {
            if (newState) ActiveAlarms.UnionWith(items);
            else ActiveAlarms.ExceptWith(items);
        }
        else if (module is GSModule.Trigger)
        {
            if (newState) ActiveTriggers.UnionWith(items);
            else ActiveTriggers.ExceptWith(items);
        }
    }

    // Seperated from cache intentionally.
    public void UpdateAliasTrigger(Guid id, AliasTrigger? newData)
    {
        var alias = SharedAliases.FirstOrDefault(a => a.Identifier == id);
        if (alias is not null)
        {
            if (newData is not null && newData.CanView(MainHub.UID))
            {
                alias.ApplyChanges(newData);
                _logger.LogDebug($"Updating Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            }
            else
            {
                SharedAliases.Remove(alias);
                _logger.LogDebug($"Removing Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
            }
        }
        else if (newData is not null && newData.CanView(MainHub.UID))
        {
            SharedAliases.Add(newData);
            _logger.LogDebug($"Adding Alias for {GetNickAliasOrUid()}", LoggerType.PairDataTransfer);
        }
        // Inform marionettes tab of the change.
        _mediator.Publish(new FolderUpdateKinksterAliases(this));
    }

    public void UpdateIsListening(bool newState)
    {
        _logger.LogDebug($"Updating IsListening for {GetNickAliasOrUid()} to {newState}", LoggerType.PairDataTransfer);
        IsListeningToClient = newState;
        _mediator.Publish(new FolderUpdateMarionettes());

    }

    #endregion Data Updates

    // Update this method to be obtained by the Kinkster Cache.
    public void UpdateCachedLockedSlots()
    {
        var result = new Dictionary<EquipSlot, (EquipItem, string)>();
        // Rewrite this completely. It sucks, and it does nothing with the 2.0 structure.
        _logger.LogDebug("Updated Locked Slots for " + UserData.UID, LoggerType.PairInfo);
        LockedSlots = result;
    }

    #region Debug
    // ----- Debuggers -----
    public void DrawRenderDebug()
    {
        using var node = ImRaii.TreeNode($"Player Info##{UserData.UID}-visible");
        if (!node) return;

        using (var t = ImRaii.Table($"##debug-visible{UserData.UID}", 12, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingFixedFit))
        {
            if (!t) return;
            ImGui.TableSetupColumn("OwnedObject");
            ImGui.TableSetupColumn("Rendered?");
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Address");
            ImGui.TableSetupColumn("ObjectIdx");
            ImGui.TableSetupColumn("EntityId");
            ImGui.TableSetupColumn("ObjectId");
            ImGui.TableSetupColumn("ParentId");
            ImGui.TableSetupColumn("DrawObjValid");
            ImGui.TableSetupColumn("RenderFlags");
            ImGui.TableSetupColumn("MdlInSlot");
            ImGui.TableSetupColumn("MdlFilesInSlot");
            ImGui.TableHeadersRow();
            // Handle Player.
            ImGuiUtil.DrawFrameColumn("Player");
            ImGui.TableNextColumn();
            CkGui.IconText(IsRendered ? FAI.Check : FAI.Times, IsRendered ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
            ImGuiUtil.DrawFrameColumn(PlayerName);
            if (IsRendered)
            {
                ImGui.TableNextColumn();
                CkGui.ColorText($"{PlayerAddress:X}", ImGuiColors.TankBlue);
                ImGuiUtil.DrawFrameColumn(_player.ObjIndex.ToString());
                ImGuiUtil.DrawFrameColumn(PlayerEntityId.ToString());
                ImGuiUtil.DrawFrameColumn(PlayerObjectId.ToString());
                ImGuiUtil.DrawFrameColumn("N/A");

                ImGui.TableNextColumn();
                var drawObjValid = _player.DrawObjAddress != IntPtr.Zero;
                CkGui.IconText(drawObjValid ? FAI.Check : FAI.Times, drawObjValid ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);

                ImGui.TableNextColumn();
                CkGui.ColorText(_player.RenderFlags.ToString(), ImGuiColors.DalamudGrey2);

                if (drawObjValid)
                {
                    ImGui.TableNextColumn();
                    CkGui.IconText(_player.HasModelInSlotLoaded ? FAI.Check : FAI.Times, _player.HasModelInSlotLoaded ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
                    ImGui.TableNextColumn();
                    CkGui.IconText(_player.HasModelFilesInSlotLoaded ? FAI.Check : FAI.Times, _player.HasModelFilesInSlotLoaded ? ImGuiColors.HealerGreen : ImGuiColors.DalamudRed);
                }
            }
            ImGui.TableNextRow();
        }

        _player.DrawDebugInfo();
    }
    #endregion Debug
}
